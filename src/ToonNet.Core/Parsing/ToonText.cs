using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ToonNet.Core.Parsing;

/// <summary>
///     The text of a document to parse: a string, or the first <see cref="Length"/> characters of an array (text decoded
///     from UTF-8 into a pooled buffer, for example), so byte input is parsed without creating a string.
/// </summary>
internal readonly struct ToonText
{
    private readonly string? _string;
    private readonly char[]? _chars;

    /// <summary>The number of characters.</summary>
    public readonly int Length;

    public ToonText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _string = text;
        Length = text.Length;
    }

    /// <remarks><paramref name="length"/> must not exceed the length of <paramref name="chars"/>.</remarks>
    public ToonText(char[] chars, int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)length, (uint)chars.Length, nameof(length));
        _chars = chars;
        Length = length;
    }

    /// <summary>The characters.</summary>
    public ReadOnlySpan<char> Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _chars != null
            ? MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(_chars), Length)
            : _string.AsSpan();
    }

    public static implicit operator ToonText(string text) => new(text);
}
