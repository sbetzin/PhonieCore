using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhonieCore.Logging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace PhonieCore
{
    public class PhonieBackgroundWorker(ILogger<PhonieBackgroundWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Logger.SetLogger(logger);

            cancellationToken.Register(() =>
            {
                logger.LogInformation("PhonieBackgroundWorker shutdown requested");
            });

            var pcDebug = false;
            if (RuntimeInformation.ProcessArchitecture != Architecture.Arm)
            {
                pcDebug = true;
            }

            logger.LogInformation("Loading settings...");
            var settings = await Persistance.SettingsAdapter.LoadAsync();
            var state = new PlayerState(0, 1, "/media")
            {
                CancellationToken = cancellationToken,
                Volume = settings.volume,
                IfName = "wlan0",
                WebSocketUrl = @"ws://localhost:6680/mopidy/ws",
                PCDebug = pcDebug
            };
            logger.LogInformation($"Settings loaded. Volume: {settings.volume}");
            logger.LogInformation($"PCDebug={state.PCDebug}");

            if (state.PCDebug)
            {
                state.WebSocketUrl = @"ws://192.168.0.10:6680/mopidy/ws";
            }

            logger.LogInformation("PhonieBackgroundWorker is starting...");
            await PhonieController.Run(state);

            logger.LogInformation("PhonieBackgroundWorker is shutting down...");

            settings.volume = state.Volume;
            logger.LogInformation($"Saving Volume: {settings.volume} to settings");
            await Persistance.SettingsAdapter.SaveAsync(settings);
        }
    }
}
