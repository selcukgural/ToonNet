using System.Text.Json;
using ToonNet.Core.Encoding;
using ToonNet.Core.Interop;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;
using Xunit;

namespace ToonNet.Tests.Interop;

/// <summary>
/// Tests for JSON ↔️ TOON conversion functionality.
/// </summary>
public class ToonJsonConverterTests
{
    #region JSON to TOON Tests

    [Fact]
    public void FromJson_SimpleObject_ConvertsCorrectly()
    {
        var json = """{"name":"Alice","age":30}""";
        
        var doc = ToonJsonConverter.FromJson(json);
        var root = (ToonObject)doc.Root;
        
        Assert.Equal("Alice", ((ToonString)root["name"]).Value);
        Assert.Equal(30, ((ToonNumber)root["age"]).Value);
    }

    [Fact]
    public void FromJson_WithArray_ConvertsCorrectly()
    {
        var json = """{"tags":["dev","admin","user"]}""";
        
        var doc = ToonJsonConverter.FromJson(json);
        var root = (ToonObject)doc.Root;
        var tags = (ToonArray)root["tags"];
        
        Assert.Equal(3, tags.Items.Count);
        Assert.Equal("dev", ((ToonString)tags.Items[0]).Value);
        Assert.Equal("admin", ((ToonString)tags.Items[1]).Value);
        Assert.Equal("user", ((ToonString)tags.Items[2]).Value);
    }

    [Fact]
    public void FromJson_NestedObject_ConvertsCorrectly()
    {
        var json = """
        {
            "user": {
                "name": "Bob",
                "profile": {
                    "email": "bob@example.com",
                    "verified": true
                }
            }
        }
        """;
        
        var doc = ToonJsonConverter.FromJson(json);
        var root = (ToonObject)doc.Root;
        var user = (ToonObject)root["user"];
        var profile = (ToonObject)user["profile"];
        
        Assert.Equal("Bob", ((ToonString)user["name"]).Value);
        Assert.Equal("bob@example.com", ((ToonString)profile["email"]).Value);
        Assert.True(((ToonBoolean)profile["verified"]).Value);
    }

    [Fact]
    public void FromJson_AllPrimitiveTypes_ConvertsCorrectly()
    {
        var json = """
        {
            "string": "hello",
            "number": 42.5,
            "boolean": true,
            "null_value": null
        }
        """;
        
        var doc = ToonJsonConverter.FromJson(json);
        var root = (ToonObject)doc.Root;
        
        Assert.IsType<ToonString>(root["string"]);
        Assert.IsType<ToonNumber>(root["number"]);
        Assert.IsType<ToonBoolean>(root["boolean"]);
        Assert.IsType<ToonNull>(root["null_value"]);
    }

    [Fact]
    public void FromJson_ArrayOfObjects_ConvertsCorrectly()
    {
        var json = """
        {
            "users": [
                {"id": 1, "name": "Alice"},
                {"id": 2, "name": "Bob"}
            ]
        }
        """;
        
        var doc = ToonJsonConverter.FromJson(json);
        var root = (ToonObject)doc.Root;
        var users = (ToonArray)root["users"];
        
        Assert.Equal(2, users.Items.Count);
        
        var user1 = (ToonObject)users.Items[0];
        Assert.Equal(1, ((ToonNumber)user1["id"]).Value);
        Assert.Equal("Alice", ((ToonString)user1["name"]).Value);
    }

