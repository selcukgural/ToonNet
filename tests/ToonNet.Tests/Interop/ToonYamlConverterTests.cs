using ToonNet.Core.Models;
using ToonNet.Extensions.Yaml;

namespace ToonNet.Tests.Interop;

/// <summary>
///     Tests for YAML ↔️ TOON conversion functionality.
/// </summary>
public class ToonYamlConverterTests
{
    #region Numbers

    [Theory]
    [InlineData("scientific: 1.5e-10", "scientific: 1.5e-10")]
    [InlineData("big: 1e22", "big: 1e+22")]
    [InlineData("long: 12345678901234567890123", "long: 1.2345678901234567890123e+22")]
    [InlineData("tiny: 0.0000001", "tiny: 1e-7")]
    [InlineData("plain: 1.50", "plain: 1.5")]
    public void Numbers_TakeTheirCanonicalForm_AndRoundTrip(string yaml, string expected)
    {
        var toon = ToonYamlConvert.FromYaml(yaml);

        Assert.Equal(expected, toon);
        Assert.Equal(toon, ToonYamlConvert.FromYaml(ToonYamlConvert.ToYaml(toon)));
    }

    #endregion

    #region Limit Tests

    [Fact]
    public void FromYaml_NestingBeyondMaxDepth_ThrowsYamlException()
    {
        var yaml = new string('[', 5_000) + new string(']', 5_000);

        var ex = Assert.Throws<YamlDotNet.Core.YamlException>(() => ToonYamlConverter.FromYaml(yaml));

        Assert.Contains("Maximum nesting depth of 100 exceeded", ex.Message);
    }

    [Fact]
    public void FromYaml_NestingAtMaxDepth_Succeeds()
    {
        // The root sequence is at depth 0, so 101 nested sequences reach depth 100
        var yaml = new string('[', 101) + new string(']', 101);

        Assert.Throws<YamlDotNet.Core.YamlException>(() => ToonYamlConverter.FromYaml("[" + yaml + "]"));

        var doc = ToonYamlConverter.FromYaml(yaml);

        Assert.IsType<ToonArray>(doc.Root);
    }

    [Fact]
    public void FromYaml_CustomMaxDepth_IsEnforced()
    {
        var yaml = "a:\n  b:\n    c: 1\n";

        Assert.NotNull(ToonYamlConverter.FromYaml(yaml, new ToonNet.Core.ToonOptions { MaxDepth = 2 }));
        Assert.Throws<YamlDotNet.Core.YamlException>(() => ToonYamlConverter.FromYaml(yaml, new ToonNet.Core.ToonOptions { MaxDepth = 1 }));
    }

    [Fact]
    public void FromYaml_AliasBomb_ThrowsYamlException()
    {
        var sb = new System.Text.StringBuilder("a: &a [x,x,x,x,x,x,x,x,x,x]\n");
        var previous = 'a';

        foreach (var name in "bcdefghi")
        {
            sb.Append(name).Append(": &").Append(name).Append(" [")
              .AppendJoin(',', Enumerable.Repeat("*" + previous, 10))
              .Append("]\n");
            previous = name;
        }

        var ex = Assert.Throws<YamlDotNet.Core.YamlException>(() => ToonYamlConverter.FromYaml(sb.ToString()));

        Assert.Contains("aliases expand", ex.Message);
    }

    [Fact]
    public void FromYaml_AliasExpansionBeyondMaxDepth_ThrowsYamlException()
    {
        // Each level nests the previous anchor two levels deeper: the text stays shallow, the expanded tree does not
        var sb = new System.Text.StringBuilder("l0: &l0 [[x]]\n");

        for (var i = 1; i < 60; i++)
        {
            sb.Append('l').Append(i).Append(": &l").Append(i).Append(" [[*l").Append(i - 1).Append("]]\n");
        }

        var ex = Assert.Throws<YamlDotNet.Core.YamlException>(() => ToonYamlConverter.FromYaml(sb.ToString()));

        Assert.Contains("Maximum nesting depth of 100 exceeded by the expansion of alias", ex.Message);
    }

