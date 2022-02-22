using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SimpleRequestLogger.Benchmarks
{
    [MemoryDiagnoser]
    public class Benchmarks
    {
        private const string _Endpoint = "/endpoint";

        private readonly HttpClient _NoSimpleRequestLoggingMiddlewareClient = GetTestClient();

        private readonly HttpClient _DefaultConfigClient = GetTestClient(app => app.UseSimpleRequestLogging());

        private readonly HttpClient _CustomConfigWith1IgnoredPathClient = GetTestClient(app =>
        {
            app.UseSimpleRequestLogging(config =>
            {
                config.IgnorePath("/ignore/*");
            });
        });

        private readonly HttpClient _CustomConfigWith10IgnoredPathsClient = GetTestClient(app =>
        {
            app.UseSimpleRequestLogging(config =>
            {
                for (var i = 0; i < 10; i++)
                {
                    config.IgnorePath($"/ignore{i}/*");
                }
            });
        });

        [Benchmark]
        public async Task NoSimpleRequestLoggingMiddleware()
        {
            await _NoSimpleRequestLoggingMiddlewareClient.GetAsync(_Endpoint);
        }

        [Benchmark(Baseline = true)]
        public async Task DefaultConfig()
        {
            await _DefaultConfigClient.GetAsync(_Endpoint);
        }

        [Benchmark]
        public async Task CustomConfigWith1IgnoredPath()
        {
            await _CustomConfigWith1IgnoredPathClient.GetAsync(_Endpoint);
        }

        [Benchmark]
        public async Task CustomConfigWith10IgnoredPaths()
        {
            await _CustomConfigWith10IgnoredPathsClient.GetAsync(_Endpoint);
        }

        private static HttpClient GetTestClient(Action<IApplicationBuilder>? configure = null)
        {
            var client = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder.UseTestServer();
                    webHostBuilder.ConfigureServices(services =>
                    {
                        services.AddRouting();
                    });
                    webHostBuilder.Configure(app =>
                    {
                        configure?.Invoke(app);
                        app.UseRouting();
                        app.UseEndpoints(config =>
                        {
                            config.MapGet(_Endpoint, () => "Hello, world!");
                        });
                    });
                })
                .Start()
                .GetTestClient();

            return client;
        }
    }
}
