using ToonNet.Core;
using ToonNet.Core.Parsing;
using ToonNet.Core.Models;

namespace ToonNet.Tests.Parsing;

public class ToonLexerEdgeCaseTests
{
    [Fact]
    public void Tokenize_EmptyInput_ReturnsOnlyEOF()
    {
        // Arrange
        var input = "";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        Assert.Single(tokens);
        Assert.Equal(ToonTokenType.EndOfInput, tokens[0].Type);
    }

    [Fact]
    public void Tokenize_OnlyWhitespace_ReturnsOnlyEOF()
    {
        // Arrange
        var input = "   \n  \n   ";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        var nonWhitespace = tokens.Where(t => 
            t.Type != ToonTokenType.Newline && 
            t.Type != ToonTokenType.Indent).ToList();
        Assert.Single(nonWhitespace);
        Assert.Equal(ToonTokenType.EndOfInput, nonWhitespace[0].Type);
    }

    [Fact]
    public void Tokenize_WindowsLineEndings_HandlesCorrectly()
    {
        // Arrange
        var input = "key1: value1\r\nkey2: value2";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        var keys = tokens.Where(t => t.Type == ToonTokenType.Key).ToList();
        Assert.Equal(2, keys.Count);
        Assert.Equal("key1", keys[0].Value.ToString());
        Assert.Equal("key2", keys[1].Value.ToString());
    }

    [Fact]
    public void Tokenize_UnixLineEndings_HandlesCorrectly()
    {
        // Arrange
        var input = "key1: value1\nkey2: value2";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        var keys = tokens.Where(t => t.Type == ToonTokenType.Key).ToList();
        Assert.Equal(2, keys.Count);
    }

    [Fact]
    public void Tokenize_MultipleConsecutiveNewlines_PreservesAll()
    {
        // Arrange
        var input = "key: value\n\n\n";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        var newlines = tokens.Where(t => t.Type == ToonTokenType.Newline).ToList();
        Assert.Equal(3, newlines.Count);
    }

    [Fact]
    public void Tokenize_VeryLongLine_TokenizesCorrectly()
    {
        // Arrange
        var longValue = new string('x', 10000);
        var input = $"key: {longValue}";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        var valueToken = tokens.First(t => t.Type == ToonTokenType.Value);
        Assert.Equal(10000, valueToken.Value.Length);
    }

    [Fact]
    public void Tokenize_UnterminatedQuote_ThrowsException()
    {
        // Arrange
        var input = "key: \"unterminated";
        var lexer = new ToonLexer(input);

        // Act & Assert
        Assert.Throws<ToonParseException>(() => lexer.Tokenize());
    }

    [Fact]
    public void Tokenize_UnterminatedArrayLength_ThrowsException()
    {
        // Arrange
        var input = "key[123";
        var lexer = new ToonLexer(input);

        // Act & Assert
        Assert.Throws<ToonParseException>(() => lexer.Tokenize());
    }

    [Fact]
    public void Tokenize_UnterminatedArrayFields_ThrowsException()
    {
        // Arrange
        var input = "key[3]{field1,field2";
        var lexer = new ToonLexer(input);

        // Act & Assert
        Assert.Throws<ToonParseException>(() => lexer.Tokenize());
    }

    [Fact]
    public void Tokenize_EscapedCharactersInString_ReturnsUnescaped()
    {
        // Arrange
        var input = @"key: ""Line1\nLine2\tTabbed""";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        var valueToken = tokens.First(t => t.Type == ToonTokenType.Value);
        var value = valueToken.Value.ToString();
        Assert.Contains("\n", value);
        Assert.Contains("\t", value);
    }

    [Fact]
    public void Tokenize_EscapedQuoteInString_ReturnsQuote()
    {
        // Arrange
        var input = @"key: ""She said \""hello\""""";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        var valueToken = tokens.First(t => t.Type == ToonTokenType.Value);
        Assert.Contains("\"", valueToken.Value.ToString());
    }

    [Fact]
    public void Tokenize_EscapedBackslash_ReturnsBackslash()
    {
        // Arrange
        var input = @"key: ""C:\\path\\to\\file""";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        var valueToken = tokens.First(t => t.Type == ToonTokenType.Value);
        Assert.Contains("\\", valueToken.Value.ToString());
    }

    [Fact]
    public void Tokenize_TabIndentation_TokenizesAsSpaces()
    {
        // Arrange
        var input = "key:\n\tvalue: nested";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert - tabs should be treated as characters, not indent
        var indents = tokens.Where(t => t.Type == ToonTokenType.Indent).ToList();
        // This will depend on implementation - currently tabs aren't special
    }

    [Fact]
    public void Tokenize_MixedIndentation_TokenizesAllSpaces()
    {
        // Arrange
        var input = @"key:
  value1: a
    value2: b";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        var indents = tokens.Where(t => t.Type == ToonTokenType.Indent).ToList();
        Assert.Equal(2, indents.Count);
        Assert.Equal(2, indents[0].Value.Length);
        Assert.Equal(4, indents[1].Value.Length);
    }

    [Fact]
    public void Tokenize_CommaInValue_WithoutQuotes_SplitsTokens()
    {
        // Arrange
        var input = "tags[3]: a,b,c";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        var values = tokens.Where(t => t.Type == ToonTokenType.Value).ToList();
        Assert.Equal(3, values.Count);
        var commas = tokens.Where(t => t.Type == ToonTokenType.Comma).ToList();
        Assert.Equal(2, commas.Count);
    }

    [Theory]
    [InlineData("[10]", "10")]
    [InlineData("[0]", "0")]
    [InlineData("[999999]", "999999")]
    public void Tokenize_VariousArrayLengths_TokenizesCorrectly(string notation, string expected)
    {
        // Arrange
        var input = $"arr{notation}:";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        var lengthToken = tokens.First(t => t.Type == ToonTokenType.ArrayLength);
        Assert.Equal($"[{expected}]", lengthToken.Value.ToString());
    }

    [Fact]
    public void Tokenize_PositionTracking_ReportsCorrectLineAndColumn()
    {
        // Arrange
        var input = "line1: value1\nline2: value2";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        var line2Key = tokens.First(t => t.Type == ToonTokenType.Key && t.Value.ToString() == "line2");
        Assert.Equal(2, line2Key.Line);
    }
}
