using System.Globalization;
using ToonNet.Core.Models;

namespace ToonNet.Core.Encoding;

/// <summary>
///     Formats numbers in the canonical form required by TOON spec §2.
/// </summary>
/// <remarks>
///     Numbers with 1e-6 ≤ |n| &lt; 1e21 (and zero) are written in plain decimal notation without
///     exponent, leading zeros or trailing fractional zeros; -0 becomes 0. Doubles outside that range
///     use lowercase exponent notation with an explicit sign (e.g. <c>1e-7</c>, <c>1e+21</c>).
///     Doubles use the shortest representation that round-trips; exact <see cref="decimal"/> values
///     are always written in plain decimal notation.
/// </remarks>
internal static class ToonNumberFormatter
{
    /// <summary>
    ///     Formats a TOON number.
    /// </summary>
    /// <param name="number">The number to format.</param>
    /// <returns>The canonical text, or <c>null</c> when the value is NaN or infinite (which encode as null per §3).</returns>
    public static string? Format(ToonNumber number)
    {
        return number.DecimalValue is { } exact ? Format(exact) : Format(number.Value);
    }

    /// <summary>
    ///     Formats a <see cref="decimal"/> in plain canonical decimal notation.
    /// </summary>
    public static string Format(decimal value)
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

    /// <summary>
    ///     Formats a <see cref="double"/> in canonical form.
    /// </summary>
    /// <returns>The canonical text, or <c>null</c> when the value is NaN or infinite.</returns>
    public static string? Format(double value)
    {
        if (!double.IsFinite(value))
        {
            return null;
        }

        if (value == 0)
        {
            return "0"; // also normalizes -0
        }

        // "R" yields the shortest digits that round-trip, possibly in exponent form (e.g. "1E-07", "1.5E+21")
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

        // Split into a digit string and the position of the decimal point within it
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
    }

    private static string FormatPlain(string digits, int pointPosition)
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

    private static string FormatExponent(string digits, int exponent)
    {
        var mantissa = digits.Length == 1 ? digits : digits[0] + "." + digits[1..];
        return mantissa + "e" + (exponent < 0 ? "-" : "+") + Math.Abs(exponent).ToString(CultureInfo.InvariantCulture);
    }
}
