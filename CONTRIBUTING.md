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

This project follows a Code of Conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to [selcukgural@example.com](mailto:selcukgural@example.com).

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally
3. **Create a branch** for your changes
4. **Make your changes** with tests
5. **Submit a pull request**

## Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- Git
- IDE (Visual Studio, Rider, or VS Code)

### Setup

```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/ToonNet.git
cd ToonNet

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
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
dotnet test

# Run tests for a specific project
dotnet test tests/ToonNet.Tests/

# Run tests with coverage
dotnet test /p:CollectCoverage=true
```

### Writing Tests

- **Unit tests** for all new features
- **Integration tests** for complex scenarios
- **Performance tests** for critical paths
- Aim for **80%+ code coverage**

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

- **Follow .NET conventions**
- **Use latest C# features** (C# 12+)
- **Enable nullable reference types**
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
/// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
public static string Serialize<T>(T value, ToonSerializerOptions? options = null)
{
    ArgumentNullException.ThrowIfNull(value);
    // Implementation...
}
```

### Performance

- **Avoid allocations** in hot paths
- **Use `Span<T>` and `Memory<T>`** where appropriate
- **Benchmark critical code** paths
- **Profile before optimizing**

## Questions?

- **Issues:** [GitHub Issues](https://github.com/selcukgural/ToonNet/issues)
- **Discussions:** [GitHub Discussions](https://github.com/selcukgural/ToonNet/discussions)
- **Email:** selcukgural@example.com

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
