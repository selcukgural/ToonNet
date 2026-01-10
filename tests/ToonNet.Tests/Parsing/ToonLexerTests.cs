using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Tests.Parsing;

public class ToonLexerTests
{
    [Fact]
    public void Tokenize_SimpleKeyValue_ReturnsCorrectTokens()
    {
        // Arrange
        var input = "name: Alice";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        Assert.Equal(4, tokens.Count); // key, colon, value, EOF
        Assert.Equal(ToonTokenType.Key, tokens[0].Type);
        Assert.Equal("name", tokens[0].Value.ToString());
        Assert.Equal(ToonTokenType.Colon, tokens[1].Type);
        Assert.Equal(ToonTokenType.Value, tokens[2].Type);
        Assert.Equal("Alice", tokens[2].Value.ToString());
    }

    [Fact]
    public void Tokenize_MultipleLines_ReturnsCorrectTokens()
    {
        // Arrange
        var input = "id: 1\nname: Alice";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        var keyTokens = tokens.Where(t => t.Type == ToonTokenType.Key).ToList();
        Assert.Equal(2, keyTokens.Count);
        Assert.Equal("id", keyTokens[0].Value.ToString());
        Assert.Equal("name", keyTokens[1].Value.ToString());
    }

    [Fact]
    public void Tokenize_ArrayNotation_ReturnsCorrectTokens()
    {
        // Arrange
        var input = "tags[3]: a,b,c";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        Assert.Contains(tokens, t => t.Type == ToonTokenType.Key && t.Value.ToString() == "tags");
        Assert.Contains(tokens, t => t.Type == ToonTokenType.ArrayLength && t.Value.ToString() == "[3]");
        Assert.Contains(tokens, t => t.Type == ToonTokenType.Colon);
    }

    [Fact]
    public void Tokenize_TabularArray_ReturnsCorrectTokens()
    {
        // Arrange
        var input = "users[2]{id,name}:";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        Assert.Contains(tokens, t => t.Type == ToonTokenType.Key && t.Value.ToString() == "users");
        Assert.Contains(tokens, t => t.Type == ToonTokenType.ArrayLength && t.Value.ToString() == "[2]");
        Assert.Contains(tokens, t => t.Type == ToonTokenType.ArrayFields && t.Value.ToString() == "{id,name}");
    }

    [Fact]
    public void Tokenize_QuotedString_ReturnsCorrectValue()
    {
        // Arrange
        var input = "name: \"Alice Smith\"";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        var valueToken = tokens.First(t => t.Type == ToonTokenType.QuotedString);
        // Lexer preserves the unescaped content without quotes
        Assert.Equal("Alice Smith", valueToken.Value.ToString());
    }

    [Fact]
    public void Tokenize_IndentedContent_ReturnsIndentTokens()
    {
        // Arrange
        var input = "user:\n  name: Alice";
        var lexer = new ToonLexer(input);

        // Act
        var tokens = lexer.Tokenize();

        // Assert
        Assert.Contains(tokens, t => t is { Type: ToonTokenType.Indent, Value.Length: 2 });
    }
}