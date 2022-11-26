using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SimpleRequestLogger
{
    internal sealed class SimpleRequestLoggerMiddleware
    {
        private static readonly Regex _InvalidMessageTemplateRegex = new(
            @"(\{[^\}]*\{)|(\}[^\{]*\})|((?<=\{)[a-zA-Z]*[^a-zA-Z\{\}]+[a-zA-Z]*(?=\}))|(\{\})");

        private static readonly Regex _MessageTemplatePropertiesRegex = new(@"((?<=\{)[a-zA-Z]+(?=\}))");

        private readonly string _MessageTemplate;

        private readonly Func<int, LogLevel> _LogLevelSelector;

        private readonly string[] _IgnorePatterns;

        private readonly string[] _PropertyNames;

        private readonly Stopwatch _Stopwatch;

        private readonly ILogger _Logger;

        private readonly RequestDelegate _Next;

        public SimpleRequestLoggerMiddleware(
            string configurationSection,
            Func<int, LogLevel> logLevelSelector,
            string defaultMessageTemplate,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            RequestDelegate next)
        {
            _MessageTemplate = configuration[$"{configurationSection}:MessageTemplate"] ?? defaultMessageTemplate;
            _LogLevelSelector = logLevelSelector;
            var ignorePaths = configuration.GetSection($"{configurationSection}:IgnorePaths").Get<string[]>() ?? Array.Empty<string>();
            _IgnorePatterns = ignorePaths.Distinct()
                .Select(path => $"^{Regex.Escape(path).Replace("\\*", ".*")}$")
                .ToArray();
            _PropertyNames = _MessageTemplatePropertiesRegex.Matches(_MessageTemplate).Select(m => m.Value).ToArray();
            ValidateConfiguration();
            _Stopwatch = new Stopwatch();
            _Logger = loggerFactory.CreateLogger(nameof(SimpleRequestLogger));
            _Next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            if (_IgnorePatterns.Any(pattern => Regex.IsMatch(httpContext.Request.Path, pattern)))
            {
                await _Next(httpContext);

                return;
            }
            _Stopwatch.Restart();
            var statusCode = StatusCodes.Status200OK;
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
                var logLevel = _LogLevelSelector(statusCode);
                _Stopwatch.Stop();
                var loggingContext = new LoggingContext(httpContext, statusCode, _Stopwatch.ElapsedMilliseconds);
#pragma warning disable CA2254 // Template should be a static expression.
                _Logger.Log(logLevel, _MessageTemplate, MapProperties(loggingContext));
#pragma warning restore CA2254 // Template should be a static expression.
            }
        }

        private object?[] MapProperties(LoggingContext loggingContext)
        {
            return _PropertyNames.Select(loggingContext.GetValue).ToArray();
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_MessageTemplate) ||
                _InvalidMessageTemplateRegex.IsMatch(_MessageTemplate))
            {
                throw new InvalidOperationException("Message template is invalid.");
            }

            var statusCodes = typeof(StatusCodes).GetFields()
                .Select(fi => (int)fi.GetValue(null)!)
                .Distinct();
            foreach (var statusCode in statusCodes)
            {
                try
                {
                    _ = _LogLevelSelector(statusCode);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Log level selector throws an exception on status code {statusCode}.", ex);
                }
            }

            if (_IgnorePatterns.Any(path => Regex.Unescape(path).Replace(" ", "") == "^$"))
            {
                throw new InvalidOperationException("Ignore path cannot be null or empty.");
            }

            _ = MapProperties(new LoggingContext());
        }
    }
}
