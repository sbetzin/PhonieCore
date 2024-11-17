using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PhonieCore.Logging;
using PhonieCore.Mopidy;

namespace PhonieCore
{
    public static class Player
    {
        private static readonly MopidyAdapter MopidyAdapter = new();
        private static int _volume = 80;

        public static async Task ProcessFolder(string uid)
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
                await Play(files);
            }
        }

        public static async Task Play()
        {
            await MopidyAdapter.Play();
        }

        public static async Task Next()
        {
            await MopidyAdapter.Next();
        }

        public static async Task Previous()
        {
            await MopidyAdapter.Previous();
        }

        public static async Task Seek(int sec)
        {
            await MopidyAdapter.Seek(sec);
        }

        public static async Task SetVolume(int volume)
        {
            Logger.Log($"set volumen {volume}");
            await MopidyAdapter.SetVolume(volume);
        }

        public static async Task IncreaseVolume()
        {
            if (_volume <= 95)
            {
                _volume += 5;
            }
            await SetVolume(_volume);
        }

        public static async Task DecreaseVolume()
        {
            if (_volume >= 5)
            {
                _volume -= 5;
            }
            await SetVolume(_volume);
        }

        public static async Task Play(string file)
        {
            await Play([file]);
        }

        public static async Task Play(string[] files)
        {
            var filesString = string.Join(" ", files);
            Logger.Log("Play files: " + filesString);

            await MopidyAdapter.Stop();
            await MopidyAdapter.ClearTracks();
            foreach (var file in files)
            {
                await MopidyAdapter.AddTrack("file://" + file);
            }
            await MopidyAdapter.Play();
        }

        private static async Task PlaySpotify(string uri)
        {
            Logger.Log("Play Spotify: " + uri);
                
            await MopidyAdapter.Stop();
            await MopidyAdapter.ClearTracks();
            await MopidyAdapter.AddTrack(uri);
            await MopidyAdapter.Play();
        }

        public static async Task Stop()
        {
            Logger.Log("Stop");
            await MopidyAdapter.Stop();
        }

        public static async Task Pause()
        {
            Logger.Log("Pause");
            await MopidyAdapter.Pause();
        }
    }
}
