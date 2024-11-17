using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhonieCore.Logging;

namespace PhonieCore
{
    public class PhonieBackgroundWorker(ILogger<PhonieBackgroundWorker> logger) : BackgroundService
    {
        private readonly ILogger<PhonieBackgroundWorker> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Logger.SetLogger(_logger);

            cancellationToken.Register(() =>
            {
                _logger.LogInformation("PhonieBackgroundWorker shutdown requested");
            });

            _logger.LogInformation("PhonieBackgroundWorker is running...");

            await PhonieController.Run(cancellationToken);

            _logger.LogInformation("PhonieBackgroundWorker is shutting down...");

        }
    }
}
