using System.Buffers;
using System.Text;
using System.Text.Unicode;

namespace ToonNet.Core.Encoding;

/// <summary>
///     The text encoding used when TOON is written to files and streams.
/// </summary>
internal static class ToonTextEncoding
{
    /// <summary>
    ///     UTF-8 without a byte order mark; with <see cref="System.Text.Encoding.UTF8"/>, <c>StreamWriter</c> and
    ///     <c>File.WriteAllText</c> prepend one, which other decoders read as part of the first key.
    /// </summary>
    public static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    ///     UTF-8 that throws <see cref="DecoderFallbackException"/> for ill-formed input instead of
    ///     substituting U+FFFD, as decoders of byte input must (spec §4).
    /// </summary>
    public static readonly UTF8Encoding Utf8Strict = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>The size of the byte buffer streams are read through.</summary>
    private const int ByteChunkSize = 16 * 1024;

    /// <summary>Decodes strict UTF-8 into a pooled character buffer, which the caller disposes.</summary>
    /// <exception cref="ToonParseException">Thrown when the bytes are not valid UTF-8.</exception>
    public static ToonPooledText Decode(ReadOnlySpan<byte> utf8)
    {
        // A UTF-8 sequence of n bytes decodes to at most n UTF-16 characters
        var text = new ToonPooledText(utf8.Length);

        try
        {
            var status = Utf8.ToUtf16(utf8, text.Chars, out _, out var written, replaceInvalidSequences: false);

            if (status != OperationStatus.Done)
            {
                throw InvalidUtf8();
            }

            text.Length = written;
            return text;
        }
        catch
        {
            text.Dispose();
            throw;
        }
    }

    /// <summary>Reads a stream to the end as strict UTF-8 into a pooled character buffer, which the caller disposes.</summary>
    /// <exception cref="ToonParseException">Thrown when the stream is not valid UTF-8.</exception>
    public static ToonPooledText Read(Stream stream)
    {
        var reader = new Utf8StreamDecoder(stream);

        try
        {
            int read;

            while ((read = stream.Read(reader.Bytes)) > 0)
            {
                reader.Decode(read);
            }

            return reader.Complete();
        }
        catch (DecoderFallbackException)
        {
            reader.Fail();
            throw InvalidUtf8();
        }
        catch
        {
            reader.Fail();
            throw;
        }
    }

    /// <summary>
    ///     Reads a stream to the end as strict UTF-8 into a pooled character buffer, which the caller disposes.
    /// </summary>
    /// <exception cref="ToonParseException">Thrown when the stream is not valid UTF-8.</exception>
    public static async ValueTask<ToonPooledText> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var reader = new Utf8StreamDecoder(stream);

        try
        {
            int read;

            while ((read = await stream.ReadAsync(reader.BytesMemory, cancellationToken).ConfigureAwait(false)) > 0)
            {
                reader.Decode(read);
            }

            return reader.Complete();
        }
        catch (DecoderFallbackException)
        {
            reader.Fail();
            throw InvalidUtf8();
        }
        catch
        {
            reader.Fail();
            throw;
        }
    }

    /// <summary>Reads a file as strict UTF-8 into a pooled character buffer, which the caller disposes.</summary>
    /// <exception cref="ToonParseException">Thrown when the file is not valid UTF-8.</exception>
    public static async ValueTask<ToonPooledText> ReadFileAsync(string path, CancellationToken cancellationToken)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1, useAsync: true);

        await using (stream.ConfigureAwait(false))
        {
            return await ReadAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>The error for ill-formed UTF-8 input.</summary>
    public static ToonParseException InvalidUtf8() => new("The input is not valid UTF-8", 0, 0);

    /// <summary>
    ///     Decodes a stream read in chunks: bytes go through a pooled buffer, characters into a pooled buffer that grows
    ///     as needed. A sequence split across chunks is kept by the decoder until the next one.
    /// </summary>
    private struct Utf8StreamDecoder
    {
        private readonly System.Text.Decoder _decoder;
        private ToonPooledText _text;

        public readonly byte[] Bytes;

        public Utf8StreamDecoder(Stream stream)
        {
            _decoder = Utf8Strict.GetDecoder();
            Bytes = ArrayPool<byte>.Shared.Rent(ByteChunkSize);

            // A seekable stream tells how many bytes are left, which bounds the number of characters
            var capacity = ByteChunkSize;

            if (stream.CanSeek)
            {
                capacity = (int)Math.Clamp(stream.Length - stream.Position + 2, 2, Array.MaxLength);
            }

            _text = new ToonPooledText(capacity);
        }

        public readonly Memory<byte> BytesMemory => Bytes;

        public void Decode(int count)
        {
            // A pending partial sequence completes to at most two characters
            _text.EnsureCapacity(count + 2);
            _text.Length += _decoder.GetChars(Bytes.AsSpan(0, count), _text.Chars.AsSpan(_text.Length), flush: false);
            Bytes.AsSpan(0, count).Clear();
        }

        public ToonPooledText Complete()
        {
            _text.EnsureCapacity(2);
            _text.Length += _decoder.GetChars([], _text.Chars.AsSpan(_text.Length), flush: true);
            ArrayPool<byte>.Shared.Return(Bytes);
            return _text;
        }

        public void Fail()
        {
            ArrayPool<byte>.Shared.Return(Bytes, clearArray: true);
            _text.Dispose();
        }
    }
}
