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

            _player = new Player(modipyAdapter, _state);
            await _player.SetVolume(50);
            await _player.Play("/media/start.mp3");

            RfidReader.NewCardDetected += NewCardDetected;
            await RfidReader.DetectCards( _state);

            await _player.Play("/media/shutdown.mp3");
            await Task.Delay(1000);
        }

        private static void ModipyAdapter_MessageReceived(string eventName, IDictionary<string, JToken> data)
        {
            Logger.Log(eventName);

            switch (eventName)
            {
                case "track_playback_ended":
                    ResetCurrentRfidTag();
                    break;

                case "volume_changed":
                    CheckVolumeOnStateChanged(data);
                    break;

            }
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

        private static void NewCardDetected(string uid)
        {
            _player.ProcessFolder(uid).Wait();
        }
    }
}
