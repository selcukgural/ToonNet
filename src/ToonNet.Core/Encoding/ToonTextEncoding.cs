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
    public static readonly System.Text.UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    ///     UTF-8 that throws <see cref="System.Text.DecoderFallbackException"/> for ill-formed input instead of
    ///     substituting U+FFFD, as decoders of byte input must (spec §4).
    /// </summary>
    public static readonly System.Text.UTF8Encoding Utf8Strict = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Reads a stream to the end as strict UTF-8.</summary>
    /// <exception cref="ToonParseException">Thrown when the stream is not valid UTF-8.</exception>
    public static async Task<string> ReadToEndAsync(Stream stream, bool leaveOpen, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(stream, Utf8Strict, detectEncodingFromByteOrderMarks: false, bufferSize: -1, leaveOpen);
            return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (System.Text.DecoderFallbackException ex)
        {
            throw InvalidUtf8(ex);
        }
    }

    /// <summary>Reads a file as strict UTF-8.</summary>
    /// <exception cref="ToonParseException">Thrown when the file is not valid UTF-8.</exception>
    public static async Task<string> ReadFileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            return await File.ReadAllTextAsync(path, Utf8Strict, cancellationToken).ConfigureAwait(false);
        }
        catch (System.Text.DecoderFallbackException ex)
        {
            throw InvalidUtf8(ex);
        }
    }

    /// <summary>The error for ill-formed UTF-8 input.</summary>
    public static ToonParseException InvalidUtf8(System.Text.DecoderFallbackException _) =>
        new("The input is not valid UTF-8", 0, 0);
}
