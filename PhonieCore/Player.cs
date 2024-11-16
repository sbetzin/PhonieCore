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
        private readonly Mopidy.MopidyAdapter _mopidyMopidyAdapter;

        private const string StopFile = "STOP";
        private const string PauseFile = "PAUSE";
        private const string PlayFile = "PLAY";
        private const string SpotifyFile = "SPOTIFY";

        private int _volume = 80;

        public Player()
        {
            _library = new Library();
            _mopidyMopidyAdapter = new Mopidy.MopidyAdapter();

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
            await _mopidyMopidyAdapter.Play();
        }

        public async Task Next()
        {
            await _mopidyMopidyAdapter.Next();
        }

        public async Task Previous()
        {
            await _mopidyMopidyAdapter.Previous();
        }

        public async Task Seek(int sec)
        {
            await _mopidyMopidyAdapter.Seek(sec);
        }

        private async Task SetVolume(int volume)
        {
            Console.WriteLine($"set volumen {volume}");
            await _mopidyMopidyAdapter.SetVolume(volume);
        }

        public async Task IncreaseVolume()
        {
            if (_volume <= 95)
            {
                _volume += 5;
            }
            await _mopidyMopidyAdapter.SetVolume(_volume);
        }

        public async Task DecreaseVolume()
        {
            if (_volume >= 5)
            {
                _volume -= 5;
            }
            await _mopidyMopidyAdapter.SetVolume(_volume);
        }

        public async Task Play(string[] files)
        {
            var arguments = string.Join(" ", files);
            Console.WriteLine("Play files: " + arguments);

            await Stop();

            await _mopidyMopidyAdapter.ClearTracks();
            foreach (var file in files)
            {
                await _mopidyMopidyAdapter.AddTrack("file://" + file);
            }

            await _mopidyMopidyAdapter.Play();
        }

        private async Task PlaySpotify(string uri)
        {
            Console.WriteLine("Play Spotify: " + uri);

            await Stop();

            await _mopidyMopidyAdapter.ClearTracks();
            await _mopidyMopidyAdapter.AddTrack(uri);

            await _mopidyMopidyAdapter.Play();
        }

        public async Task Stop()
        {
            Console.WriteLine("Stop");
            await _mopidyMopidyAdapter.Stop();
        }

        public async Task Pause()
        {
            Console.WriteLine("Pause");
            await _mopidyMopidyAdapter.Pause();
        }
    }
}
