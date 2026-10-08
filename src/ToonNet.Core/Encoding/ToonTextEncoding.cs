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
}
