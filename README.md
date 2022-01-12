# SimpleRequestLogger

This package provides a small and customizable ASP.NET Core middleware for structured logging of requests using `Microsoft.Extensions.Logging`. The built-in request logging is a bit noisy and emits multiple events per request. With this middleware you can fit all the information you need in a single log entry:

```
// Plaintext
[21:51:46.5705 INFO] GET / responded 200 in 1.210 ms

// JSON
{
    "Time": "21:51:46.5705",
    "Level": "INFO",
    "Message": "GET \/ responded 200 in 1.210 ms",
    "Properties": {
        "Method": "GET",
        "Path": "\/",
        "QueryString": "",
        "StatusCode": 200,
        "ElapsedMs": 1.210
    }
}
```

## Installation

You can install the [NuGet package](https://www.nuget.org/packages/SimpleRequestLogger) via the NuGet Package Manager inside Visual Studio or via the console:

```
// Package Manager Console
Install-Package SimpleRequestLogger

// .NET Core CLI
dotnet add package SimpleRequestLogger
```

## Usage

The only thing you should do is simply add the middleware to the request pipeline.

### Default configuration

By default, all requests would be logged at the information log level with the message template `"{Method} {Path}{QueryString} responded {StatusCode} in {ElapsedMs} ms"`. To use the middleware you should only add a single line:

```csharp
app.UseSimpleRequestLogging();
```

### Custom configuration

It is possible to customize the message template and/or change the log level based on status code.

```csharp
app.UseSimpleRequestLogging(config =>
{
    config.MessageTemplate = "{Scheme} {Method} {Path} => {StatusCode}";
    config.LogLevelSelector = (statusCode) => (statusCode < 400) ? 
        LogLevel.Information : LogLevel.Error;
});
```

#### Properties

- Method
- Path
- QueryString
- Protocol
- Scheme
- UserAgent
- StatusCode
- ElapsedMs

### Pipeline placement

You might want to consider placing `SimpleRequestLogger` after request-heavy middlewares like `UseStaticFiles()` if those requests are not interesting for you.

If `SimpleRequestLogger` catches an exception, the request will be logged with a status code 500 and the exception will be rethrown. If you have an error handling middleware that alters the status code based on exception type, you should consider adding `SimpleRequestLogger` before it. 

### Exceptions

In normal circumstances, `SimpleRequestLogger` should not throw exceptions. 

On startup, when the middleware is instantiated, the configuration is verified. `MessageTemplate` is checked for validity. Additionally, it is also ensured that `LogLevelSelector` delegate will not throw for the standard response status codes. 

In case of a problem with the configuration, an `InvalidOperationException` is thrown.