    [Fact]
    public void FromYaml_AliasExpansionWithinMaxDepth_Succeeds()
    {
        const string yaml = "a: &a [[[1]]]\nb: [[*a]]";

        // b (1) > [ (2) > *a: [ (3) > [ (4) > [ (5)
        var root = ToonYamlConverter.FromYaml(yaml, new ToonNet.Core.ToonOptions { MaxDepth = 5 }).AsObject();

        Assert.IsType<ToonArray>(root["b"]);
        Assert.Throws<YamlDotNet.Core.YamlException>(() => ToonYamlConverter.FromYaml(yaml, new ToonNet.Core.ToonOptions { MaxDepth = 4 }));
    }

    [Theory]
    [InlineData("&a [*a]")]
    [InlineData("a: &a\n  b: *a")]
    [InlineData("x: &a\n  - y: [1, *a]")]
    public void FromYaml_RecursiveAlias_ThrowsYamlException(string yaml)
    {
        var ex = Assert.Throws<YamlDotNet.Core.YamlException>(() => ToonYamlConverter.FromYaml(yaml));

        Assert.Contains("recursive", ex.Message);
    }

    [Theory]
    [InlineData("a: *x\nb: &x [1, 2]", "*x")]
    [InlineData("a: &a [*b]\nb: &b [*a]", "*b")]
    [InlineData("--- &x [1]\n--- [*x]", "*x")]
    public void FromYaml_AliasBeforeItsAnchor_ThrowsYamlException(string yaml, string alias)
    {
        var ex = Assert.Throws<YamlDotNet.Core.YamlException>(() => ToonYamlConverter.FromYaml(yaml));

        Assert.Contains($"The alias {alias} refers to an anchor that is not defined before it", ex.Message);
    }

    [Fact]
    public void FromYaml_AliasBombWithAnchorsAfterTheirAliases_ThrowsYamlException()
    {
        // Each line refers to the anchor of the next one: a scan that counts only aliases of earlier anchors sees no
        // expansion, while the loader resolves them all (ten levels of ten copies)
        var sb = new System.Text.StringBuilder();

        for (var i = 9; i > 0; i--)
        {
            sb.Append('l').Append(i).Append(": &l").Append(i).Append(" [")
              .AppendJoin(',', Enumerable.Repeat("*l" + (i - 1), 10))
              .Append("]\n");
        }

        sb.Append("l0: &l0 [x,x,x,x,x,x,x,x,x,x]\n");

        var ex = Assert.Throws<YamlDotNet.Core.YamlException>(() => ToonYamlConverter.FromYaml(sb.ToString()));

        Assert.Contains("The alias *l8 refers to an anchor that is not defined before it", ex.Message);
    }

    [Fact]
    public void FromYaml_RedefinedAnchor_IsNotRecursive()
    {
        var root = ToonYamlConverter.FromYaml("a: &a [&a [1], *a]").AsObject();

        Assert.Equal(2, Assert.IsType<ToonArray>(root["a"]).Count);
    }

    [Fact]
    public void FromYaml_OrdinaryAliases_AreExpanded()
    {
        var yaml = """
                   base: &base
                     host: localhost
                     port: 5432
                   dev:
                     db: *base
                   """;

        var root = (ToonObject)ToonYamlConverter.FromYaml(yaml).Root;
        var db = (ToonObject)((ToonObject)root["dev"]!)["db"]!;

        Assert.Equal("localhost", ((ToonString)db["host"]!).Value);
    }

    #endregion

    #region YAML to TOON Tests

    [Fact]
    public void FromYaml_SimpleObject_ConvertsCorrectly()
    {
        var yaml = """
                   name: Alice
                   age: 30
                   """;

        var doc = ToonYamlConverter.FromYaml(yaml);
        var root = (ToonObject)doc.Root;

        Assert.Equal("Alice", ((ToonString)root["name"]!).Value);
        Assert.Equal(30, ((ToonNumber)root["age"]!).Value);
    }

    [Fact]
    public void FromYaml_WithArray_ConvertsCorrectly()
    {
        var yaml = """
                   tags:
                     - dev
                     - admin
                     - user
                   """;

        var doc = ToonYamlConverter.FromYaml(yaml);
        var root = (ToonObject)doc.Root;
        var tags = (ToonArray)root["tags"]!;

        Assert.Equal(3, tags.Items.Count);
        Assert.Equal("dev", ((ToonString)tags.Items[0]!).Value);
        Assert.Equal("admin", ((ToonString)tags.Items[1]!).Value);
        Assert.Equal("user", ((ToonString)tags.Items[2]!).Value);
    }

