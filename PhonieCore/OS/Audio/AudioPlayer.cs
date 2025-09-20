using NAudio.Wave;
using PhonieCore.Logging;
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
using System.Threading.Tasks;

namespace PhonieCore.OS.Audio
{
    public sealed class AudioPlayer : IDisposable
    {
        private readonly MiniAudioEngine _engine;
        private readonly AudioFormat _deviceFormat;
        private readonly AudioPlaybackDevice _playback;

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
            var filePath = GetExecutingAssemblyDirectory(fileName);

            Logger.Log($"playing system sound {filePath}");

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

            byte[] wav = File.ReadAllBytes(filePath);
            int idx = Array.IndexOf(wav, (byte)'d') - 4; // find "data" chunk durch Byte-Vergleich
            byte[] pcm = wav.Skip(idx).ToArray();
            using var stream = new MemoryStream(pcm);
            using var rawProvider = new RawDataProvider(stream, _deviceFormat.Format, _deviceFormat.SampleRate, _deviceFormat.Channels);

            using var player = new SoundPlayer(_engine, _deviceFormat, rawProvider);

            player.Volume = Math.Clamp(volume / 100f, 0f, 1f);
            player.PlaybackEnded  += (_, __) => { finished = true; };
            _playback.MasterMixer.AddComponent(player);
            player.Play();

            while (!finished)
            {
                await Task.Delay(50);
            }

            try { _playback.MasterMixer.RemoveComponent(player); } catch { }

            Logger.Log($"Finished playing {filePath}");
        }

        private static string GetExecutingAssemblyDirectory(string mp3File)
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var location = assembly.Location;
            var file = Path.Combine(Path.GetDirectoryName(location) ?? "/", "sounds", mp3File);

            return file;
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
