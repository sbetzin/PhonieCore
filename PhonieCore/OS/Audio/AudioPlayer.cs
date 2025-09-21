using PhonieCore.Logging;
using PhonieCore.OS.Audio.Wave;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PhonieCore.OS.Audio
{
    public sealed class AudioPlayer : IDisposable
    {
        private readonly MiniAudioEngine _engine;
        private readonly AudioFormat _deviceFormat;
        private readonly AudioPlaybackDevice _playback;

        private static readonly Regex RangeRegex = new(@"\{(\d+)-(\d+)\}", RegexOptions.Compiled);
        private static readonly Random _random = new();

        public AudioPlayer()
        {
            _engine = new MiniAudioEngine();
            _deviceFormat = new AudioFormat()
            {
                SampleRate = 24000,
                Channels = 2,
                Format = SampleFormat.S16
            };
            var defaultDevice = _engine.PlaybackDevices.FirstOrDefault(d => d.IsDefault);
            _playback = _engine.InitializePlaybackDevice(defaultDevice, _deviceFormat);

            Console.WriteLine($"Using Playback Device: {defaultDevice.Name} - {_playback.Format.SampleRate}");
            _playback.Start();
        }

        public async Task PlayAsync(string fileName, bool wait = false, int volume = 50)
        {
            var filePath = GetFileToPlay(fileName);

            //Logger.Log($"playing system sound {filePath}");

            var task = Task.Run(async () =>
            {
                try
                {
                    await StartPlaybackTask(filePath, volume);
                }
                catch (Exception e)
                {
                    Logger.Error($"could not play {filePath}", e);
                }
            });
            if (wait)
            {
                await task;
            }
        }
        private async Task StartPlaybackTask(string filePath, int volume)
        {
            var finished = false;

            var file = WavAudioFile.Parse(File.ReadAllBytes(filePath));
            using var stream = new MemoryStream(file.Data);
            using var rawProvider = new RawDataProvider(stream, _deviceFormat.Format, _deviceFormat.SampleRate, _deviceFormat.Channels);

            using var player = new SoundPlayer(_engine, _deviceFormat, rawProvider);

            player.Volume = Math.Clamp(volume / 100f, 0f, 1f);
            player.PlaybackEnded += (_, __) => { finished = true; };
            _playback.MasterMixer.AddComponent(player);
            player.Play();

            while (!finished)
            {
                await Task.Delay(50);
            }

            try { _playback.MasterMixer.RemoveComponent(player); } catch { }

            Logger.Log($"Finished playing {filePath}");
        }

        private static string GetFileToPlay(string mp3File)
        {
            var filename = ResolveFileRandomized(mp3File);
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var location = assembly.Location;
            var fullPath = Path.Combine(Path.GetDirectoryName(location) ?? "/", "sounds", filename);

            return fullPath;
        }


        public static string ResolveFileRandomized(string fileName)
        {
            var match = RangeRegex.Match(fileName);

            int min = 1, max = 1;
            if (match.Success &&
                int.TryParse(match.Groups[1].Value, out int minVal) &&
                int.TryParse(match.Groups[2].Value, out int maxVal))
            {
                min = minVal;
                max = maxVal;
            }

            int randomNum = _random.Next(min, max + 1);
            string newFileName = RangeRegex.Replace(fileName, randomNum.ToString());

            return newFileName;
        }

        public void Dispose()
        {
            foreach (var c in _playback.MasterMixer.Components.ToArray())
                _playback.MasterMixer.RemoveComponent(c);
            _playback.Stop();
            _playback.Dispose();
            _engine.Dispose();
        }
    }
}
