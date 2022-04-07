using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using NUnit.Framework;

namespace SimpleRequestLogger.Tests
{
    public abstract class MiddlewareTestBase
    {
        protected IList<string> Logs => _MemoryTarget.Logs;

        protected HttpClient Client = null!;

        private MemoryTarget _MemoryTarget = null!;

        private IHost? _Host;

        [TearDown]
        public void Teardown()
        {
            _Host?.Dispose();
        }

        protected void PrepareTestServer(string messageLayout, object? configuration, Action<IApplicationBuilder> configure)
        {
            _MemoryTarget = new MemoryTarget() { Layout = messageLayout };
            var loggingConfiguration = new LoggingConfiguration();
            loggingConfiguration.AddRuleForAllLevels(_MemoryTarget, nameof(SimpleRequestLogger));
            var hostBuilder = new HostBuilder();
            hostBuilder.ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseTestServer();
                webHostBuilder.ConfigureAppConfiguration(configurationBuilder =>
                {
                    if (configuration != null)
                    {
                        var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(configuration)));
                        configurationBuilder.AddJsonStream(jsonStream);
                    }
                });
                webHostBuilder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddNLog(loggingConfiguration);
                });
                webHostBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                });
                webHostBuilder.Configure(app => configure(app));
            });
            _Host = hostBuilder.Start();
            Client = _Host.GetTestClient();
        }
    }
}
