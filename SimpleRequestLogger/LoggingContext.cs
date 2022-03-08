using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace SimpleRequestLogger
{
    internal struct LoggingContext
    {
        private readonly HttpContext? _HttpContext;

        private readonly int _StatusCode;

        private readonly int _ElapsedMs;

        internal LoggingContext(HttpContext httpContext, int statusCode, int elapsedMs)
        {
            _HttpContext = httpContext;
            _StatusCode = statusCode;
            _ElapsedMs = elapsedMs;
        }

        internal object? GetValue(string propertyName)
        {
            object? propertyValue = propertyName switch
            {
                "Method" => _HttpContext?.Request.Method,
                "Path" => _HttpContext?.Request.Path,
                "QueryString" => _HttpContext?.Request.QueryString,
                "Protocol" => _HttpContext?.Request.Protocol,
                "Scheme" => _HttpContext?.Request.Scheme,
                "UserAgent" => GetUserAgent(),
                "StatusCode" => _StatusCode,
                "ElapsedMs" => _ElapsedMs,
                _ => throw new InvalidOperationException($"Encountered an unexpected property '{propertyName}'.")
            };

            return propertyValue;
        }

        private string GetUserAgent()
        {
            StringValues userAgent = default;
            _HttpContext?.Request.Headers.TryGetValue("User-Agent", out userAgent);

            return userAgent;
        }
    }
}
