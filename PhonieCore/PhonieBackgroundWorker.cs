using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhonieCore.Logging;

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

            logger.LogInformation("PhonieBackgroundWorker is starting...");

            var state = new PlayerState(0, 1, "/media")
            {
                CancellationToken = cancellationToken
            };

            await PhonieController.Run(state);

            logger.LogInformation("PhonieBackgroundWorker is shutting down...");

        }
    }
}
