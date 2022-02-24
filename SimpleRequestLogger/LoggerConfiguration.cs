using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SimpleRequestLogger
{
    /// <summary>
    /// Contains configuration for SimpleRequestLogger.
    /// </summary>
    public class LoggerConfiguration
    {
        /// <summary>
        /// Sets the log message template. The available properties are 
        /// <c>Method</c>, <c>Path</c>, <c>QueryString</c>, <c>Protocol</c>, 
        /// <c>Scheme</c>, <c>UserAgent</c>, <c>StatusCode</c> and <c>ElapsedMs</c>.
        /// </summary>
        /// <remarks>The default value is <c>"{Method} {Path}{QueryString} responded {StatusCode} in {ElapsedMs} ms."</c>.</remarks>
        public string MessageTemplate { internal get => _MessageTemplate; set => SetMessageTemplate(value); }

        /// <summary>
        /// Sets the log level selector.
        /// </summary>
        /// <remarks>By default, information log level is selected for all status codes.</remarks>
        public Func<int, LogLevel> LogLevelSelector { internal get => _LogLevelSelector; set => SetLogLevelSelector(value); }

        internal IReadOnlyList<string> IgnorePathPatterns => _IgnorePathPatterns;

        private string _MessageTemplate = "{Method} {Path}{QueryString} responded {StatusCode} in {ElapsedMs} ms.";

        private Func<int, LogLevel> _LogLevelSelector = (statusCode) => LogLevel.Information;

        private readonly List<string> _IgnorePathPatterns = new List<string>();

        internal LoggerConfiguration()
        {
        }

        /// <summary>
        /// Adds a path to skip logging for.
        /// </summary>
        /// <remarks>The path may contain <c>*</c> as wildcard.</remarks>
        public void IgnorePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException("Ignore path cannot be null or empty.");
            }
            var pathPattern = $"^{Regex.Escape(path).Replace("\\*", ".*")}$";
            if (!_IgnorePathPatterns.Contains(pathPattern))
            {
                _IgnorePathPatterns.Add(pathPattern);
            }
        }

        private void SetMessageTemplate(string messageTemplate)
        {
            if (string.IsNullOrWhiteSpace(messageTemplate) ||
                Regex.IsMatch(messageTemplate, @"(\{[^\}]*\{)|(\}[^\{]*\})|((?<=\{)[a-zA-Z]*[^a-zA-Z\{\}]+[a-zA-Z]*(?=\}))|(\{\})"))
            {
                throw new InvalidOperationException("Message template is invalid.");
            }
            _MessageTemplate = messageTemplate;
        }

        private void SetLogLevelSelector(Func<int, LogLevel> logLevelSelector)
        {
            if (logLevelSelector == null)
            {
                throw new InvalidOperationException("Log level selector cannot be null.");
            }
            var statusCodes = typeof(StatusCodes).GetFields()
                .Select(fi => (int)fi.GetValue(null)!)
                .Distinct();
            foreach (var statusCode in statusCodes)
            {
                try
                {
                    _ = logLevelSelector(statusCode);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Log level selector throws an exception on status code {statusCode}.", ex);
                }
            }
            _LogLevelSelector = logLevelSelector;
        }
    }
}
