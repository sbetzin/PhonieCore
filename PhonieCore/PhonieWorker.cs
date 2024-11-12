using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PhonieCore
{
    public class PhonieWorker(ILogger<PhonieWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation($"Worker running at: {DateTime.Now}");
            await Task.Factory.StartNew(x => { new Radio(); }, new object(), stoppingToken);
        }
    }
}
