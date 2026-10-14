using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;
using Text = System.Text;

namespace ToonNet.Tests.Documents;

public class ToonDocumentReaderTests
{
    private const string Toon = """
                                name: Spring
                                count: 3
                                users[2]{id,name}:
                                  1,Ada
                                  2,"Bob, Jr."
                                servers[2:]{host,port}:
                                  eu: eu.example.com,443
                                  us: us.example.com,8443
                                items[2]:
                                  - kind: box
                                    tags[2]: a,b
                                  -
                                empty: []
                                """;

    private static List<string> Tokens(ToonDocumentReader reader)
    {
        var tokens = new List<string>();

        while (reader.Read())
        {
            tokens.Add(reader.TokenType switch
            {
                ToonTokenType.PropertyName => "name:" + reader.GetPropertyName(),
                ToonTokenType.String => "s:" + reader.GetString(),
                ToonTokenType.Number => "n:" + reader.GetDecimal(),
                ToonTokenType.StartArray => "[" + reader.ArrayLength,
                ToonTokenType.True or ToonTokenType.False => reader.GetBoolean().ToString(),
                _ => reader.TokenType.ToString()
            });
        }

        return tokens;
    }

    [Fact]
    public void Tokens_FollowTheDataModel()
    {
        using var reader = new ToonDocumentReader(Toon);

        Assert.Equal(
        [
            "StartObject",
            "name:name", "s:Spring",
            "name:count", "n:3",
            "name:users", "[2",
            "StartObject", "name:id", "n:1", "name:name", "s:Ada", "EndObject",
            "StartObject", "name:id", "n:2", "name:name", "s:Bob, Jr.", "EndObject",
            "EndArray",
            "name:servers", "StartObject",
            "name:eu", "StartObject", "name:host", "s:eu.example.com", "name:port", "n:443", "EndObject",
            "name:us", "StartObject", "name:host", "s:us.example.com", "name:port", "n:8443", "EndObject",
            "EndObject",
            "name:items", "[2",
            "StartObject", "name:kind", "s:box", "name:tags", "[2", "s:a", "s:b", "EndArray", "EndObject",
            "StartObject", "EndObject",
            "EndArray",
            "name:empty", "[0", "EndArray",
            "EndObject"
        ], Tokens(reader));

        Assert.Equal(ToonTokenType.None, reader.TokenType);
        Assert.False(reader.Read());
    }

    [Fact]
    public async Task Inputs_GiveTheSameTokens()
    {
        var expected = Tokens(new ToonDocumentReader(Toon));
        var bytes = Text.Encoding.UTF8.GetBytes(Toon);

        Assert.Equal(expected, Tokens(new ToonDocumentReader(bytes)));
        Assert.Equal(expected, Tokens(new ToonDocumentReader(("x" + Toon).AsSpan(1))));
        Assert.Equal(expected, Tokens(ToonDocumentReader.Create(new MemoryStream(bytes))));
        Assert.Equal(expected, Tokens(await ToonDocumentReader.CreateAsync(new TrickleStream(bytes))));
    }

    [Fact]
    public void Skip_AndReadValue_MoveOverAValue()
    {
        using var reader = new ToonDocumentReader(Toon);
        var names = new List<string>();
        ToonValue? servers = null;

        Assert.True(reader.Read());

        while (reader.Read() && reader.TokenType == ToonTokenType.PropertyName)
        {
            var name = reader.GetPropertyName();
            names.Add(name);

            if (name == "servers")
            {
                servers = reader.ReadValue();
                Assert.Equal(ToonTokenType.EndObject, reader.TokenType);
            }
            else
            {
                reader.Skip();
            }
        }

        Assert.Equal(ToonTokenType.EndObject, reader.TokenType);
        Assert.Equal(["name", "count", "users", "servers", "items", "empty"], names);
        Assert.Equal(443m, ((ToonNumber)((ToonObject)((ToonObject)servers!).Properties["eu"]).Properties["port"]).DecimalValue);
        Assert.False(reader.Read());
    }