    [Fact]
    public void FromYaml_NestedObject_ConvertsCorrectly()
    {
        var yaml = """
                   user:
                     name: Bob
                     profile:
                       bio: Software Engineer
                       age: 35
                   """;

        var doc = ToonYamlConverter.FromYaml(yaml);
        var root = (ToonObject)doc.Root;
        var user = (ToonObject)root["user"]!;
        var profile = (ToonObject)user["profile"]!;

        Assert.Equal("Bob", ((ToonString)user["name"]!).Value);
        Assert.Equal("Software Engineer", ((ToonString)profile["bio"]!).Value);
        Assert.Equal(35, ((ToonNumber)profile["age"]!).Value);
    }

    [Fact]
    public void FromYaml_BooleanValues_FollowYaml12()
    {
        var yaml = """
                   enabled: true
                   disabled: false
                   yesValue: yes
                   noValue: no
                   onValue: on
                   offValue: off
                   """;

        var doc = ToonYamlConverter.FromYaml(yaml);
        var root = (ToonObject)doc.Root;

        Assert.True(((ToonBoolean)root["enabled"]!).Value);
        Assert.False(((ToonBoolean)root["disabled"]!).Value);

        // The YAML 1.1 booleans are strings in YAML 1.2
        Assert.Equal("yes", ((ToonString)root["yesValue"]!).Value);
        Assert.Equal("no", ((ToonString)root["noValue"]!).Value);
        Assert.Equal("on", ((ToonString)root["onValue"]!).Value);
        Assert.Equal("off", ((ToonString)root["offValue"]!).Value);
    }

    [Fact]
    public void FromYaml_NullValue_ConvertCorrectly()
    {
        var yaml = """
                   nullValue: null
                   emptyValue: ~
                   """;

        var doc = ToonYamlConverter.FromYaml(yaml);
        var root = (ToonObject)doc.Root;

        Assert.IsType<ToonNull>(root["nullValue"]);
        Assert.IsType<ToonNull>(root["emptyValue"]);
    }

    [Fact]
    public void FromYaml_Numbers_ConvertCorrectly()
    {
        var yaml = """
                   integer: 42
                   float: 3.14
                   negative: -10
                   scientific: 1.5e10
                   """;

        var doc = ToonYamlConverter.FromYaml(yaml);
        var root = (ToonObject)doc.Root;

        Assert.Equal(42, ((ToonNumber)root["integer"]!).Value);
        Assert.Equal(3.14, ((ToonNumber)root["float"]!).Value);
        Assert.Equal(-10, ((ToonNumber)root["negative"]!).Value);
        Assert.Equal(1.5e10, ((ToonNumber)root["scientific"]!).Value);
    }

    [Fact]
    public void FromYaml_EmptyYaml_CreatesEmptyDocument()
    {
        Assert.Empty(Assert.IsType<ToonObject>(ToonYamlConverter.FromYaml("").Root).Properties);
        Assert.Empty(Assert.IsType<ToonObject>(ToonYamlConverter.FromYaml("# only a comment").Root).Properties);
        Assert.IsType<ToonNull>(ToonYamlConverter.FromYaml("---").Root);
    }

    #endregion

    #region TOON to YAML Tests

    [Fact]
    public void ToYaml_SimpleObject_ConvertsCorrectly()
    {
        var toonObj = new ToonObject
        {
            ["name"] = new ToonString("Alice"),
            ["age"] = new ToonNumber(30)
        };
        var doc = new ToonDocument(toonObj);

        var yaml = ToonYamlConverter.ToYaml(doc);

        Assert.Contains("name: Alice", yaml);
        Assert.Contains("age: 30", yaml);
    }

    [Fact]
    public void ToYaml_WithArray_ConvertsCorrectly()
    {
        var array = new ToonArray(new List<ToonValue>
        {
            new ToonString("dev"),
            new ToonString("admin"),
            new ToonString("user")
        });

        var toonObj = new ToonObject { ["tags"] = array };
        var doc = new ToonDocument(toonObj);

        var yaml = ToonYamlConverter.ToYaml(doc);

        Assert.Contains("tags:", yaml);
        Assert.Contains("- dev", yaml);
        Assert.Contains("- admin", yaml);
        Assert.Contains("- user", yaml);
    }

