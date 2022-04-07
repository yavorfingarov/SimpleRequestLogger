using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
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

        private readonly HttpClient _DefaultConfigClient = GetTestClient(configure: app => app.UseRequestLogging());

        private readonly HttpClient _CustomConfigOneIgnoredPathClient = GetTestClient(
            new
            {
                RequestLogging = new
                {
                    IgnorePaths = new[] { "/ignore/*" }
                }
            },
            app => app.UseRequestLogging());

        private readonly HttpClient _CustomConfigFiveIgnoredPathsClient = GetTestClient(
            new
            {
                RequestLogging = new
                {
                    IgnorePaths = Enumerable.Range(0, 5).Select(i => $"/ignore{i}/*").ToArray()
                }
            },
            app => app.UseRequestLogging());

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

        private static HttpClient GetTestClient(object? configuration = null, Action<IApplicationBuilder>? configure = null)
        {
            var client = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
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
