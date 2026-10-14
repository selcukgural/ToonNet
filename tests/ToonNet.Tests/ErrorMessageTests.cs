using ToonNet.Core;
using ToonNet.Core.Parsing;
using Xunit.Abstractions;

namespace ToonNet.Tests;

/// <summary>
///     Tests for improved, developer-friendly error messages.
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
        var ex = ToonParseException.Create("Missing colon after key 'name'", 5, 10, "Add a colon after the key. Example: name: value");

        // Act
        var message = ex.ToString();

        // Assert
        Assert.Contains("Missing colon", message);
        Assert.Contains("Missing colon after key 'name' (line 5, column 10)", ex.Message);
        Assert.Contains("Suggestion: Add a colon", message);
        Assert.Contains("name: value", message);

        _output.WriteLine("=== PARSE EXCEPTION EXAMPLE ===");
        _output.WriteLine(message);
        _output.WriteLine("================================");
    }

    [Fact]
    public void EncodingException_WithContext_ShowsDetailedInfo()
    {
        // Arrange
        var ex = ToonEncodingException.Create("Cannot encode circular reference", "User.Friends.User", "UserObject",
                                              "Use [ToonIgnore] attribute or implement custom converter to handle circular references");

        // Act
        var message = ex.ToString();

        // Assert
        Assert.Contains("circular reference", message);
        Assert.Equal("Cannot encode circular reference (property: User.Friends.User, value type: String)", ex.Message);
        Assert.Contains("Suggestion: Use [ToonIgnore]", message);
        Assert.Contains("[ToonIgnore]", message);

        _output.WriteLine("=== ENCODING EXCEPTION EXAMPLE ===");
        _output.WriteLine(message);
        _output.WriteLine("===================================");
    }

    [Fact]
    public void SerializationException_WithTypeInfo_ShowsContext()
    {
        // Arrange
        var ex = ToonSerializationException.Create("Cannot deserialize value to target type", typeof(int), "Age", "not-a-number",
                                                   "Ensure the TOON value matches the expected type. Expected a number but got a string.");

        // Act
        var message = ex.ToString();

        // Assert
        Assert.Contains("Cannot deserialize", message);
        Assert.Equal("Cannot deserialize value to target type (target type: Int32, property: Age, value type: String)", ex.Message);
        Assert.DoesNotContain("not-a-number", message); // values may be personal data; they stay out of messages
        Assert.Equal("not-a-number", ex.Value);
        Assert.Contains("Suggestion: Ensure", message);

        _output.WriteLine("=== SERIALIZATION EXCEPTION EXAMPLE ===");
        _output.WriteLine(message);
        _output.WriteLine("========================================");
    }

    [Fact]
    public void ParseException_WithCodeSnippet_ShowsExample()
    {
        // Arrange
        var ex = ToonParseException.Create("Missing value after colon", 3, 7, suggestion: "Provide a value after the colon",
                                           codeSnippet: "  key: value\n  OR\n  key:\n    nestedKey: nestedValue");

        // Act
        var message = ex.ToString();

        // Assert
        Assert.Contains("Missing value", message);
        Assert.Contains("Code:\n  key: value", message);
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
age: 30"; // Unterminated string

        var parser = new ToonParser();

        // Act & Assert
        var ex = Assert.Throws<ToonParseException>(() => parser.Parse(invalidToon));

        _output.WriteLine("=== REAL WORLD PARSING ERROR ===");
        _output.WriteLine("Input:");
        _output.WriteLine(invalidToon);
        _output.WriteLine("");
        _output.WriteLine("Error:");
        _output.WriteLine(ex.ToString());
        _output.WriteLine("=================================");

        Assert.Equal("Unterminated string", ex.Reason);
        Assert.Equal((1, 7), (ex.Line, ex.Column));
    }

    [Fact]
    public void ExceptionProperties_AreAccessible()
    {
        // Arrange
        var ex = ToonParseException.Create("Test error", 10, 5, suggestion: "Fix it");

        // Assert
        Assert.Equal(10, ex.Line);
        Assert.Equal(5, ex.Column);
        Assert.Equal("Fix it", ex.Suggestion);
    }

    [Fact]
    public void SerializationException_Properties_AreAccessible()
    {
        // Arrange
        var ex = ToonSerializationException.Create("Test", typeof(string), "Name", 123);

        // Assert
        Assert.Equal(typeof(string), ex.TargetType);
        Assert.Equal("Name", ex.Path);
        Assert.Equal(123, ex.Value);
    }

    [Theory]
    [InlineData("a: 1\na: 2", "Duplicate key 'a' (line 2, column 1)")]
    [InlineData("Age: x", "The string is not a number (path: $.Age, target type: Int32)")]
    public void Messages_AreOneLineWithoutSymbols(string toon, string expected)
    {
        var ex = Assert.ThrowsAny<ToonException>(() => ToonNet.Core.Serialization.ToonSerializer.Deserialize<AgeHolder>(toon));

        Assert.Equal(expected, ex.Message);
        Assert.DoesNotContain('\n', ex.Message);
    }

    [Theory]
    [InlineData("Age: jane.doe@example.com", typeof(AgeHolder))]
    [InlineData("Age: 98765432109876", typeof(AgeHolder))]
    [InlineData("Level: jane.doe@example.com", typeof(LevelHolder))]
    [InlineData("Flag: jane.doe@example.com", typeof(PrimitiveHolder))]
    [InlineData("When: jane.doe@example.com", typeof(PrimitiveHolder))]
    [InlineData("Offset: jane.doe@example.com", typeof(PrimitiveHolder))]
    [InlineData("Day: jane.doe@example.com", typeof(PrimitiveHolder))]
    [InlineData("At: jane.doe@example.com", typeof(PrimitiveHolder))]
    [InlineData("Wait: jane.doe@example.com", typeof(PrimitiveHolder))]
    [InlineData("Id: jane.doe@example.com", typeof(PrimitiveHolder))]
    [InlineData("Big: jane.doe@example.com", typeof(PrimitiveHolder))]
    public void DeserializationErrors_DoNotQuoteTheValue(string toon, Type type)
    {
        var ex = Assert.Throws<ToonSerializationException>(() => ToonNet.Core.Serialization.ToonSerializer.Deserialize(toon, type));
        var value = toon[(toon.IndexOf(": ", StringComparison.Ordinal) + 2)..];

        // Not in the message, and not in an inner exception either: loggers write ex.ToString()
        Assert.DoesNotContain(value, ex.Message);
        Assert.DoesNotContain(value, ex.ToString());
        Assert.Contains("$.", ex.Message);
        Assert.Equal(value, ex.Value?.ToString());
    }

    public sealed class AgeHolder
    {
        public int Age { get; set; }
    }

    public sealed class LevelHolder
    {
        public DayOfWeek Level { get; set; }
    }

    public sealed class PrimitiveHolder
    {
        public bool Flag { get; set; }
        public DateTime When { get; set; }
        public DateTimeOffset Offset { get; set; }
        public DateOnly Day { get; set; }
        public TimeOnly At { get; set; }
        public TimeSpan Wait { get; set; }
        public Guid Id { get; set; }
        public Int128 Big { get; set; }
    }
}
