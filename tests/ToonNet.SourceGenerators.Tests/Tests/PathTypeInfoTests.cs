using System.Text;
using ToonNet.Core;
using ToonNet.Core.Llm;
using ToonNet.Core.Parsing;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;
using ToonNet.Core.Validation;

namespace ToonNet.SourceGenerators.Tests.Tests;

[ToonSerializable]
public partial class PathRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>The path methods that take a ToonTypeInfo: results and errors, in strict and non-strict mode.</summary>
public class PathTypeInfoTests
{
    private const string Document = """
                                    meta:
                                      min: 2
                                    data:
                                      rows[3]{Id,Name}:
                                        1,a
                                        2,b
                                        3,c
                                    """;

    private static IEnumerable<ToonSerializerOptions> Modes() =>
    [
        new ToonSerializerOptions(),
        new ToonSerializerOptions { ToonOptions = new ToonOptions { StrictMode = false } }
    ];

    [Fact]
    public async Task DeserializeAt_WithATypeInfo()
    {
        foreach (var options in Modes())
        {
            Assert.Equal("b", ToonSerializer.DeserializeAt(Document, "$.data.rows[1]", PathRow.ToonTypeInfo, options)!.Name);
            Assert.Equal("c", ToonSerializer.DeserializeAt(Encoding.UTF8.GetBytes(Document), "$.data.rows[-1]", PathRow.ToonTypeInfo, options)!.Name);
            Assert.Equal(3, (await ToonSerializer.DeserializeAtAsync(new MemoryStream(Encoding.UTF8.GetBytes(Document)), "$.data.rows[2]",
                                                                    PathRow.ToonTypeInfo, options))!.Id);
            Assert.Null(ToonSerializer.DeserializeAt(Document, "$.data.rows[9]", PathRow.ToonTypeInfo, options));

            var ex = Assert.Throws<ToonSerializationException>(() =>
                ToonSerializer.DeserializeAt(Document.Replace("2,b", "x,b"), "$.data.rows[1]", PathRow.ToonTypeInfo, options));
            Assert.Equal("$.data.rows[1].Id", ex.Path);
        }
    }

    [Fact]
    public async Task DeserializeAll_WithATypeInfo()
    {
        foreach (var options in Modes())
        {
            Assert.Equal([1, 3], ToonSerializer.DeserializeAll(Document, "$.data.rows[0,2]", PathRow.ToonTypeInfo, options).Select(r => r!.Id));
            Assert.Equal([2, 3], ToonSerializer.DeserializeAll(Encoding.UTF8.GetBytes(Document), "$.data.rows[?@.Id >= 2]", PathRow.ToonTypeInfo, options)
                                               .Select(r => r!.Id));
            Assert.Equal([2, 3], (await ToonSerializer.DeserializeAllAsync(new MemoryStream(Encoding.UTF8.GetBytes(Document)), "$.data.rows[?@.Id >= $.meta.min]",
                                                                          PathRow.ToonTypeInfo, options)).Select(r => r!.Id));

            // Errors over text and on a tree (several selectors) have the path in the document
            var bad = Document.Replace("2,b", "x,b");
            Assert.Equal("$.data.rows[1].Id", Assert.Throws<ToonSerializationException>(() =>
                ToonSerializer.DeserializeAll(bad, "$.data.rows[?@.Name != 'a']", PathRow.ToonTypeInfo, options)).Path);
            Assert.Equal("$.data.rows[1].Id", Assert.Throws<ToonSerializationException>(() =>
                ToonSerializer.DeserializeAll(bad, "$.data.rows[2,1]", PathRow.ToonTypeInfo, options)).Path);
        }
    }

    [Fact]
    public void Reader_WithATypeInfo_ContinuesAfterTheValue()
    {
        using var reader = new ToonDocumentReader(Document);

        while (reader.Read() && !(reader.TokenType == ToonTokenType.PropertyName && reader.GetPropertyName() == "rows"))
        {
        }

        reader.Read(); // StartArray
        var rows = new List<PathRow?>();

        while (reader.Read() && reader.TokenType != ToonTokenType.EndArray)
        {
            rows.Add(ToonSerializer.Deserialize(reader, PathRow.ToonTypeInfo));
        }

        Assert.Equal(["a", "b", "c"], rows.Select(r => r!.Name));
        Assert.True(reader.Read()); // EndObject of data
        Assert.Equal(ToonTokenType.EndObject, reader.TokenType);
    }

    [Fact]
    public void DeserializeValidated_WithATypeInfo()
    {
        var rules = new ToonRuleSet().Rule("$", "@.Id > 0", "Id must be positive");

        Assert.Equal("x", ToonPrompt.DeserializeValidated("```toon\nId: 1\nName: x\n```", rules, PathRow.ToonTypeInfo)!.Name);
        Assert.Throws<ToonRuleException>(() => ToonPrompt.DeserializeValidated("Id: 0\nName: x", rules, PathRow.ToonTypeInfo));
    }
}
