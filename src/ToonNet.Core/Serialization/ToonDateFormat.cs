using System.Globalization;

namespace ToonNet.Core.Serialization;

/// <summary>
///     Formats dates and times as ISO 8601 text: the round-trip ("O") form without trailing zeros in the fraction of a
///     second (<c>2026-01-02T03:04:05Z</c>, <c>2026-01-02T03:04:05.123+03:00</c>), as System.Text.Json writes them.
/// </summary>
internal static class ToonDateFormat
{
    /// <summary>A buffer size that fits every formatted value.</summary>
    public const int MaxLength = 40;

    // "O" is yyyy-MM-ddTHH:mm:ss.fffffff followed by the kind or offset; the fraction is at a fixed position
    private const int FractionStart = 19;
    private const int FractionEnd = 27;

    public static int Format(DateTime value, Span<char> destination)
    {
        value.TryFormat(destination, out var written, "O", CultureInfo.InvariantCulture);
        return TrimFraction(destination, written);
    }

    public static int Format(DateTimeOffset value, Span<char> destination)
    {
        value.TryFormat(destination, out var written, "O", CultureInfo.InvariantCulture);
        return TrimFraction(destination, written);
    }

    public static string ToString(DateTime value)
    {
        Span<char> text = stackalloc char[MaxLength];
        return new string(text[..Format(value, text)]);
    }

    public static string ToString(DateTimeOffset value)
    {
        Span<char> text = stackalloc char[MaxLength];
        return new string(text[..Format(value, text)]);
    }

    private static int TrimFraction(Span<char> text, int length)
    {
        if (length < FractionEnd || text[FractionStart] != '.')
        {
            return length;
        }

        var cut = FractionEnd;

        while (cut > FractionStart + 1 && text[cut - 1] == '0')
        {
            cut--;
        }

        if (cut == FractionStart + 1)
        {
            cut = FractionStart; // no fraction left: drop the point too
        }

        text[FractionEnd..length].CopyTo(text[cut..]);
        return length - (FractionEnd - cut);
    }
}
