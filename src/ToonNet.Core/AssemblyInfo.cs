using System.Runtime.CompilerServices;

/// <summary>
/// Assembly information and visibility declarations for ToonNet.Core.
/// </summary>
/// <remarks>
/// Internal types (e.g., ToonParser, ToonLexer) are exposed to:
/// - Extension packages for format conversion (JSON, YAML)
/// - ASP.NET Core integration packages
/// - Test projects for comprehensive testing
/// - Benchmark projects for performance measurement
/// </remarks>
[assembly: InternalsVisibleTo("ToonNet.Extensions.Json")]
[assembly: InternalsVisibleTo("ToonNet.Extensions.Yaml")]
[assembly: InternalsVisibleTo("ToonNet.AspNetCore")]
[assembly: InternalsVisibleTo("ToonNet.AspNetCore.Mvc")]
[assembly: InternalsVisibleTo("ToonNet.Tests")]
[assembly: InternalsVisibleTo("ToonNet.SourceGenerators")]
[assembly: InternalsVisibleTo("ToonNet.SourceGenerators.Tests")]
[assembly: InternalsVisibleTo("ToonNet.Benchmarks")]