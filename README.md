# SimpleRequestLogger

[![nuget](https://img.shields.io/nuget/v/SimpleRequestLogger)](https://www.nuget.org/packages/SimpleRequestLogger)
[![downloads](https://img.shields.io/nuget/dt/SimpleRequestLogger?color=blue)](https://www.nuget.org/stats/packages/SimpleRequestLogger?groupby=Version)
[![cd](https://img.shields.io/github/actions/workflow/status/yavorfingarov/SimpleRequestLogger/cd.yml?branch=master&label=cd)](https://github.com/yavorfingarov/SimpleRequestLogger/actions/workflows/cd.yml?query=branch%3Amaster)
[![codeql](https://img.shields.io/github/actions/workflow/status/yavorfingarov/SimpleRequestLogger/codeql.yml?branch=master&label=codeql)](https://github.com/yavorfingarov/SimpleRequestLogger/actions/workflows/codeql.yml?query=branch%3Amaster)
[![loc](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/yavorfingarov/ee725e01afca4342ff8ea785553d05d2/raw/lines-of-code.json)](https://github.com/yavorfingarov/SimpleRequestLogger/actions/workflows/cd.yml?query=branch%3Amaster)
[![test coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/yavorfingarov/ee725e01afca4342ff8ea785553d05d2/raw/test-coverage.json)](https://github.com/yavorfingarov/SimpleRequestLogger/actions/workflows/cd.yml?query=branch%3Amaster)
[![mutation score](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/yavorfingarov/ee725e01afca4342ff8ea785553d05d2/raw/mutation-score.json)](https://github.com/yavorfingarov/SimpleRequestLogger/actions/workflows/cd.yml?query=branch%3Amaster)

SimpleRequestLogger is a small and customizable ASP.NET Core middleware for structured logging
of requests using `Microsoft.Extensions.Logging`. The built-in request logging is a bit noisy
and emits multiple events per request. With SimpleRequestLogger you can fit all the information
you need in a single log entry:

```
// Plaintext
21:51:46.5705 [INF] GET / responded 200 in 31 ms.

// JSON
{
    "Time": "21:51:46.5705",
    "Level": "INF",
    "Message": "GET / responded 200 in 31 ms.",
    "Properties": {
        "Method": "GET",
        "Path": "/",
        "QueryString": "",
        "StatusCode": 200,
        "ElapsedMs": 31
    }
}
```

## Getting started

Install the [NuGet package](https://www.nuget.org/packages/SimpleRequestLogger) and
add the middleware at the beginning of your request pipeline:

```csharp
app.UseRequestLogging();
```

## Configuration

By default, SimpleRequestLogger logs all requests at information log level with message
template `"{Method} {Path}{QueryString} responded {StatusCode} in {ElapsedMs} ms."`.

It is possible to customize the message template, to change the log level based on
status code and to disable logging for specific paths. SimpleRequestLogger uses
`Microsoft.Extensions.Configuration` and would by default expect a section `RequestLogging`.

```json
"RequestLogging": {
    "MessageTemplate": "{Scheme} {Method} {Path} => {StatusCode}",
    "IgnorePaths": [ "/health", "/static/*" ]
}
```

It is also possible to pass a custom configuration section:

```csharp
app.UseRequestLogging("YourCustomSection:CustomSubsectionRequestLogging");
```

To change the log level based on status code, you should pass a delegate to the middleware:

```csharp
app.UseRequestLogging(statusCode => (statusCode < 400) ? LogLevel.Information : LogLevel.Error);
```

You might as well have both custom configuration section and a log level selector.

```csharp
app.UseRequestLogging("YourCustomSection:CustomSubsectionRequestLoging",
    statusCode => (statusCode < 400) ? LogLevel.Information : LogLevel.Error);
```

### Properties

- Method
- Path
- QueryString
- Header* - A Pascal case field name will be transformed to Kebab case. Example: HeaderFooBar => foo-bar
- Protocol
- Scheme
- RemoteIpAddress - If you log this property you might want to consider adding `UseForwardedHeaders()` to your pipeline.
- Claim* - A Pascal case claim type will be transformed to Kebab case. Example: ClaimFooBar => foo-bar
- StatusCode
- ElapsedMs

## Pipeline placement

You might want to consider placing SimpleRequestLogger after request-heavy middleware like `UseStaticFiles()`
if those requests are not interesting for you. Alternatively, you might ignore those via the configuration.

If SimpleRequestLogger catches an exception, the request will be logged with a status code 500
and the exception will be rethrown. If you have an error handling middleware that alters the response
status code based on exception type, you should consider adding SimpleRequestLogger before it.

## Self-checks

On startup, when the middleware is instantiated, the configuration is verified. `MessageTemplate`
and `IgnorePaths` are checked for validity. Additionally, it is also ensured that the log level selector
delegate will not throw for the standard response status codes. In case of a problem with the configuration,
an `InvalidOperationException` is thrown.

## Additional resources

* [Changelog](https://github.com/yavorfingarov/SimpleRequestLogger/blob/master/CHANGELOG.md)

* [License](https://github.com/yavorfingarov/SimpleRequestLogger/blob/master/LICENSE)

## Help and support

For bug reports and feature requests, please use the [issue tracker](https://github.com/yavorfingarov/SimpleRequestLogger/issues).
For questions, ideas and other topics, please check the [discussions](https://github.com/yavorfingarov/SimpleRequestLogger/discussions).
