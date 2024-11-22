using System;
using System.Threading.Tasks;
using PhonieCore.Logging;

namespace PhonieCore
{
    public  class InactivityWatcher(PlayerState state)
    {
        public event Action Inactive;
        public async Task WatchForInactivity(int minutes)
        {
            Logger.Log($"Watching for inactivty after {minutes} minutes");
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

            if (state.PlaybackStateChanged.AddMinutes(minutes) > DateTime.Now)
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
