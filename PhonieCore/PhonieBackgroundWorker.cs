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
            Logger.SetLogger(logger);

            cancellationToken.Register(() =>
            {
                _logger.LogInformation("Cancel requested");
            });

            await Task.Factory.StartNew(x =>
            {
                PhonieController.Start(cancellationToken);

            }, new object(), cancellationToken);

            _logger.LogInformation("PhonieBackgroundWorker is running...");
        }
    }
}
