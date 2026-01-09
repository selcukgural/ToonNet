using ToonNet.Core;
using ToonNet.Core.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace ToonNet.Tests;

/// <summary>
/// Tests for improved, developer-friendly error messages.
/// </summary>
public class ErrorMessageTests
{
    private readonly ITestOutputHelper _output;

    public ErrorMessageTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ParseException_WithSuggestion_ShowsHelpfulMessage()
    {
        // Arrange
        var ex = ToonParseException.Create(
            message: "Missing colon after key 'name'",
            line: 5,
            column: 10,
            actual: "Value",
            expected: "Colon (:)",
            suggestion: "Add a colon after the key. Example: name: value"
        );

        // Act
        var message = ex.ToString();

        // Assert
        Assert.Contains("Missing colon", message);
        Assert.Contains("Line 5, Column 10", message);
        Assert.Contains("💡 Suggestion", message);
        Assert.Contains("name: value", message);
        
        _output.WriteLine("=== PARSE EXCEPTION EXAMPLE ===");
        _output.WriteLine(message);
        _output.WriteLine("================================");
    }

    [Fact]
    public void EncodingException_WithContext_ShowsDetailedInfo()
    {
        // Arrange
        var ex = ToonEncodingException.Create(
            message: "Cannot encode circular reference",
            propertyPath: "User.Friends.User",
            problematicValue: "UserObject",
            suggestion: "Use [ToonIgnore] attribute or implement custom converter to handle circular references"
        );

        // Act
        var message = ex.ToString();

        // Assert
        Assert.Contains("circular reference", message);
        Assert.Contains("Property: User.Friends.User", message);
        Assert.Contains("💡 Suggestion", message);
        Assert.Contains("[ToonIgnore]", message);
        
        _output.WriteLine("=== ENCODING EXCEPTION EXAMPLE ===");
        _output.WriteLine(message);
        _output.WriteLine("===================================");
    }

    [Fact]
    public void SerializationException_WithTypeInfo_ShowsContext()
    {
        // Arrange
        var ex = ToonSerializationException.Create(
            message: "Cannot deserialize value to target type",
            targetType: typeof(int),
            propertyName: "Age",
            value: "not-a-number",
            suggestion: "Ensure the TOON value matches the expected type. Expected a number but got a string."
        );

        // Act
        var message = ex.ToString();

        // Assert
        Assert.Contains("Cannot deserialize", message);
        Assert.Contains("Target Type: Int32", message);
        Assert.Contains("Property: Age", message);
        Assert.Contains("Value: not-a-number", message);
        Assert.Contains("💡 Suggestion", message);
        
        _output.WriteLine("=== SERIALIZATION EXCEPTION EXAMPLE ===");
        _output.WriteLine(message);
        _output.WriteLine("========================================");
    }

    [Fact]
    public void ParseException_WithCodeSnippet_ShowsExample()
    {
        // Arrange
        var ex = ToonParseException.Create(
            message: "Missing value after colon",
            line: 3,
            column: 7,
            suggestion: "Provide a value after the colon",
            codeSnippet: "  key: value\n  OR\n  key:\n    nestedKey: nestedValue"
        );

        // Act
        var message = ex.ToString();

        // Assert
        Assert.Contains("Missing value", message);
        Assert.Contains("📝 Code:", message);
        Assert.Contains("key: value", message);
        Assert.Contains("nestedKey: nestedValue", message);
        
        _output.WriteLine("=== EXCEPTION WITH CODE SNIPPET ===");
        _output.WriteLine(message);
        _output.WriteLine("====================================");
    }

    [Fact]
    public void RealWorldExample_InvalidToonFormat_ShowsHelpfulError()
    {
        // Arrange - Missing colon is actually valid in TOON (means nested object)
        var invalidToon = @"name: ""Alice
age: 30";  // Unterminated string

        var parser = new ToonNet.Core.Parsing.ToonParser();

        // Act & Assert
        var ex = Assert.Throws<ToonParseException>(() => parser.Parse(invalidToon));
        
        _output.WriteLine("=== REAL WORLD PARSING ERROR ===");
        _output.WriteLine("Input:");
        _output.WriteLine(invalidToon);
        _output.WriteLine("");
        _output.WriteLine("Error:");
        _output.WriteLine(ex.ToString());
        _output.WriteLine("=================================");
        
        Assert.NotNull(ex);
        Assert.Contains("string", ex.Message.ToLower());
    }

    [Fact]
    public void ExceptionProperties_AreAccessible()
    {
        // Arrange
        var ex = ToonParseException.Create(
            message: "Test error",
            line: 10,
            column: 5,
            actual: "Something",
            expected: "SomethingElse"
        );

        // Assert
        Assert.Equal(10, ex.Line);
        Assert.Equal(5, ex.Column);
        Assert.Equal("Something", ex.ActualToken);
        Assert.Equal("SomethingElse", ex.ExpectedToken);
    }

    [Fact]
    public void SerializationException_Properties_AreAccessible()
    {
        // Arrange
        var ex = ToonSerializationException.Create(
            message: "Test",
            targetType: typeof(string),
            propertyName: "Name",
            value: 123
        );

        // Assert
        Assert.Equal(typeof(string), ex.TargetType);
        Assert.Equal("Name", ex.PropertyName);
        Assert.Equal(123, ex.Value);
    }
}
