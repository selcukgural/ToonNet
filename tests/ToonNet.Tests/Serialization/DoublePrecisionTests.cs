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

    // The cast from decimal to double is correctly rounded only from .NET 11 on: on .NET 8 and 10 it gave
    // 95139460995.18956 for the first value (the closest double is 95139460995.18954).
    [Theory]
    [InlineData("95139460995.18955")]
    [InlineData("923199714790672.1")]
    [InlineData("497229171663659359375330.5918")]
    [InlineData("-92788615756.64711")]
    [InlineData("0.1")]
    [InlineData("-1.5")]
    [InlineData("9007199254740993")]
    [InlineData("0.0000000000000000000000000001")]
    [InlineData("79228162514264337593543950335")]
    [InlineData("-79228162514264337593543950335")]
    public void DecimalNumbers_HaveTheClosestDouble_OnEveryRuntime(string text)
    {
        var value = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        var closest = double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        var number = new ToonNumber(value);

        Assert.Equal(closest, number.Value);
        Assert.Equal(closest, number.Value);
        Assert.Equal(value, number.DecimalValue);
        Assert.True(number.IsFinite);
        Assert.Equal(closest, ToonSerializer.DeserializeFromValue<double>(number));
        Assert.Equal((float)closest, ToonSerializer.DeserializeFromValue<float>(number));
        Assert.Equal(value, ToonSerializer.Deserialize<decimal>(ToonSerializer.Serialize(value)));
    }

    [Fact]
    public void DecimalNumbers_HaveTheClosestDouble_ForRandomMantissasAndScales()
    {
        var random = new Random(11);

        for (var i = 0; i < 100_000; i++)
        {
            var value = (i % 3) switch
            {
                0 => new decimal(random.Next(), random.Next(), random.Next(), random.Next(2) == 0, (byte)random.Next(29)),
                1 => new decimal(random.Next(), random.Next(1 << 21), 0, random.Next(2) == 0, (byte)random.Next(29)),
                _ => new decimal(random.Next(), random.Next(), random.Next(1, 1 << 8), random.Next(2) == 0, (byte)random.Next(29))
            };
            var closest = double.Parse(value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                                       System.Globalization.CultureInfo.InvariantCulture);

            Assert.Equal(closest, new ToonNumber(value).Value);
        }
    }

    [Fact]
    public void DecimalNegativeZero_StaysNegativeZero()
    {
        var number = new ToonNumber(new decimal(0, 0, 0, isNegative: true, scale: 3));

        Assert.True(double.IsNegative(number.Value));
        Assert.Equal(0d, number.Value);
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
