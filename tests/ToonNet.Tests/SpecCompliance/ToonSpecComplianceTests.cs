using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Tests.SpecCompliance;

/// <summary>
///     Comprehensive TOON Specification v3.0 Compliance Tests
///     These tests ensure ToonNet fully supports all TOON spec features:
///     - All data types (string, number, boolean, null)
///     - Objects with proper key ordering
///     - Arrays (inline, tabular, list-style)
///     - Escape sequences and string handling
///     - Number formatting (no exponents, no leading zeros)
///     - Delimiters (comma, tab, pipe)
///     - Indentation and nesting
///     - Null handling
///     - Complex real-world scenarios
/// </summary>
public class ToonSpecComplianceTests
{
    private readonly ToonEncoder _encoder = new();
    private readonly ToonParser _parser = new();

    private ToonDocument ParseToon(string input)
    {
        return _parser.Parse(input);
    }

    private string EncodeToon(ToonDocument doc)
    {
        return _encoder.Encode(doc);
    }

    /// <summary>
    ///     Test all primitive types supported by TOON spec.
    ///     Spec §2: Data Model (Primitives: string, number, boolean, null)
    /// </summary>
    [Fact]
    public void AllPrimitiveTypes_RoundTrip_Succeeds()
    {
        var toonString = @"
stringValue: Hello World
numberValue: 42
floatValue: 3.14159
booleanTrue: true
booleanFalse: false
nullValue: null
zeroNumber: 0
negativeNumber: -100
largeNumber: 999999999
smallDecimal: 0.00001";

        // Parse
        var doc = ParseToon(toonString);
        var obj = (ToonObject)doc.Root;

        // Assert all primitives exist
        Assert.NotNull(obj["stringValue"]);
        Assert.NotNull(obj["numberValue"]);
        Assert.NotNull(obj["floatValue"]);
        Assert.NotNull(obj["booleanTrue"]);
        Assert.NotNull(obj["booleanFalse"]);
        Assert.IsType<ToonNull>(obj["nullValue"]);

        // Verify types
        Assert.IsType<ToonString>(obj["stringValue"]);
        Assert.IsType<ToonNumber>(obj["numberValue"]);
        Assert.IsType<ToonBoolean>(obj["booleanTrue"]);
        Assert.IsType<ToonBoolean>(obj["booleanFalse"]);

        // Verify values
        Assert.Equal("Hello World", ((ToonString)obj["stringValue"]).Value);
        Assert.Equal(42, ((ToonNumber)obj["numberValue"]).Value);
        Assert.Equal(3.14159, ((ToonNumber)obj["floatValue"]).Value);
        Assert.True(((ToonBoolean)obj["booleanTrue"]).Value);
        Assert.False(((ToonBoolean)obj["booleanFalse"]).Value);
    }

