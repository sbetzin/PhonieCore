using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PhonieCore.Logging;
using PhonieCore.Mopidy;

namespace PhonieCore
{
    public class Player(MopidyAdapter adapter, MediaAdapter mediaAdapter, PlayerState state)
    {
        public async Task ProcessFolder(string uid)
        {
            var files = mediaAdapter.GetFilesForId(uid);

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
                var file = files.First();
                var url = await File.ReadAllTextAsync(file);
                await PlaySpotify(url);
            }
            else if (files.Any(f => f.EndsWith("mp3")))
            {
                if (state.PlayingTag == uid)
                {
                    return;
                }

                state.PlayingTag = uid;
                await Play(files);
            }
        }
        public async Task Play()
        {
            await adapter.Play();
        }

        public async Task Next()
        {
            await adapter.Next();
        }

        public async Task Previous()
        {
            await adapter.Previous();
        }

        public async Task Seek(int sec)
        {
            await adapter.Seek(sec);
        }

        public async Task SetVolume(int volume)
        {
            Logger.Log($"set volumen {volume}");
            await adapter.SetVolume(volume);
        }

        public async Task IncreaseVolume()
        {
            if (state.Volume <= 95)
            {
                await SetVolume(state.Volume += 5);
            }
        }

        public async Task DecreaseVolume()
        {
            if (state.Volume >= 10)
            {
                await SetVolume(state.Volume -= 5);
            }
        }

        public async Task Play(string file)
        {
            await Play([file]);
        }

        public async Task Play(string[] files)
        {
            files = files.Order().ToArray();
            var filesString = string.Join(" ", files);
            Logger.Log("Play files: " + filesString);

            await adapter.Stop();
            await adapter.ClearTracks();

            var tracks = files.Select(file => $@"file://{file}").ToArray();
            await adapter.AddTracks(tracks);
            await adapter.DontRepeat();
            await adapter.Play();
        }

        private async Task PlaySpotify(string uri)
        {
            Logger.Log("Play Spotify: " + uri);

            await adapter.Stop();
            await adapter.ClearTracks();
            await adapter.AddTrack(uri);
            await adapter.Play();
        }

        public async Task Stop()
        {
            Logger.Log("Stop");
            await adapter.Stop();
            state.PlayingTag = string.Empty;
        }

        public async Task Pause()
        {
            Logger.Log("Pause");
            await adapter.Pause();
        }
    }
}
