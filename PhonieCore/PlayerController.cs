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
            if (files.Length == 0)
            {
                return;
            }

            if (files.First().Contains("STOP"))
            {
                await StopAsync();
            }
            else if (files.First().Contains("PLAY"))
            {
                await PlayAsync();
            }
            else if (files.First().Contains("PAUSE"))
            {
                await PauseAsync();
            }
            else if (files.First().Contains("INCREASE_VOLUME"))
            {
                await IncreaseVolume();
            }
            else if (files.First().Contains("DECREASE_VOLUME"))
            {
                await DecreaseVolume();
            }
            else if (files.First().Contains("SPOTIFY"))
            {
                var file = files.First();
                var url = await File.ReadAllTextAsync(file);
                await PlaySpotifyAsync(url);
            }
            else if (files.Any(f => f.EndsWith("mp3")))
            {
                if (state.PlayingTag == uid)
                {
                    return;
                }

                var mp3Files = files.Where(f => f.EndsWith("mp3")).ToArray();
                state.PlayingTag = uid;
                await PlayAsync(mp3Files);
            }
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

        public async Task PlayAsync(string file)
        {
            await PlayAsync([file]);
        }

        public async Task PlayAsync(string[] files)
        {
            files = files.Order().ToArray();
            var filesString = string.Join(" ", files);
            Logger.Log("Play files: " + filesString);

            await adapter.StopAsync();
            await adapter.ClearTracksAsync();

            var tracks = files.Select(file => $@"file://{file}").ToArray();
            await adapter.AddTracks(tracks);
            await adapter.DontRepeat();
            await adapter.PlayAsync();
        }

        private async Task PlaySpotifyAsync(string uri)
        {
            Logger.Log("Play Spotify: " + uri);

            await adapter.StopAsync();
            await adapter.ClearTracksAsync();
            await adapter.AddTrackAsync(uri);
            await adapter.PlayAsync();
        }

        public async Task PlayAsync()
        {
            if (state.PlaybackState == "paused" || state.PlaybackState == "stopped")
            {
                await adapter.PlayAsync();
            }
        }

        public async Task StopAsync()
        {
            if (state.PlaybackState == "playing" || state.PlaybackState == "paused")
            {
                await adapter.StopAsync();
                ResetCurrentRfidTag();
            }
        }

        public async Task PauseAsync()
        {
            if (state.PlaybackState == "playing")
            {
                await adapter.PauseAsync();
            }

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

        private void StorePlaybackState(IDictionary<string, JToken> data)
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

        internal void PlaySystemSoundAsync(object disconnected, bool v)
        {
            throw new NotImplementedException();
        }
    }
}