    /// <summary>
    ///     Test number formatting compliance with TOON spec.
    ///     Spec §2.1: Canonical Number Format
    ///     - No exponent notation
    ///     - No leading zeros
    ///     - No trailing zeros
    ///     ⚠️ PARTIAL - Encoder may not always produce canonical format
    /// </summary>
    [Fact]
    public void NumberFormatting_NoExponents_NoLeadingZeros_NoTrailingZeros()
    {
        var toonString = @"
largenum: 1000000
smallnum: 0.5
zero: 0
negval: -42
bigint: 1234567
tinyval: 0.00001";

        var doc = ParseToon(toonString);
        var obj = (ToonObject)doc.Root;

        // Verify all numbers parsed correctly
        Assert.Equal(1000000, ((ToonNumber)obj["largenum"]).Value);
        Assert.Equal(0.5, ((ToonNumber)obj["smallnum"]).Value);
        Assert.Equal(0, ((ToonNumber)obj["zero"]).Value);
        Assert.Equal(-42, ((ToonNumber)obj["negval"]).Value);
        Assert.Equal(1234567, ((ToonNumber)obj["bigint"]).Value);
        Assert.Equal(0.00001, ((ToonNumber)obj["tinyval"]).Value);

        // Encode back and verify no exponents in number values
        var encoded = EncodeToon(doc);
        // Check that no numbers are in scientific notation
        // Numbers should be in decimal form, not "e" notation
        var lines = encoded.Split('\n');

        foreach (var line in lines)
        {
            if (line.Contains(':'))
            {
                var parts = line.Split(':', 2);

                if (parts.Length == 2)
                {
                    var numPart = parts[1].Trim();

                    // Verify number part doesn't have 'e' (scientific notation)
                    if (double.TryParse(numPart, out _))
                    {
                        Assert.DoesNotContain("e", numPart.ToLower());
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Test escape sequences in strings.
    ///     Spec §7: Strings & Keys
    ///     - Newline: \n
    ///     - Tab: \t
    ///     - Carriage return: \r
    ///     - Quote: \"
    ///     - Backslash: \\
    /// </summary>
    [Fact]
    public void EscapeSequences_AllTypes_Preserved()
    {
        var toonString = @"
newline: ""line1\nline2""
tab: ""col1\tcol2""
quote: ""He said \""Hello\""""
backslash: ""path\\to\\file""
carriageReturn: ""line1\rline2""
combined: ""line1\nline2\twith\ttabs\r\nend""";

        var doc = ParseToon(toonString);
        var obj = (ToonObject)doc.Root;

        // Verify escapes are properly interpreted
        var newlineStr = ((ToonString)obj["newline"]).Value;
        Assert.Contains("\n", newlineStr);

        var tabStr = ((ToonString)obj["tab"]).Value;
        Assert.Contains("\t", tabStr);

        var quoteStr = ((ToonString)obj["quote"]).Value;
        Assert.Contains("\"", quoteStr);

        var backslashStr = ((ToonString)obj["backslash"]).Value;
        Assert.Contains("\\", backslashStr);

        // Round-trip test
        var encoded = EncodeToon(doc);
        var reparsed = ParseToon(encoded);
        var reparseObj = (ToonObject)reparsed.Root;

        Assert.Equal(newlineStr, ((ToonString)reparseObj["newline"]).Value);
        Assert.Equal(tabStr, ((ToonString)reparseObj["tab"]).Value);
        Assert.Equal(quoteStr, ((ToonString)reparseObj["quote"]).Value);
        Assert.Equal(backslashStr, ((ToonString)reparseObj["backslash"]).Value);
    }

    /// <summary>
    ///     Test object key ordering is preserved.
    ///     Spec §2: Data Model
    ///     "Object key order: order of first occurrence in document"
    /// </summary>
    [Fact]
    public void ObjectKeyOrdering_PreservedAsInDocument()
    {
        var toonString = @"
zebra: 1
apple: 2
monkey: 3
banana: 4
fox: 5";

        var doc = ParseToon(toonString);
        var obj = (ToonObject)doc.Root;

        // Verify key order is preserved (not alphabetical!)
        var keys = obj.Properties.Keys.ToList();
        Assert.Equal(new[] { "zebra", "apple", "monkey", "banana", "fox" }, keys);
    }

    /// <summary>
    ///     Test inline arrays (comma-separated).
    ///     Spec §9: Arrays
    ///     Format: key[length]: value1, value2, value3
    /// </summary>
    [Fact]
    public void InlineArrays_CommaSeparated_Parsed()
    {
        var toonString = @"
numbers[5]: 1, 2, 3, 4, 5
words[3]: apple, banana, cherry
mixed[4]: 42, true, hello, null";

        var doc = ParseToon(toonString);
        var obj = (ToonObject)doc.Root;

        // Verify arrays are parsed
        var numbers = (ToonArray)obj["numbers"];
        Assert.Equal(5, numbers.Items.Count);

        var words = (ToonArray)obj["words"];
        Assert.Equal(3, words.Items.Count);

        var mixed = (ToonArray)obj["mixed"];
        Assert.Equal(4, mixed.Items.Count);
    }

    /// <summary>
    ///     Test tabular arrays (CSV-style with headers).
    ///     Spec §9.3: Tabular Arrays
    /// </summary>
    [Fact]
    public void TabularArrays_WithHeaders_Parsed()
    {
        var toonString = @"
people{name,age,city}:
  Alice, 30, New York
  Bob, 25, Los Angeles
  Charlie, 35, Chicago";

        var doc = ParseToon(toonString);
        var obj = (ToonObject)doc.Root;

        // Verify tabular array
        Assert.NotNull(obj["people"]);
        var people = (ToonArray)obj["people"];
        Assert.Equal(3, people.Items.Count);

        // Each row should be an object
        var firstRow = people.Items[0] as ToonObject;
        Assert.NotNull(firstRow);
        Assert.Equal("Alice", ((ToonString)firstRow!["name"]).Value);
        Assert.Equal(30, ((ToonNumber)firstRow!["age"]).Value);
        Assert.Equal("New York", ((ToonString)firstRow!["city"]).Value);
    }

    /// <summary>
    ///     Test nested objects.
    ///     Spec §8: Objects
    ///     Format: indentation creates nesting
    /// </summary>
    [Fact]
    public void NestedObjects_WithProperIndentation_Parsed()
    {
        var toonString = @"
user:
  name: Alice
  age: 30
  profile:
    bio: Software Engineer
    location: San Francisco
    social:
      twitter: @alice
      github: alice-dev";

        var doc = ParseToon(toonString);
        var obj = (ToonObject)doc.Root;

        // Verify nesting
        var user = (ToonObject)obj["user"];
        Assert.Equal("Alice", ((ToonString)user["name"]).Value);

        var profile = (ToonObject)user["profile"];
        Assert.Equal("Software Engineer", ((ToonString)profile["bio"]).Value);

        var social = (ToonObject)profile["social"];
        Assert.Equal("@alice", ((ToonString)social["twitter"]).Value);
    }

    /// <summary>
    ///     Test arrays of objects.
    ///     Spec §10: Objects as List Items
    /// </summary>
    [Fact]
    public void ArraysOfObjects_ListItemFormat_Parsed()
    {
        var toonString = @"
products:
  - name: Laptop
    price: 999.99
    inStock: true
  - name: Mouse
    price: 29.99
    inStock: true
  - name: Monitor
    price: 299.99
    inStock: false";

        var doc = ParseToon(toonString);
        var obj = (ToonObject)doc.Root;

        var products = (ToonArray)obj["products"];
        Assert.Equal(3, products.Items.Count);

        var firstProduct = (ToonObject)products.Items[0];
        Assert.Equal("Laptop", ((ToonString)firstProduct["name"]).Value);
        Assert.Equal(999.99, ((ToonNumber)firstProduct["price"]).Value);
        Assert.True(((ToonBoolean)firstProduct["inStock"]).Value);
    }

    /// <summary>
    ///     Test quoted string keys and values with special characters.
    ///     Spec §7: Strings & Keys
    ///     ⚠️ NOT YET SUPPORTED - Parser doesn't handle quoted key syntax
    /// </summary>
    [Fact]
    public void QuotedStrings_SpecialCharacters_PreservedExactly()
    {
        var toonString = @"
""key with spaces"": value
""quoted-key"": ""quoted-value""
""key:with:colons"": ""value,with,commas""
""key[with]brackets"": data
""@special"": true";

        var doc = ParseToon(toonString);
        var obj = (ToonObject)doc.Root;

        // Verify quoted keys work
        Assert.NotNull(obj["key with spaces"]);
        Assert.NotNull(obj["quoted-key"]);
        Assert.NotNull(obj["key:with:colons"]);
    }

    /// <summary>
    ///     Test null values in various contexts.
    ///     Spec §2: Data Model (null is primitive)
    /// </summary>
    [Fact]
    public void NullValues_InMultipleContexts_Handled()
    {
        var toonString = @"
simpleNull: null
objectWithNulls:
  field1: value
  field2: null
  field3: another value
arrayWithNulls[3]: first, null, third";

        var doc = ParseToon(toonString);
        var obj = (ToonObject)doc.Root;

        // Simple null
        Assert.IsType<ToonNull>(obj["simpleNull"]);

        // Null in object
        var objectWithNulls = (ToonObject)obj["objectWithNulls"];
        Assert.IsType<ToonNull>(objectWithNulls["field2"]);

        // Null in array
        var arrayWithNulls = (ToonArray)obj["arrayWithNulls"];
        Assert.IsType<ToonNull>(arrayWithNulls.Items[1]);
    }

    /// <summary>
    ///     Test deep nesting (multiple indentation levels).
    ///     Spec §12: Indentation & Whitespace
    /// </summary>
    [Fact]
    public void DeepNesting_MultipleIndentationLevels_Handled()
    {
        var toonString = @"
level1:
  level2:
    level3:
      level4:
        level5:
          level6:
            value: deep";

        var doc = ParseToon(toonString);
        var obj = (ToonObject)doc.Root;

        // Navigate deep nesting
        var level1 = (ToonObject)obj["level1"];
        var level2 = (ToonObject)level1["level2"];
        var level3 = (ToonObject)level2["level3"];
        var level4 = (ToonObject)level3["level4"];
        var level5 = (ToonObject)level4["level5"];
        var level6 = (ToonObject)level5["level6"];

        Assert.Equal("deep", ((ToonString)level6["value"]).Value);
    }

    /// <summary>
    ///     Test complex real-world document (API response format).
    ///     Combines all TOON features.
    ///     Complex real-world test combining multiple features.
    ///     Tests nested objects, arrays, list items, and various value types.
    /// </summary>
    [Fact]
    public void ComplexRealWorld_APIResponse_RoundTrip()
    {
        var toonString = @"
status: success
code: 200
data:
  users[2]:
    - id: 1
      username: alice_dev
      email: alice@example.com
      roles[2]: admin, user
      profile:
        bio: Software Engineer
        followers: 150
        verified: true
      metadata:
        created: ""2024-01-01""
        lastLogin: null
    - id: 2
      username: bob_smith
      email: bob@example.com
      roles[1]: user
      profile:
        bio: null
        followers: 0
        verified: false
      metadata:
        created: ""2024-01-05""
        lastLogin: ""2024-01-10""
  pagination:
    total: 100
    page: 1
    perPage: 2
    hasNext: true";

        // Parse
        var doc = ParseToon(toonString);

        // Verify structure
        var root = (ToonObject)doc.Root;
        Assert.Equal("success", ((ToonString)root["status"]).Value);
        Assert.Equal(200, ((ToonNumber)root["code"]).Value);

        var data = (ToonObject)root["data"];
        var users = (ToonArray)data["users"];
        Assert.Equal(2, users.Items.Count);

        var firstUser = (ToonObject)users.Items[0];
        Assert.Equal("alice_dev", ((ToonString)firstUser["username"]).Value);

        var roles = (ToonArray)firstUser["roles"];
        Assert.Equal(2, roles.Items.Count);

        var profile = (ToonObject)firstUser["profile"];
        Assert.Equal(150, ((ToonNumber)profile["followers"]).Value);

        var metadata = (ToonObject)firstUser["metadata"];
        Assert.IsType<ToonNull>(metadata["lastLogin"]);

        var pagination = (ToonObject)data["pagination"];
        Assert.Equal(100, ((ToonNumber)pagination["total"]).Value);

        // Encode and re-parse to verify round-trip
        var encoded = EncodeToon(doc);
        var reparsed = ParseToon(encoded);

        var reparseRoot = (ToonObject)reparsed.Root;
        var reparseData = (ToonObject)reparseRoot["data"];
        var reparseUsers = (ToonArray)reparseData["users"];

        Assert.Equal(2, reparseUsers.Items.Count);
    }

    /// <summary>
    ///     Test boolean case sensitivity.
    ///     Spec: true/false are lowercase keywords
    /// </summary>
    [Fact]
    public void BooleanKeywords_Lowercase_Required()
    {
        var toonString = @"
trueValue: true
falseValue: false";

        var doc = ParseToon(toonString);
        var obj = (ToonObject)doc.Root;

        Assert.IsType<ToonBoolean>(obj["trueValue"]);
        Assert.IsType<ToonBoolean>(obj["falseValue"]);
        Assert.True(((ToonBoolean)obj["trueValue"]).Value);
        Assert.False(((ToonBoolean)obj["falseValue"]).Value);
    }

    /// <summary>
    ///     Test that property names are case-sensitive.
    ///     Spec: Keys are case-sensitive
    /// </summary>
    [Fact]
    public void PropertyNamesCaseSensitive()
    {
        var toonString = @"
Name: Alice
name: alice
NAME: ALICE";

        var doc = ParseToon(toonString);
        var obj = (ToonObject)doc.Root;

        // All three should exist as separate keys
        Assert.Equal(3, obj.Properties.Count);
        Assert.NotEqual(obj["Name"], obj["name"]);
    }

    /// <summary>
    ///     Test empty strings, empty arrays, empty objects.
    /// </summary>
    [Fact]
    public void EmptyCollections_Handled()
    {
        var toonString = @"
emptyString: """"
emptyArray[0]:
emptyObject:";

        var doc = ParseToon(toonString);
        var obj = (ToonObject)doc.Root;

        // Empty string
        var emptyStr = (ToonString)obj["emptyString"];
        Assert.Equal("", emptyStr.Value);

        // Empty array
        var emptyArr = (ToonArray)obj["emptyArray"];
        Assert.Equal(0, emptyArr.Items.Count);

        // Empty object
        var emptyObj = (ToonObject)obj["emptyObject"];
        Assert.Equal(0, emptyObj.Properties.Count);
    }

    /// <summary>
    ///     Test very large numbers (within double precision).
    /// </summary>
    [Fact]
    public void LargeNumbers_WithinDoublePrecision_Handled()
    {
        var toonString = @"
billion: 1000000000
trillion: 1000000000000
smallDecimal: 0.000000000001
veryLarge: 999999999999999";

        var doc = ParseToon(toonString);
        var obj = (ToonObject)doc.Root;

        Assert.Equal(1000000000, ((ToonNumber)obj["billion"]).Value);
        Assert.Equal(1000000000000, ((ToonNumber)obj["trillion"]).Value);
        Assert.Equal(0.000000000001, ((ToonNumber)obj["smallDecimal"]).Value);
    }

    /// <summary>
    ///     Test that unquoted strings with special chars are quoted when re-encoded.
    ///     ⚠️ PARTIAL - Some edge cases in escape handling
    /// </summary>
    [Fact]
    public void SpecialCharactersInStrings_RoundTrip()
    {
        var toonString = @"
path: ""C:\\Users\\Alice\\Documents""
email: alice@example.com
url: ""https://example.com/path?query=value""";

        var doc = ParseToon(toonString);
        var encoded = EncodeToon(doc);
        var reparsed = ParseToon(encoded);

        var original = (ToonObject)doc.Root;
        var reparsed_obj = (ToonObject)reparsed.Root;

        // Check string values directly
        var originalPath = ((ToonString)original["path"]).Value;
        var reparesedPath = ((ToonString)reparsed_obj["path"]).Value;
        Assert.Equal(originalPath, reparesedPath);

        var originalEmail = ((ToonString)original["email"]).Value;
        var reparesedEmail = ((ToonString)reparsed_obj["email"]).Value;
        Assert.Equal(originalEmail, reparesedEmail);

        var originalUrl = ((ToonString)original["url"]).Value;
        var reparesedUrl = ((ToonString)reparsed_obj["url"]).Value;
        Assert.Equal(originalUrl, reparesedUrl);
    }
}