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
        Assert.Equal("input", ex.ParamName);
    }

    [Fact]
    public void ToonParser_Parse_EmptyString_DoesNotThrow()
    {
        // Arrange
        var parser = new ToonParser();

        // Act
        var doc = parser.Parse("");

        // Assert
        Assert.NotNull(doc);
    }

    #endregion

    #region ToonLexer Input Validation

    [Fact]
    public void ToonLexer_Constructor_NullString_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new ToonLexer((string)null!));
        Assert.Equal("input", ex.ParamName);
    }

    [Fact]
    public void ToonLexer_Constructor_EmptyString_DoesNotThrow()
    {
        // Act
        var lexer = new ToonLexer("");

        // Assert
        Assert.NotNull(lexer);
    }

    [Fact]
    public void ToonLexer_Constructor_EmptyMemory_DoesNotThrow()
    {
        // Act
        var lexer = new ToonLexer(ReadOnlyMemory<char>.Empty);

        // Assert
        Assert.NotNull(lexer);
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
    public void ToonEncoder_Encode_DocumentWithNullRoot_ThrowsArgumentNullException()
    {
        // Arrange
        var encoder = new ToonEncoder();
        var document = new ToonDocument(null!);

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => encoder.Encode(document));
        Assert.Contains("Document root cannot be null", ex.Message);
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
        Assert.NotNull(result);
        Assert.Contains("key", result);
    }

    #endregion

    #region ToonOptions Constructor Validation

    [Fact]
    public void ToonParser_Constructor_NullOptions_UsesDefault()
    {
        // Act
        var parser = new ToonParser(null);

        // Assert - Should not throw, parser should work with defaults
        var doc = parser.Parse("key: value");
        Assert.NotNull(doc);
    }

    [Fact]
    public void ToonEncoder_Constructor_NullOptions_UsesDefault()
    {
        // Act
        var encoder = new ToonEncoder(null);

        // Assert - Should not throw, encoder should work with defaults
        var doc = new ToonDocument(new ToonObject());
        var result = encoder.Encode(doc);
        Assert.NotNull(result);
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

        // Assert
        Assert.NotNull(doc);
    }

    [Fact]
    public void ToonEncoder_WithInvalidOptions_ThrowsDuringConstruction()
    {
        // Arrange - Create options with invalid values will throw during property set
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var options = new ToonOptions { IndentSize = 1 };
        });
    }

    #endregion
}
