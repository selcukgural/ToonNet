using System.Text.Json;
using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.SpecCompliance;

/// <summary>
///     Runs the official TOON spec conformance fixtures (see Fixtures/v4.4/README.md).
/// </summary>
/// <remarks>
///     Cases are identified by file and index (<c>decode/objects/12</c>), as upstream asks. Cases listed in
///     KnownNonConformance.txt are expected to fail; <see cref="KnownNonConformance_StillFails"/> fails once such a case
///     starts passing, so the list has to shrink as conformance improves. Cases whose <c>minSpecVersion</c> is newer
///     than <see cref="TargetSpecVersion"/> are not run.
/// </remarks>
public class SpecFixtureConformanceTests
{
    private const string FixtureVersion = "v4.4";

    /// <summary>The spec version ToonNet targets; newer cases are skipped.</summary>
    private static readonly Version TargetSpecVersion = new(4, 4);

    private static readonly string FixtureRoot = Path.Combine(AppContext.BaseDirectory, "SpecCompliance", "Fixtures", FixtureVersion);

    private static readonly Lazy<IReadOnlyDictionary<string, FixtureCase>> Cases = new(LoadCases);

    private static readonly Lazy<HashSet<string>> KnownFailures = new(() =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "SpecCompliance", "KnownNonConformance.txt"))
            .Select(line => line.Split('#')[0].Trim()) // "id  # name"
            .Where(line => line.Length > 0)
            .ToHashSet(StringComparer.Ordinal));

    public static IEnumerable<object[]> ConformingCases =>
        Cases.Value.Keys.Where(id => !KnownFailures.Value.Contains(id)).Order(StringComparer.Ordinal).Select(id => new object[] { id });

    // xUnit fails a theory without data, so an empty list yields a placeholder row
    private const string NoKnownFailures = "(none)";

    public static IEnumerable<object[]> NonConformingCases =>
        KnownFailures.Value.Count == 0
            ? [[NoKnownFailures]]
            : Cases.Value.Keys.Where(id => KnownFailures.Value.Contains(id)).Order(StringComparer.Ordinal).Select(id => new object[] { id });

    [Theory]
    [MemberData(nameof(ConformingCases))]
    public void Fixture_Conforms(string id)
    {
        var failure = Run(Cases.Value[id]);

        Assert.True(failure is null, $"{id} ({Cases.Value[id].Test.GetProperty("name").GetString()}): {failure}");
    }

    [Theory]
    [MemberData(nameof(NonConformingCases))]
    public void KnownNonConformance_StillFails(string id)
    {
        if (id == NoKnownFailures)
        {
            return;
        }

        var failure = Run(Cases.Value[id]);

        Assert.True(failure is not null, $"{id} now conforms; remove it from KnownNonConformance.txt");
    }

    [Fact]
    public void KnownNonConformance_ListsOnlyExistingCases()
    {
        var unknown = KnownFailures.Value.Where(id => !Cases.Value.ContainsKey(id)).ToList();

        Assert.True(unknown.Count == 0, "Unknown ids in KnownNonConformance.txt: " + string.Join(", ", unknown));
    }

    /// <summary>
    ///     Maintenance helper: when TOONNET_WRITE_NONCONFORMANCE is set to a file path, writes every failing case id
    ///     to that file in KnownNonConformance.txt format. Does nothing otherwise.
    /// </summary>
    [Fact]
    public void WriteNonConformanceReport()
    {
        var path = Environment.GetEnvironmentVariable("TOONNET_WRITE_NONCONFORMANCE");

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var failing = Cases.Value.Where(c => Run(c.Value) is not null)
                           .OrderBy(c => c.Key, StringComparer.Ordinal)
                           .Select(c => $"{c.Key}  # {c.Value.Test.GetProperty("name").GetString()}");
        File.WriteAllLines(path, failing);
    }

    #region Fixture loading

    private sealed record FixtureCase(string Category, JsonElement Test);

    private static IReadOnlyDictionary<string, FixtureCase> LoadCases()
    {
        var cases = new Dictionary<string, FixtureCase>(StringComparer.Ordinal);

        foreach (var category in new[] { "encode", "decode" })
        {
            foreach (var file in Directory.GetFiles(Path.Combine(FixtureRoot, category), "*.json").Order(StringComparer.Ordinal))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                var fileName = Path.GetFileNameWithoutExtension(file);

                var index = 0;

                foreach (var test in doc.RootElement.GetProperty("tests").EnumerateArray())
                {
                    var id = $"{category}/{fileName}/{index++}";

                    if (test.TryGetProperty("minSpecVersion", out var minimum) && Version.Parse(minimum.GetString()!) > TargetSpecVersion)
                    {
                        continue;
                    }

                    cases.Add(id, new FixtureCase(category, test.Clone()));
                }
            }
        }

        return cases;
    }

    #endregion

    /// <summary>
    ///     The validator checks documents without building them; it must report exactly the errors parsing reports.
    /// </summary>
    [Fact]
    public void Validate_ReportsTheSameErrorsAsParse_ForEveryDecodeFixture()
    {
        var errors = 0;

        foreach (var fixture in Cases.Value.Values.Where(c => c.Category == "decode"))
        {
            var options = CreateOptions(fixture.Test);
            var input = fixture.Test.GetProperty("input").GetString()!;

            var parse = Record.Exception(() => new ToonParser(options).Parse(input));
            var validate = Record.Exception(() => new ToonParser(options).Validate(input));

            Assert.Equal(parse?.GetType(), validate?.GetType());
            Assert.Equal(parse?.Message, validate?.Message);
            errors += parse != null ? 1 : 0;
        }

        Assert.True(errors > 10, "the fixtures should include error cases");
    }

    /// <summary>
    ///     The serializer reads the value a ToonDocumentReader is on through a parser checkpoint: after reading the value
    ///     and returning to the checkpoint, the value read again and the rest of the document must be exactly what an
    ///     uninterrupted reader gives, at every value of every decode fixture.
    /// </summary>
    [Fact]
    public void ParserCheckpoint_RereadsEveryValue_ForEveryDecodeFixture()
    {
        var checkedValues = 0;

        foreach (var fixture in Cases.Value.Values.Where(c => c.Category == "decode"))
        {
            var options = CreateOptions(fixture.Test);
            var input = fixture.Test.GetProperty("input").GetString()!;

            if (Record.Exception(() => ToonDocument.Parse(input, options)) != null)
            {
                continue;
            }

            for (var k = 0; ; k++)
            {
                var expected = ReadTokens(input, options, k, checkpoint: false);

                if (expected == null)
                {
                    break; // fewer than k + 1 values
                }

                // Reading the value fully, or giving up after a few steps (as direct reading gives up)
                foreach (var steps in (int[])[int.MaxValue, 0, 1, 2, 3, 5, 8])
                {
                    var actual = ReadTokens(input, options, k, checkpoint: true, steps)!;

                    Assert.Equal(expected.Count, actual.Count);

                    for (var i = 0; i < expected.Count; i++)
                    {
                        Assert.True(expected[i] is ToonValue tree
                                        ? ToonValue.DeepEquals(tree, actual[i] as ToonValue)
                                        : Equals(expected[i], actual[i]), $"{input}\nvalue {k}, steps {steps}, token {i}: {expected[i]} != {actual[i]}");
                    }
                }

                checkedValues++;
            }
        }

        Assert.True(checkedValues > 500, $"only {checkedValues} values checked");
    }

    /// <summary>
    ///     The tokens of a document, with the value at <paramref name="target"/> (counting value starts) read as a tree:
    ///     directly, or after skipping it and returning to a checkpoint. Null when the document has fewer values.
    /// </summary>
    private static List<object>? ReadTokens(string input, ToonOptions options, int target, bool checkpoint, int steps = int.MaxValue)
    {
        using var reader = new ToonDocumentReader(input, options);
        var tokens = new List<object>();
        var values = 0;
        var found = false;

        while (reader.Read())
        {
            var type = reader.TokenType;

            if (type is not (ToonTokenType.PropertyName or ToonTokenType.EndObject or ToonTokenType.EndArray) && values++ == target)
            {
                found = true;

                if (checkpoint)
                {
                    var parser = reader.Parser;
                    var value = reader.BeginValue("test");
                    var saved = parser.Save(value);

                    try
                    {
                        ReadPartly(parser, value, ref steps);
                    }
                    catch (OperationCanceledException)
                    {
                    }

                    parser.Restore(saved);
                    tokens.Add(parser.BuildValue(value));
                    reader.EndValue();
                }
                else
                {
                    tokens.Add(reader.ReadValue());
                }

                continue;
            }

            tokens.Add(type switch
            {
                ToonTokenType.PropertyName => "name:" + reader.GetPropertyName(),
                ToonTokenType.StartArray => "array:" + reader.ArrayLength,
                ToonTokenType.String or ToonTokenType.Number => type + ":" + reader.ValueSpan.ToString(),
                _ => type.ToString()
            });
        }

        return found ? tokens : null;
    }

    /// <summary>Reads into a value with the parser's cursors, giving up (throwing) after <paramref name="steps"/> fields and items.</summary>
    private static void ReadPartly(ToonParser parser, in ToonParser.ValueRef value, ref int steps)
    {
        switch (value.NodeKind)
        {
            case ToonNodeKind.Primitive:
                return;
            case ToonNodeKind.Array:
            {
                var cursor = parser.BeginArray(value);

                while (parser.NextItem(ref cursor, out var item))
                {
                    if (steps-- == 0)
                    {
                        throw new OperationCanceledException();
                    }

                    ReadPartly(parser, item, ref steps);
                }

                return;
            }
            default:
            {
                var cursor = parser.BeginObject(value);

                while (parser.NextProperty(ref cursor, out _, out var field))
                {
                    if (steps-- == 0)
                    {
                        throw new OperationCanceledException();
                    }

                    ReadPartly(parser, field, ref steps);
                }

                return;
            }
        }
    }

    /// <summary>
    ///     UTF-8 input is decoded into a pooled buffer and parsed without a string; it must give exactly what parsing the
    ///     string gives, also when a stream delivers the bytes a few at a time, splitting multi-byte sequences.
    /// </summary>
    [Fact]
    public void Utf8Input_ParsesLikeTheString_ForEveryDecodeFixture()
    {
        foreach (var fixture in Cases.Value.Values.Where(c => c.Category == "decode"))
        {
            var options = CreateOptions(fixture.Test);
            var input = fixture.Test.GetProperty("input").GetString()!;
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);

            var expected = Describe(() => ToonDocument.Parse(input, options));

            Assert.Equal(expected, Describe(() => ToonDocument.Parse(bytes, options)));
            Assert.Equal(expected, Describe(() => ToonDocument.Parse(input.AsSpan(), options)));
            Assert.Equal(expected, Describe(() => new ToonParser(options).ParseFromStreamAsync(new TrickleStream(bytes)).GetAwaiter().GetResult()));
        }

        static string Describe(Func<ToonDocument> parse)
        {
            try
            {
                return ToJson(parse().Root);
            }
            catch (ToonException ex)
            {
                return ex.GetType().Name + ": " + ex.Message;
            }
        }
    }

    /// <summary>
    ///     ToonDocumentWriter writes the canonical layout: writing each encode fixture's data value by value, in the forms
    ///     the encoder picks, must give exactly the encoder's text.
    /// </summary>
    [Fact]
    public void DocumentWriter_WritesLikeTheEncoder_ForEveryEncodeFixture()
    {
        var written = 0;

        foreach (var fixture in Cases.Value.Values.Where(c => c.Category == "encode"))
        {
            var test = fixture.Test;

            if (test.TryGetProperty("shouldError", out var se) && se.GetBoolean())
            {
                continue;
            }

            var options = CreateOptions(test);
            var value = FromJson(test.GetProperty("input"));
            var expected = new ToonEncoder(options).Encode(new ToonDocument(value));

            var text = new StringWriter();

            using (var writer = new ToonDocumentWriter(text, options))
            {
                DocumentTreeWriter.WriteRoot(writer, value);
                Assert.True(writer.IsComplete, test.GetProperty("name").GetString());
            }

            Assert.True(expected == text.ToString(), $"{test.GetProperty("name").GetString()}\nexpected:\n{expected}\nactual:\n{text}");
            written++;
        }

        Assert.True(written > 100);
    }

    /// <summary>
    ///     ToonDocumentReader yields the data model token by token: rebuilding each decode fixture from its tokens must give
    ///     exactly what ToonDocument.Parse gives, and an invalid document the same error.
    /// </summary>
    [Fact]
    public void DocumentReader_ReadsLikeParse_ForEveryDecodeFixture()
    {
        foreach (var fixture in Cases.Value.Values.Where(c => c.Category == "decode"))
        {
            var options = CreateOptions(fixture.Test);
            var input = fixture.Test.GetProperty("input").GetString()!;

            var expected = Describe(() => ToJson(ToonDocument.Parse(input, options).Root));

            Assert.Equal(expected, Describe(() =>
            {
                using var reader = new ToonDocumentReader(input, options);
                Assert.True(reader.Read());
                var value = DocumentTreeReader.ReadValue(reader);
                Assert.False(reader.Read());
                return ToJson(value);
            }));

            // ReadValue on the root, and Skip on it, check the same document
            Assert.Equal(expected, Describe(() =>
            {
                using var reader = new ToonDocumentReader(System.Text.Encoding.UTF8.GetBytes(input), options);
                reader.Read();
                var value = reader.ReadValue();
                Assert.False(reader.Read());
                return ToJson(value);
            }));

            var skipped = Describe(() =>
            {
                using var reader = new ToonDocumentReader(input, options);
                reader.Read();
                reader.Skip();
                Assert.False(reader.Read());
                return "ok";
            });
            Assert.Equal(expected.StartsWith("Toon", StringComparison.Ordinal) ? expected : "ok", skipped);
        }

        static string Describe(Func<string> read)
        {
            try
            {
                return read();
            }
            catch (ToonException ex)
            {
                return ex.GetType().Name + ": " + ex.Message;
            }
        }
    }

    /// <summary>
    ///     DeserializeAsyncEnumerable reads a root array a batch at a time; for every decode fixture it must give the items
    ///     (or the error) reading the whole document gives, with batches of one item and of many, from a stream that
    ///     splits lines and multi-byte sequences.
    /// </summary>
    [Fact]
    public async Task AsyncEnumerable_ReadsLikeTheWholeDocument_ForEveryDecodeFixture()
    {
        foreach (var fixture in Cases.Value.Values.Where(c => c.Category == "decode"))
        {
            var options = new ToonSerializerOptions { ToonOptions = CreateOptions(fixture.Test) };
            var input = fixture.Test.GetProperty("input").GetString()!;
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var name = fixture.Test.GetProperty("name").GetString();

            // A document that is only `null` has no items
            var expected = Describe(() => Task.FromResult<List<object?>?>(ToonSerializer.Deserialize<List<object?>>(input, options) ?? []));

            foreach (var batch in new[] { 1, 1 << 20 })
            {
                var actual = Describe(async () =>
                {
                    var items = new List<object?>();

                    await foreach (var item in ToonSerializer.DeserializeAsyncEnumerable<object>(new TrickleStream(bytes, 2), options, batch))
                    {
                        items.Add(item);
                    }

                    return items;
                });

                Assert.True(expected == actual, $"{name} (batch {batch})\nexpected: {expected}\nactual:   {actual}");
            }
        }

        static string Describe(Func<Task<List<object?>?>> read)
        {
            try
            {
                return JsonSerializer.Serialize(read().GetAwaiter().GetResult());
            }
            catch (ToonException ex)
            {
                return ex.GetType().Name + ": " + ex.Message;
            }
        }

        await Task.CompletedTask;
    }

    #region Execution

    /// <summary>Runs a fixture case and returns a failure description, or null when it conforms.</summary>
    private static string? Run(FixtureCase fixture)
    {
        var test = fixture.Test;
        var shouldError = test.TryGetProperty("shouldError", out var se) && se.GetBoolean();

        try
        {
            if (fixture.Category == "encode")
            {
                var options = CreateOptions(test);
                var actual = new ToonEncoder(options).Encode(new ToonDocument(FromJson(test.GetProperty("input"))));

                if (shouldError)
                {
                    return "expected an error but encoding succeeded";
                }

                var expected = test.GetProperty("expected").GetString();
                return actual == expected ? null : $"expected:\n{expected}\nactual:\n{actual}";
            }
            else
            {
                var options = CreateOptions(test);
                var actual = new ToonParser(options).Parse(test.GetProperty("input").GetString()!).Root;

                if (shouldError)
                {
                    return "expected an error but decoding succeeded: " + ToJson(actual);
                }

                var expected = test.GetProperty("expected");
                return JsonEquals(expected, actual) ? null : $"expected: {expected.GetRawText()}\nactual:   {ToJson(actual)}";
            }
        }
        catch (Exception ex) when (shouldError && ex is ToonException)
        {
            return null;
        }
        catch (Exception ex)
        {
            return $"{ex.GetType().Name}: {ex.Message}";
        }
    }

    private static ToonOptions CreateOptions(JsonElement test)
    {
        var options = new ToonOptions();

        if (!test.TryGetProperty("options", out var json))
        {
            return options;
        }

        // "indentSize" since fixtures 4.0; "indent" in older fixtures
        if (json.TryGetProperty("indentSize", out var indent) || json.TryGetProperty("indent", out indent))
        {
            options.IndentSize = indent.GetInt32();
        }

        foreach (var option in json.EnumerateObject())
        {
            if (option.Name is not ("indentSize" or "indent" or "delimiter" or "strict"))
            {
                throw new NotSupportedException($"Unknown fixture option '{option.Name}'");
            }
        }

        if (json.TryGetProperty("delimiter", out var delimiter))
        {
            options.Delimiter = delimiter.GetString()![0];
        }

        if (json.TryGetProperty("strict", out var strict))
        {
            options.StrictMode = strict.GetBoolean();
        }

        return options;
    }

    #endregion

    #region JSON helpers

    private static ToonValue FromJson(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null   => ToonNull.Instance,
            JsonValueKind.True   => new ToonBoolean(true),
            JsonValueKind.False  => new ToonBoolean(false),
            // Keep the exact value (as ToonNet's JSON conversion does), not just a double
            JsonValueKind.Number => ToonNumber.TryParse(element.GetRawText(), out var number) ? number : new ToonNumber(element.GetDouble()),
            JsonValueKind.String => new ToonString(element.GetString()!),
            JsonValueKind.Array  => new ToonArray(element.EnumerateArray().Select(FromJson).ToList()),
            JsonValueKind.Object => new ToonObject(element.EnumerateObject().ToDictionary(p => p.Name, p => FromJson(p.Value))),
            _                    => throw new NotSupportedException(element.ValueKind.ToString())
        };
    }

    private static bool JsonEquals(JsonElement expected, ToonValue actual)
    {
        switch (expected.ValueKind)
        {
            case JsonValueKind.Null:
                return actual is ToonNull;
            case JsonValueKind.True:
            case JsonValueKind.False:
                return actual is ToonBoolean b && b.Value == expected.GetBoolean();
            case JsonValueKind.Number:
                // Compare exactly when both sides have a decimal value; double precision would hide differences
                return actual is ToonNumber n &&
                       (n.DecimalValue is { } exact && expected.TryGetDecimal(out var expectedExact)
                           ? exact == expectedExact
                           : n.Value.Equals(expected.GetDouble()));
            case JsonValueKind.String:
                return actual is ToonString s && s.Value == expected.GetString();
            case JsonValueKind.Array:
                if (actual is not ToonArray array || array.Count != expected.GetArrayLength())
                {
                    return false;
                }

                return expected.EnumerateArray().Select((item, i) => JsonEquals(item, array[i])).All(equal => equal);
            case JsonValueKind.Object:
                if (actual is not ToonObject obj)
                {
                    return false;
                }

                var expectedProps = expected.EnumerateObject().ToList();

                // Key order is significant in TOON (§2)
                return expectedProps.Select(p => p.Name).SequenceEqual(obj.Properties.Keys) &&
                       expectedProps.All(p => JsonEquals(p.Value, obj.Properties[p.Name]));
            default:
                return false;
        }
    }

    private static string ToJson(ToonValue value)
    {
        return value switch
        {
            ToonNull      => "null",
            ToonBoolean b => b.Value ? "true" : "false",
            ToonNumber n  => n.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            ToonString s  => JsonSerializer.Serialize(s.Value),
            ToonArray a   => "[" + string.Join(",", a.Items.Select(ToJson)) + "]",
            ToonObject o  => "{" + string.Join(",", o.Properties.Select(p => JsonSerializer.Serialize(p.Key) + ":" + ToJson(p.Value))) + "}",
            _             => value.ToString() ?? ""
        };
    }

    #endregion
}
