using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PhonieCore.Hardware;
using PhonieCore.Logging;
using PhonieCore.Mopidy;
using PhonieCore.OS;
using PhonieCore.OS.Audio;

namespace PhonieCore
{
    public static class PhonieController
    {
        private static MopidyPlayer _mopidyPlayer;
        private static PlayerState _state;

        public static async Task Run(PlayerState state)
        {
            _state = state;
          
            using var modipyAdapter = new MopidyAdapter(state.WebSocketUrl);
            modipyAdapter.MessageReceived += async (eventName, data) => await ModipyAdapter_MessageReceivedAsync(modipyAdapter, eventName, data);
            await modipyAdapter.ConnectAsync();

            var mediaAdapter = new MediaFilesAdapter(state);
            _mopidyPlayer = new MopidyPlayer(modipyAdapter, mediaAdapter, _state);
            await _mopidyPlayer.SetVolume(state.Volume);

            var watcher = new InactivityWatcher(state);
            watcher.Inactive += BashAdapter.Shutdown;

            using var audioPlayer = new AudioPlayer();
            await audioPlayer.PlayAsync("start.wav", false, state.Volume);

            // We connect the hardware only if not PCDebug
            if (!state.PCDebug)
            {
                _ = Task.Run(async () => await watcher.WatchForInactivity(30));

                var networkManagerAdapter = new NetworkManagerAdapter();
                _ = Task.Run(async () =>
                {
                    await networkManagerAdapter.StartAsync(state.IfName);
                    //await networkManagerAdapter.EnsureWifiProfileAsync("generic.de Data", "generic.de Data", "surf_the_green_wave");
                });

                _ = Task.Run(async () =>
                {
                    await GpioButton.RunAsync(26, PinMode.InputPullUp, TimeSpan.FromMilliseconds(100), false,

                    onPressed: async () =>
                    {
                        Logger.Log("Button PRESSED");
                        await networkManagerAdapter.TryConnectAsync();
                    },
                    onReleased: () => Logger.Log("Button RELEASED"),
                    state.CancellationToken);
                });

                _ = Task.Run(async () =>
                {
                    RfidReader.NewCardDetected += async (uid) => await NewCardDetected(uid);
                    await RfidReader.DetectCards(_state);
                });
            }

            while (!state.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(500);
            }

            await audioPlayer.PlayAsync("shutdown.wav", true, state.Volume);
        }

        private static async Task ModipyAdapter_MessageReceivedAsync(MopidyAdapter mopidyAdapter, string eventName, IDictionary<string, JToken> data)
        {
            switch (eventName)
            {
                case "track_playback_started":
                    _state.NextTrackId = await mopidyAdapter.GetNextTrackId().ConfigureAwait(false);
                    break;
                case "track_playback_ended":
                    Logger.Log($"track_playback_ended");
                    if (_state.NextTrackId == 0)
                    {
                        Logger.Log("No next Track Id. Resetting rfid tag");
                        ResetCurrentRfidTag();
                    }
                    break;

                case "volume_changed":
                    CheckVolumeOnStateChanged(data);
                    break;

                case "playback_state_changed":
                    StorePlaybackState(data);

                    break;
            }
        }

        private static void StorePlaybackState(IDictionary<string, JToken> data)
        {
            _state.PlaybackStateChanged = DateTime.Now;
            _state.PlaybackState = (string)data["new_state"];

            Logger.Log($"New playback state: {_state.PlaybackState}");
        }

        private static void CheckVolumeOnStateChanged(IDictionary<string, JToken> data)
        {
            var volume = (int)data["volume"];

            if (volume == _state.Volume)
            {
                return;
            }

            Logger.Log($"Volume set to {volume}");
            _state.Volume = volume;
        }

        private static void ResetCurrentRfidTag()
        {
            _state.PlayingTag = string.Empty;
        }

        private static async Task NewCardDetected(string uid)
        {
            await _mopidyPlayer.ProcessFolder(uid);
        }
    }
}
