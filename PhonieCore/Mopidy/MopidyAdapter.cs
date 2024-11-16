using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhonieCore.Logging;

// https://docs.mopidy.com/en/latest/api/core/#playback-controller

namespace PhonieCore.Mopidy
{
    public class MopidyAdapter : IDisposable
    {
        private const string MopidyUrl = "http://localhost:6680/mopidy/rpc";
        private readonly HttpClient _httpClient = new();
        private bool _disposedValue;

        private async Task Call(string method, Dictionary<string, object[]> parameters)
        {
            var request = new MultiParamRequest(method, parameters);
            await Fire(request);
        }

        private async Task Call(string method, Dictionary<string, object> parameters)
        {
            var request = new SingleParamRequest(method, parameters);
            await Fire(request);
        }

        private async Task Call(string method)
        {
            var request = new SingleParamRequest(method, null);
            await Fire(request);
        }

        private async Task Fire(Request request)
        {
            var setting = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var json = JsonConvert.SerializeObject(request, setting);

            var httpContent = new StringContent(json, null, "application/json");
            if (httpContent.Headers.ContentType != null) httpContent.Headers.ContentType.CharSet = "";

            try
            {
                var response = await _httpClient.PostAsync(MopidyUrl, httpContent);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Log("Error: Mopidy request failed.");
                    Logger.Log($"Response: {response.StatusCode}, {responseString}");
                }
            }
            catch(HttpRequestException e)
            {
                Logger.Error(e);
            }            
        }

        public async Task Stop()
        {
            await Call("playback.stop");
        }

        public async Task Pause()
        {
            await Call("playback.pause");
        }

        public async Task Play()
        {
            await Call("playback.play");
        }

        public async Task Next()
        {
            await Call("playback.next");
        }

        public async Task Previous()
        {
            await Call("playback.previous");
        }

        public async Task Seek(int sec)
        {
            await Call("playback.seek", new Dictionary<string, object> { { "time_position", sec * 1000 } });
        }

        public async Task SetVolume(int volume)
        {
            await Call("mixer.set_volume", new Dictionary<string, object> { { "volume",  volume } });
        }

        public async Task AddTrack(string uri)
        {
            await Call("tracklist.add", new Dictionary<string, object[]> { { "uris", new object[] { uri } } });
        }

        public async Task ClearTracks()
        {
            await Call("tracklist.clear");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                _httpClient.Dispose();
            }

            _disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
