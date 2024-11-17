using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PhonieCore.Logging;
using PhonieCore.Mopidy;

namespace PhonieCore
{
    public static class PhonieController
    {
        private static Player _player;
        private static readonly PlayerState State = new PlayerState();

        public static async Task Run(CancellationToken cancellationToken)
        {
            using var modipyAdapter = new MopidyAdapter();
            modipyAdapter.MessageReceived += ModipyAdapter_MessageReceived;
            await modipyAdapter.ConnectAsync();

            _player = new Player(modipyAdapter, State);
            await _player.SetVolume(50);
            await _player.Play("/media/start.mp3");

            RfidReader.NewCardDetected += NewCardDetected;
            await RfidReader.DetectCards(cancellationToken);

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

            if (volume == State.Volume)
            {
                return;
            }

            Logger.Log($"Volume set to {volume}");
            State.Volume = volume;
        }

        private static void ResetCurrentRfidTag()
        {
            State.PlayingTag = string.Empty;
        }

        private static void NewCardDetected(string uid)
        {
            _player.ProcessFolder(uid).Wait();
        }
    }
}
