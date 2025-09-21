using Newtonsoft.Json.Linq;
using PhonieCore.Logging;
using PhonieCore.Mopidy;
using PhonieCore.OS;
using PhonieCore.OS.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PhonieCore
{
    public class PlayerController(MopidyAdapter adapter, MediaFilesAdapter mediaAdapter, AudioPlayer systemSounds, PlayerState state)
    {
        public void Startup()
        {
            adapter.MessageReceived += async (eventName, data) => await ModipyAdapter_MessageReceivedAsync(eventName, data);
        }

        public async Task PlaySystemSoundAsync(string sound, bool wait)
        {
            await systemSounds.PlayAsync(sound, wait, state.Volume);
        }

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
                await systemSounds.PlayAsync(SystemSounds.Click, false, state.Volume);
            }
        }

        public async Task DecreaseVolume()
        {
            if (state.Volume >= 10)
            {
                await SetVolume(state.Volume -= 5);
                await systemSounds.PlayAsync(SystemSounds.Click, false, state.Volume);
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

        private async Task ModipyAdapter_MessageReceivedAsync(string eventName, IDictionary<string, JToken> data)
        {
            switch (eventName)
            {
                case "track_playback_started":
                    state.NextTrackId = await adapter.GetNextTrackId().ConfigureAwait(false);
                    break;
                case "track_playback_ended":
                    Logger.Log($"track_playback_ended");
                    if (state.NextTrackId == 0)
                    {
                        Logger.Log("No next Track Id. Resetting rfid tag");
                        ResetCurrentRfidTag();
                    }
                    break;

                case "volume_changed":
                    CheckVolumeOnStateChanged(data);
                    break;

                case "playback_state_changed":
                    StorePlaybackState(data);

                    break;
            }
        }

        private  void StorePlaybackState(IDictionary<string, JToken> data)
        {
            state.PlaybackStateChanged = DateTime.Now;
            state.PlaybackState = (string)data["new_state"];

            Logger.Log($"New playback state: {state.PlaybackState}");
        }

        private void CheckVolumeOnStateChanged(IDictionary<string, JToken> data)
        {
            var volume = (int)data["volume"];

            if (volume == state.Volume)
            {
                return;
            }

            Logger.Log($"Volume set to {volume}");
            state.Volume = volume;
        }

        private void ResetCurrentRfidTag()
        {
            state.PlayingTag = string.Empty;
        }
    }
}
