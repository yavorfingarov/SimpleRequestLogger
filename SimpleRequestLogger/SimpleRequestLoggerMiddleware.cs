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
            _Configuration.MessageTemplate ??= "";
            _Logger = loggerFactory.CreateLogger(nameof(SimpleRequestLogger));
            _Next = next;
            _PropertyNames = Regex.Matches(configuration.MessageTemplate, @"((?<=\{)[a-zA-Z]+(?=\}))")
                .Select(m => m.Value)
                .ToList();
            VerifyConfiguration();
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
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
                var elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000 / (double)Stopwatch.Frequency;
                var logLevel = _Configuration.LogLevelSelector(statusCode);
                var loggingContext = new LoggingContext(httpContext, statusCode, elapsedMs);
#pragma warning disable CA2254 // Template should be a static expression.
                _Logger.Log(logLevel, _Configuration.MessageTemplate, MapProperties(loggingContext));
#pragma warning restore CA2254 // Template should be a static expression.
            }
        }

        private void VerifyConfiguration()
        {
            if (Regex.IsMatch(_Configuration.MessageTemplate,
                @"(\{[^\}]*\{)|(\}[^\{]*\})|((?<=\{)[a-zA-Z]*[^a-zA-Z\{\}]+[a-zA-Z]*(?=\}))|(\{\})"))
            {
                throw new InvalidOperationException("Message template is invalid.");
            }
            _ = MapProperties(new LoggingContext(default, default, default));
            var statusCodes = typeof(StatusCodes).GetFields()
                .Select(fi => (int)fi.GetValue(null)!)
                .Distinct();
            foreach (var statusCode in statusCodes)
            {
                try
                {
                    _ = _Configuration.LogLevelSelector(statusCode);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Log level selector throws an exception on status code {statusCode}.", e);
                }
            }
        }

        private object?[] MapProperties(LoggingContext loggingContext)
        {
            var properties = _PropertyNames.Select(pn => loggingContext.GetValue(pn));

            return properties.ToArray();
        }
    }
}
