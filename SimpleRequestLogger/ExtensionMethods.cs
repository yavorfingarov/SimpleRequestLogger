using System;
using Microsoft.AspNetCore.Builder;

namespace SimpleRequestLogger
{
    /// <summary>
    /// Extension methods for enabling SimpleRequestLogger.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Adds SimpleRequestLogger middleware to the pipeline with the default configuration.
        /// </summary>
        public static IApplicationBuilder UseSimpleRequestLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SimpleRequestLoggerMiddleware>(new LoggerConfiguration());
        }

        /// <summary>
        /// Adds SimpleRequestLogger middleware to the pipeline with a custom configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static IApplicationBuilder UseSimpleRequestLogging(this IApplicationBuilder app,
            Action<LoggerConfiguration> configureLogger)
        {
            var configuration = new LoggerConfiguration();
            configureLogger(configuration);

            return app.UseMiddleware<SimpleRequestLoggerMiddleware>(configuration);
        }
    }
}
