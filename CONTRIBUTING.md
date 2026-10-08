# Contributing to ToonNet

Thank you for your interest in contributing to ToonNet! 🎉

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Making Changes](#making-changes)
- [Testing](#testing)
- [Submitting Changes](#submitting-changes)
- [Coding Standards](#coding-standards)

## Code of Conduct

This project follows a Code of Conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to [selcukgural@gmail.com](mailto:selcukgural@gmail.com).

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally
3. **Create a branch** for your changes
4. **Make your changes** with tests
5. **Submit a pull request**

## Development Setup

### Prerequisites

- .NET 10 SDK (the solution file is `ToonNet.slnx`, which needs SDK 9.0.200 or later, and
  `ToonNet.SourceGenerators.Tests` also targets `net10.0`)
- .NET 8 runtime (the libraries and most test projects target `net8.0`)
- Git
- Node.js 22 (only to build the documentation site in `website/`)
- IDE (Visual Studio, Rider, or VS Code)

### Setup

```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/ToonNet.git
cd ToonNet

# Restore dependencies
dotnet restore ToonNet.slnx

# Build the solution
dotnet build ToonNet.slnx

# Run tests
dotnet test ToonNet.slnx
```

### Repository Layout

```
src/                  # Packages: ToonNet.Core, ToonNet.Extensions.Json, ToonNet.Extensions.Yaml,
                      #           ToonNet.AspNetCore, ToonNet.AspNetCore.Mvc, ToonNet.SourceGenerators
tests/                # ToonNet.Tests, ToonNet.SourceGenerators.Tests
benchmark/            # ToonNet.Benchmarks (BenchmarkDotNet)
demo/                 # ToonNet.Demo (sample console app)
docs/                 # API guide and spec compliance notes
website/              # Docusaurus documentation site
Directory.Build.props # Shared package metadata
```

## Making Changes

### Branch Naming

Use descriptive branch names:
- `feature/add-xml-support` - New features
- `fix/null-reference-bug` - Bug fixes
- `docs/update-readme` - Documentation
- `perf/optimize-serialization` - Performance improvements

### Commit Messages

Follow conventional commit format:
```
type(scope): subject

body (optional)

footer (optional)
```

Examples:
- `feat(core): add support for TimeOnly type`
- `fix(json): handle null values in arrays`
- `docs(readme): update installation instructions`
- `perf(serialization): optimize string allocation`

## Testing

### Running Tests

```bash
# Run all tests
dotnet test ToonNet.slnx

# Run tests for a specific project
dotnet test tests/ToonNet.Tests/
dotnet test tests/ToonNet.SourceGenerators.Tests/

# Run tests with coverage (coverlet.collector)
dotnet test tests/ToonNet.Tests/ --collect:"XPlat Code Coverage"
```

The official TOON spec v3.3.2 conformance fixtures run as part of `ToonNet.Tests`. If your change makes a case in
`tests/ToonNet.Tests/SpecCompliance/KnownNonConformance.txt` pass, remove it from that file (the test fails otherwise).

### Writing Tests

- **Unit tests** for all new features
- **Integration tests** for complex scenarios
- **Benchmarks** (in `benchmark/ToonNet.Benchmarks`) for performance-critical changes
- Cover both success and error paths

Example:
```csharp
[Fact]
public void Serialize_WithValidObject_ReturnsExpectedToon()
{
    // Arrange
    var obj = new TestModel { Name = "Test", Age = 30 };
    
    // Act
    var result = ToonSerializer.Serialize(obj);
    
    // Assert
    Assert.Contains("Name: Test", result);
    Assert.Contains("Age: 30", result);
}
```

## Submitting Changes

### Pull Request Process

1. **Update documentation** if needed
2. **Add tests** for new features
3. **Ensure all tests pass**
4. **Update CHANGELOG.md** with your changes
5. **Submit PR** with clear description

### PR Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests added/updated
- [ ] All tests passing
- [ ] Manual testing completed

## Checklist
- [ ] Code follows project style guidelines
- [ ] Documentation updated
- [ ] CHANGELOG.md updated
```

## Coding Standards

### C# Style

- **Follow .NET conventions** (the repository has no `.editorconfig`; match the style of the surrounding code)
- **Library projects target `net8.0`** (C# 12); `ToonNet.SourceGenerators` targets `netstandard2.0` and must not
  reference `ToonNet.Core`
- **Nullable reference types are enabled** in every project
- **Use expression-bodied members** where appropriate
- **Prefer `var` for obvious types**

### Documentation

- **XML comments** for public APIs
- **Code comments** for complex logic
- **README updates** for new features
- **API guide updates** when needed

### Example

```csharp
/// <summary>
/// Serializes an object to TOON format.
/// </summary>
/// <typeparam name="T">The type of object to serialize.</typeparam>
/// <param name="value">The value to serialize.</param>
/// <param name="options">Serialization options.</param>
/// <returns>TOON format string.</returns>
/// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
public static string Serialize<T>(T? value, ToonSerializerOptions? options = null)
{
    return Serialize(value, typeof(T), options);
}
```

### Performance

- **Avoid allocations** in hot paths
- **Use `Span<T>` and `Memory<T>`** where appropriate
- **Benchmark critical code** paths
- **Profile before optimizing**

## Continuous Integration

Every push to `master` and every pull request targeting `master` runs [`ci.yml`](.github/workflows/ci.yml): it builds
the solution in Release, runs all test projects, packs every package under `src/` (validation only) and builds the
documentation site. Pushes to `master` that change `website/` also deploy the site with
[`docs.yml`](.github/workflows/docs.yml). Please make sure
`dotnet test ToonNet.slnx` passes locally before opening a pull request.

## Releasing (maintainers)

Publishing to NuGet is manual:

1. Bump `<Version>` (and `<PackageReleaseNotes>`) in the `.csproj` of every package that changed, and add the
   release to `CHANGELOG.md`.
2. Merge to `master` and wait for CI to pass.
3. Run the **Publish to NuGet** workflow from the Actions tab. Use **dry-run** first to see which packages would
   be published.

The workflow runs CI again, then packs and pushes only packages whose version is not yet on NuGet.org, and creates
a GitHub release `v<ToonNet.Core version>` with the packages attached. It uses the `NUGET_API_KEY` secret of the
`nuget` environment; protect that environment with required reviewers.

## Questions?

- **Issues:** [GitHub Issues](https://github.com/selcukgural/ToonNet/issues)
- **Discussions:** [GitHub Discussions](https://github.com/selcukgural/ToonNet/discussions)
- **Email:** selcukgural@gmail.com

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
