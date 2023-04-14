using System.Diagnostics;
using System.Globalization;

namespace SimpleRequestLogger
{
    internal sealed class SimpleRequestLoggerMiddleware
    {
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
            _PropertyNames = Regex.Matches(_MessageTemplate, @"((?<=\{)[a-zA-Z]+(?=\}))").Select(m => m.Value).ToArray();
            foreach (var propertyName in _PropertyNames)
            {
                TryAddToPropertyNameMap("Header", propertyName);
                TryAddToPropertyNameMap("Claim", propertyName);
            }
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
                _Logger.Log(logLevel, _MessageTemplate, MapProperties(loggingContext));
            }
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_MessageTemplate) ||
                Regex.IsMatch(_MessageTemplate, @"(\{[^\}]*\{)|(\}[^\{]*\})|((?<=\{)[a-zA-Z]*[^a-zA-Z\{\}]+[a-zA-Z]*(?=\}))|(\{\})"))
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

        private object?[] MapProperties(LoggingContext loggingContext)
        {
            return _PropertyNames.Select(loggingContext.GetValue).ToArray();
        }

        private static void TryAddToPropertyNameMap(string prefix, string propertyName)
        {
            if (propertyName.StartsWith(prefix, StringComparison.InvariantCulture) && propertyName.Length > prefix.Length)
            {
                LoggingContext._PropertyNameMap[propertyName] = Regex.Replace(propertyName, @"[A-Z]", "-$0")[(prefix.Length + 2)..]
                    .ToLower(CultureInfo.InvariantCulture);
            }
        }
    }
}
