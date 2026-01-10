namespace ToonNet.Core.Serialization;

/// <summary>
///     Specifies how multiple TOON documents are delimited when deserializing from a stream.
/// </summary>
public enum ToonMultiDocumentSeparatorMode
{
    /// <summary>
    ///     Documents are separated by blank lines.
    /// </summary>
    BlankLine = 0,

    /// <summary>
    ///     Documents are separated by an explicit separator line (for example: <c>---</c>).
    /// </summary>
    ExplicitSeparator = 1
}

/// <summary>
///     Options for deserializing multiple TOON documents from a single stream.
/// </summary>
public sealed class ToonMultiDocumentReadOptions
{
    /// <summary>
    ///     Gets a shared instance configured for blank-line separation.
    /// </summary>
    public static ToonMultiDocumentReadOptions BlankLine { get; } = new()
    {
        Mode = ToonMultiDocumentSeparatorMode.BlankLine
    };

    /// <summary>
    ///     Gets a shared instance configured for explicit separator (<c>---</c>) separation.
    /// </summary>
    public static ToonMultiDocumentReadOptions ExplicitSeparator { get; } = new()
    {
        Mode = ToonMultiDocumentSeparatorMode.ExplicitSeparator,
        DocumentSeparator = "---"
    };

    /// <summary>
    ///     Gets or sets the multi-document separation mode.
    /// </summary>
    public ToonMultiDocumentSeparatorMode Mode { get; init; } = ToonMultiDocumentSeparatorMode.BlankLine;

    /// <summary>
    ///     Gets or sets the separator line used when <see cref="Mode"/> is <see cref="ToonMultiDocumentSeparatorMode.ExplicitSeparator"/>.
    /// </summary>
    public string DocumentSeparator { get; init; } = "---";
}
