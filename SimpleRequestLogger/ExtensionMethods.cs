using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace SimpleRequestLogger
{
    /// <summary>
    /// Extension methods for enabling SimpleRequestLogger.
    /// </summary>
    public static class ExtensionMethods
    {
        private static readonly string _DefaultConfigurationSection = "RequestLogging";

        private static readonly Func<int, LogLevel> _DefaultLogLevelSelector = (statusCode) => LogLevel.Information;

        private static readonly string _DefaultMessageTemplate = "{Method} {Path}{QueryString} responded {StatusCode} in {ElapsedMs} ms.";

        /// <summary>
        /// Adds SimpleRequestLogger middleware to the pipeline.
        /// </summary>
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SimpleRequestLoggerMiddleware>(_DefaultConfigurationSection,
                _DefaultLogLevelSelector, _DefaultMessageTemplate);
        }

        /// <summary>
        /// Adds SimpleRequestLogger middleware to the pipeline specifying a configuration section.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app,
            string configurationSection)
        {
            return app.UseMiddleware<SimpleRequestLoggerMiddleware>(configurationSection,
                _DefaultLogLevelSelector, _DefaultMessageTemplate);
        }

        /// <summary>
        /// Adds SimpleRequestLogger middleware to the pipeline specifying a log level selector.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app,
            Func<int, LogLevel> logLevelSelector)
        {
            return app.UseMiddleware<SimpleRequestLoggerMiddleware>(_DefaultConfigurationSection,
                logLevelSelector, _DefaultMessageTemplate);
        }

        /// <summary>
        /// Adds SimpleRequestLogger middleware to the pipeline specifying a configuration section and a log level selector.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app,
            string configurationSection, Func<int, LogLevel> logLevelSelector)
        {
            return app.UseMiddleware<SimpleRequestLoggerMiddleware>(configurationSection,
                logLevelSelector, _DefaultMessageTemplate);
        }
    }
}
