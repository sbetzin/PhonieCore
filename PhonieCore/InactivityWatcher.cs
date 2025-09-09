using System;
using System.Threading.Tasks;
using System.Diagnostics;
using PhonieCore.Logging;

namespace PhonieCore
{
    public class InactivityWatcher(PlayerState state)
    {
        public event Action Inactive;

        private readonly Stopwatch _idle = new Stopwatch();
        private DateTime _lastPlaybackStateChangedSeen;
        private bool _initialized;

        public async Task WatchForInactivity(int minutes)
        {
            Logger.Log($"Watching for inactivty after {minutes} minutes");

            // Lazy-Init, damit wir nichts am Konstruktor ändern müssen
            if (!_initialized)
            {
                _lastPlaybackStateChangedSeen = state.PlaybackStateChanged;
                _idle.Start();
                _initialized = true;
            }

            while (!state.CancellationToken.IsCancellationRequested)
            {
                try
                {
                    CheckForTimeout(minutes);
                }
                catch (Exception e)
                {
                    Logger.Error("Watching for inactivity failed", e);
                }

                await Task.Delay(10000);
            }
        }

        private void CheckForTimeout(int minutes)
        {
            if (state.PlaybackState is not null && !state.PlaybackState.Equals("stopped") && !state.PlaybackState.Equals("paused"))
            {
                return;
            }

            // Wenn sich der Zustands-Timestamp geändert hat, Stoppuhr neu starten
            if (state.PlaybackStateChanged != _lastPlaybackStateChangedSeen)
            {
                _lastPlaybackStateChangedSeen = state.PlaybackStateChanged;
                if (_idle.IsRunning) _idle.Restart(); else _idle.Start();
            }

            if (_idle.Elapsed < TimeSpan.FromMinutes(minutes))
            {
                return;
            }

            Logging.Logger.Log("Shuting down because of inactivity");
            OnInactive();
        }

        protected virtual void OnInactive()
        {
            Inactive?.Invoke();
        }
    }
}
