using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace SimpleRequestLogger.Tests
{
    public class MiddlewareShould : MiddlewareTestBase
    {
        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("test{}")]
        [TestCase("test{0}")]
        [TestCase("{foo bar}")]
        [TestCase("{foo{bar}foo}")]
        [TestCase("{test {foo{bar}foo}")]
        [TestCase("{foo{Method}foo}")]
        [TestCase("{foo{Method}{Path}foo}")]
        [TestCase("{foo{Method {Path}} foo}")]
        [TestCase("test {foo{{{Method{Path}}foo}")]
        [TestCase("{Method} {Path}}{QueryString} responded {StatusCode} in {ElapsedMs} ms")]
        [TestCase("{Scheme} {Method} {Path} {=> {StatusCode}")]
        [TestCase("{Scheme:X} {Method} {Path} => {StatusCode}")]
        [TestCase("{Scheme} {foo0bar} {Path} => {StatusCode}")]
        [TestCase("{Scheme} {foo0.0bar} {Path} => {StatusCode}")]
        [TestCase("{Scheme} {Method} {Path:0.00} => {StatusCode}")]
        [TestCase("{Scheme} {Method} {Path} => {StatusCode!}")]
        [TestCase("{Scheme} {Method} {Path} => {0}")]
        public void ThrowOnStartup_WhenMessageTemplateIsInvalid(string messageTemplate)
        {
            Action prepare = () => PrepareTestServer(
                "",
                new
                {
                    RequestLogging = new
                    {
                        MessageTemplate = messageTemplate
                    }
                },
                app =>
                {
                    app.UseRequestLogging();
                });

            var exception = Assert.Throws<InvalidOperationException>(() => prepare());
            Assert.AreEqual($"Message template is invalid.", exception?.Message);
        }

        [Test]
        [TestCase("{Foo}", "Foo")]
        [TestCase("Test {Bar} {Baz}", "Bar")]
        public void ThrowOnStartup_WhenMessageTemplateContainsInvalidPropertyName(string messageTemplate,
            string expectedPropertyName)
        {
            Action prepare = () => PrepareTestServer(
                "",
                new
                {
                    RequestLogging = new
                    {
                        MessageTemplate = messageTemplate
                    }
                },
                app =>
                {
                    app.UseRequestLogging();
                });

            var exception = Assert.Throws<InvalidOperationException>(() => prepare());
            Assert.AreEqual($"Encountered an unexpected property '{expectedPropertyName}'.",
                exception?.Message);
        }

        [Test]
        public void ThrowOnStartup_WhenLogLevelSelectorThrows()
        {
            var logLevelSelectorException = new Exception("Test message");
            Action prepare = () => PrepareTestServer(
                "",
                null,
                app =>
                {
                    app.UseRequestLogging(logLevelSelector: (statusCode) => (statusCode == StatusCodes.Status418ImATeapot) ?
                        throw logLevelSelectorException : LogLevel.Information);
                });

            var exception = Assert.Throws<InvalidOperationException>(() => prepare());
            Assert.AreEqual("Log level selector throws an exception on status code 418.", exception?.Message);
            Assert.AreSame(logLevelSelectorException, exception?.InnerException);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ThrowOnStartup_WhenIgnorePathIsNullOrEmpty(string path)
        {
            Action prepare = () => PrepareTestServer(
                "",
                new
                {
                    RequestLogging = new
                    {
                        IgnorePaths = new[] { path }
                    }
                },
                app =>
                {
                    app.UseRequestLogging();
                });

            var exception = Assert.Throws<InvalidOperationException>(() => prepare());
            Assert.AreEqual($"Ignore path cannot be null or empty.", exception?.Message);
        }

        [Test]
        [TestCase("{Foo}", "Foo")]
        [TestCase("Test {Bar} {Baz}", "Bar")]
        [TestCase("Test {Header} {Baz}", "Header")]
        [TestCase("Test {Claim} {Baz}", "Claim")]
        public void ThrowWhenMessageTemplateContainsInvalidPropertyName(string messageTemplate,
            string expectedPropertyName)
        {
            Action prepare = () => PrepareTestServer(
                "${message}",
                new
                {
                    RequestLogging = new
                    {
                        MessageTemplate = messageTemplate
                    }
                },
                app =>
                {
                    app.UseRequestLogging();
                });

            var exception = Assert.Throws<InvalidOperationException>(() => prepare());
            Assert.AreEqual($"Encountered an unexpected property '{expectedPropertyName}'.",
                exception?.Message);
        }

        [Test]
        [TestCase("test")]
        [TestCase("{foo")]
        [TestCase("foo}")]
        [TestCase("test {foo")]
        [TestCase("test foo}")]
        [TestCase("{foo test")]
        [TestCase("foo} test")]
        public async Task LogWhenTemplateHasNoProperties(string messageTemplate)
        {
            PrepareTestServer(
                "${message}",
                new
                {
                    RequestLogging = new
                    {
                        MessageTemplate = messageTemplate
                    }
                },
                app =>
                {
                    app.UseRequestLogging();
                });

            await Client.GetAsync("/");

            Assert.AreEqual(messageTemplate, Logs.Single());
        }

        [Test]
        public async Task LogWithDefaultTemplate()
        {
            PrepareTestServer(
                "[${level:uppercase=true}] ${message}",
                null,
                app =>
                {
                    app.UseRequestLogging();
                    app.UseRouting();
                    app.UseEndpoints(config =>
                    {
                        config.MapGet("/", () => Results.Ok());
                        config.MapPost("/", () => Results.NoContent());
                        config.MapGet("/bad-request", () => Results.BadRequest());
                        config.MapGet("/auth", () => Results.Unauthorized());
                        config.MapGet("/problem", () => Results.Problem());
                    });
                });

            await Client.GetAsync("/");
            await Client.PostAsync("/", null);
            await Client.GetAsync("/bad-request");
            await Client.GetAsync("/auth");
            await Client.GetAsync("/not-found?q=foo bar");
            await Client.PatchAsync("/auth", null);
            await Client.GetAsync("/problem");

            Assert.AreEqual(7, Logs.Count);
            StringAssert.IsMatch(@"^\[INFO\] GET \/ responded 200 in [0-9]+ ms\.$", Logs[0]);
            StringAssert.IsMatch(@"^\[INFO\] POST \/ responded 204 in [0-9]+ ms\.$", Logs[1]);
            StringAssert.IsMatch(@"^\[INFO\] GET \/bad-request responded 400 in [0-9]+ ms\.$", Logs[2]);
            StringAssert.IsMatch(@"^\[INFO\] GET \/auth responded 401 in [0-9]+ ms\.$", Logs[3]);
            StringAssert.IsMatch(@"^\[INFO\] GET \/not-found\?q=foo\%20bar responded 404 in [0-9]+ ms\.$", Logs[4]);
            StringAssert.IsMatch(@"^\[INFO\] PATCH \/auth responded 405 in [0-9]+ ms\.$", Logs[5]);
            StringAssert.IsMatch(@"^\[INFO\] GET \/problem responded 500 in [0-9]+ ms\.$", Logs[6]);
        }

        [Test]
        public async Task LogWithValidCustomMessageTemplate()
        {
            PrepareTestServer(
                "[${level:uppercase=true}] ${message}",
                new
                {
                    RequestLogging = new
                    {
                        MessageTemplate = "{Scheme} {Method} {Path} => {StatusCode}"
                    }
                },
                app =>
                {
                    app.UseRequestLogging();
                    app.UseRouting();
                    app.UseEndpoints(config =>
                    {
                        config.MapGet("/", () => Results.Ok());
                        config.MapPost("/", () => Results.NoContent());
                        config.MapGet("/bad-request", () => Results.BadRequest());
                        config.MapGet("/auth", () => Results.Unauthorized());
                        config.MapGet("/problem", () => Results.Problem());
                    });
                });

            await Client.GetAsync("/");
            await Client.PostAsync("/", null);
            await Client.GetAsync("/bad-request");
            await Client.GetAsync("/auth");
            await Client.GetAsync("/not-found?q=foo");
            await Client.PatchAsync("/auth", null);
            await Client.GetAsync("/problem");

            Assert.AreEqual(7, Logs.Count);
            Assert.AreEqual("[INFO] http GET / => 200", Logs[0]);
            Assert.AreEqual("[INFO] http POST / => 204", Logs[1]);
            Assert.AreEqual("[INFO] http GET /bad-request => 400", Logs[2]);
            Assert.AreEqual("[INFO] http GET /auth => 401", Logs[3]);
            Assert.AreEqual("[INFO] http GET /not-found => 404", Logs[4]);
            Assert.AreEqual("[INFO] http PATCH /auth => 405", Logs[5]);
            Assert.AreEqual("[INFO] http GET /problem => 500", Logs[6]);
        }

        [Test]
        public async Task LogWithLogLevelSelector()
        {
            PrepareTestServer(
                "${level:uppercase=true}",
                null,
                app =>
                {
                    app.Use(async (context, next) =>
                    {
                        try
                        {
                            await next(context);
                        }
                        catch (Exception)
                        {
                        }
                    });
                    app.UseRequestLogging((statusCode) => (statusCode < 400) ? LogLevel.Information : LogLevel.Error);
                    app.UseRouting();
                    app.UseEndpoints(config =>
                    {
                        config.MapGet("/", () => Results.Ok());
                        config.MapPost("/", () => Results.NoContent());
                        config.MapGet("/bad-request", () => Results.BadRequest());
                        config.MapGet("/auth", () => Results.Unauthorized());
                        config.MapGet("/problem", () => Results.Problem());
                        config.MapGet("/throw", (context) => throw new NotImplementedException());
                    });
                });

            await Client.GetAsync("/");
            await Client.PostAsync("/", null);
            await Client.GetAsync("/bad-request");
            await Client.GetAsync("/auth");
            await Client.GetAsync("/not-found?q=foo");
            await Client.PatchAsync("/auth", null);
            await Client.GetAsync("/problem");
            await Client.GetAsync("/throw");

            CollectionAssert.AreEqual(new[] { "INFO", "INFO", "ERROR", "ERROR", "ERROR", "ERROR", "ERROR", "ERROR" }, Logs);
        }

        [Test]
        public async Task LogOnExceptionDownThePipeline()
        {
            var exception = new Exception("Test message");
            Exception? caughtException = null;
            PrepareTestServer(
                "[${level:uppercase=true}] ${message}",
                null,
                app =>
                {
                    app.Use(async (context, next) =>
                    {
                        try
                        {
                            await next(context);
                        }
                        catch (Exception ex)
                        {
                            caughtException = ex;
                        }
                    });
                    app.UseRequestLogging();
                    app.UseRouting();
                    app.UseEndpoints(config =>
                    {
                        config.MapGet("/", (context) => throw exception);
                    });
                });

            await Client.GetAsync("/");

            StringAssert.IsMatch(@"^\[INFO\] GET \/ responded 500 in [0-9]+ ms\.$", Logs.Single());
            Assert.AreSame(exception, caughtException);
        }

        [Test]
        public async Task NotLogIgnoredPaths()
        {
            PrepareTestServer(
                "${message}",
                new
                {
                    RequestLogging = new
                    {
                        MessageTemplate = "{Path}",
                        IgnorePaths = new[]
                        {
                            "/ignore",
                            "*/ignore-wildcard-start",
                            "/ignore-wildcard-end*",
                            "*/ignore-wildcard-both-start-and-end*",
                            "/ignore-wildcard*middle"
                        }
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
                            return Results.Ok();
                        });
                    });
                });

            await Client.GetAsync("/");
            await Client.GetAsync("/ignore");
            await Client.GetAsync("/log/ignore");
            await Client.GetAsync("/ignore/log");
            await Client.GetAsync("/log/ignore/log");
            await Client.GetAsync("/ignore-wildcard-start");
            await Client.GetAsync("/log/ignore-wildcard-start");
            await Client.GetAsync("/ignore-wildcard-start/log");
            await Client.GetAsync("/log/ignore-wildcard-start/log");
            await Client.GetAsync("/ignore-wildcard-end");
            await Client.GetAsync("/log/ignore-wildcard-end");
            await Client.GetAsync("/ignore-wildcard-end/log");
            await Client.GetAsync("/log/ignore-wildcard-end/log");
            await Client.GetAsync("/ignore-wildcard-both-start-and-end");
            await Client.GetAsync("/log/ignore-wildcard-both-start-and-end");
            await Client.GetAsync("/ignore-wildcard-both-start-and-end/log");
            await Client.GetAsync("/log/ignore-wildcard-both-start-and-end/log");
            await Client.GetAsync("/ignore-wildcard/log/middle");
            await Client.GetAsync("/ignore-wildcard/log/middle/log");
            await Client.GetAsync("/log/ignore-wildcard/log/middle");
            await Client.GetAsync("/log/ignore-wildcard/log/middle/log");
            await Client.GetAsync("/ignore-wildcardmiddle");

            var expectedPaths = new[]
            {
                "/",
                "/log/ignore",
                "/ignore/log",
                "/log/ignore/log",
                "/ignore-wildcard-start/log",
                "/log/ignore-wildcard-start/log",
                "/log/ignore-wildcard-end",
                "/log/ignore-wildcard-end/log",
                "/ignore-wildcard/log/middle/log",
                "/log/ignore-wildcard/log/middle",
                "/log/ignore-wildcard/log/middle/log"
            };
            CollectionAssert.AreEqual(expectedPaths, Logs);
        }

        [Test]
        public async Task LogWithCustomConfigurationSection()
        {
            PrepareTestServer(
                "[${level:uppercase=true}] ${message}",
                new
                {
                    CustomSection = new
                    {
                        CustomRequestLogging = new
                        {
                            MessageTemplate = "{Path}",
                            IgnorePaths = new[] { "/ignore" }
                        }
                    }
                },
                app =>
                {
                    app.UseRequestLogging("CustomSection:CustomRequestLogging");
                    app.UseRouting();
                    app.UseEndpoints(config =>
                    {
                        config.MapGet("/", () =>
                        {
                            return Results.Ok();
                        });
                    });
                });

            await Client.GetAsync("/");

            Assert.AreEqual("[INFO] /", Logs.Single());
        }

        [Test]
        public async Task LogAllProperties()
        {
            PrepareTestServer(
                "[${level:uppercase=true}] ${message}",
                new
                {
                    RequestLogging = new
                    {
                        MessageTemplate = "{Method} {Path} {QueryString} {HeaderTest} {Protocol} {Scheme} " +
                            "{RemoteIpAddress} {ClaimId} {StatusCode} {ElapsedMs}"
                    }
                },
                app =>
                {
                    app.UseRequestLogging();
                    app.UseRouting();
                    app.UseEndpoints(config =>
                    {
                        config.MapGet("/", () => Results.Ok());
                    });
                });

            await Client.GetAsync("/");

            Assert.IsFalse(string.IsNullOrWhiteSpace(Logs.Single()));
        }
    }
}
