using System.Text.Json.Nodes;
using Toon.Format;
using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Extensions.Json;
using ToonNetEncoder = ToonNet.Core.Encoding.ToonEncoder;

namespace ToonNet.Comparison;

/// <summary>
///     Checks that ToonNet and the official library (Toon.Format) agree: on the spec conformance fixtures, on random
///     JSON documents and on typed data.
/// </summary>
public static class Parity
{
    /// <summary>
    ///     Serializes <paramref name="value"/> with both libraries and checks that the text is the same and that each
    ///     reads it back to the same value.
    /// </summary>
    /// <returns>The text.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the libraries disagree.</exception>
    public static string CheckTyped<T>(string name, T value)
    {
        var ours = ToonSerializer.Serialize(value);
        var theirs = ToonEncoder.Encode(value);

        if (ours != theirs)
        {
            throw new InvalidOperationException($"{name}: ToonNet and Toon.Format write different text\n{ours}\n---\n{theirs}");
        }

        if (ToonSerializer.Serialize(ToonDecoder.Decode<T>(ours)) != ours || ToonEncoder.Encode(ToonSerializer.Deserialize<T>(ours)) != ours)
        {
            throw new InvalidOperationException($"{name}: the libraries read the text back to different values");
        }

        return ours;
    }

    /// <summary>Runs every check and prints a report.</summary>
    /// <returns>Whether the libraries agree everywhere they are expected to.</returns>
    public static bool Report(int fuzzDocuments)
    {
        var ok = true;
        var fixtures = FindFixtures();

        Console.WriteLine("Spec v4.4 conformance fixtures");
        Console.WriteLine($"  ToonNet:     {RunFixtures(fixtures, ToonNetEncode, ToonNetDecode)}");
        Console.WriteLine($"  Toon.Format: {RunFixtures(fixtures, ToonFormatEncode, ToonFormatDecode)}");
        Console.WriteLine();

        var (documents, encodeDifferences, decodeDifferences) = Fuzz(fuzzDocuments);
        Console.WriteLine($"Random JSON documents: {documents}");
        Console.WriteLine($"  different encodings:               {encodeDifferences}");
        Console.WriteLine($"  other library's text read wrongly: {decodeDifferences}");
        ok &= encodeDifferences == 0 && decodeDifferences == 0;
        Console.WriteLine();

        Console.WriteLine("Typed data (same text, read back to the same values)");

        foreach (var (name, check) in new (string, Action)[]
                 {
                     ("Employees", () => CheckTyped("Employees", Datasets.Employees(100))),
                     ("Orders", () => CheckTyped("Orders", Datasets.Orders(100))),
                     ("Products", () => CheckTyped("Products", Datasets.Products(100))),
                     ("Servers", () => CheckTyped("Servers", Datasets.Servers(100)))
                 })
        {
            try
            {
                check();
                Console.WriteLine($"  {name}: same");
            }
            catch (InvalidOperationException ex)
            {
                ok = false;
                Console.WriteLine($"  {name}: DIFFERENT\n{ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Known differences: enum mapping, which spec §3 leaves to implementations");
        Show("enum", new { Status = DayOfWeek.Friday });

        Console.WriteLine();
        Console.WriteLine("Known differences: Toon.Format loses precision, so decode(encode(x)) != x (spec §2 requires equality)");
        Show("decimal with 19 digits", new { Amount = 12345678901234567.89m });
        Show("decimal beyond 1e21", new { Amount = 123456789012345678901234567m });
        Show("ulong.MaxValue", new { Max = ulong.MaxValue });
        return ok;
    }

    private static void Show<T>(string name, T value)
    {
        Console.WriteLine($"  {name}:");
        Console.WriteLine($"    ToonNet:     {ToonSerializer.Serialize(value)}");
        Console.WriteLine($"    Toon.Format: {ToonEncoder.Encode(value)}");
    }

    #region Fixtures

    private sealed record Fixture(string Category, JsonNode Test);

    public static string FindFixtures()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, "tests", "ToonNet.Tests", "SpecCompliance", "Fixtures", "v4.4");

            if (Directory.Exists(path))
            {
                return path;
            }
        }

