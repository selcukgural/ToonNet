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
///     are always written in plain decimal notation. Formatting writes into a span, so the encoder appends numbers
///     without allocating a string for each.
/// </remarks>
internal static class ToonNumberFormatter
{
    /// <summary>
    ///     A buffer size that fits every canonical number: a decimal has at most 29 digits plus sign and point; a double
    ///     at most 17 significant digits, up to 20 zeros in plain form, or a sign, point and three-digit exponent.
    /// </summary>
    public const int MaxLength = 64;

    /// <summary>
    ///     Formats a TOON number.
    /// </summary>
    /// <param name="number">The number to format.</param>
    /// <returns>The canonical text, or <c>null</c> when the value is NaN or infinite (which encode as null per §3).</returns>
    public static string? Format(ToonNumber number)
    {
        Span<char> buffer = stackalloc char[MaxLength];
        return TryFormat(number, buffer, out var written) ? new string(buffer[..written]) : null;
    }

    /// <summary>
    ///     Formats a <see cref="decimal"/> in plain canonical decimal notation.
    /// </summary>
    public static string Format(decimal value)
    {
        Span<char> buffer = stackalloc char[MaxLength];
        return new string(buffer[..FormatDecimal(value, buffer)]);
    }

    /// <summary>
    ///     Formats a <see cref="double"/> in canonical form.
    /// </summary>
    /// <returns>The canonical text, or <c>null</c> when the value is NaN or infinite.</returns>
    public static string? Format(double value)
    {
        Span<char> buffer = stackalloc char[MaxLength];
        return TryFormatDouble(value, buffer, out var written) ? new string(buffer[..written]) : null;
    }

    /// <summary>
    ///     Writes the canonical text of a TOON number into <paramref name="destination"/>, which must hold at least
    ///     <see cref="MaxLength"/> characters.
    /// </summary>
    /// <returns><c>false</c> when the value is NaN or infinite (nothing is written).</returns>
    public static bool TryFormat(ToonNumber number, Span<char> destination, out int written)
    {
        if (number.DecimalValue is { } exact)
        {
            written = FormatDecimal(exact, destination);
            return true;
        }

        return TryFormatDouble(number.Value, destination, out written);
    }

    /// <returns>The number of characters written.</returns>
    public static int FormatDecimal(decimal value, Span<char> destination)
    {
        if (value == 0)
        {
            destination[0] = '0';
            return 1;
        }

        value.TryFormat(destination, out var written, default, CultureInfo.InvariantCulture);
        var text = destination[..written];

        if (text.IndexOf('.') >= 0)
        {
            text = text.TrimEnd('0').TrimEnd('.');
        }

        return text.Length;
    }

    public static bool TryFormatDouble(double value, Span<char> destination, out int written)
    {
        if (!double.IsFinite(value))
        {
            written = 0;
            return false;
        }

        if (value == 0)
        {
            destination[0] = '0'; // also normalizes -0
            written = 1;
            return true;
        }

        // "R" yields the shortest digits that round-trip. Without an exponent that text is already canonical (no
        // leading or trailing zeros); exponent forms such as "1E-07" or "1.5E+21" are rewritten below.
        Span<char> shortest = stackalloc char[32];
        value.TryFormat(shortest, out var length, "R", CultureInfo.InvariantCulture);
        var text = shortest[..length];
        var exponentIndex = text.IndexOfAny('E', 'e');

        if (exponentIndex < 0)
        {
            text.CopyTo(destination);
            written = length;
            return true;
        }

        var negative = text[0] == '-';
        var exponent = int.Parse(text[(exponentIndex + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        var body = text[(negative ? 1 : 0)..exponentIndex];

        // Split into a digit string and the position of the decimal point within it
        Span<char> digitBuffer = stackalloc char[32];
        var digitCount = 0;
        var dotIndex = -1;

        for (var i = 0; i < body.Length; i++)
        {
            if (body[i] == '.')
            {
                dotIndex = i;
            }
            else
            {
                digitBuffer[digitCount++] = body[i];
            }
        }

        var pointPosition = (dotIndex >= 0 ? dotIndex : body.Length) + exponent;
        var leadingZeros = 0;

        while (leadingZeros < digitCount - 1 && digitBuffer[leadingZeros] == '0')
        {
            leadingZeros++;
        }

        ReadOnlySpan<char> digits = digitBuffer[leadingZeros..digitCount].TrimEnd('0');
        pointPosition -= leadingZeros;

        var writer = new SpanWriter(destination);

        if (negative)
        {
            writer.Append('-');
        }

        var scientificExponent = pointPosition - 1;

        if (scientificExponent is >= -6 and <= 20)
        {
            WritePlain(ref writer, digits, pointPosition);
        }
        else
        {
            WriteExponent(ref writer, digits, scientificExponent);
        }

        written = writer.Position;
        return true;
    }

    private static void WritePlain(ref SpanWriter writer, scoped ReadOnlySpan<char> digits, int pointPosition)
    {
        if (pointPosition <= 0)
        {
            writer.Append("0.");
            writer.Append('0', -pointPosition);
            writer.Append(digits);
        }
        else if (pointPosition >= digits.Length)
        {
            writer.Append(digits);
            writer.Append('0', pointPosition - digits.Length);
        }
        else
        {
            writer.Append(digits[..pointPosition]);
            writer.Append('.');
            writer.Append(digits[pointPosition..]);
        }
    }

    private static void WriteExponent(ref SpanWriter writer, scoped ReadOnlySpan<char> digits, int exponent)
    {
        writer.Append(digits[0]);

        if (digits.Length > 1)
        {
            writer.Append('.');
            writer.Append(digits[1..]);
        }

        writer.Append('e');
        writer.Append(exponent < 0 ? '-' : '+');
        writer.Append(Math.Abs(exponent));
    }

    /// <summary>
    ///     Appends characters to a span that is known to be large enough.
    /// </summary>
    private ref struct SpanWriter(Span<char> destination)
    {
        private readonly Span<char> _destination = destination;

        public int Position { get; private set; }

        public void Append(char value) => _destination[Position++] = value;

        public void Append(char value, int count)
        {
            _destination.Slice(Position, count).Fill(value);
            Position += count;
        }

        public void Append(scoped ReadOnlySpan<char> value)
        {
            value.CopyTo(_destination[Position..]);
            Position += value.Length;
        }

        public void Append(int value)
        {
            value.TryFormat(_destination[Position..], out var written, default, CultureInfo.InvariantCulture);
            Position += written;
        }
    }
}
