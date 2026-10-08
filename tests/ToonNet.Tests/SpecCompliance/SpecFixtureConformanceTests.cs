using System.Text.Json;
using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Tests.SpecCompliance;

/// <summary>
///     Runs the official TOON spec conformance fixtures (see Fixtures/v3.3.2/README.md).
/// </summary>
/// <remarks>
///     Cases listed in KnownNonConformance.txt are expected to fail; <see cref="KnownNonConformance_StillFails"/>
///     fails once such a case starts passing, so the list has to shrink as conformance improves.
///     Fixtures for the optional key folding and path expansion features (§13.4) are not run.
/// </remarks>
public class SpecFixtureConformanceTests
{
    private const string SpecVersion = "v3.3.2";

    private static readonly string FixtureRoot = Path.Combine(AppContext.BaseDirectory, "SpecCompliance", "Fixtures", SpecVersion);

    private static readonly Lazy<IReadOnlyDictionary<string, FixtureCase>> Cases = new(LoadCases);

    private static readonly Lazy<HashSet<string>> KnownFailures = new(() =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "SpecCompliance", "KnownNonConformance.txt"))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
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

        Assert.True(failure is null, $"{id}: {failure}");
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

        var failing = Cases.Value.Where(c => Run(c.Value) is not null).Select(c => c.Key).Order(StringComparer.Ordinal);
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

                foreach (var test in doc.RootElement.GetProperty("tests").EnumerateArray())
                {
                    if (UsesOptionalFeature(test))
                    {
                        continue;
                    }

                    var id = $"{category}/{fileName}/{test.GetProperty("name").GetString()}";
                    cases.Add(id, new FixtureCase(category, test.Clone()));
                }
            }
        }

        return cases;
    }

    private static bool UsesOptionalFeature(JsonElement test)
    {
        if (!test.TryGetProperty("options", out var options))
        {
            return false;
        }

        return (options.TryGetProperty("keyFolding", out var folding) && folding.GetString() != "off") ||
               (options.TryGetProperty("expandPaths", out var expand) && expand.GetString() != "off");
    }

    #endregion

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

        if (json.TryGetProperty("indent", out var indent))
        {
            options.IndentSize = indent.GetInt32();
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
            JsonValueKind.Number => new ToonNumber(element.GetDouble()),
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
                return actual is ToonNumber n && n.Value.Equals(expected.GetDouble());
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
