using System.Text.Json;
using System.Text.Json.Nodes;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Serialization;

/// <summary>Values that already are documents (ToonDocument, JsonElement, JsonNode) are written as what they hold.</summary>
public class DocumentValueTests
{
    public sealed class Envelope
    {
        public string Id { get; set; } = "";
        public JsonElement Payload { get; set; }
        public JsonNode? Extra { get; set; }
        public JsonElement? Maybe { get; set; }
    }

    private const string Json = """{"name":"Ada","tags":["a","b"],"big":12345678901234567890,"ratio":0.1,"nested":{"ok":true,"none":null}}""";

    [Fact]
    public void ToonDocuments_AreWrittenAsTheirRoot()
    {
        var document = ToonDocument.Parse("a: 1\nb[2]: x,y");

        Assert.Equal("a: 1\nb[2]: x,y", ToonSerializer.Serialize(document));
        Assert.Equal("a: 1\nb[2]: x,y", new ToonEncoder().Encode(ToonSerializer.Deserialize<ToonDocument>("a: 1\nb[2]: x,y")!));
        Assert.Equal("doc:\n  a: 1\n  b[2]: x,y", ToonSerializer.Serialize(new Dictionary<string, ToonDocument> { ["doc"] = document }));
    }

    [Fact]
    public void JsonValues_AreWrittenAsTheirJson_AndReadBack()
    {
        using var json = JsonDocument.Parse(Json);
        var envelope = new Envelope { Id = "e1", Payload = json.RootElement.Clone(), Extra = JsonNode.Parse("[1,2.50,\"x\"]"), Maybe = null };

        var text = ToonSerializer.Serialize(envelope);

        Assert.Equal("""
                     Id: e1
                     Payload:
                       name: Ada
                       tags[2]: a,b
                       big: 12345678901234567890
                       ratio: 0.1
                       nested:
                         ok: true
                         none: null
                     Extra[3]: 1,2.5,x
                     Maybe: null
                     """, text);

        // The same through the tree, and back to JSON with every digit
        Assert.Equal(text, new ToonEncoder().Encode(new ToonDocument(ToonSerializer.SerializeToValue(envelope))));

        var back = ToonSerializer.Deserialize<Envelope>(text)!;
        Assert.Equal(Json, back.Payload.GetRawText().Replace(" ", ""));
        Assert.Equal("[1,2.5,\"x\"]", back.Extra!.ToJsonString());
        Assert.Null(back.Maybe);
        Assert.Equal(back.Payload.GetRawText(), ToonSerializer.DeserializeFromValue<Envelope>(ToonDocument.Parse(text).Root)!.Payload.GetRawText());
    }

    [Fact]
    public void JsonRoots_RoundTrip()
    {
        var node = JsonNode.Parse(Json)!;
        var text = ToonSerializer.Serialize(node);

        Assert.Equal(node.ToJsonString(), ToonSerializer.Deserialize<JsonNode>(text)!.ToJsonString());
        Assert.Equal(node.ToJsonString(), ToonSerializer.Deserialize<JsonObject>(text)!.ToJsonString());
        Assert.Throws<ToonNet.Core.ToonSerializationException>(() => ToonSerializer.Deserialize<JsonArray>(text));

        using var document = JsonDocument.Parse(Json);
        Assert.Equal(text, ToonSerializer.Serialize(document));
        Assert.Equal(text, ToonSerializer.Serialize(document.RootElement));
    }
}
