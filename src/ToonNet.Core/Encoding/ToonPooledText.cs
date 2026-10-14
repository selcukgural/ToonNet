using System.Buffers;
using ToonNet.Core.Parsing;

namespace ToonNet.Core.Encoding;

/// <summary>
///     Text in a character array rented from <see cref="ArrayPool{T}.Shared"/>: decoded UTF-8 input, parsed without
///     creating a string. The written part is cleared before the array goes back to the pool, so document text does not
///     linger in arrays that other code rents later. Dispose exactly once (a copy shares the array).
/// </summary>
internal struct ToonPooledText : IDisposable
{
    /// <summary>The characters; the first <see cref="Length"/> are the text.</summary>
    public char[] Chars;

    /// <summary>The number of characters of the text.</summary>
    public int Length;

    public ToonPooledText(int capacity)
    {
        Chars = ArrayPool<char>.Shared.Rent(Math.Max(capacity, 1));
    }

    /// <summary>Copies characters into a new pooled buffer.</summary>
    public static ToonPooledText Copy(ReadOnlySpan<char> chars)
    {
        var text = new ToonPooledText(chars.Length);
        chars.CopyTo(text.Chars);
        text.Length = chars.Length;
        return text;
    }

    /// <summary>The text, for the parser.</summary>
    public readonly ToonText Text => new(Chars, Length);

    /// <summary>Makes room for at least <paramref name="count"/> more characters after the text.</summary>
    public void EnsureCapacity(int count)
    {
        if (Chars.Length - Length >= count)
        {
            return;
        }

        var size = (int)Math.Min(Math.Max((long)Chars.Length * 2, (long)Length + count), Array.MaxLength);
        var larger = ArrayPool<char>.Shared.Rent(size);
        Chars.AsSpan(0, Length).CopyTo(larger);
        Return(Chars, Length);
        Chars = larger;
    }

    public void Dispose()
    {
        if (Chars is { Length: > 0 })
        {
            Return(Chars, Length);
        }

        Chars = [];
        Length = 0;
    }

    private static void Return(char[] chars, int length)
    {
        chars.AsSpan(0, length).Clear();
        ArrayPool<char>.Shared.Return(chars);
    }
}
