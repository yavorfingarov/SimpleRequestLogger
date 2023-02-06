using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;

namespace SimpleRequestLogger.UnitTests
{
    public abstract class MiddlewareTestBase
    {
        public IList<string> Logs => _MemoryTarget.Logs;

        public HttpClient Client { get; private set; } = null!;

        private MemoryTarget _MemoryTarget = null!;

        private IHost? _Host;

        [TearDown]
        public void Teardown()
        {
            _MemoryTarget.Dispose();
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
