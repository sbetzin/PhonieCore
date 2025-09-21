using System;
using System.Device.Gpio;
using System.Threading.Tasks;
using PhonieCore.Hardware;
using PhonieCore.Logging;
using PhonieCore.Mopidy;
using PhonieCore.OS;
using PhonieCore.OS.Audio;
using PhonieCore.OS.Network.Model;

namespace PhonieCore
{
    public static class PhonieController
    {
        private static PlayerController _playerController;
        private static PlayerState _state;
        private static NetworkManagerAdapter _networkManagerAdapter;

        public static async Task Run(PlayerState state)
        {
            _state = state;
          
            using var modipyAdapter = new MopidyAdapter(state.WebSocketUrl);
            using var audioPlayer = new AudioPlayer();
            var mediaAdapter = new MediaFilesAdapter(state);
            await modipyAdapter.ConnectAsync();

            _playerController = new PlayerController(modipyAdapter, mediaAdapter, audioPlayer, _state);
            _playerController.Startup();
            await _playerController.SetVolume(state.Volume);

            var watcher = new InactivityWatcher(state);
            watcher.Inactive += BashAdapter.Shutdown;

            // We connect the hardware only if not PCDebug
            if (!state.PCDebug)
            {
                _ = Task.Run(async () => await watcher.WatchForInactivity(30));

                _networkManagerAdapter = new NetworkManagerAdapter();
                _ = Task.Run(async () =>
                {
                    await _networkManagerAdapter.StartAsync(state.IfName);
                    _networkManagerAdapter.NetworkStatusChanged += NetworkConnectionChanged;
                    //await networkManagerAdapter.EnsureWifiProfileAsync("generic.de Data", "generic.de Data", "surf_the_green_wave");
                });

                _ = Task.Run(async () =>
                {
                    await GpioButton.RunAsync(26, PinMode.InputPullUp, TimeSpan.FromMilliseconds(100), false, ButtonPressed, ButtonReleased, state.CancellationToken);
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

        public static async void NetworkConnectionChanged(NetworkManagerState state)
        {
            if (state == NetworkManagerState.Disconnected) await _playerController.PlaySystemSoundAsync(SystemSounds.InternetDisconnected, true);
            if (state == NetworkManagerState.ConnectedGlobal) await _playerController.PlaySystemSoundAsync(SystemSounds.InternetConnected, true);
        }

        public static async void ButtonPressed()
        {
            Logger.Log("Button PRESSED");
            if (_networkManagerAdapter.CurrentState != NetworkManagerState.ConnectedGlobal)
            {
                await _playerController.PlaySystemSoundAsync(SystemSounds.NoInternet, true);
                await _networkManagerAdapter.TryConnectAsync();
            }
           
        }

        public static async void ButtonReleased()
        {
            Logger.Log("Button RELEASED");
            await Task.Delay(100);
        }

    }
}