    [Fact]
    public void ToYaml_NestedObject_ConvertsCorrectly()
    {
        var profile = new ToonObject
        {
            ["bio"] = new ToonString("Software Engineer"),
            ["age"] = new ToonNumber(35)
        };

        var user = new ToonObject
        {
            ["name"] = new ToonString("Bob"),
            ["profile"] = profile
        };

        var root = new ToonObject { ["user"] = user };
        var doc = new ToonDocument(root);

        var yaml = ToonYamlConverter.ToYaml(doc);

        Assert.Contains("user:", yaml);
        Assert.Contains("name: Bob", yaml);
        Assert.Contains("profile:", yaml);
        Assert.Contains("bio: Software Engineer", yaml);
        Assert.Contains("age: 35", yaml);
    }

    [Fact]
    public void ToYaml_BooleanValues_ConvertCorrectly()
    {
        var toonObj = new ToonObject
        {
            ["enabled"] = new ToonBoolean(true),
            ["disabled"] = new ToonBoolean(false)
        };
        var doc = new ToonDocument(toonObj);

        var yaml = ToonYamlConverter.ToYaml(doc);

        Assert.Contains("enabled: true", yaml);
        Assert.Contains("disabled: false", yaml);
    }

    [Fact]
    public void ToYaml_NullValue_ConvertCorrectly()
    {
        var toonObj = new ToonObject { ["nullValue"] = ToonNull.Instance };
        var doc = new ToonDocument(toonObj);

        var yaml = ToonYamlConverter.ToYaml(doc);

        Assert.Equal("nullValue: \n", yaml.ReplaceLineEndings("\n"));
        Assert.IsType<ToonNull>(ToonYamlConverter.FromYaml(yaml).AsObject()["nullValue"]);
    }

    #endregion

    #region Round-trip Tests

    [Fact]
    public void RoundTrip_YamlToToonToYaml_PreservesStructure()
    {
        var originalYaml = """
                           name: Alice
                           age: 30
                           tags:
                             - dev
                             - admin
                           settings:
                             theme: dark
                             notifications: true
                           """;

        // YAML -> TOON
        var doc = ToonYamlConverter.FromYaml(originalYaml);

        // TOON -> YAML
        var newYaml = ToonYamlConverter.ToYaml(doc);

        // YAML -> TOON (again)
        var doc2 = ToonYamlConverter.FromYaml(newYaml);
        var root = (ToonObject)doc2.Root;

        // Verify structure
        Assert.Equal("Alice", ((ToonString)root["name"]!).Value);
        Assert.Equal(30, ((ToonNumber)root["age"]!).Value);

        var tags = (ToonArray)root["tags"]!;
        Assert.Equal(2, tags.Items.Count);
        Assert.Equal("dev", ((ToonString)tags.Items[0]!).Value);

        var settings = (ToonObject)root["settings"]!;
        Assert.Equal("dark", ((ToonString)settings["theme"]!).Value);
        Assert.True(((ToonBoolean)settings["notifications"]!).Value);
    }

    [Fact]
    public void RoundTrip_ToonToYamlToToon_PreservesData()
    {
        // Create TOON document
        var originalToon = new ToonObject
        {
            ["name"] = new ToonString("Bob"),
            ["age"] = new ToonNumber(25),
            ["active"] = new ToonBoolean(true),
            ["data"] = ToonNull.Instance
        };
        var doc = new ToonDocument(originalToon);

        // TOON -> YAML
        var yaml = ToonYamlConverter.ToYaml(doc);

        // YAML -> TOON
        var doc2 = ToonYamlConverter.FromYaml(yaml);
        var root = (ToonObject)doc2.Root;

        // Verify data
        Assert.Equal("Bob", ((ToonString)root["name"]!).Value);
        Assert.Equal(25, ((ToonNumber)root["age"]!).Value);
        Assert.True(((ToonBoolean)root["active"]!).Value);
        Assert.IsType<ToonNull>(root["data"]);
    }

