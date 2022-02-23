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

        private readonly HttpClient _NoSimpleRequestLoggerClient = GetTestClient();

        private readonly HttpClient _DefaultConfigClient = GetTestClient(app => app.UseSimpleRequestLogging());

        private readonly HttpClient _CustomConfigOneIgnoredPathClient = GetTestClient(app =>
        {
            app.UseSimpleRequestLogging(config =>
            {
                config.IgnorePath("/ignore/*");
            });
        });

        private readonly HttpClient _CustomConfigFiveIgnoredPathsClient = GetTestClient(app =>
        {
            app.UseSimpleRequestLogging(config =>
            {
                for (var i = 0; i < 5; i++)
                {
                    config.IgnorePath($"/ignore{i}/*");
                }
            });
        });

        [Benchmark]
        public async Task<HttpResponseMessage> NoSimpleRequestLogger()
        {
            return await _NoSimpleRequestLoggerClient.GetAsync(_Endpoint);
        }

        [Benchmark]
        public async Task<HttpResponseMessage> DefaultConfig()
        {
            return await _DefaultConfigClient.GetAsync(_Endpoint);
        }

        [Benchmark]
        public async Task<HttpResponseMessage> CustomConfigOneIgnoredPath()
        {
            return await _CustomConfigOneIgnoredPathClient.GetAsync(_Endpoint);
        }

        [Benchmark]
        public async Task<HttpResponseMessage> CustomConfigFiveIgnoredPaths()
        {
            return await _CustomConfigFiveIgnoredPathsClient.GetAsync(_Endpoint);
        }

        private static HttpClient GetTestClient(Action<IApplicationBuilder>? configure = null)
        {
            var client = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder.UseTestServer();
                    webHostBuilder.Configure(app =>
                    {
                        configure?.Invoke(app);
                    });
                })
                .Start()
                .GetTestClient();

            return client;
        }
    }
}
