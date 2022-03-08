# SimpleRequestLogger

This package provides a small and customizable ASP.NET Core middleware for structured logging of requests using `Microsoft.Extensions.Logging`. The built-in request logging is a bit noisy and emits multiple events per request. With `SimpleRequestLogger` you can fit all the information you need in a single log entry:

```
// Plaintext
[21:51:46.5705 INFO] GET / responded 200 in 31 ms.

// JSON
{
    "Time": "21:51:46.5705",
    "Level": "INFO",
    "Message": "GET \/ responded 200 in 31 ms.",
    "Properties": {
        "Method": "GET",
        "Path": "\/",
        "QueryString": "",
        "StatusCode": 200,
        "ElapsedMs": 31
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

By default, `SimpleRequestLogger` logs all requests at information log level with message template `"{Method} {Path}{QueryString} responded {StatusCode} in {ElapsedMs} ms."`. To use the middleware you should only add a single line:

```csharp
app.UseSimpleRequestLogging();
```

### Custom configuration

It is possible to customize the message template, to change the log level based on status code and to disable logging for specific paths.

```csharp
app.UseSimpleRequestLogging(config =>
{
    config.MessageTemplate = "{Scheme} {Method} {Path} => {StatusCode}";
    config.LogLevelSelector = (statusCode) => 
        (statusCode < 400) ? LogLevel.Information : LogLevel.Error;
    config.IgnorePath("/health");
    config.IgnorePath("/static/*");
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

You might want to consider placing `SimpleRequestLogger` after request-heavy middlewares like `UseStaticFiles()` if those requests are not interesting for you (alternatively, you might ignore those via the configuration).

If `SimpleRequestLogger` catches an exception, the request will be logged with a status code 500 and the exception will be rethrown. If you have an error handling middleware that alters the response status code based on exception type, you should consider adding `SimpleRequestLogger` before it. 

### Exceptions

In normal circumstances, `SimpleRequestLogger` should not throw exceptions when handling requests. 

On startup, when the middleware is instantiated, the configuration is verified. `MessageTemplate` and the ignored paths are checked for validity. Additionally, it is also ensured that `LogLevelSelector` delegate will not throw for the standard response status codes. In case of a problem with the configuration, an `InvalidOperationException` is thrown.

## Performance

`SimpleRequestLogger` adds a negligible performance overhead to every request. 

### Benchmarks

The scenarios are run on a test host without other middleware.

|                       Method |     Mean |    Error |   StdDev |  Gen 0 | Allocated |
|----------------------------- |---------:|---------:|---------:|-------:|----------:|
|        NoSimpleRequestLogger | 12.01 μs | 0.197 μs | 0.175 μs | 2.3804 |      7 KB |
|                DefaultConfig | 15.93 μs | 0.317 μs | 0.530 μs | 2.5024 |      8 KB |
|   CustomConfigOneIgnoredPath | 16.46 μs | 0.309 μs | 0.624 μs | 2.5024 |      8 KB |
| CustomConfigFiveIgnoredPaths | 20.09 μs | 0.389 μs | 0.558 μs | 2.5330 |      8 KB |
