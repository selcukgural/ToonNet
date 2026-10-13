using System.Buffers;
using System.Runtime.CompilerServices;

namespace ToonNet.Core.Encoding;

/// <summary>
///     A growable character buffer rented from <see cref="ArrayPool{T}.Shared"/>, used by the encoder instead of a
///     <see cref="System.Text.StringBuilder"/> so that encoding allocates only the resulting string (or nothing, when
///     the text is written to a stream).
/// </summary>
/// <remarks>
///     The written part is cleared before an array goes back to the pool, so encoded data does not linger in arrays
///     that other code rents later. An instance is not thread-safe and must be disposed.
/// </remarks>
internal sealed class ToonTextBuffer : IDisposable
{
    private const int InitialCapacity = 16 * 1024;

    private char[] _buffer = ArrayPool<char>.Shared.Rent(InitialCapacity);
    private int _length;

    /// <summary>The number of characters written.</summary>
    public int Length => _length;

    /// <summary>The characters written so far; valid until the next append or <see cref="Dispose"/>.</summary>
    public ReadOnlySpan<char> WrittenSpan => _buffer.AsSpan(0, _length);

    /// <summary>The characters written so far; valid until the next append or <see cref="Dispose"/>.</summary>
    public ReadOnlyMemory<char> WrittenMemory => _buffer.AsMemory(0, _length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ToonTextBuffer Append(char value)
    {
        if (_length == _buffer.Length)
        {
            Grow(1);
        }

        _buffer[_length++] = value;
        return this;
    }

    public ToonTextBuffer Append(char value, int count)
    {
        if (count <= 0)
        {
            return this;
        }

        if (_length + count > _buffer.Length)
        {
            Grow(count);
        }

        _buffer.AsSpan(_length, count).Fill(value);
        _length += count;
        return this;
    }

    public ToonTextBuffer Append(string value) => Append(value.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ToonTextBuffer Append(scoped ReadOnlySpan<char> value)
    {
        if (_length + value.Length > _buffer.Length)
        {
            Grow(value.Length);
        }

        value.CopyTo(_buffer.AsSpan(_length));
        _length += value.Length;
        return this;
    }

    /// <summary>
    ///     Returns writable space of at least <paramref name="sizeHint"/> characters after the written text; call
    ///     <see cref="Advance"/> with the number of characters written into it.
    /// </summary>
    public Span<char> GetSpan(int sizeHint)
    {
        if (_length + sizeHint > _buffer.Length)
        {
            Grow(sizeHint);
        }

        return _buffer.AsSpan(_length);
    }

    /// <summary>Marks <paramref name="count"/> characters of the span from <see cref="GetSpan"/> as written.</summary>
    public void Advance(int count) => _length += count;

    /// <summary>Clears the written characters so the buffer can be reused.</summary>
    public void Clear()
    {
        _buffer.AsSpan(0, _length).Clear();
        _length = 0;
    }

    /// <summary>
    ///     Writes the characters to <paramref name="stream"/> as UTF-8 (without a byte order mark), in chunks through a
    ///     pooled byte buffer, so no string or full-size byte array is created.
    /// </summary>
    public async ValueTask WriteUtf8Async(Stream stream, CancellationToken cancellationToken)
    {
        var bytes = ArrayPool<byte>.Shared.Rent(ChunkBytes);
        var encoder = ToonTextEncoding.Utf8NoBom.GetEncoder();
        var position = 0;

        try
        {
            bool completed;

            do
            {
                var (charsUsed, bytesUsed, done) = Encode(encoder, WrittenMemory.Span[position..], bytes);
                position += charsUsed;
                completed = done;
                await stream.WriteAsync(bytes.AsMemory(0, bytesUsed), cancellationToken).ConfigureAwait(false);
            }
            while (!completed);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytes, clearArray: true);
        }

        static (int CharsUsed, int BytesUsed, bool Completed) Encode(System.Text.Encoder encoder, ReadOnlySpan<char> chars, byte[] bytes)
        {
            encoder.Convert(chars, bytes, flush: true, out var charsUsed, out var bytesUsed, out var completed);
            return (charsUsed, bytesUsed, completed);
        }
    }

    /// <summary>The size of the byte chunks <see cref="WriteUtf8Async"/> writes.</summary>
    private const int ChunkBytes = 16 * 1024;

    /// <summary>Creates a string from the written characters.</summary>
    public override string ToString() => new(WrittenSpan);

    public void Dispose()
    {
        var buffer = _buffer;

        if (buffer.Length == 0)
        {
            return;
        }

        _buffer = [];
        buffer.AsSpan(0, _length).Clear();
        _length = 0;
        ArrayPool<char>.Shared.Return(buffer);
    }

    private void Grow(int additional)
    {
        var required = _length + additional;
        var capacity = Math.Max(required, _buffer.Length <= Array.MaxLength / 2 ? _buffer.Length * 2 : Array.MaxLength);
        var larger = ArrayPool<char>.Shared.Rent(capacity);

        _buffer.AsSpan(0, _length).CopyTo(larger);
        _buffer.AsSpan(0, _length).Clear();
        ArrayPool<char>.Shared.Return(_buffer);
        _buffer = larger;
    }
}