    [Fact]
    public void FromJson_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ToonJsonConverter.FromJson((string)null!));
    }

    [Fact]
    public void FromJson_InvalidJson_ThrowsJsonException()
    {
        var invalidJson = "{invalid json";
        
        // JsonReaderException derives from JsonException
        var ex = Assert.ThrowsAny<JsonException>(() => ToonJsonConverter.FromJson(invalidJson));
        Assert.NotNull(ex);
    }

    #endregion

    #region TOON to JSON Tests

    [Fact]
    public void ToJson_SimpleObject_ConvertsCorrectly()
    {
        var toonObj = new ToonObject();
        toonObj["name"] = new ToonString("Alice");
        toonObj["age"] = new ToonNumber(30);
        
        var doc = new ToonDocument(toonObj);
        var json = ToonJsonConverter.ToJson(doc);
        
        var parsed = JsonDocument.Parse(json);
        Assert.Equal("Alice", parsed.RootElement.GetProperty("name").GetString());
        Assert.Equal(30, parsed.RootElement.GetProperty("age").GetDouble());
    }

    [Fact]
    public void ToJson_WithArray_ConvertsCorrectly()
    {
        var toonObj = new ToonObject();
        var tags = new ToonArray(new List<ToonValue>
        {
            new ToonString("dev"),
            new ToonString("admin"),
            new ToonString("user")
        });
        toonObj["tags"] = tags;
        
        var doc = new ToonDocument(toonObj);
        var json = ToonJsonConverter.ToJson(doc);
        
        var parsed = JsonDocument.Parse(json);
        var jsonTags = parsed.RootElement.GetProperty("tags");
        Assert.Equal(3, jsonTags.GetArrayLength());
        Assert.Equal("dev", jsonTags[0].GetString());
    }

    [Fact]
    public void ToJson_NestedObject_ConvertsCorrectly()
    {
        var profile = new ToonObject();
        profile["email"] = new ToonString("bob@example.com");
        profile["verified"] = new ToonBoolean(true);
        
        var user = new ToonObject();
        user["name"] = new ToonString("Bob");
        user["profile"] = profile;
        
        var root = new ToonObject();
        root["user"] = user;
        
        var doc = new ToonDocument(root);
        var json = ToonJsonConverter.ToJson(doc);
        
        var parsed = JsonDocument.Parse(json);
        var jsonUser = parsed.RootElement.GetProperty("user");
        var jsonProfile = jsonUser.GetProperty("profile");
        
        Assert.Equal("Bob", jsonUser.GetProperty("name").GetString());
        Assert.Equal("bob@example.com", jsonProfile.GetProperty("email").GetString());
        Assert.True(jsonProfile.GetProperty("verified").GetBoolean());
    }

    [Fact]
    public void ToJson_AllPrimitiveTypes_ConvertsCorrectly()
    {
        var obj = new ToonObject();
        obj["string"] = new ToonString("hello");
        obj["number"] = new ToonNumber(42.5);
        obj["boolean"] = new ToonBoolean(true);
        obj["null_value"] = ToonNull.Instance;
        
        var doc = new ToonDocument(obj);
        var json = ToonJsonConverter.ToJson(doc);
        
        var parsed = JsonDocument.Parse(json);
        Assert.Equal("hello", parsed.RootElement.GetProperty("string").GetString());
        Assert.Equal(42.5, parsed.RootElement.GetProperty("number").GetDouble());
        Assert.True(parsed.RootElement.GetProperty("boolean").GetBoolean());
        Assert.Equal(JsonValueKind.Null, parsed.RootElement.GetProperty("null_value").ValueKind);
    }

    [Fact]
    public void ToJson_WithIndentation_FormatsCorrectly()
    {
        var obj = new ToonObject();
        obj["name"] = new ToonString("Alice");
        obj["age"] = new ToonNumber(30);
        
        var doc = new ToonDocument(obj);
        var json = ToonJsonConverter.ToJson(doc, writeIndented: true);
        
        Assert.Contains("\n", json);
        Assert.Contains("  ", json); // Check for indentation
    }

    [Fact]
    public void ToJson_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ToonJsonConverter.ToJson((ToonDocument)null!));
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_JsonToToonToJson_PreservesData()
    {
        var originalJson = """{"name":"Alice","age":30,"tags":["dev","admin"]}""";
        
        // JSON → TOON → JSON
        var toonDoc = ToonJsonConverter.FromJson(originalJson);
        var resultJson = ToonJsonConverter.ToJson(toonDoc);
        
        // Parse both and compare
        var original = JsonDocument.Parse(originalJson);
        var result = JsonDocument.Parse(resultJson);
        
        Assert.Equal(
            original.RootElement.GetProperty("name").GetString(),
            result.RootElement.GetProperty("name").GetString()
        );
        Assert.Equal(
            original.RootElement.GetProperty("age").GetDouble(),
            result.RootElement.GetProperty("age").GetDouble()
        );
    }

    [Fact]
    public void RoundTrip_ToonToJsonToToon_PreservesData()
    {
        var toonString = @"
name: Alice
age: 30
tags: dev, admin
";
        
        // TOON → JSON → TOON
        var parser = new ToonParser();
        var originalDoc = parser.Parse(toonString);
        var json = ToonJsonConverter.ToJson(originalDoc);
        var resultDoc = ToonJsonConverter.FromJson(json);
        
        // Encode both and compare
        var encoder = new ToonEncoder();
        var originalToon = encoder.Encode(originalDoc);
        var resultToon = encoder.Encode(resultDoc);
        
        // Parse both to compare structure
        var originalParsed = parser.Parse(originalToon);
        var resultParsed = parser.Parse(resultToon);
        
        var originalRoot = (ToonObject)originalParsed.Root;
        var resultRoot = (ToonObject)resultParsed.Root;
        
        Assert.Equal(
            ((ToonString)originalRoot["name"]).Value,
            ((ToonString)resultRoot["name"]).Value
        );
        Assert.Equal(
            ((ToonNumber)originalRoot["age"]).Value,
            ((ToonNumber)resultRoot["age"]).Value
        );
    }

    [Fact]
    public void RoundTrip_ComplexNestedStructure_PreservesData()
    {
        var json = """
        {
            "company": "TechCorp",
            "employees": [
                {
                    "id": 1,
                    "name": "Alice",
                    "roles": ["admin", "developer"],
                    "profile": {
                        "email": "alice@techcorp.com",
                        "verified": true
                    }
                },
                {
                    "id": 2,
                    "name": "Bob",
                    "roles": ["developer"],
                    "profile": {
                        "email": "bob@techcorp.com",
                        "verified": false
                    }
                }
            ]
        }
        """;
        
        // JSON → TOON → JSON
        var toonDoc = ToonJsonConverter.FromJson(json);
        var encoder = new ToonEncoder();
        var toonString = encoder.Encode(toonDoc);
        
        // Verify TOON format is readable
        Assert.Contains("company:", toonString);
        Assert.Contains("employees", toonString);
        
        // Convert back to JSON
        var parser = new ToonParser();
        var reparsed = parser.Parse(toonString);
        var resultJson = ToonJsonConverter.ToJson(reparsed);
        
        // Verify structure is preserved
        var result = JsonDocument.Parse(resultJson);
        Assert.Equal("TechCorp", result.RootElement.GetProperty("company").GetString());
        Assert.Equal(2, result.RootElement.GetProperty("employees").GetArrayLength());
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void FromJson_EmptyObject_ConvertsCorrectly()
    {
        var json = "{}";
        
        var doc = ToonJsonConverter.FromJson(json);
        var root = (ToonObject)doc.Root;
        
        Assert.Empty(root.Properties);
    }

    [Fact]
    public void FromJson_EmptyArray_ConvertsCorrectly()
    {
        var json = """{"items":[]}""";
        
        var doc = ToonJsonConverter.FromJson(json);
        var root = (ToonObject)doc.Root;
        var items = (ToonArray)root["items"];
        
        Assert.Empty(items.Items);
    }

    [Fact]
    public void ToJson_EmptyObject_ConvertsCorrectly()
    {
        var obj = new ToonObject();
        var doc = new ToonDocument(obj);
        
        var json = ToonJsonConverter.ToJson(doc);
        
        Assert.Equal("{}", json);
    }

    [Fact]
    public void ToJson_EmptyArray_ConvertsCorrectly()
    {
        var obj = new ToonObject();
        obj["items"] = new ToonArray(new List<ToonValue>());
        
        var doc = new ToonDocument(obj);
        var json = ToonJsonConverter.ToJson(doc);
        
        var parsed = JsonDocument.Parse(json);
        Assert.Equal(0, parsed.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public void FromJson_SpecialCharactersInStrings_PreservesCorrectly()
    {
        var json = """{"text":"Hello\nWorld\t\"Quoted\""}""";
        
        var doc = ToonJsonConverter.FromJson(json);
        var root = (ToonObject)doc.Root;
        
        var text = ((ToonString)root["text"]).Value;
        Assert.Contains("\n", text);
        Assert.Contains("\t", text);
        Assert.Contains("\"", text);
    }

    #endregion
}