    [Fact]
    public void Skip_OnAStartToken_ReadsToItsEnd()
    {
        using var reader = new ToonDocumentReader(Toon);
        reader.Read();
        reader.Read(); // name
        reader.Skip(); // the value
        Assert.Equal(ToonTokenType.String, reader.TokenType);

        while (reader.Read() && !(reader.TokenType == ToonTokenType.PropertyName && reader.GetPropertyName() == "users"))
        {
        }

        reader.Read();
        Assert.Equal(ToonTokenType.StartArray, reader.TokenType);
        reader.Skip();
        Assert.Equal(ToonTokenType.EndArray, reader.TokenType);
        Assert.True(reader.Read());
        Assert.Equal("servers", reader.GetPropertyName());
    }

    [Fact]
    public void Numbers_ConvertWhenTheyFit()
    {
        using var reader = new ToonDocumentReader("[5]: 12,1.5,1e2,99999999999,-0");
        reader.Read();
        reader.Read();
        Assert.Equal(12, reader.GetInt32());
        Assert.Equal("12", reader.ValueSpan.ToString());

        reader.Read();
        Assert.False(reader.TryGetInt32(out _));
        Assert.Equal(1.5m, reader.GetDecimal());
        Assert.Equal(1.5, reader.GetDouble());
        Assert.Throws<FormatException>(() => reader.GetInt64());

        reader.Read();
        Assert.Equal(100, reader.GetInt32());

        reader.Read();
        Assert.Throws<FormatException>(() => reader.GetInt32());
        Assert.Equal(99999999999L, reader.GetInt64());

        reader.Read();
        Assert.Equal(0, reader.GetInt32());
    }

    [Fact]
    public void Getters_CheckTheTokenType()
    {
        using var reader = new ToonDocumentReader("a: 1");
        Assert.Throws<InvalidOperationException>(() => reader.GetString());

        reader.Read();
        Assert.Throws<InvalidOperationException>(() => reader.GetPropertyName());
        Assert.Throws<InvalidOperationException>(() => reader.ArrayLength);
        Assert.Throws<InvalidOperationException>(() => { _ = reader.ValueSpan.Length; });

        Assert.IsType<ToonObject>(reader.ReadValue());
        Assert.Equal(ToonTokenType.EndObject, reader.TokenType);
        Assert.Throws<InvalidOperationException>(() => reader.ReadValue());
    }

    [Fact]
    public void Strings_AreTyped()
    {
        using var reader = new ToonDocumentReader("[6]: \"true\",true,\"12\",12,x y,\"\"");
        reader.Read();

        var types = new List<ToonTokenType>();

        while (reader.Read() && reader.TokenType != ToonTokenType.EndArray)
        {
            types.Add(reader.TokenType);
        }

        Assert.Equal([ToonTokenType.String, ToonTokenType.True, ToonTokenType.String, ToonTokenType.Number, ToonTokenType.String, ToonTokenType.String], types);
    }

    [Fact]
    public void InvalidDocuments_ThrowWhileReading()
    {
        using var reader = new ToonDocumentReader("a: 1\nb: \"unterminated");
        reader.Read();
        reader.Read();
        reader.Read();
        reader.Read();

        var ex = Assert.Throws<ToonParseException>(() => reader.Read());
        Assert.Equal(2, ex.Line);

        Assert.Throws<ToonParseException>(() => new ToonDocumentReader([0xFF]));
    }

    [Fact]
    public void LineNumbers_FollowTheTokens()
    {
        using var reader = new ToonDocumentReader("a: 1\nb:\n  c: 2");
        reader.Read();
        reader.Read();
        Assert.Equal(1, reader.LineNumber);

        reader.Read();
        reader.Read();
        Assert.Equal(2, reader.LineNumber);
        Assert.Equal(1, reader.CurrentDepth);
    }

    [Fact]
    public void Disposed_Throws()
    {
        var reader = new ToonDocumentReader([.. "a: 1"u8]);
        reader.Dispose();
        reader.Dispose();

        Assert.Throws<ObjectDisposedException>(() => reader.Read());
    }
}
