using System;
using Microsoft.AspNetCore.Builder;

namespace SimpleRequestLogger
{
    public static class ExtensionMethods
    {
        public static IApplicationBuilder UseSimpleRequestLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SimpleRequestLoggerMiddleware>(new LoggerConfiguration());
        }

        public static IApplicationBuilder UseSimpleRequestLogging(this IApplicationBuilder app,
            Action<LoggerConfiguration> configureLogger)
        {
            var configuration = new LoggerConfiguration();
            configureLogger(configuration);

            return app.UseMiddleware<SimpleRequestLoggerMiddleware>(configuration);
        }
    }
}
