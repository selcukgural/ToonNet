using System.Text;

namespace ToonNet.AspNetCore.Mvc.Formatters;

/// <summary>Reading TOON request bodies, shared by <see cref="ToonInputFormatter"/> and <c>ReadFromToonAsync</c>.</summary>
internal static class RequestBody
{
    private const int ReadBufferSize = 16 * 1024;

    /// <summary>Reads a body in an encoding other than UTF-8 as text.</summary>
    /// <exception cref="DecoderFallbackException">Thrown when the body is not valid text in the encoding.</exception>
    public static async Task<string> ReadTextAsync(Stream body, Encoding encoding, CancellationToken cancellationToken)
    {
        // Strict decoding in the negotiated charset (a byte order mark at the start is read as U+FEFF, which TOON ignores)
        var strict = Encoding.GetEncoding(encoding.CodePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        using var reader = new StreamReader(body, strict, detectEncodingFromByteOrderMarks: false, ReadBufferSize, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Whether decoded text has more than whitespace (spaces, tabs, line breaks) and a byte order mark.</summary>
    public static bool HasContent(string text) => text.AsSpan().TrimStart('\uFEFF').IndexOfAnyExcept(" \t\r\n") >= 0;
}

/// <summary>
///     A read-only view of a stream that throws once more than a given number of bytes has been read, and notes whether
///     a UTF-8 body has anything other than whitespace (bodies in other charsets are checked once decoded, with
///     <see cref="RequestBody.HasContent"/>).
/// </summary>
internal sealed class LimitedReadStream(Stream inner, long limit, Func<Exception> createLimitException) : Stream
{
    private static readonly System.Buffers.SearchValues<byte> Blank = System.Buffers.SearchValues.Create(" \t\r\n"u8);

    private long _read;

    /// <summary>
    ///     Whether a UTF-8 body has a byte other than whitespace and a byte order mark at its start; only these bytes are
    ///     whitespace in UTF-8 (the bytes of other characters are 0x80 and above).
    /// </summary>
    public bool HasContent { get; private set; }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer) => Count(buffer, inner.Read(buffer));

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        return Count(buffer.Span, read);
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    private int Count(ReadOnlySpan<byte> buffer, int read)
    {
        var start = _read;
        _read += read;

        if (_read > limit)
        {
            throw createLimitException();
        }

        if (!HasContent)
        {
            var bytes = buffer[..read];

            // The byte order mark EF BB BF at the start of the body is not content
            while (start < 3 && bytes.Length > 0 && bytes[0] == "\uFEFF"u8[(int)start])
            {
                bytes = bytes[1..];
                start++;
            }

            HasContent = bytes.IndexOfAnyExcept(Blank) >= 0;
        }

        return read;
    }
}
