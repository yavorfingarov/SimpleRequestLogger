using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using NUnit.Framework;

namespace SimpleRequestLoggerTests
{
    public abstract class MiddlewareTestBase
    {
        protected IList<string> Logs => _MemoryTarget.Logs;

        protected HttpClient Client = default!;

        private MemoryTarget _MemoryTarget = default!;

        private IHost? _Host;

        [TearDown]
        public void Teardown()
        {
            _Host?.Dispose();
        }

        protected void PrepareTestServer(string messageLayout, Action<IApplicationBuilder> configure)
        {
            _MemoryTarget = new MemoryTarget() { Layout = messageLayout };
            var loggingConfiguration = new LoggingConfiguration();
            loggingConfiguration.AddRuleForAllLevels(_MemoryTarget, nameof(SimpleRequestLogger));
            var hostBuilder = new HostBuilder();
            hostBuilder.ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseTestServer();
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