    [Fact]
    public void Integration_YamlToToonToJsonToToonToYaml_WorksCorrectly()
    {
        // YAML -> TOON -> JSON -> TOON -> YAML keeps the data
        var yaml = """
                   user:
                     name: Charlie
                     id: 123
                     roles:
                       - admin
                       - editor
                   """;

        var doc1 = ToonYamlConverter.FromYaml(yaml);
        var viaJson = ToonNet.Extensions.Json.ToonJsonConverter.FromJson(ToonNet.Extensions.Json.ToonJsonConverter.ToJson(doc1));
        var yaml2 = ToonYamlConverter.ToYaml(viaJson);
        var doc2 = ToonYamlConverter.FromYaml(yaml2);

        Assert.True(ToonValue.DeepEquals(doc1.Root, doc2.Root));

        var root = (ToonObject)doc2.Root;
        var user = (ToonObject)root["user"]!;
        var roles = (ToonArray)user["roles"]!;

        Assert.Equal("Charlie", ((ToonString)user["name"]!).Value);
        Assert.Equal(123, ((ToonNumber)user["id"]!).Value);
        Assert.Equal(2, roles.Items.Count);
        Assert.Equal("admin", ((ToonString)roles.Items[0]!).Value);
        Assert.Equal("editor", ((ToonString)roles.Items[1]!).Value);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void FromYaml_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ToonYamlConverter.FromYaml(null!));
    }

    [Fact]
    public void ToYaml_NullDocument_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ToonYamlConverter.ToYaml((ToonDocument)null!));
    }

    [Fact]
    public void ToYaml_NullValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ToonYamlConverter.ToYaml((ToonValue)null!));
    }

    #endregion

    #region Scalar typing

    [Fact]
    public void FromYaml_QuotedAndBlockScalars_StayStrings()
    {
        var toon = ToonYamlConvert.FromYaml("a: \"42\"\nb: 'true'\nc: \"null\"\nd: ''\ne: |\n  7\n");

        Assert.Equal("a: \"42\"\nb: \"true\"\nc: \"null\"\nd: \"\"\ne: \"7\\n\"", toon);
    }

    [Theory]
    [InlineData("42", "42")]
    [InlineData("+5", "5")]
    [InlineData("-1.50", "-1.5")]
    [InlineData("12345678901234567890", "12345678901234567890")]
    [InlineData("True", "true")]
    [InlineData("FALSE", "false")]
    [InlineData("NO", "NO")]
    [InlineData("~", "null")]
    [InlineData("007", "7")]
    [InlineData("-007", "-7")]
    [InlineData("000", "0")]
    [InlineData("0xFF", "255")]
    [InlineData("0o17", "15")]
    [InlineData(".5", "0.5")]
    [InlineData("-.5", "-0.5")]
    [InlineData("5.", "5")]
    [InlineData("1e3", "1000")]
    [InlineData("01.50E-1", "0.15")]
    [InlineData("1,000", "\"1,000\"")]
    [InlineData("1_000", "1_000")]
    [InlineData("(5)", "(5)")]
    [InlineData("+0x1F", "+0x1F")]
    [InlineData("0x", "0x")]
    [InlineData("0o8", "0o8")]
    [InlineData("0xFFFFFFFFFFFFFFFFFFFFFFFFFFFF", "0xFFFFFFFFFFFFFFFFFFFFFFFFFFFF")]
    [InlineData(".", ".")]
    [InlineData("1e", "1e")]
    [InlineData(".inf", ".inf")]
    [InlineData(".NaN", ".NaN")]
    [InlineData("yes", "yes")]
    [InlineData("Off", "Off")]
    [InlineData("!!str 42", "\"42\"")]
    [InlineData("! 42", "\"42\"")]
    public void FromYaml_PlainScalars_FollowTheYaml12CoreSchema(string yaml, string toon)
    {
        Assert.Equal($"v: {toon}", ToonYamlConvert.FromYaml($"v: {yaml}"));
    }

    [Fact]
    public void ToYaml_StringsThatLookLikeOtherTypes_AreQuoted_AndRoundTrip()
    {
        const string toon = "a: \"42\"\nb: \"true\"\nc: \"null\"\nd: \"\"\nn: 42\nbig: 12345678901234567890";

        var yaml = ToonYamlConvert.ToYaml(toon);

        Assert.Equal(toon, ToonYamlConvert.FromYaml(yaml));
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("Yes")]
    [InlineData("YES")]
    [InlineData("no")]
    [InlineData("No")]
    [InlineData("on")]
    [InlineData("ON")]
    [InlineData("off")]
    [InlineData("Off")]
    [InlineData("True")]
    [InlineData("FALSE")]
    [InlineData("Null")]
    [InlineData("NULL")]
    [InlineData("~")]
    [InlineData("+5")]
    [InlineData("-0")]
    [InlineData("1e3")]
    [InlineData("0.5")]
    [InlineData("007")]
    [InlineData("0x1F")]
    [InlineData("0o7")]
    [InlineData(".5")]
    [InlineData("5.")]
    [InlineData("y")]
    [InlineData(".inf")]
    [InlineData("-.inf")]
    [InlineData(".nan")]
    // Typed by YAML 1.1 readers only
    [InlineData("2001-12-14")]
    [InlineData("2001-12-14T21:59:43.10-05:00")]
    [InlineData("12:30:00")]
    [InlineData("1:20")]
    [InlineData("0b101")]
    [InlineData("1_000")]
    [InlineData("-1_000.5")]
    public void ToYaml_EveryStringTheReaderWouldType_IsQuoted(string text)
    {
        var value = new ToonObject { ["v"] = new ToonString(text) };

        var yaml = ToonYamlConverter.ToYaml(value);
        var back = ToonYamlConverter.FromYaml(yaml).AsObject();

        Assert.Equal(text, Assert.IsType<ToonString>(back["v"]).Value);

        // Quoted, so YAML 1.1 readers do not type it either
        Assert.Contains($"\"{text}\"", yaml);
    }

    [Fact]
    public void ToYaml_UsesToonOptions()
    {
        const string toon = "a:\n    b: 1";

        Assert.Throws<ToonNet.Core.ToonParseException>(() => ToonYamlConvert.ToYaml(toon));
        Assert.Contains("b: 1", ToonYamlConvert.ToYaml(toon, new ToonNet.Core.ToonOptions { IndentSize = 4 }));
    }

    #endregion

    [Theory]
    [InlineData("? [a, b]\n: 1")]
    [InlineData("? {a: 1}\n: 2")]
    public void FromYaml_NonScalarKey_Throws(string yaml)
    {
        var ex = Assert.Throws<YamlDotNet.Core.YamlException>(() => ToonYamlConverter.FromYaml(yaml));

        Assert.Contains("scalar mapping keys", ex.Message);
    }

    [Fact]
    public void FromYaml_EmptyKey_IsKept()
    {
        Assert.Equal("\"\": 1\nb: 2", ToonYamlConvert.FromYaml("\"\": 1\nb: 2"));
    }

    [Fact]
    public async Task ToYaml_ConcurrentCalls_ProduceTheSameOutput()
    {
        var documents = Enumerable.Range(0, 32).Select(i => new ToonObject { ["n"] = i, ["s"] = new ToonString("yes" + i), ["flag"] = new ToonString("on") }).ToArray();
        var expected = documents.Select(d => ToonYamlConverter.ToYaml(d)).ToArray();

        var actual = await Task.WhenAll(documents.Select(d => Task.Run(() => ToonYamlConverter.ToYaml(d))));

        Assert.Equal(expected, actual);
    }

    #region YAML features

    [Theory]
    [InlineData("a: !!int \"42\"", "a: 42")]
    [InlineData("a: !!float '1.5'", "a: 1.5")]
    [InlineData("a: !!bool \"true\"", "a: true")]
    [InlineData("a: !!null ''", "a: null")]
    [InlineData("a: !!str 42", "a: \"42\"")]
    [InlineData("a: ! true", "a: \"true\"")]
    [InlineData("a: !!int 0x1F", "a: 31")]
    [InlineData("a: !!int +7", "a: 7")]
    [InlineData("a: !!float 7", "a: 7")]
    [InlineData("a: !!null ~", "a: null")]
    public void ExplicitTags_DecideTheType(string yaml, string toon)
    {
        Assert.Equal(toon, ToonYamlConvert.FromYaml(yaml));
    }

    [Theory]
    [InlineData("a: !!int abc")]
    [InlineData("a: !!bool yes")]
    [InlineData("a: !!int 1.5")]
    [InlineData("a: !!int -0x1F")]
    [InlineData("a: !!int 1e3")]
    [InlineData("a: !!int ''")]
    [InlineData("a: !!null foo")]
    [InlineData("a: !!null 0")]
    public void ExplicitTags_ThatDoNotFit_AreErrors(string yaml)
    {
        Assert.Throws<YamlDotNet.Core.YamlException>(() => ToonYamlConvert.FromYaml(yaml));
    }

    [Theory]
    [InlineData("a\rb")]
    [InlineData("a\r\nb")]
    [InlineData("a\u0085b")]
    [InlineData("a\u2028b")]
    [InlineData("a\u2029b\nc")]
    public void ToYaml_KeepsCarriageReturnsAndOtherLineBreaks(string text)
    {
        // Readers turn these into line feeds or spaces outside double quotes
        var yaml = ToonYamlConverter.ToYaml(new ToonObject { ["s"] = new ToonString(text) });

        Assert.StartsWith("s: \"", yaml);
        Assert.Equal(text, ToonYamlConverter.FromYaml(yaml).Root["s"]!.GetValue<string>());
    }

    [Fact]
    public void ToYaml_WritesDeepTrees()
    {
        // YamlDotNet stops at 50 levels by default
        ToonValue Nest(int depth) => depth == 0 ? new ToonNumber(1) : depth % 2 == 0 ? new ToonObject { ["k"] = Nest(depth - 1) } : new ToonArray { Items = { Nest(depth - 1) } };
        var tree = Nest(201); // collections at depths 0 to 200

        var yaml = ToonYamlConverter.ToYaml(tree);

        Assert.True(ToonValue.DeepEquals(tree, ToonYamlConverter.FromYaml(yaml, new Core.ToonOptions { MaxDepth = 200 }).Root));
        Assert.Equal("Maximum nesting depth of 200 exceeded",
                     Assert.Throws<YamlDotNet.Core.YamlException>(() => ToonYamlConverter.ToYaml(Nest(202))).Message);
        Assert.Equal(ToonYamlConverter.ToYaml(new ToonArray { Items = { ToonNull.Instance, new ToonNumber(1) } }),
                     ToonYamlConverter.ToYaml(new ToonArray { Items = { null!, new ToonNumber(1) } }));
    }

    [Fact]
    public void ByteOrderMark_IsIgnored()
    {
        Assert.Equal("a: 1", ToonYamlConvert.FromYaml("\uFEFFa: 1"));
    }

    [Fact]
    public void SeveralDocuments_GiveTheFirst()
    {
        Assert.Equal("a: 1", ToonYamlConvert.FromYaml("---\na: 1\n---\nb: 2"));
    }

    [Fact]
    public void MergeKeys_AreOrdinaryKeys()
    {
        // << is a YAML 1.1 feature; the 1.2 core schema reads it as a key
        var toon = ToonYamlConvert.FromYaml("b: &b\n  x: 1\nc:\n  <<: *b\n  z: 3");

        Assert.Equal("b:\n  x: 1\nc:\n  \"<<\":\n    x: 1\n  z: 3", toon);
    }

    [Fact]
    public void Anchors_AndAliasesOfScalars_AreCopied()
    {
        Assert.Equal("a: 5\nb: 5", ToonYamlConvert.FromYaml("a: &x 5\nb: *x"));
    }

    [Fact]
    public void DuplicateKeys_AreErrors_InEitherMode()
    {
        Assert.ThrowsAny<YamlDotNet.Core.YamlException>(() => ToonYamlConvert.FromYaml("a: 1\na: 2"));
        Assert.ThrowsAny<Exception>(() => ToonYamlConverter.FromYaml("a: 1\na: 2", new ToonNet.Core.ToonOptions { StrictMode = false }));
    }

    [Theory]
    [InlineData("b: [1,")]
    [InlineData("a:\n\tb: 1")]
    [InlineData("a: \"unterminated")]
    public void InvalidYaml_IsAYamlException(string yaml)
    {
        Assert.ThrowsAny<YamlDotNet.Core.YamlException>(() => ToonYamlConvert.FromYaml(yaml));
    }

    [Fact]
    public void InvalidOptions_Throw()
    {
        Assert.Throws<InvalidOperationException>(() => ToonYamlConverter.FromYaml("a: 1", new ToonNet.Core.ToonOptions { MaxDepth = 500 }));
    }

    [Fact]
    public void BlockScalars_KeepTheirNewlines()
    {
        Assert.Equal("a: \"folded text\\npara\\n\"", ToonYamlConvert.FromYaml("a: >\n  folded\n  text\n\n  para\n"));
        Assert.Equal("a: \"x\\n\\n\"", ToonYamlConvert.FromYaml("a: |+\n  x\n\n"));
        Assert.Equal("a: x", ToonYamlConvert.FromYaml("a: |-\n  x\n"));
    }

    [Fact]
    public void BigIntegers_BeyondDecimal_AreRoundedToDouble()
    {
        Assert.Equal("n: 1.2345678901234568e+29", ToonYamlConvert.FromYaml("n: 123456789012345678901234567890"));
    }

    #endregion

    #region Untrusted input

    private static void AssertFast(Action action, int milliseconds = 2_000)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        action();
        Assert.True(watch.ElapsedMilliseconds < milliseconds, $"took {watch.ElapsedMilliseconds} ms");
    }

    [Theory]
    [InlineData("0x", 'f', 1_000_000)]
    [InlineData("0o", '7', 1_000_000)]
    public void LongHexadecimalAndOctalScalars_AreStrings_InLinearTime(string prefix, char digit, int count)
    {
        var text = prefix + new string(digit, count);

        AssertFast(() => Assert.Equal(text, ToonYamlConverter.FromYaml("a: " + text).Root["a"]!.GetValue<string>()));
        AssertFast(() => Assert.Equal(text, ToonYamlConverter.FromYaml(ToonYamlConvert.ToYaml("a: " + text)).Root["a"]!.GetValue<string>()));

        // Leading zeros do not count against the size, and the largest values still fit
        Assert.Equal(255m, ToonYamlConverter.FromYaml("a: 0x" + new string('0', 100) + "ff").Root["a"]!.GetValue<decimal>());
        Assert.Equal(decimal.MaxValue, ToonYamlConverter.FromYaml("a: 0xffffffffffffffffffffffff").Root["a"]!.GetValue<decimal>());
    }

    [Fact]
    public void AliasesOfLongScalars_CannotExpandBeyondTheCharacterBudget()
    {
        var yaml = new System.Text.StringBuilder("s: &a ").Append('A', 10_000).Append("\nl:\n");

        for (var i = 0; i < 99_000; i++)
        {
            yaml.Append("  - *a\n");
        }

        var ex = Assert.ThrowsAny<YamlDotNet.Core.YamlException>(() => ToonYamlConverter.FromYaml(yaml.ToString()));
        Assert.Contains("characters", ex.Message);

        // Within ten times the length of the document, aliases expand
        var small = "s: &a " + new string('A', 1_000) + "\nl:\n" + string.Concat(Enumerable.Repeat("  - *a\n", 50));
        Assert.Equal(50, ToonYamlConverter.FromYaml(small).Root["l"]!.AsArray().Count);
    }

    [Theory]
    [InlineData(100_000, "_")]
    [InlineData(100_000, "0_")]
    public void LongDigitStrings_AreQuoted_InLinearTime(int count, string unit)
    {
        var text = "1" + string.Concat(Enumerable.Repeat(unit, count)) + "x";
        var document = new ToonDocument(new ToonObject { ["a"] = new ToonString(text) });

        string yaml = "";
        AssertFast(() => yaml = ToonYamlConverter.ToYaml(document));
        Assert.StartsWith("a: \"1", yaml);
        Assert.Equal(text, ToonYamlConverter.FromYaml(yaml).Root["a"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("<<")]
    [InlineData("=")]
    [InlineData("\uFEFFid")]
    public void KeysAndValuesYaml11ReadersWouldChange_AreQuoted(string text)
    {
        var document = new ToonDocument(new ToonObject { [text] = new ToonObject { ["x"] = new ToonString(text) } });

        var yaml = ToonYamlConverter.ToYaml(document);

        Assert.StartsWith("\"", yaml);
        var back = ToonYamlConverter.FromYaml(yaml).Root;
        Assert.Equal(text, Assert.Single(back.AsObject().Properties).Key);
        Assert.Equal(text, back[text]!["x"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("a: 1\n!!str a: 2")]
    [InlineData("!!str a: 1\na: 2")]
    [InlineData("? !!str a\n: 1\na: 2")]
    public void KeysThatDifferOnlyInTheirTag_AreDuplicates(string yaml)
    {
        var ex = Assert.ThrowsAny<YamlDotNet.Core.YamlException>(() => ToonYamlConverter.FromYaml(yaml));
        Assert.Contains("Duplicate key", ex.Message);
    }

    [Fact]
    public void TagErrors_DoNotRepeatTheValue()
    {
        var ex = Assert.ThrowsAny<YamlDotNet.Core.YamlException>(() => ToonYamlConverter.FromYaml("a: !!bool jane@example.com"));

        Assert.Contains("!!bool", ex.Message);
        Assert.DoesNotContain("jane", ex.ToString());
    }

    #endregion
}
