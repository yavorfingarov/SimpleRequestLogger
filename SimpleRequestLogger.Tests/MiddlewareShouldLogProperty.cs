using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace SimpleRequestLogger.Tests
{
    public class MiddlewareShouldLogProperty : MiddlewareTestBase
    {
        [Test]
        public async Task Method()
        {
            PrepareTestServer(
                "${message}|${event-properties:item=Method}",
                new
                {
                    RequestLogging = new
                    {
                        MessageTemplate = "{Method}"
                    }
                },
                app =>
                {
                    app.UseRequestLogging();
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
        public async Task Path()
        {
            PrepareTestServer(
                "${message}|${event-properties:item=Path}",
                new
                {
                    RequestLogging = new
                    {
                        MessageTemplate = "{Path}"
                    }
                },
                app =>
                {
                    app.UseRequestLogging();
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
            PrepareTestServer(
                "${message}|${event-properties:item=QueryString}",
                new
                {
                    RequestLogging = new
                    {
                        MessageTemplate = "{QueryString}"
                    }
                },
                app =>
                {
                    app.UseRequestLogging();
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
            PrepareTestServer(
                "${message}|${event-properties:item=Protocol}",
                new
                {
                    RequestLogging = new
                    {
                        MessageTemplate = "{Protocol}"
                    }
                },
                app =>
                {
                    app.UseRequestLogging();
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
            PrepareTestServer(
                "${message}|${event-properties:item=Scheme}",
                new
                {
                    RequestLogging = new
                    {
                        MessageTemplate = "{Scheme}"
                    }
                },
                app =>
                {
                    app.UseRequestLogging();
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
        public async Task Header()
        {
            PrepareTestServer(
                "${message}|${event-properties:item=HeaderUserAgent}",
                new
                {
                    RequestLogging = new
                    {
                        MessageTemplate = "{HeaderUserAgent}"
                    }
                },
                app =>
                {
                    app.UseRequestLogging();
                    app.UseRouting();
                    app.UseEndpoints(config =>
                    {
                        config.MapGet("/api/endpoint", () => Results.Ok());
                    });
                });
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/endpoint");
            request.Headers.Add("user-agent", "Test User Agent");

            await Client.SendAsync(request);

            Assert.AreEqual("Test User Agent|Test User Agent", Logs.Single());
        }

        [Test]
        public async Task RemoteIpAddress()
        {
            var remoteIpAddress = Random.Shared.Next();
            PrepareTestServer(
                "${message}|${event-properties:item=RemoteIpAddress}",
                new
                {
                    RequestLogging = new
                    {
                        MessageTemplate = "{RemoteIpAddress}"
                    }
                },
                app =>
                {
                    app.UseRequestLogging();
                    app.UseRouting();
                    app.UseEndpoints(config =>
                    {
                        config.MapGet("/api/endpoint", (HttpContext context) =>
                        {
                            context.Connection.RemoteIpAddress = new IPAddress(remoteIpAddress);

                            return Results.Ok();
                        });
                    });
                });

            await Client.GetAsync("/api/endpoint");
            await Client.GetAsync("/api/endpoint");

            var logValues = Logs[0].Split('|');
            Assert.AreEqual(2, logValues.Length);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(logValues[0]));
            Assert.AreEqual(logValues[0], logValues[1]);
            Assert.AreEqual(logValues[0], Logs[1].Split('|')[0]);
        }

        [Test]
        public async Task Claim()
        {
            PrepareTestServer(
                "${message}|${event-properties:item=ClaimUserId}",
                new
                {
                    RequestLogging = new
                    {
                        MessageTemplate = "{ClaimUserId}"
                    }
                },
                app =>
                {
                    app.UseRequestLogging();
                    app.UseRouting();
                    app.UseEndpoints(config =>
                    {
                        config.MapGet("/api/endpoint", (HttpContext context) =>
                        {
                            var claims = new Claim[] { new("user-id", "testClaimUserId") };
                            context.User.AddIdentity(new ClaimsIdentity(claims));

                            return Results.Ok();
                        });
                    });
                });

            await Client.GetAsync("/api/endpoint");

            Assert.AreEqual("testClaimUserId|testClaimUserId", Logs.Single());
        }

        [Test]
        public async Task StatusCode()
        {
            PrepareTestServer(
                "${message}|${event-properties:item=StatusCode}",
                new
                {
                    RequestLogging = new
                    {
                        MessageTemplate = "{StatusCode}"
                    }
                },
                app =>
                {
                    app.UseRequestLogging();
                    app.UseRouting();
                    app.UseEndpoints(config =>
                    {
                        config.MapGet("/", () => Results.StatusCode(StatusCodes.Status418ImATeapot));
                    });
                });

            await Client.GetAsync("/");

            Assert.AreEqual("418|418", Logs.Single());
        }

        [Test]
        public async Task ElapsedMs()
        {
            var timeAtEndpoint = 1000;
            PrepareTestServer(
                "${message}|${event-properties:item=ElapsedMs}",
                new
                {
                    RequestLogging = new
                    {
                        MessageTemplate = "{ElapsedMs}"
                    }
                },
                app =>
                {
                    app.UseRequestLogging();
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
            var elapsedMs = long.Parse(logValues[0]);
            var delta = elapsedMs - timeAtEndpoint;
            Assert.IsTrue(delta >= 0 && delta < 100);
        }
    }
}
