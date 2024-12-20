﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhonieCore.Logging;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using PhonieCore.Mopidy.Model;

namespace PhonieCore.Mopidy
{
    public partial class MopidyAdapter : IDisposable
    {
        private const string MopidyWebSocketUrl = "ws://localhost:6680/mopidy/ws";
        private readonly ClientWebSocket _webSocket = new();
        private readonly CancellationTokenSource _cts = new();
        private int _messageId = 0;
        private bool _disposedValue;

        public event Action<string, IDictionary<string, JToken>> MessageReceived;

        public async Task ConnectAsync()
        {
            try
            {
                await _webSocket.ConnectAsync(new Uri(MopidyWebSocketUrl), _cts.Token);
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
            Logger.Log($"Received message: {response.Event} - {JsonConvert.SerializeObject(response.AdditionalData)}");

            OnMessageReceived(response.Event, response.AdditionalData);
        }

        private void HandleResponse(WebSocketResponse response)
        {
            if (response.Error != null)
            {
                Logger.Error($"Error in response: {response.Error.Message}");
            }
            else
            {
                //Logger.Log($"Received response for request {response.Id}: {response.Result}");
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

        private async Task Call(string method, Dictionary<string, object> parameters = null)
        {
            var request = new Request
            {
                Jsonrpc = "2.0",
                Id = ++_messageId,
                Method = method,
                Params = parameters
            };

            await SendAsync(request);
        }

        public async Task Stop()
        {
            await Call("core.playback.stop");
        }

        public async Task Pause()
        {
            await Call("core.playback.pause");
        }

        public async Task Play()
        {
            await Call("core.playback.play");
        }

        public async Task Next()
        {
            await Call("core.playback.next");
        }

        public async Task Previous()
        {
            await Call("core.playback.previous");
        }

        public async Task Seek(int sec)
        {
            await Call("core.playback.seek", new Dictionary<string, object> { { "time_position", sec * 1000 } });
        }

        public async Task SetVolume(int volume)
        {
            await Call("core.mixer.set_volume", new Dictionary<string, object> { { "volume", volume } });
        }

        public async Task AddTrack(string uri)
        {
            await Call("core.tracklist.add", new Dictionary<string, object> { { "uris", new object[] { uri } } });
        }

        public async Task ClearTracks()
        {
            await Call("core.tracklist.clear");
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

        protected virtual void OnMessageReceived(string eventName, IDictionary<string, JToken> data)
        {
            MessageReceived?.Invoke(eventName, data);
        }
    }
}
