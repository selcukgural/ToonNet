using System.Globalization;
using System.Text.Json;
using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Extensions.Json;
using ToonNet.Extensions.Yaml;

namespace ToonNet.Tests.Serialization;

/// <summary>
///     Text is the same in every culture: these cultures differ from the invariant culture in the decimal separator
///     (sv-SE, fi-FI: ','), the minus sign (U+2212), the time separator (fi-FI: '.') and the calendar (ar-SA: Hijri,
///     th-TH: Buddhist), so any formatting or parsing that uses the current culture shows up.
/// </summary>
public class CultureIndependenceTests
{
    public static TheoryData<string> Cultures => ["sv-SE", "fi-FI", "ar-SA", "th-TH", "tr-TR", "de-CH", "hi-IN"];

    public sealed class Values
    {
        public double D { get; set; } = -1.5;
        public float F { get; set; } = -0.25f;
        public decimal M { get; set; } = -1234.5678m;
        public int I { get; set; } = -3;
        public double Tiny { get; set; } = -1e-7;
        public double Huge { get; set; } = 1.5e300;
        public DateTime When { get; set; } = new(2024, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc);
        public DateTimeOffset Offset { get; set; } = new(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(-3));
        public DateOnly Day { get; set; } = new(2024, 2, 29);
        public TimeOnly At { get; set; } = new(13, 45, 10, 500);
        public TimeSpan Wait { get; set; } = new(-1, -2, -3, -4, -5);
        public List<double> List { get; set; } = [-0.5, 2.5];
        public Dictionary<double, string> ByNumber { get; set; } = new() { [-1.5] = "a" };
    }

    private const string Expected = """
                                    D: -1.5
                                    F: -0.25
                                    M: -1234.5678
                                    I: -3
                                    Tiny: -1e-7
                                    Huge: 1.5e+300
                                    When: "2024-01-02T03:04:05.678Z"
                                    Offset: "2024-01-02T03:04:05-03:00"
                                    Day: 2024-02-29
                                    At: "13:45:10.5"
                                    Wait: "-1.02:03:04.0050000"
                                    List[2]: -0.5,2.5
                                    ByNumber:
                                      "-1.5": a
                                    """;

    private static void InCulture(string name, Action action)
    {
        var (culture, uiCulture) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(name);
            action();
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (culture, uiCulture);
        }
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void Serializer_WritesAndReadsTheSameText(string culture)
    {
        // The invariant text, computed outside the culture
        Assert.Equal(Expected, ToonSerializer.Serialize(new Values()));

        InCulture(culture, () =>
        {
            Assert.Equal(Expected, ToonSerializer.Serialize(new Values()));
            Assert.Equal(Expected, ToonSerializer.Serialize(new Values(), new ToonSerializerOptions { UseGeneratedCode = false }));

            var back = ToonSerializer.Deserialize<Values>(Expected)!;
            Assert.Equal(Expected, ToonSerializer.Serialize(back));

            // Through the tree-based path, and from quoted text
            var fromTree = ToonDocument.Parse(Expected).Root.Deserialize<Values>()!;
            Assert.Equal(Expected, ToonSerializer.Serialize(fromTree));
            var quoted = ToonSerializer.Deserialize<Values>("D: \"-1.5\"\nM: \"-1234.5678\"\nDay: \"2024-02-29\"\nI: \"-3\"")!;
            Assert.Equal((-1.5, -1234.5678m, new DateOnly(2024, 2, 29), -3), (quoted.D, quoted.M, quoted.Day, quoted.I));
            Assert.Equal(-1234.5678m, back.M);
            Assert.Equal(new TimeOnly(13, 45, 10, 500), back.At);
            Assert.Equal(new DateOnly(2024, 2, 29), back.Day);
        });
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void TreesAndConversions_AreTheSame(string culture)
    {
        const string toon = "a: -1.5\nb[3]: 1e-7,-0.25,12345678901234567890\nc: x";
        var json = ToonConvert.ToJson(toon);
        var yaml = ToonYamlConvert.ToYaml(toon);

        InCulture(culture, () =>
        {
            var root = ToonDocument.Parse(toon).Root;
            Assert.Equal(-1.5, root["a"]!.GetValue<double>());
            Assert.Equal(toon, new ToonEncoder().Encode(new ToonDocument(root)));
            Assert.Equal(json, ToonConvert.ToJson(toon));
            Assert.Equal(toon, ToonConvert.FromJson(json));
            Assert.Equal(yaml, ToonYamlConvert.ToYaml(toon));
            Assert.Equal(toon, ToonYamlConvert.FromYaml(yaml));
            Assert.Equal(-0.25m, root["b"]![1].GetValue<decimal>());
            Assert.True(ToonNumber.TryParse("-2.5", out var number) && number.Value == -2.5);
            Assert.Single(root.Select("$.b[?@ < 0]"));
        });
    }

    /// <summary>All decode fixtures of the spec give the same document in every culture.</summary>
    [Theory]
    [MemberData(nameof(Cultures))]
    public void SpecFixtures_DecodeTheSame(string culture)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "SpecCompliance", "Fixtures", "v4.4", "decode");
        var inputs = Directory.GetFiles(root, "*.json")
                              .SelectMany(f => JsonDocument.Parse(File.ReadAllText(f)).RootElement.GetProperty("tests").EnumerateArray())
                              .Select(t => t.GetProperty("input").GetString()!)
                              .ToList();

        static string Describe(string input)
        {
            try
            {
                var options = new ToonOptions { StrictMode = false };
                return new ToonEncoder().Encode(ToonDocument.Parse(input, options)) + "|" + ToonConvert.ToJson(input, toonOptions: options);
            }
            catch (Exception ex)
            {
                return ex.GetType().Name + ": " + ex.Message;
            }
        }

        var expected = inputs.Select(Describe).ToList();
        InCulture(culture, () => Assert.Equal(expected, inputs.Select(Describe).ToList()));
    }
}
