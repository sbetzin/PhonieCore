using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PhonieCore.Logging;
using PhonieCore.Mopidy;

namespace PhonieCore
{
    public static class PhonieController
    {
        private static Player _player;
        private static PlayerState _state;

        public static async Task Run(PlayerState state)
        {
            _state = state;
            using var modipyAdapter = new MopidyAdapter();
            modipyAdapter.MessageReceived += ModipyAdapter_MessageReceived;
            await modipyAdapter.ConnectAsync();

            var mediaAdapter = new MediaAdapter(state);

            _player = new Player(modipyAdapter, mediaAdapter, _state);
            await _player.SetVolume(50);
            await _player.Play($"{state.MediaFolder}/start.mp3");
            await Task.Delay(1000);

            var watcher = new InactivityWatcher(state);
            watcher.Inactive += BashAdapter.Shutdown;
            _ = Task.Run(async () => await watcher.WatchForInactivity(30));

            var buttonAdapter = new ButtonAdapter(state);
            _ = Task.Run(async () => await buttonAdapter.WatchButton());
            
            RfidReader.NewCardDetected += async (uid) => await NewCardDetected(uid);
            await RfidReader.DetectCards(_state);

            await _player.Play($"/{state.MediaFolder}/shutdown.mp3");
            await Task.Delay(1000);
        }

        private static void ModipyAdapter_MessageReceived(string eventName, IDictionary<string, JToken> data)
        {
            switch (eventName)
            {
                case "track_playback_ended":
                    ResetCurrentRfidTag();
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
            await _player.ProcessFolder(uid);
        }
    }
}