        throw new DirectoryNotFoundException("The spec fixtures were not found; run from the repository.");
    }

    /// <summary>Runs the fixtures with ToonNet; returns the passed and total counts.</summary>
    public static (int Passed, int Total) RunToonNetFixtures(string root) => CountFixtures(root, ToonNetEncode, ToonNetDecode);

    /// <summary>Runs the fixtures with Toon.Format; returns the passed and total counts.</summary>
    public static (int Passed, int Total) RunToonFormatFixtures(string root) => CountFixtures(root, ToonFormatEncode, ToonFormatDecode);

    private static string RunFixtures(string root, Func<JsonNode?, JsonNode?, string> encode, Func<string, JsonNode?, JsonNode?> decode)
    {
        var (passed, total) = CountFixtures(root, encode, decode);
        return $"{passed}/{total} pass";
    }

    private static (int Passed, int Total) CountFixtures(string root, Func<JsonNode?, JsonNode?, string> encode,
                                                         Func<string, JsonNode?, JsonNode?> decode)
    {
        int passed = 0, total = 0;

        foreach (var category in new[] { "encode", "decode" })
        {
            foreach (var file in Directory.GetFiles(Path.Combine(root, category), "*.json").Order(StringComparer.Ordinal))
            {
                foreach (var test in JsonNode.Parse(File.ReadAllText(file))!["tests"]!.AsArray())
                {
                    total++;
                    var shouldError = test!["shouldError"]?.GetValue<bool>() ?? false;

                    try
                    {
                        var conforms = category == "encode"
                            ? encode(test["input"], test["options"]) == test["expected"]?.GetValue<string>()
                            : DeepEqualsOrdered(decode(test["input"]!.GetValue<string>(), test["options"]), test["expected"]);
                        passed += !shouldError && conforms ? 1 : 0;
                    }
                    catch (Exception) when (shouldError)
                    {
                        passed++;
                    }
                    catch (Exception)
                    {
                        // a failed case
                    }
                }
            }
        }

        return (passed, total);
    }

    private static string ToonNetEncode(JsonNode? input, JsonNode? options)
    {
        var toonOptions = new ToonOptions();

        if (options?["indentSize"] is { } indent)
        {
            toonOptions.IndentSize = indent.GetValue<int>();
        }

        if (options?["delimiter"] is { } delimiter)
        {
            toonOptions.Delimiter = delimiter.GetValue<string>()[0];
        }

        return new ToonNetEncoder(toonOptions).Encode(ToonJsonConverter.FromJson(input?.ToJsonString() ?? "null"));
    }

    private static JsonNode? ToonNetDecode(string input, JsonNode? options)
    {
        var toonOptions = new ToonOptions();

        if (options?["indentSize"] is { } indent)
        {
            toonOptions.IndentSize = indent.GetValue<int>();
        }

        if (options?["strict"] is { } strict)
        {
            toonOptions.StrictMode = strict.GetValue<bool>();
        }

        return JsonNode.Parse(ToonJsonConverter.ToJson(ToonDocument.Parse(input, toonOptions)));
    }

    private static string ToonFormatEncode(JsonNode? input, JsonNode? options)
    {
        var encodeOptions = new ToonEncodeOptions();

        if (options?["indentSize"] is { } indent)
        {
            encodeOptions.IndentSize = indent.GetValue<int>();
        }

        if (options?["delimiter"] is { } delimiter)
        {
            encodeOptions.Delimiter = ToDelimiter(delimiter.GetValue<string>()[0]);
        }

        return ToonEncoder.Encode(input?.DeepClone(), encodeOptions);
    }

    private static JsonNode? ToonFormatDecode(string input, JsonNode? options)
    {
        var decodeOptions = new ToonDecodeOptions();

        if (options?["indentSize"] is { } indent)
        {
            decodeOptions.IndentSize = indent.GetValue<int>();
        }

        if (options?["strict"] is { } strict)
        {
            decodeOptions.Strict = strict.GetValue<bool>();
        }

        return ToonDecoder.Decode(input, decodeOptions);
    }

    private static ToonDelimiter ToDelimiter(char delimiter) => delimiter switch
    {
        '|' => ToonDelimiter.Pipe,
        '\t' => ToonDelimiter.Tab,
        _ => ToonDelimiter.Comma
    };

    /// <summary>JSON equality where object key order matters, as it does in TOON (spec §2).</summary>
    private static bool DeepEqualsOrdered(JsonNode? a, JsonNode? b) => JsonNode.DeepEquals(a, b) && SameOrder(a, b);

    private static bool SameOrder(JsonNode? a, JsonNode? b) => (a, b) switch
    {
        (JsonObject x, JsonObject y) => x.Select(p => p.Key).SequenceEqual(y.Select(p => p.Key)) && x.All(p => SameOrder(p.Value, y[p.Key])),
        (JsonArray x, JsonArray y) => x.Count == y.Count && x.Zip(y).All(t => SameOrder(t.First, t.Second)),
        _ => true
    };

    #endregion

    #region Random documents

    private static readonly string[] Strings =
    [
        "", "a", "hello world", " lead", "trail ", "#x", "+1", "-x", "1.5", "05", "true", "null", "a,b", "a|b", "a\tb", "x:y", "[1]",
        "{a}", "quote\"d", "back\\slash", "line\nbreak", "ünïcödé", "🚀 rocket", " nbsp ", "﻿bom", "e1", "1e5", "-", "a b c",
        "key-with-dash", "2key", "__proto__", "x.y.z", "Ab_c"
    ];

    private static readonly string[] Keys = ["id", "name", "a", "b", "c", "x.y", "my key", "2k", "-k", "#k", "", "Ab_1", "ünï", "a:b", "a[1]", "k\"q"];

    /// <summary>
    ///     Encodes random JSON documents with both libraries; counts the documents encoded differently and those whose
    ///     text the other library reads to other JSON.
    /// </summary>
    public static (int Documents, int EncodeDifferences, int DecodeDifferences) Fuzz(int count)
    {
        int encodeDifferences = 0, decodeDifferences = 0;

        for (var seed = 0; seed < count; seed++)
        {
            var random = new Random(seed);
            var node = random.Next(3) == 0 ? Value(random, 0) : Object(random, 0);
            var delimiter = ",|\t"[random.Next(3)];
            var json = node?.ToJsonString() ?? "null";

            var ours = new ToonNetEncoder(new ToonOptions { Delimiter = delimiter }).Encode(ToonJsonConverter.FromJson(json));
            var theirs = ToonEncoder.Encode(node, new ToonEncodeOptions { Delimiter = ToDelimiter(delimiter) });

            if (ours != theirs)
            {
                encodeDifferences++;
                continue;
            }

            var expected = JsonNode.Parse(json);

            if (!DeepEqualsOrdered(ToonDecoder.Decode(ours), expected) || !DeepEqualsOrdered(ToonNetDecode(theirs, null), expected))
            {
                decodeDifferences++;
            }
        }

        return (count, encodeDifferences, decodeDifferences);
    }

    private static JsonNode? Value(Random random, int depth) => (depth > 3 ? random.Next(4) : random.Next(9)) switch
    {
        0 => null,
        1 => JsonValue.Create(random.Next(2) == 0),
        2 => Number(random),
        3 => JsonValue.Create(Strings[random.Next(Strings.Length)]),
        4 or 5 => Object(random, depth),
        6 => Array(random, depth),
        7 => Uniform(random, depth),
        _ => Keyed(random)
    };

    private static JsonNode Number(Random random) => random.Next(6) switch
    {
        0 => JsonValue.Create(random.Next(-1000, 1000)),
        1 => JsonValue.Create(random.NextDouble() * Math.Pow(10, random.Next(-8, 23))),
        2 => JsonValue.Create(Math.Round(random.NextDouble() * 1000, random.Next(0, 4))),
        3 => JsonValue.Create(0.1 + 0.2),
        4 => JsonNode.Parse(new[] { "1e21", "1e-7", "-0", "0.000001", "123456789012345678", "1.5000", "3.8000000000000003" }[random.Next(7)])!,
        _ => JsonValue.Create((long)random.Next() * random.Next())
    };

    private static JsonObject Object(Random random, int depth)
    {
        var obj = new JsonObject();

        for (var i = random.Next(5); i > 0; i--)
        {
            obj[Keys[random.Next(Keys.Length)]] = Value(random, depth + 1);
        }

        return obj;
    }

    private static JsonArray Array(Random random, int depth)
    {
        var array = new JsonArray();

        for (var i = random.Next(5); i > 0; i--)
        {
            array.Add(Value(random, depth + 1));
        }

        return array;
    }

    /// <summary>Objects that share a shape: the tabular form, with a nested field group half of the time.</summary>
    private static JsonArray Uniform(Random random, int depth)
    {
        var shape = Object(random, depth + 2);

        if (shape.Count == 0)
        {
            shape["id"] = 1;
        }

        if (random.Next(2) == 0)
        {
            shape["sub"] = new JsonObject();
        }

        var array = new JsonArray();

        for (var i = random.Next(1, 4); i > 0; i--)
        {
            var item = new JsonObject();

            foreach (var (key, _) in shape)
            {
                item[key] = key == "sub"
                    ? new JsonObject { ["p"] = random.Next(9), ["q"] = Strings[random.Next(Strings.Length)] }
                    : Primitive(random);
            }

            array.Add(item);
        }

        return array;
    }

    /// <summary>An object of uniform objects: the keyed tabular form when it has two entries or more.</summary>
    private static JsonObject Keyed(Random random)
    {
        var obj = new JsonObject();

        for (var i = random.Next(1, 4); i > 0; i--)
        {
            obj["e" + i + Keys[random.Next(Keys.Length)]] = new JsonObject { ["host"] = Strings[random.Next(Strings.Length)], ["port"] = random.Next(9999) };
        }

        return obj;
    }

    private static JsonNode? Primitive(Random random) => random.Next(4) switch
    {
        0 => null,
        1 => JsonValue.Create(random.Next(100)),
        2 => JsonValue.Create(random.Next(2) == 1),
        _ => JsonValue.Create(Strings[random.Next(Strings.Length)])
    };

    #endregion
}
