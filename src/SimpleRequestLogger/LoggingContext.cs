namespace SimpleRequestLogger
{
    internal readonly struct LoggingContext
    {
        internal static readonly Dictionary<string, string> _PropertyNameMap = new();

        private readonly HttpContext? _HttpContext;

        private readonly int _StatusCode;

        private readonly long _ElapsedMs;

        internal LoggingContext(HttpContext httpContext, int statusCode, long elapsedMs)
        {
            _HttpContext = httpContext;
            _StatusCode = statusCode;
            _ElapsedMs = elapsedMs;

        }

        internal object? GetValue(string propertyName)
        {
            object? propertyValue;
            if (propertyName.StartsWith("Header", StringComparison.InvariantCulture) && _PropertyNameMap.TryGetValue(propertyName, out var headerKey))
            {
                propertyValue = _HttpContext?.Request.Headers[headerKey].ToString();
            }
            else if (propertyName.StartsWith("Claim", StringComparison.InvariantCulture) && _PropertyNameMap.TryGetValue(propertyName, out var claimType))
            {
                propertyValue = _HttpContext?.User.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
            }
            else
            {
                propertyValue = propertyName switch
                {
                    "Method" => _HttpContext?.Request.Method,
                    "Path" => _HttpContext?.Request.Path.ToString(),
                    "QueryString" => _HttpContext?.Request.QueryString.ToString(),
                    "Protocol" => _HttpContext?.Request.Protocol,
                    "Scheme" => _HttpContext?.Request.Scheme,
                    "RemoteIpAddress" => _HttpContext?.Connection.RemoteIpAddress?.ToString(),
                    "StatusCode" => _StatusCode,
                    "ElapsedMs" => _ElapsedMs,
                    _ => throw new InvalidOperationException($"Encountered an unexpected property '{propertyName}'.")
                };
            }

            return propertyValue;
        }
    }
}
