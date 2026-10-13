using System.Globalization;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;

namespace ToonNet.Tests.Encoding;

/// <summary>
///     Compares the span-based <see cref="ToonNumberFormatter"/> with the string-based implementation it replaced,
///     which is kept here as the reference.
/// </summary>
public class ToonNumberFormatterParityTests
{
    [Fact]
    public void Doubles_FormatLikeTheReference()
    {
        var random = new Random(20261013);
        var values = new List<double>
        {
            0, -0.0, 1, -1, 0.1, 0.5, 1.5, 100, 1e-6, 9.99999e-7, 1e-7, 1e15, 1e16, 1e20, 1e21, 9.999999999999999e20,
            123456789012345680000.0, 0.000001234, 5e-324, double.Epsilon, double.MaxValue, double.MinValue, 1.7976931348623157e308,
            Math.PI, -Math.E, 12.5, 0.1 + 0.2, 1 / 3.0, 2e-5, 1.5e300, -4.94e-322
        };

        for (var i = 0; i < 200_000; i++)
        {
            // Random bit patterns cover every exponent; finite ones only
            var value = BitConverter.Int64BitsToDouble(random.NextInt64());

            if (double.IsFinite(value))
            {
                values.Add(value);
            }

            values.Add(Math.Round(random.NextDouble() * Math.Pow(10, random.Next(-8, 23)), random.Next(0, 8)));
        }

        foreach (var value in values)
        {
            Assert.Equal(ReferenceFormat(value), ToonNumberFormatter.Format(value));
        }
    }

    [Fact]
    public void Decimals_AndToonNumbers_FormatLikeTheReference()
    {
        var random = new Random(7);
        var values = new List<decimal> { 0m, -0m, 0.000m, 1m, 1.10m, -1.5000m, decimal.MaxValue, decimal.MinValue, 0.0000000000000000000000000001m, 100m };

        for (var i = 0; i < 20_000; i++)
        {
            values.Add(new decimal(random.Next(), random.Next(), random.Next(), random.Next(2) == 0, (byte)random.Next(0, 29)));
        }

        foreach (var value in values)
        {
            Assert.Equal(ReferenceFormat(value), ToonNumberFormatter.Format(value));
            Assert.Equal(ReferenceFormat(value), ToonNumberFormatter.Format(new ToonNumber(value)));
        }

        Assert.Null(ToonNumberFormatter.Format(new ToonNumber(double.NaN)));
        Assert.Null(ToonNumberFormatter.Format(double.PositiveInfinity));
    }

    #region Reference implementation (before the span-based rewrite)

    private static string ReferenceFormat(decimal value)
    {
        if (value == 0)
        {
            return "0";
        }

        var text = value.ToString(CultureInfo.InvariantCulture);

        if (text.Contains('.'))
        {
            text = text.TrimEnd('0').TrimEnd('.');
        }

        return text;
    }

    private static string? ReferenceFormat(double value)
    {
        if (!double.IsFinite(value))
        {
            return null;
        }

        if (value == 0)
        {
            return "0";
        }

        var shortest = value.ToString("R", CultureInfo.InvariantCulture);
        var negative = shortest[0] == '-';
        var body = negative ? shortest[1..] : shortest;

        var exponent = 0;
        var exponentIndex = body.IndexOfAny(['E', 'e']);

        if (exponentIndex >= 0)
        {
            exponent = int.Parse(body.AsSpan(exponentIndex + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            body = body[..exponentIndex];
        }

        var dotIndex = body.IndexOf('.');
        var digits = dotIndex >= 0 ? body.Remove(dotIndex, 1) : body;
        var pointPosition = (dotIndex >= 0 ? dotIndex : body.Length) + exponent;

        var leadingZeros = 0;

        while (leadingZeros < digits.Length - 1 && digits[leadingZeros] == '0')
        {
            leadingZeros++;
        }

        digits = digits[leadingZeros..].TrimEnd('0');
        pointPosition -= leadingZeros;

        var scientificExponent = pointPosition - 1;
        var result = scientificExponent is >= -6 and <= 20
            ? FormatPlain(digits, pointPosition)
            : FormatExponent(digits, scientificExponent);

        return negative ? "-" + result : result;

        static string FormatPlain(string digits, int pointPosition)
        {
            if (pointPosition <= 0)
            {
                return "0." + new string('0', -pointPosition) + digits;
            }

            if (pointPosition >= digits.Length)
            {
                return digits + new string('0', pointPosition - digits.Length);
            }

            return digits[..pointPosition] + "." + digits[pointPosition..];
        }

        static string FormatExponent(string digits, int exponent)
        {
            var mantissa = digits.Length == 1 ? digits : digits[0] + "." + digits[1..];
            return mantissa + "e" + (exponent < 0 ? "-" : "+") + Math.Abs(exponent).ToString(CultureInfo.InvariantCulture);
        }
    }

    #endregion
}
