using System.Buffers;
using System.Runtime.CompilerServices;

namespace ToonNet.Core.Encoding;

/// <summary>
///     A character buffer made of chunks rented from <see cref="ArrayPool{T}.Shared"/>, used by the encoder instead of
///     a <see cref="System.Text.StringBuilder"/> so that encoding allocates only the resulting string (or nothing, when
///     the text is written to a stream).
/// </summary>
/// <remarks>
///     A full chunk is kept and a larger one is rented after it, so growing never copies what was written. The written
///     part of every chunk is cleared before it goes back to the pool, so encoded data does not linger in arrays that
///     other code rents later. An instance is not thread-safe and must be disposed.
/// </remarks>
internal sealed class ToonTextBuffer : IDisposable
{
    private const int InitialChunkSize = 16 * 1024;
    private const int MaxChunkSize = 1024 * 1024;

    /// <summary>The size of the byte chunks <see cref="WriteUtf8Async"/> writes.</summary>
    private const int ByteChunkSize = 16 * 1024;

    /// <summary>The chunks before the current one, each with the number of characters written to it.</summary>
    private List<(char[] Chunk, int Length)>? _previous;

    private char[] _current = ArrayPool<char>.Shared.Rent(InitialChunkSize);
    private int _currentLength;

    /// <summary>The number of characters written to the chunks before the current one.</summary>
    private int _previousLength;

    /// <summary>The number of characters written.</summary>
    public int Length => _previousLength + _currentLength;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ToonTextBuffer Append(char value)
    {
        if (_currentLength == _current.Length)
        {
            NextChunk(1);
        }

        _current[_currentLength++] = value;
        return this;
    }

    public ToonTextBuffer Append(char value, int count)
    {
        while (count > 0)
        {
            if (_currentLength == _current.Length)
            {
                NextChunk(count);
            }

            var part = Math.Min(count, _current.Length - _currentLength);
            _current.AsSpan(_currentLength, part).Fill(value);
            _currentLength += part;
            count -= part;
        }

        return this;
    }

