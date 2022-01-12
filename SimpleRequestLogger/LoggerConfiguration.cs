using System;
using Microsoft.Extensions.Logging;

namespace SimpleRequestLogger
{
    public class LoggerConfiguration
    {
        public string MessageTemplate { get; set; } = "{Method} {Path}{QueryString} responded {StatusCode} in {ElapsedMs} ms";

        public Func<int, LogLevel> LogLevelSelector { get; set; } = (statusCode) => LogLevel.Information;

        internal LoggerConfiguration()
        {
        }
    }
}
