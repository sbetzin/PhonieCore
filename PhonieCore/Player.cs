using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PhonieCore.Logging;
using PhonieCore.Mopidy;

namespace PhonieCore
{
    public class Player(MopidyAdapter adapter, PlayerState state)
    {
        private readonly MopidyAdapter _adapter = adapter;
        private readonly PlayerState _state = state;

        public async Task ProcessFolder(string uid)
        {
            var folder = MediaAdapter.GetFolderForId(uid);
            var files = Directory.EnumerateFiles(folder).ToArray();

            if (files.Any(f => f.Contains("STOP")))
            {
                await Stop();
            }
            else if (files.Any(f => f.Contains("PLAY")))
            {
                await Play();
            }
            else if (files.Any(f => f.Contains("PAUSE")))
            {
                await Pause();
            }
            else if (files.Any(f => f.Contains("INCREASE_VOLUME")))
            {
                await IncreaseVolume();
            }
            else if (files.Any(f => f.Contains("DECREASE_VOLUME")))
            {
                await DecreaseVolume();
            }
            else if (files.Any(f => f.Contains("SPOTIFY")))
            {
                if (files.Length == 0)
                {
                    return;
                }

                var file = files.First();
                var url = await File.ReadAllTextAsync(file);
                await PlaySpotify(url);
            }
            else if (files.Any(f => f.EndsWith("mp3")))
            {
                if (_state.PlayingTag == uid)
                {
                    return;
                }

                _state.PlayingTag = uid;
                await Play(files);
            }
        }
        public async Task Play()
        {
            await _adapter.Play();
        }

        public async Task Next()
        {
            await _adapter.Next();
        }

        public async Task Previous()
        {
            await _adapter.Previous();
        }

        public async Task Seek(int sec)
        {
            await _adapter.Seek(sec);
        }

        public async Task SetVolume(int volume)
        {
            Logger.Log($"set volumen {volume}");
            await _adapter.SetVolume(volume);
        }

        public async Task IncreaseVolume()
        {
            if (_state.Volume <= 95)
            {
                await SetVolume(_state.Volume += 5);
            }
        }

        public async Task DecreaseVolume()
        {
            if (_state.Volume >= 5)
            {
                await SetVolume(_state.Volume -= 5);
            }
        }

        public async Task Play(string file)
        {
            await Play([file]);
        }

        public async Task Play(string[] files)
        {
            var filesString = string.Join(" ", files);
            Logger.Log("Play files: " + filesString);

            await _adapter.Stop();
            await _adapter.ClearTracks();
            foreach (var file in files)
            {
                await _adapter.AddTrack("file://" + file);
            }
            await _adapter.Play();
        }

        private async Task PlaySpotify(string uri)
        {
            Logger.Log("Play Spotify: " + uri);

            await _adapter.Stop();
            await _adapter.ClearTracks();
            await _adapter.AddTrack(uri);
            await _adapter.Play();
        }

        public async Task Stop()
        {
            Logger.Log("Stop");
            await _adapter.Stop();
        }

        public async Task Pause()
        {
            Logger.Log("Pause");
            await _adapter.Pause();
        }
    }
}
