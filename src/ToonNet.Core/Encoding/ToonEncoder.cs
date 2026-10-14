using ToonNet.Core.Models;

namespace ToonNet.Core.Encoding;

/// <summary>
///     Provides functionality to encode a <see cref="ToonDocument"/> into a TOON format string.
/// </summary>
/// <param name="options">
///     Optional encoding options to customize the behavior of the encoder. If not provided, 
///     the default options (<see cref="ToonOptions.Default"/>) will be used.
/// </param>
/// <remarks>
///     The output follows the canonical encoding of TOON spec v4.4: arrays of uniform objects use the tabular form (with
///     nested field groups and the keyed form where they apply), primitive arrays are written inline, other arrays as
///     expanded lists, empty arrays as <c>[]</c>, numbers in canonical decimal form, and strings and keys are quoted only
///     when the spec requires it.
///     The document has LF line endings, no trailing spaces and no trailing newline.
///     An instance is not thread-safe; use one encoder per thread.
/// </remarks>
/// <example>
///     <code>
///     var document = new ToonDocument(new ToonObject
///     {
///         { "key", new ToonString("value") }
///     });
///     var encoder = new ToonEncoder();
///     var encodedString = encoder.Encode(document);
///     Console.WriteLine(encodedString);
///     </code>
/// </example>
public sealed class ToonEncoder(ToonOptions? options = null)
{
    private readonly ToonOptions _options = options ?? ToonOptions.Default;

    /// <summary>
    ///     Encodes a TOON document into its string representation.
    /// </summary>
    /// <param name="document">
    ///     The document to encode. This parameter must not be null.
    /// </param>
    /// <returns>
    ///     The encoded TOON format string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when a document is null.
    /// </exception>
    /// <exception cref="ToonEncodingException">
    ///     Thrown when encoding exceeds the maximum depth specified in the options.
    /// </exception>
    public string Encode(ToonDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var buffer = new ToonTextBuffer();
        Write(document, buffer);
        return buffer.ToString();
    }

    /// <summary>
    ///     Encodes a TOON document into UTF-8 encoded text (without a byte order mark), without creating an intermediate
    ///     string.
    /// </summary>
    /// <param name="document">The document to encode.</param>
    /// <returns>The UTF-8 encoded TOON text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a document is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when encoding exceeds the maximum depth specified in the options.</exception>
    public byte[] EncodeToUtf8Bytes(ToonDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var buffer = new ToonTextBuffer();
        Write(document, buffer);
        return buffer.ToUtf8Bytes();
    }

    /// <summary>
    ///     Encodes a TOON document and writes it as UTF-8 encoded text (without a byte order mark) to a buffer writer,
    ///     such as a <c>PipeWriter</c> or an <see cref="System.Buffers.ArrayBufferWriter{T}"/>.
    /// </summary>
    /// <param name="document">The document to encode.</param>
    /// <param name="utf8Destination">The buffer writer to write to; the text is written only after the document is encoded.</param>
    /// <exception cref="ArgumentNullException">Thrown when a document or the destination is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when encoding exceeds the maximum depth specified in the options.</exception>
    public void Encode(ToonDocument document, System.Buffers.IBufferWriter<byte> utf8Destination)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(utf8Destination);

        using var buffer = new ToonTextBuffer();
        Write(document, buffer);
        buffer.WriteUtf8(utf8Destination);
    }

    /// <summary>
    ///     Encodes a document into <paramref name="buffer"/>, so stream output does not need an intermediate string.
    /// </summary>
    internal void Write(ToonDocument document, ToonTextBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(document);

        _ = _options.ValidatedMaxDepth;

        new ToonDomWriter(new ToonWriter(buffer, _options)).WriteRoot(document.Root);
    }

    /// <summary>
    ///     Asynchronously encodes a TOON document into its string representation.
    /// </summary>
    /// <param name="document">The document to encode.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous encoding operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a document is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public async Task<string> EncodeAsync(ToonDocument document, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Encode(document);
        }, cancellationToken);
    }

    /// <summary>
    ///     Asynchronously encodes a TOON document and writes it to a file.
    /// </summary>
    /// <param name="document">The document to encode.</param>
    /// <param name="filePath">The file path to write to.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous encoding and write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a document or filePath is null.</exception>
    /// <exception cref="IOException">Thrown when file I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public async Task EncodeToFileAsync(ToonDocument document, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        // Encode before creating the file, so a failure does not leave an empty or truncated file behind
        using var buffer = await EncodeToBufferAsync(document, cancellationToken).ConfigureAwait(false);
        var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize: 1, useAsync: true);

        await using (fileStream.ConfigureAwait(false))
        {
            await buffer.WriteUtf8Async(fileStream, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Asynchronously encodes a TOON document and writes it to a stream.
    /// </summary>
    /// <param name="document">The document to encode.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous encoding and write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a document or stream is null.</exception>
    /// <exception cref="IOException">Thrown when stream I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    /// The text is encoded to UTF-8 in chunks through pooled buffers, without an intermediate string or a
    /// full-size byte array.
    /// </remarks>
    public async Task EncodeToStreamAsync(ToonDocument document, Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var buffer = await EncodeToBufferAsync(document, cancellationToken).ConfigureAwait(false);
        await buffer.WriteUtf8Async(stream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Encodes a document into a new pooled buffer on the thread pool, like <see cref="EncodeAsync"/>; the caller
    ///     disposes the buffer.
    /// </summary>
    private async Task<ToonTextBuffer> EncodeToBufferAsync(ToonDocument document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var buffer = new ToonTextBuffer();

        try
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                Write(document, buffer);
            }, cancellationToken).ConfigureAwait(false);

            return buffer;
        }
        catch
        {
            buffer.Dispose();
            throw;
        }
    }
}