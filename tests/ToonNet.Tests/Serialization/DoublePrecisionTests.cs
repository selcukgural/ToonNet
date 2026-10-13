using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Serialization;

/// <summary>
///     Doubles read back exactly: a token that also fits a decimal keeps the double closest to its text, not the
///     decimal converted to double (that conversion is not correctly rounded, so 3.8000000000000003 became 3.8).
/// </summary>
public class DoublePrecisionTests
{
    public sealed class Holder
    {
        public double D { get; set; }
        public float F { get; set; }
        public List<double> Items { get; set; } = [];
    }

    private static IEnumerable<double> Values()
    {
        yield return 3.8000000000000003;
        yield return 3.9000000000000004;
        yield return 0.30000000000000004;
        yield return 0.1;
        yield return 123456789012345678;
        yield return -987654321.12345678;
        yield return 1e-7;
        yield return 5e-324;
        yield return 1.7976931348623157e308;

        var random = new Random(38);

        for (var i = 0; i < 20_000; i++)
        {
            yield return Math.Round(random.NextDouble() * Math.Pow(10, random.Next(-6, 20)), random.Next(0, 16)) + random.NextDouble() / 3;
        }
    }

    [Fact]
    public void Doubles_RoundTripExactly_OnEveryPath()
    {
        foreach (var value in Values())
        {
            var toon = ToonSerializer.Serialize(new Holder { D = value, Items = [value] });

            var direct = ToonSerializer.Deserialize<Holder>(toon)!;
            var tree = ToonSerializer.DeserializeFromValue<Holder>(ToonDocument.Parse(toon).Root)!;

            Assert.Equal(value, direct.D);
            Assert.Equal(value, direct.Items[0]);
            Assert.Equal(value, tree.D);
            Assert.Equal(value, tree.Items[0]);
        }
    }

    [Fact]
    public void ParsedNumbers_KeepTheClosestDouble_AndTheExactDecimal()
    {
        var number = (ToonNumber)((ToonObject)ToonDocument.Parse("v: 3.8000000000000003").Root)["v"]!;

        Assert.Equal(3.8000000000000003, number.Value);
        Assert.Equal(3.8000000000000003m, number.DecimalValue);
        Assert.True(ToonNumber.TryParse("123456789012345678", out var integer));
        Assert.Equal(123456789012345678d, integer.Value);
        Assert.Equal(123456789012345678m, integer.DecimalValue);
    }

    [Fact]
    public void Floats_RoundTripExactly()
    {
        var random = new Random(7);

        for (var i = 0; i < 5_000; i++)
        {
            var value = (float)(random.NextDouble() * Math.Pow(10, random.Next(-4, 9)));
            var toon = ToonSerializer.Serialize(new Holder { F = value });

            Assert.Equal(value, ToonSerializer.Deserialize<Holder>(toon)!.F);
            Assert.Equal(value, ToonSerializer.DeserializeFromValue<Holder>(ToonDocument.Parse(toon).Root)!.F);
        }
    }
}
