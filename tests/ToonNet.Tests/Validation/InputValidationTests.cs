using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Tests.Validation;

/// <summary>
///     Tests for input validation in public APIs.
/// </summary>
public class InputValidationTests
{
    #region ToonParser Input Validation

    [Fact]
    public void ToonParser_Parse_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        var parser = new ToonParser();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => parser.Parse((string)null!));
        Assert.Equal("text", ex.ParamName);
    }

    [Fact]
    public void ToonParser_Parse_EmptyString_DoesNotThrow()
    {
        // Arrange
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse("");

        // Assert: an empty document is an empty object
        Assert.Empty(doc.AsObject().Properties);
    }

    #endregion

    #region ToonEncoder Input Validation

    [Fact]
    public void ToonEncoder_Encode_NullDocument_ThrowsArgumentNullException()
    {
        // Arrange
        var encoder = new ToonEncoder();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => encoder.Encode(null!));
        Assert.Equal("document", ex.ParamName);
    }

    [Fact]
    public void ToonDocument_Constructor_NullRoot_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new ToonDocument(null!));
        Assert.Equal("root", ex.ParamName);
    }

    [Fact]
    public void ToonEncoder_Encode_ValidDocument_Succeeds()
    {
        // Arrange
        var encoder = new ToonEncoder();
        var document = new ToonDocument(new ToonObject
        {
            ["key"] = new ToonString("value")
        });

        // Act
        var result = encoder.Encode(document);

        // Assert
        Assert.Equal("key: value", result);
    }

    #endregion

    #region ToonOptions Constructor Validation

    [Fact]
    public void ToonParser_Constructor_NullOptions_UsesDefault()
    {
        // Act
        var parser = new ToonParser(null);

        // Assert: the defaults, such as strict mode, apply
        Assert.Equal("value", parser.Parse("key: value").Root["key"]!.GetValue<string>());
        Assert.Throws<ToonParseException>(() => parser.Parse("a: 1\na: 2"));
    }

    [Fact]
    public void ToonEncoder_Constructor_NullOptions_UsesDefault()
    {
        // Act
        var encoder = new ToonEncoder(null);

        // Assert: the defaults (two spaces, commas) apply
        var doc = new ToonDocument(new ToonObject { ["a"] = new ToonObject { ["b"] = new ToonArray([1, 2]) } });
        Assert.Equal("a:\n  b[2]: 1,2", encoder.Encode(doc));
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void ToonParser_Parse_WhitespaceOnly_DoesNotThrow()
    {
        // Arrange
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse("   \n   \n   ");

        // Assert: blank lines only are an empty document
        Assert.Empty(doc.AsObject().Properties);
    }

    [Fact]
    public void ToonOptions_InvalidIndentSize_ThrowsWhenSet()
    {
        // Arrange - Create options with invalid values will throw during property set
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var options = new ToonOptions { IndentSize = 0 };
        });
    }

    #endregion
}
