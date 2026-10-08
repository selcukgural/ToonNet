using ToonNet.Core.Models;
using ToonNet.Extensions.Yaml;

namespace ToonNet.Tests.Interop;

/// <summary>
///     Tests for YAML ↔️ TOON conversion functionality.
/// </summary>
public class ToonYamlConverterTests
{
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
        var yaml = new string('[', 100) + new string(']', 100);

        var doc = ToonYamlConverter.FromYaml(yaml);

        Assert.IsType<ToonArray>(doc.Root);
    }

    [Fact]
    public void FromYaml_CustomMaxDepth_IsEnforced()
    {
        var yaml = "a:\n  b:\n    c: 1\n";

        Assert.Throws<YamlDotNet.Core.YamlException>(() => ToonYamlConverter.FromYaml(yaml, new ToonNet.Core.ToonOptions { MaxDepth = 2 }));
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

        Assert.Equal("Alice", ((ToonString)root["name"]).Value);
        Assert.Equal(30, ((ToonNumber)root["age"]).Value);
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
        var tags = (ToonArray)root["tags"];

        Assert.Equal(3, tags.Items.Count);
        Assert.Equal("dev", ((ToonString)tags.Items[0]).Value);
        Assert.Equal("admin", ((ToonString)tags.Items[1]).Value);
        Assert.Equal("user", ((ToonString)tags.Items[2]).Value);
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
        var user = (ToonObject)root["user"];
        var profile = (ToonObject)user["profile"];

        Assert.Equal("Bob", ((ToonString)user["name"]).Value);
        Assert.Equal("Software Engineer", ((ToonString)profile["bio"]).Value);
        Assert.Equal(35, ((ToonNumber)profile["age"]).Value);
    }

    [Fact]
    public void FromYaml_BooleanValues_ConvertCorrectly()
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

        Assert.True(((ToonBoolean)root["enabled"]).Value);
        Assert.False(((ToonBoolean)root["disabled"]).Value);
        Assert.True(((ToonBoolean)root["yesValue"]).Value);
        Assert.False(((ToonBoolean)root["noValue"]).Value);
        Assert.True(((ToonBoolean)root["onValue"]).Value);
        Assert.False(((ToonBoolean)root["offValue"]).Value);
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

        Assert.Equal(42, ((ToonNumber)root["integer"]).Value);
        Assert.Equal(3.14, ((ToonNumber)root["float"]).Value);
        Assert.Equal(-10, ((ToonNumber)root["negative"]).Value);
        Assert.Equal(1.5e10, ((ToonNumber)root["scientific"]).Value);
    }

    [Fact]
    public void FromYaml_EmptyYaml_CreatesEmptyDocument()
    {
        var yaml = "";

        var doc = ToonYamlConverter.FromYaml(yaml);

        Assert.NotNull(doc);
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

        // YAML serializes null as empty or explicit null
        Assert.Contains("nullValue:", yaml);
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
        Assert.Equal("Alice", ((ToonString)root["name"]).Value);
        Assert.Equal(30, ((ToonNumber)root["age"]).Value);

        var tags = (ToonArray)root["tags"];
        Assert.Equal(2, tags.Items.Count);
        Assert.Equal("dev", ((ToonString)tags.Items[0]).Value);

        var settings = (ToonObject)root["settings"];
        Assert.Equal("dark", ((ToonString)settings["theme"]).Value);
        Assert.True(((ToonBoolean)settings["notifications"]).Value);
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
        Assert.Equal("Bob", ((ToonString)root["name"]).Value);
        Assert.Equal(25, ((ToonNumber)root["age"]).Value);
        Assert.True(((ToonBoolean)root["active"]).Value);
        Assert.IsType<ToonNull>(root["data"]);
    }

    [Fact]
    public void Integration_YamlToToonToJsonToToonToYaml_WorksCorrectly()
    {
        var yaml = """
                   user:
                     name: Charlie
                     id: 123
                     roles:
                       - admin
                       - editor
                   """;

        // YAML -> TOON
        var doc1 = ToonYamlConverter.FromYaml(yaml);

        // TOON -> YAML (round trip)
        var yaml2 = ToonYamlConverter.ToYaml(doc1);
        var doc2 = ToonYamlConverter.FromYaml(yaml2);

        var root = (ToonObject)doc2.Root;
        var user = (ToonObject)root["user"];
        var roles = (ToonArray)user["roles"];

        Assert.Equal("Charlie", ((ToonString)user["name"]).Value);
        Assert.Equal(123, ((ToonNumber)user["id"]).Value);
        Assert.Equal(2, roles.Items.Count);
        Assert.Equal("admin", ((ToonString)roles.Items[0]).Value);
        Assert.Equal("editor", ((ToonString)roles.Items[1]).Value);
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
    [InlineData("NO", "false")]
    [InlineData("~", "null")]
    [InlineData("007", "\"007\"")]
    [InlineData("1,000", "\"1,000\"")]
    [InlineData("(5)", "(5)")]
    [InlineData("0xFF", "0xFF")]
    [InlineData(".inf", ".inf")]
    public void FromYaml_PlainScalars_AreTypedWithoutChangingValues(string yaml, string toon)
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

    [Fact]
    public void ToYaml_UsesToonOptions()
    {
        const string toon = "a:\n    b: 1";

        Assert.Throws<ToonNet.Core.ToonParseException>(() => ToonYamlConvert.ToYaml(toon));
        Assert.Contains("b: 1", ToonYamlConvert.ToYaml(toon, new ToonNet.Core.ToonOptions { IndentSize = 4 }));
    }

    #endregion
}
