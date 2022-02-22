using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SimpleRequestLogger
{
    internal sealed class SimpleRequestLoggerMiddleware
    {
        private readonly LoggerConfiguration _Configuration;

        private readonly RequestDelegate _Next;

        private readonly ILogger _Logger;

        private readonly IReadOnlyCollection<string> _PropertyNames;

        public SimpleRequestLoggerMiddleware(LoggerConfiguration configuration,
            ILoggerFactory loggerFactory, RequestDelegate next)
        {
            _Configuration = configuration;
            _Logger = loggerFactory.CreateLogger(nameof(SimpleRequestLogger));
            _Next = next;
            _PropertyNames = Regex.Matches(_Configuration.MessageTemplate, @"((?<=\{)[a-zA-Z]+(?=\}))")
                .Select(m => m.Value)
                .ToList();
            _ = MapProperties(new LoggingContext(default, default, default));
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            if (_Configuration.IgnorePathPatterns.Any(p => Regex.IsMatch(httpContext.Request.Path, p)))
            {
                await _Next(httpContext);

                return;
            }
            var start = Stopwatch.GetTimestamp();
            var statusCode = 0;
            try
            {
                await _Next(httpContext);
                statusCode = httpContext.Response.StatusCode;
            }
            catch (Exception)
            {
                statusCode = StatusCodes.Status500InternalServerError;

                throw;
            }
            finally
            {
                var elapsedMs = (int)Math.Round((Stopwatch.GetTimestamp() - start) * 1000 / (double)Stopwatch.Frequency);
                var logLevel = _Configuration.LogLevelSelector(statusCode);
                var loggingContext = new LoggingContext(httpContext, statusCode, elapsedMs);
#pragma warning disable CA2254 // Template should be a static expression.
                _Logger.Log(logLevel, _Configuration.MessageTemplate, MapProperties(loggingContext));
#pragma warning restore CA2254 // Template should be a static expression.
            }
        }

        private object?[] MapProperties(LoggingContext loggingContext)
        {
            var properties = _PropertyNames.Select(pn => loggingContext.GetValue(pn));

            return properties.ToArray();
        }
    }
}
