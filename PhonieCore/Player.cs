using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PhonieCore
{
    public class Player
    {
        private readonly Library _library;
        private readonly Mopidy.Client _mopidyClient;

        private const string StopFile = "STOP";
        private const string PauseFile = "PAUSE";
        private const string PlayFile = "PLAY";
        private const string SpotifyFile = "SPOTIFY";

        private int _volume = 80;

        public Player()
        {
            _library = new Library();
            _mopidyClient = new Mopidy.Client();

            SetVolume(_volume).Wait();
        }

        public async Task ProcessFolder(string uid)
        {
            var folder = _library.GetFolderForId(uid);
            var files = Directory.EnumerateFiles(folder).ToArray();

            foreach (var file in files)
            {
                Console.WriteLine(file);
            }

            if (files.Any(f => f.Contains(StopFile)))
            {
                await Stop();
            }
            else if (files.Any(f => f.Contains(PlayFile)))
            {
                await Play();
            }
            else if (files.Any(f => f.Contains(PauseFile)))
            {
                await Pause();
            }
            else if (files.Any(f => f.Contains(SpotifyFile)))
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

        public async Task Play()
        {
            await _mopidyClient.Play();
        }

        public async Task Next()
        {
            await _mopidyClient.Next();
        }

        public async Task Previous()
        {
            await _mopidyClient.Previous();
        }

        public async Task Seek(int sec)
        {
            await _mopidyClient.Seek(sec);
        }

        private async Task SetVolume(int volume)
        {
            Console.WriteLine($"set volumen {volume}");
            await _mopidyClient.SetVolume(volume);
        }

        public async Task IncreaseVolume()
        {
            if (_volume <= 95)
            {
                _volume += 5;
            }
            await _mopidyClient.SetVolume(_volume);
        }

        public async Task DecreaseVolume()
        {
            if (_volume >= 5)
            {
                _volume -= 5;
            }
            await _mopidyClient.SetVolume(_volume);
        }

        public async Task Play(string[] files)
        {
            var arguments = string.Join(" ", files);
            Console.WriteLine("Play files: " + arguments);

            await Stop();

            await _mopidyClient.ClearTracks();
            foreach (var file in files)
            {
                await _mopidyClient.AddTrack("file://" + file);
            }

            await _mopidyClient.Play();
        }

        private async Task PlaySpotify(string uri)
        {
            Console.WriteLine("Play Spotify: " + uri);

            await Stop();

            await _mopidyClient.ClearTracks();
            await _mopidyClient.AddTrack(uri);

            await _mopidyClient.Play();
        }

        public async Task Stop()
        {
            Console.WriteLine("Stop");
            await _mopidyClient.Stop();
        }

        public async Task Pause()
        {
            Console.WriteLine("Pause");
            await _mopidyClient.Pause();
        }
    }
}
