using System.Text.Json;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Tests.Paths;

namespace ToonNet.Tests.Models;

public class ToonMergePatchTests
{
    private static ToonValue Json(string json) => ToonPathComplianceTests.ToToon(JsonDocument.Parse(json).RootElement);

    // RFC 7386, Appendix A
    [Theory]
    [InlineData("""{"a":"b"}""", """{"a":"c"}""", """{"a":"c"}""")]
    [InlineData("""{"a":"b"}""", """{"b":"c"}""", """{"a":"b","b":"c"}""")]
    [InlineData("""{"a":"b"}""", """{"a":null}""", """{}""")]
    [InlineData("""{"a":"b","b":"c"}""", """{"a":null}""", """{"b":"c"}""")]
    [InlineData("""{"a":["b"]}""", """{"a":"c"}""", """{"a":"c"}""")]
    [InlineData("""{"a":"c"}""", """{"a":["b"]}""", """{"a":["b"]}""")]
    [InlineData("""{"a":{"b":"c"}}""", """{"a":{"b":"d","c":null}}""", """{"a":{"b":"d"}}""")]
    [InlineData("""{"a":[{"b":"c"}]}""", """{"a":[1]}""", """{"a":[1]}""")]
    [InlineData("""["a","b"]""", """["c","d"]""", """["c","d"]""")]
    [InlineData("""{"a":"b"}""", """["c"]""", """["c"]""")]
    [InlineData("""{"a":"foo"}""", """null""", """null""")]
    [InlineData("""{"a":"foo"}""", "\"bar\"", "\"bar\"")]
    [InlineData("""{"e":null}""", """{"a":1}""", """{"e":null,"a":1}""")]
    [InlineData("""[1,2]""", """{"a":"b","c":null}""", """{"a":"b"}""")]
    [InlineData("""{}""", """{"a":{"bb":{"ccc":null}}}""", """{"a":{"bb":{}}}""")]
    public void Apply_RfcExamples(string target, string patch, string expected)
    {
        var result = ToonMergePatch.Apply(Json(target), Json(patch));

        Assert.True(ToonValue.DeepEquals(Json(expected), result), ToonSerializer.Serialize(result));
    }

    [Fact]
    public void Apply_ChangesObjectsInPlace_AndCopiesThePatch()
    {
        var target = ToonDocument.Parse("meta:\n  region: eu\n  draft: true\nname: x").Root;
        var patch = ToonDocument.Parse("meta:\n  region: us\n  draft: null\n  tags[1]: a").Root;

        var result = ToonMergePatch.Apply(target, patch);

        Assert.Same(target, result);
        Assert.Equal("meta:\n  region: us\n  tags[1]: a\nname: x", ToonSerializer.Serialize(result));

        patch["meta"]!["tags"]!.AsArray().Add("b");
        Assert.Single(result["meta"]!["tags"]!.AsArray().Items);
    }

    [Theory]
    [InlineData("""{"a":1,"b":{"c":2,"d":3},"e":[1]}""", """{"a":1,"b":{"c":5},"e":[1,2],"f":"new"}""",
                """{"b":{"d":null,"c":5},"e":[1,2],"f":"new"}""")]
    [InlineData("""{"a":1}""", """{"a":1}""", """{}""")]
    [InlineData("""{"a":1}""", """[1]""", """[1]""")]
    [InlineData("""[1]""", """{"a":1}""", """{"a":1}""")]
    [InlineData("""{"a":{"b":1}}""", """{"a":2}""", """{"a":2}""")]
    public void Create_ProducesThePatch(string original, string modified, string expected)
    {
        var patch = ToonMergePatch.Create(Json(original), Json(modified));

        Assert.True(ToonValue.DeepEquals(Json(expected), patch), ToonSerializer.Serialize(patch));
        Assert.True(ToonValue.DeepEquals(Json(modified), ToonMergePatch.Apply(Json(original), patch)));
    }

    [Fact]
    public void Create_DoesNotShareValues()
    {
        var modified = Json("""{"list":[1,2]}""");
        var patch = ToonMergePatch.Create(Json("{}"), modified);

        modified["list"]!.AsArray().Add(3);
        Assert.Equal(2, patch["list"]!.AsArray().Count);
    }

    [Fact]
    public void Arguments_AreChecked()
    {
        Assert.Throws<ArgumentNullException>(() => ToonMergePatch.Apply(null!, new ToonObject()));
        Assert.Throws<ArgumentNullException>(() => ToonMergePatch.Apply(new ToonObject(), null!));
        Assert.Throws<ArgumentNullException>(() => ToonMergePatch.Create(null!, new ToonObject()));
    }

    [Fact]
    public void Apply_PatchIsTheTarget()
    {
        var target = ToonDocument.Parse("a: null\nb:\n  c: 1").Root;

        Assert.Equal("b:\n  c: 1", ToonSerializer.Serialize(ToonMergePatch.Apply(target, target)));
    }
}
