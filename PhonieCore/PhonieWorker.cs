using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhonieCore.Logging;

namespace PhonieCore
{
    public class PhonieWorker(ILogger<PhonieWorker> logger) : BackgroundService
    {
        private readonly ILogger<PhonieWorker> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Logger.SetLogger(logger);

            cancellationToken.Register(() =>
            {
                _logger.LogInformation("Cancel requested");
            });

            await Task.Factory.StartNew(x =>
            {
                Radio.Start(cancellationToken);

            }, new object(), cancellationToken);

            _logger.LogInformation("PhonieWorker is running...");
        }
    }
}
