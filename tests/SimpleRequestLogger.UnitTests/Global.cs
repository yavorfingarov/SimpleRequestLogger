global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Http;
global using Microsoft.Extensions.Logging;
global using NUnit.Framework;

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
    Justification = "All instances are disposed in Teardown.")]
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names can contain underscores.")]
[assembly: SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Plain Exception is used in tests.")]
