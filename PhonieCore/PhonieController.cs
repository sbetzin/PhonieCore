using System;
using System.Device.Gpio;
using System.Threading.Tasks;
using PhonieCore.Hardware;
using PhonieCore.Logging;
using PhonieCore.Mopidy;
using PhonieCore.OS;
using PhonieCore.OS.Audio;

namespace PhonieCore
{
    public static class PhonieController
    {
        private static PlayerController _playerController;
        private static PlayerState _state;

        public static async Task Run(PlayerState state)
        {
            _state = state;
          
            using var modipyAdapter = new MopidyAdapter(state.WebSocketUrl);
            using var audioPlayer = new AudioPlayer();
            var mediaAdapter = new MediaFilesAdapter(state);
            await modipyAdapter.ConnectAsync();

            _playerController = new PlayerController(modipyAdapter, mediaAdapter, audioPlayer, _state);
            await _playerController.SetVolume(state.Volume);

            var watcher = new InactivityWatcher(state);
            watcher.Inactive += BashAdapter.Shutdown;

            // We connect the hardware only if not PCDebug
            if (!state.PCDebug)
            {
                _ = Task.Run(async () => await watcher.WatchForInactivity(30));

                var networkManagerAdapter = new NetworkManagerAdapter();
                _ = Task.Run(async () =>
                {
                    await networkManagerAdapter.StartAsync(state.IfName);
                    //await networkManagerAdapter.EnsureWifiProfileAsync("generic.de Data", "generic.de Data", "surf_the_green_wave");
                });

                _ = Task.Run(async () =>
                {
                    await GpioButton.RunAsync(26, PinMode.InputPullUp, TimeSpan.FromMilliseconds(100), false,

                    onPressed: async () =>
                    {
                        Logger.Log("Button PRESSED");
                        await networkManagerAdapter.TryConnectAsync();
                    },
                    onReleased: () => Logger.Log("Button RELEASED"),
                    state.CancellationToken);
                });

                _ = Task.Run(async () =>
                {
                    RfidReader.NewCardDetected += async (uid) => await _playerController.ProcessFolder(uid);
                    await RfidReader.DetectCards(_state);
                });
            }

            await _playerController.PlaySystemSoundAsync(SystemSounds.Startup, true);

            while (!state.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(500);
            }

            await _playerController.PlaySystemSoundAsync(SystemSounds.Shutdown, true);
        }

    }
}
