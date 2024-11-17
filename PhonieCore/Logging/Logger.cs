using System;
using Microsoft.Extensions.Logging;

namespace PhonieCore.Logging
{
    public static class Logger
    {
        private static ILogger<PhonieBackgroundWorker> _logger;

        public static void SetLogger(ILogger<PhonieBackgroundWorker> logger)
        {
            _logger = logger;
        }

        public static void Log(string text)
        {
            _logger?.LogInformation(text);
        }

        public static void Error(Exception exception)
        {
            _logger?.LogError(exception, exception.Message, new object());
        }
    }
}
