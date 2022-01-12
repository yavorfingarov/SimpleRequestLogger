using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using SimpleRequestLogger;

namespace SimpleRequestLoggerTests
{
    public class MiddlewareShouldLogProperty : MiddlewareTestBase
    {
        [Test]
        public async Task Method()
        {
            PrepareTestServer("${message}|${event-properties:item=Method}", app =>
            {
                app.UseSimpleRequestLogging(config =>
                {
                    config.MessageTemplate = "{Method}";
                });
                app.UseRouting();
                app.UseEndpoints(config =>
                {
                    config.MapDelete("/api/endpoint", () => Results.NoContent());
                });
            });

            await Client.DeleteAsync("/api/endpoint");

            Assert.AreEqual("DELETE|DELETE", Logs.Single());
        }

        [Test]
        public async Task Route()
        {
            PrepareTestServer("${message}|${event-properties:item=Path}", app =>
            {
                app.UseSimpleRequestLogging(config =>
                {
                    config.MessageTemplate = "{Path}";
                });
                app.UseRouting();
                app.UseEndpoints(config =>
                {
                    config.MapGet("/api/endpoint", () => Results.Ok());
                });
            });

            await Client.GetAsync("/api/endpoint");

            Assert.AreEqual("/api/endpoint|/api/endpoint", Logs.Single());
        }

        [Test]
        public async Task QueryString()
        {
            PrepareTestServer("${message}|${event-properties:item=QueryString}", app =>
            {
                app.UseSimpleRequestLogging(config =>
                {
                    config.MessageTemplate = "{QueryString}";
                });
                app.UseRouting();
                app.UseEndpoints(config =>
                {
                    config.MapGet("/api/endpoint", () => Results.Ok());
                });
            });

            await Client.GetAsync("/api/endpoint?q=test&t=foo bar");

            Assert.AreEqual("?q=test&t=foo%20bar|?q=test&t=foo%20bar", Logs.Single());
        }

        [Test]
        public async Task Protocol()
        {
            PrepareTestServer("${message}|${event-properties:item=Protocol}", app =>
            {
                app.UseSimpleRequestLogging(config =>
                {
                    config.MessageTemplate = "{Protocol}";
                });
                app.UseRouting();
                app.UseEndpoints(config =>
                {
                    config.MapGet("/api/endpoint", () => Results.Ok());
                });
            });

            await Client.GetAsync("/api/endpoint");

            Assert.AreEqual("HTTP/1.1|HTTP/1.1", Logs.Single());
        }

        [Test]
        public async Task Scheme()
        {
            PrepareTestServer("${message}|${event-properties:item=Scheme}", app =>
            {
                app.UseSimpleRequestLogging(config =>
                {
                    config.MessageTemplate = "{Scheme}";
                });
                app.UseRouting();
                app.UseEndpoints(config =>
                {
                    config.MapGet("/api/endpoint", () => Results.Ok());
                });
            });

            await Client.GetAsync("/api/endpoint");

            Assert.AreEqual("http|http", Logs.Single());
        }

        [Test]
        public async Task UserAgent()
        {
            PrepareTestServer("${message}|${event-properties:item=UserAgent}", app =>
            {
                app.UseSimpleRequestLogging(config =>
                {
                    config.MessageTemplate = "{UserAgent}";
                });
                app.UseRouting();
                app.UseEndpoints(config =>
                {
                    config.MapGet("/api/endpoint", () => Results.Ok());
                });
            });

            var request = new HttpRequestMessage(HttpMethod.Get, "/api/endpoint");
            request.Headers.Add("User-Agent", "Test User Agent");
            await Client.SendAsync(request);

            Assert.AreEqual("Test User Agent|Test User Agent", Logs.Single());
        }

        [Test]
        public async Task StatusCode()
        {
            var expectedStatusCode = StatusCodes.Status418ImATeapot;
            PrepareTestServer("${message}|${event-properties:item=StatusCode}", app =>
            {
                app.UseSimpleRequestLogging(config =>
                {
                    config.MessageTemplate = "{StatusCode}";
                });
                app.UseRouting();
                app.UseEndpoints(config =>
                {
                    config.MapGet("/", () => Results.StatusCode(expectedStatusCode));
                });
            });

            await Client.GetAsync("/");

            var logValues = Logs.Single().Split('|');
            Assert.AreEqual(2, logValues.Length);
            Assert.AreEqual(logValues[0], logValues[1]);
            var statusCode = int.Parse(logValues[0]);
            Assert.AreEqual(expectedStatusCode, statusCode);
        }

        [Test]
        public async Task ElapsedMs()
        {
            var timeAtEndpoint = 200;
            PrepareTestServer("${message}|${event-properties:item=ElapsedMs}", app =>
            {
                app.UseSimpleRequestLogging(config =>
                {
                    config.MessageTemplate = "{ElapsedMs}";
                });
                app.UseRouting();
                app.UseEndpoints(config =>
                {
                    config.MapGet("/", () =>
                    {
                        Thread.Sleep(timeAtEndpoint);

                        return Results.Ok();
                    });
                });
            });

            await Client.GetAsync("/");

            var logValues = Logs.Single().Split('|');
            Assert.AreEqual(2, logValues.Length);
            Assert.AreEqual(logValues[0], logValues[1]);
            var elapsedMs = double.Parse(logValues[0]);
            var delta = elapsedMs - timeAtEndpoint;
            Assert.IsTrue(delta >= 0 && delta < 30);
        }
    }
}