    public ToonTextBuffer Append(string value) => Append(value.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ToonTextBuffer Append(scoped ReadOnlySpan<char> value)
    {
        if (value.Length <= _current.Length - _currentLength)
        {
            value.CopyTo(_current.AsSpan(_currentLength));
            _currentLength += value.Length;
            return this;
        }

        return AppendAcrossChunks(value);
    }

    private ToonTextBuffer AppendAcrossChunks(scoped ReadOnlySpan<char> value)
    {
        while (!value.IsEmpty)
        {
            if (_currentLength == _current.Length)
            {
                NextChunk(value.Length);
            }

            var part = Math.Min(value.Length, _current.Length - _currentLength);
            value[..part].CopyTo(_current.AsSpan(_currentLength));
            _currentLength += part;
            value = value[part..];
        }

        return this;
    }

    /// <summary>
    ///     Returns writable space of at least <paramref name="sizeHint"/> characters after the written text; call
    ///     <see cref="Advance"/> with the number of characters written into it.
    /// </summary>
    public Span<char> GetSpan(int sizeHint)
    {
        if (_current.Length - _currentLength < sizeHint)
        {
            NextChunk(sizeHint);
        }

        return _current.AsSpan(_currentLength);
    }

    /// <summary>Marks <paramref name="count"/> characters of the span from <see cref="GetSpan"/> as written.</summary>
    public void Advance(int count) => _currentLength += count;

    /// <summary>Discards the characters written after the first <paramref name="length"/>.</summary>
    public void Truncate(int length)
    {
        while (length < _previousLength)
        {
            // The cut is in an earlier chunk: drop the current one and continue in the previous one
            ReturnChunk(_current, _currentLength);
            var (chunk, chunkLength) = _previous![^1];
            _previous.RemoveAt(_previous.Count - 1);
            _current = chunk;
            _currentLength = chunkLength;
            _previousLength -= chunkLength;
        }

        var keep = length - _previousLength;
        _current.AsSpan(keep, _currentLength - keep).Clear();
        _currentLength = keep;
    }

    /// <summary>Clears the written characters so the buffer can be reused (the first chunk is kept).</summary>
    public void Clear() => Truncate(0);

    /// <summary>
    ///     Writes the characters to <paramref name="stream"/> as UTF-8 (without a byte order mark), in chunks through a
    ///     pooled byte buffer, so no string or full-size byte array is created.
    /// </summary>
    public async ValueTask WriteUtf8Async(Stream stream, CancellationToken cancellationToken)
    {
        var bytes = ArrayPool<byte>.Shared.Rent(ByteChunkSize);
        var encoder = ToonTextEncoding.Utf8NoBom.GetEncoder();

        try
        {
            var chunkCount = (_previous?.Count ?? 0) + 1;

            for (var c = 0; c < chunkCount; c++)
            {
                var (chunk, length) = c < chunkCount - 1 ? _previous![c] : (_current, _currentLength);
                var last = c == chunkCount - 1;
                var position = 0;
                bool completed;

                // The encoder keeps a high surrogate at the end of a chunk until the next one
                do
                {
                    var (charsUsed, bytesUsed, done) = Encode(encoder, chunk.AsSpan(position, length - position), bytes, last);
                    position += charsUsed;
                    completed = done;
                    await stream.WriteAsync(bytes.AsMemory(0, bytesUsed), cancellationToken).ConfigureAwait(false);
                }
                while (!completed);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytes, clearArray: true);
        }

        static (int CharsUsed, int BytesUsed, bool Completed) Encode(System.Text.Encoder encoder, ReadOnlySpan<char> chars, byte[] bytes,
                                                                    bool flush)
        {
            encoder.Convert(chars, bytes, flush, out var charsUsed, out var bytesUsed, out var completed);
            return (charsUsed, bytesUsed, completed);
        }
    }

    /// <summary>Writes the characters to <paramref name="writer"/>, chunk by chunk.</summary>
    public async ValueTask WriteToAsync(TextWriter writer, CancellationToken cancellationToken)
    {
        if (_previous != null)
        {
            foreach (var (chunk, length) in _previous)
            {
                await writer.WriteAsync(chunk.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
            }
        }

        await writer.WriteAsync(_current.AsMemory(0, _currentLength), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates a string from the written characters.</summary>
    public override string ToString()
    {
        if (_previous == null)
        {
            return new string(_current, 0, _currentLength);
        }

        return string.Create(Length, this, static (destination, buffer) =>
        {
            foreach (var (chunk, length) in buffer._previous!)
            {
                chunk.AsSpan(0, length).CopyTo(destination);
                destination = destination[length..];
            }

            buffer._current.AsSpan(0, buffer._currentLength).CopyTo(destination);
        });
    }

    public void Dispose()
    {
        if (_current.Length == 0)
        {
            return;
        }

        if (_previous != null)
        {
            foreach (var (chunk, length) in _previous)
            {
                ReturnChunk(chunk, length);
            }

            _previous = null;
        }

        ReturnChunk(_current, _currentLength);
        _current = [];
        _currentLength = 0;
        _previousLength = 0;
    }

    /// <summary>Keeps the current chunk and continues in a new one with room for at least <paramref name="sizeHint"/>.</summary>
    private void NextChunk(int sizeHint)
    {
        var size = Math.Max(sizeHint, Math.Min(_current.Length * 2, MaxChunkSize));
        (_previous ??= []).Add((_current, _currentLength));
        _previousLength += _currentLength;
        _current = ArrayPool<char>.Shared.Rent(size);
        _currentLength = 0;
    }

    private static void ReturnChunk(char[] chunk, int length)
    {
        chunk.AsSpan(0, length).Clear();
        ArrayPool<char>.Shared.Return(chunk);
    }
}
