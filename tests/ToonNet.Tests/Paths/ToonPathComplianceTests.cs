using System.Text.Json;
using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Paths;

namespace ToonNet.Tests.Paths;

/// <summary>Runs the JSONPath Compliance Test Suite (RFC 9535) against <see cref="ToonPath"/>.</summary>
public class ToonPathComplianceTests
{
    private static readonly Lazy<Dictionary<string, JsonElement>> Cases = new(() =>
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Paths", "Cts", "cts.json"));
        var suite = JsonDocument.Parse(stream).RootElement.Clone();
        var cases = new Dictionary<string, JsonElement>();

        foreach (var test in suite.GetProperty("tests").EnumerateArray())
        {
            cases[test.GetProperty("name").GetString()!] = test;
        }

        return cases;
    });

    public static TheoryData<string> Names()
    {
        var data = new TheoryData<string>();

        foreach (var name in Cases.Value.Keys)
        {
            data.Add(name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void Case(string name)
    {
        var test = Cases.Value[name];
        var selector = test.GetProperty("selector").GetString()!;

        // The ToonNet extensions do not change any standard result: the suite passes with and without them
        var standardOnly = new ToonPathOptions { StandardFunctionsOnly = true };

        if (test.TryGetProperty("invalid_selector", out _))
        {
            Assert.False(ToonPath.TryParse(selector, out _), $"'{selector}' should be invalid");
            Assert.Throws<ToonPathException>(() => ToonPath.Parse(selector));
            Assert.False(ToonPath.TryParse(selector, standardOnly, out _));
            return;
        }

        Assert.True(ToonPath.TryParse(selector, standardOnly, out _), $"'{selector}' should be valid with standard functions only");

        var path = ToonPath.Parse(selector);
        var document = ToToon(test.GetProperty("document"));
        var matches = path.SelectMatches(document);

        // Some cases allow several orders (members of an object have no order in JSON)
        var results = test.TryGetProperty("result", out var result)
            ? [(result, test.GetProperty("result_paths"))]
            : test.GetProperty("results").EnumerateArray().Zip(test.GetProperty("results_paths").EnumerateArray()).ToList();

        Assert.Contains(results, expected => Same(matches, expected.Item1, expected.Item2));

        // The values-only evaluation selects the same values
        var values = path.Select(document);
        Assert.Equal(matches.Count, values.Count);

        for (var i = 0; i < values.Count; i++)
        {
            Assert.Same(matches[i].Value, values[i]);
        }
    }

    private static bool Same(IReadOnlyList<ToonPathMatch> matches, JsonElement values, JsonElement paths)
    {
        if (matches.Count != values.GetArrayLength())
        {
            return false;
        }

        var i = 0;

        foreach (var (value, path) in values.EnumerateArray().Zip(paths.EnumerateArray()))
        {
            if (!ToonValue.DeepEquals(matches[i].Value, ToToon(value)) || matches[i].Path != path.GetString())
            {
                return false;
            }

            i++;
        }

        return true;
    }

    internal static ToonValue ToToon(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var obj = new ToonObject();

                foreach (var property in element.EnumerateObject())
                {
                    obj[property.Name] = ToToon(property.Value);
                }

                return obj;
            }
            case JsonValueKind.Array:
                return new ToonArray(element.EnumerateArray().Select(ToToon).ToList());
            case JsonValueKind.String:
                return new ToonString(element.GetString()!);
            case JsonValueKind.Number:
                return ToonNumber.TryParse(element.GetRawText(), out var number) ? number : new ToonNumber(element.GetDouble());
            case JsonValueKind.True:
                return new ToonBoolean(true);
            case JsonValueKind.False:
                return new ToonBoolean(false);
            default:
                return ToonNull.Instance;
        }
    }
}
