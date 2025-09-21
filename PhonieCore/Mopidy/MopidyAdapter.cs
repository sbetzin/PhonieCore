using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PhonieCore.Logging;
using PhonieCore.Mopidy.Model;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnitsNet;

namespace PhonieCore.Mopidy
{
    public class MopidyAdapter : IDisposable
    {
        private string _mopidyWebSocketUrl;
        private ClientWebSocket _webSocket = new();
        private readonly CancellationTokenSource _cts = new();
        private int _messageId = 0;
        private bool _disposedValue;
        private Dictionary<int, WebSocketResponse> _requestResponses = new();

        private static readonly AsyncRetryPolicy _connectRetryPolicy = Policy
        .Handle<WebSocketException>()
        .Or<IOException>()
        .Or<InvalidOperationException>()
        .WaitAndRetryAsync(
            retryCount: 10,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(attempt),
            onRetry: (ex, delay, attempt, context) =>
                Logger.Log($"Mopidy WebSocket nicht erreichbar (Versuch {attempt}), war {delay.TotalSeconds:F0}s. Fehler: {ex.Message}")
        );

        public event Action<string, IDictionary<string, JToken>> MessageReceived;

        public MopidyAdapter(string websocketurl)
        {
            _mopidyWebSocketUrl = websocketurl;
        }

        public async Task ConnectAsync()
        {
            try
            {
                Logger.Log("Connecting to Mopidy WebSocket API...");
                await _connectRetryPolicy.ExecuteAsync(async ct =>
                {
                    _webSocket = new();
                    await _webSocket.ConnectAsync(new Uri(_mopidyWebSocketUrl), ct);
                }
                , _cts.Token);
                Logger.Log("Connected to Mopidy WebSocket API.");

                // Start listening for messages
                _ = Task.Run(StartListeningAsync, _cts.Token);
            }
            catch (Exception e)
            {
                Logger.Error("Could not connect to modipy websocket", e);
                throw;
            }
        }

        public async Task StartListeningAsync()
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);

            try
            {
                while (_webSocket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _webSocket.ReceiveAsync(buffer, _cts.Token);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    } while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);
                    var message = Encoding.UTF8.GetString(ms.ToArray());

                    ProcessMessage(message);
                }
            }
            catch (OperationCanceledException)
            {
                // Receiving was canceled
            }
            catch (Exception ex)
            {
                Logger.Error("Error while receiving data from Mopidy WebSocket.", ex);
            }
        }

        private void ProcessMessage(string message)
        {
            try
            {
                var jsonMessage = JsonConvert.DeserializeObject<WebSocketResponse>(message);

                if (jsonMessage == null) return;

                if (jsonMessage.Event != null)
                {
                    // It's an event from Mopidy
                    HandleEvent(jsonMessage);
                }
                else if (jsonMessage.Id != null)
                {
                    // It's a response to one of our requests
                    HandleResponse(jsonMessage);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to process message from Mopidy WebSocket.", ex);
            }
        }

        private void HandleEvent(WebSocketResponse response)
        {
            //Logger.Log($"Received message: {response.Event} - {JsonConvert.SerializeObject(response.AdditionalData)}");

            OnMessageReceived(response.Event, response.AdditionalData);
        }

        private void HandleResponse(WebSocketResponse response)
        {
            if (response.Error != null)
            {
                Logger.Error($"Error in response: {response.Error.Message}");
            }

            //Logger.Log($"Received response for request {response.Id}: {response.Result}");
            if (_requestResponses.ContainsKey(response.Id.Value))
            {
                _requestResponses[response.Id.Value] = response;
            }
        }

        private async Task SendAsync(Request request)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var json = JsonConvert.SerializeObject(request, settings);
            var buffer = Encoding.UTF8.GetBytes(json);
            var segment = new ArraySegment<byte>(buffer);

            await _webSocket.SendAsync(segment, WebSocketMessageType.Text, true, _cts.Token);
        }

        private async Task<Request> Call(string method, Dictionary<string, object> parameters = null)
        {
            if (_messageId == int.MaxValue) _messageId = 0;

            var request = new Request
            {
                Jsonrpc = "2.0",
                Id = ++_messageId,
                Method = method,
                Params = parameters
            };

            await SendAsync(request);

            return request;
        }

        public async Task StopAsync()
        {
            var request = await Call("core.playback.stop");
            _ = await WaitForResponse(request);
        }

        public async Task PauseAsync()
        {
            var request = await Call("core.playback.pause");
            _ = await WaitForResponse(request);
        }

        public async Task PlayAsync()
        {
            var request = await Call("core.playback.play");
            _ = await WaitForResponse(request);
        }

        public async Task Next()
        {
            var request = await Call("core.playback.next");
            _ = await WaitForResponse(request);
        }

        public async Task Previous()
        {
            var request = await Call("core.playback.previous");
            _ = await WaitForResponse(request);
        }

        public async Task Seek(int sec)
        {
            var request = await Call("core.playback.seek", new Dictionary<string, object> { { "time_position", sec * 1000 } });
            _ = await WaitForResponse(request);
        }

        public async Task SetVolume(int volume)
        {
            var request = await Call("core.mixer.set_volume", new Dictionary<string, object> { { "volume", volume } });
            _ = await WaitForResponse(request);
        }

        public async Task AddTrackAsync(string uri)
        {
            _ = await AddTracks([uri]);
        }
        public async Task<WebSocketResponse> AddTracks(string[] uri)
        {
            var request = await Call("core.tracklist.add", new Dictionary<string, object> { { "uris", uri } });
            var response = await WaitForResponse(request);

            return response;
        }

        public async Task ClearTracksAsync()
        {
            var request = await Call("core.tracklist.clear");
            _ = await WaitForResponse(request);
        }

        public async Task<long> GetNextTrackId()
        {
            var request = await Call("core.tracklist.get_eot_tlid");
            var response = await WaitForResponse(request);

            if (response.Result == null)
            {
                return 0;
            }

            var nextId = (long)response.Result;

            return nextId;
        }

        internal async Task<WebSocketResponse> WaitForResponse(Request request)
        {
            _requestResponses.Add(request.Id, null);

            while (_requestResponses[request.Id] == null)
            {
                await Task.Delay(50);
            }

            var response = _requestResponses[request.Id];
            _requestResponses.Remove(request.Id);

            return response;
        }

        public async Task DontRepeat()
        {
            await Call("core.tracklist.set_repeat", new Dictionary<string, object> { { "value", false } });
        }

        protected virtual void OnMessageReceived(string eventName, IDictionary<string, JToken> data)
        {
            MessageReceived?.Invoke(eventName, data);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                _cts.Cancel();
                _webSocket.Dispose();
                _cts.Dispose();
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
