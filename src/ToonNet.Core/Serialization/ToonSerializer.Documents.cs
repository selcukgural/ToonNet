using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ToonNet.Core.Encoding;

namespace ToonNet.Core.Serialization;

// Multi-document reading from a stream, and the names the multi-document methods had before 3.2 (SerializeStreamAsync,
// DeserializeStreamAsync and SerializeCollectionToStreamAsync), which were easy to confuse with the methods that write
// or read one document to or from a stream.
public static partial class ToonSerializer
{
    private const string RenamedToSerializeDocuments =
        "Renamed to SerializeDocumentsAsync, which writes each item as a separate TOON document. This name will be removed in a future major version.";

    private const string RenamedToDeserializeDocuments =
        "Renamed to DeserializeDocumentsAsync, which reads each TOON document as an item. This name will be removed in a future major version.";

    /// <summary>
    ///     Asynchronously deserializes TOON documents separated by blank lines from a UTF-8 stream, one item per document.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Stream">The stream to read; it is left open.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable of deserialized objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static IAsyncEnumerable<T?> DeserializeDocumentsAsync<T>(Stream utf8Stream, ToonSerializerOptions? options = null,
                                                                   CancellationToken cancellationToken = default) =>
        DeserializeDocumentsAsync<T>(utf8Stream, options, ToonMultiDocumentReadOptions.BlankLine, cancellationToken);

    /// <summary>
    ///     Asynchronously deserializes TOON documents from a UTF-8 stream, one item per document, separated as the
    ///     multi-document options say.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Stream">The stream to read; it is left open.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="multiDocumentOptions">Options that control how multiple TOON documents are delimited.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable of deserialized objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream or multiDocumentOptions is null.</exception>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async IAsyncEnumerable<T?> DeserializeDocumentsAsync<T>(Stream utf8Stream, ToonSerializerOptions? options,
                                                                         ToonMultiDocumentReadOptions multiDocumentOptions,
                                                                         [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(utf8Stream);
        ArgumentNullException.ThrowIfNull(multiDocumentOptions);

        // As the file overloads read: strict UTF-8, a byte order mark at the start is skipped
        using var reader = new StreamReader(utf8Stream, ToonTextEncoding.Utf8Strict, detectEncodingFromByteOrderMarks: true, bufferSize: -1, leaveOpen: true);

        await foreach (var item in DeserializeDocumentsAsync<T>(reader, options, multiDocumentOptions, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    #region Names before 3.2

    /// <summary>Renamed to <see cref="SerializeDocumentsAsync{T}(IEnumerable{T}, Stream, ToonSerializerOptions, CancellationToken)"/>.</summary>
    /// <typeparam name="T">The type of objects to serialize.</typeparam>
    /// <param name="values">The collection of values to serialize.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous serialization and write operation.</returns>
    [Obsolete(RenamedToSerializeDocuments)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static ValueTask SerializeCollectionToStreamAsync<T>(IEnumerable<T> values, Stream stream, ToonSerializerOptions? options = null,
                                                                CancellationToken cancellationToken = default) =>
        SerializeDocumentsAsync(values, stream, options, cancellationToken);

    /// <summary>Renamed to <see cref="SerializeDocumentsAsync{T}(IAsyncEnumerable{T}, string, ToonSerializerOptions, CancellationToken)"/>.</summary>
    /// <typeparam name="T">The type of objects to serialize.</typeparam>
    /// <param name="items">The async enumerable stream of items to serialize.</param>
    /// <param name="filePath">The file path to write to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous serialization and write operation.</returns>
    [Obsolete(RenamedToSerializeDocuments)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static ValueTask SerializeStreamAsync<T>(IAsyncEnumerable<T> items, string filePath, ToonSerializerOptions? options = null,
                                                    CancellationToken cancellationToken = default) =>
        SerializeDocumentsAsync(items, filePath, options, cancellationToken);

    /// <summary>Renamed to <see cref="SerializeDocumentsAsync{T}(IAsyncEnumerable{T}, string, ToonSerializerOptions, ToonMultiDocumentWriteOptions, CancellationToken)"/>.</summary>
    /// <typeparam name="T">The type of objects to serialize.</typeparam>
    /// <param name="items">The async enumerable stream of items to serialize.</param>
    /// <param name="filePath">The file path to write to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="writeOptions">Options controlling multi-document separation and batching behavior.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous serialization and write operation.</returns>
    [Obsolete(RenamedToSerializeDocuments)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static ValueTask SerializeStreamAsync<T>(IAsyncEnumerable<T> items, string filePath, ToonSerializerOptions? options,
                                                    ToonMultiDocumentWriteOptions writeOptions, CancellationToken cancellationToken = default) =>
        SerializeDocumentsAsync(items, filePath, options, writeOptions, cancellationToken);

    /// <summary>Renamed to <see cref="SerializeDocumentsAsync{T}(IAsyncEnumerable{T}, Stream, ToonSerializerOptions, CancellationToken)"/>.</summary>
    /// <typeparam name="T">The type of objects to serialize.</typeparam>
    /// <param name="items">The async enumerable stream of items to serialize.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous serialization and write operation.</returns>
    [Obsolete(RenamedToSerializeDocuments)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static ValueTask SerializeStreamAsync<T>(IAsyncEnumerable<T> items, Stream stream, ToonSerializerOptions? options = null,
                                                    CancellationToken cancellationToken = default) =>
        SerializeDocumentsAsync(items, stream, options, cancellationToken);

    /// <summary>Renamed to <see cref="SerializeDocumentsAsync{T}(IAsyncEnumerable{T}, Stream, ToonSerializerOptions, ToonMultiDocumentWriteOptions, CancellationToken)"/>.</summary>
    /// <typeparam name="T">The type of objects to serialize.</typeparam>
    /// <param name="items">The async enumerable stream of items to serialize.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="writeOptions">Options controlling multi-document separation and batching behavior.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous serialization and write operation.</returns>
    [Obsolete(RenamedToSerializeDocuments)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static ValueTask SerializeStreamAsync<T>(IAsyncEnumerable<T> items, Stream stream, ToonSerializerOptions? options,
                                                    ToonMultiDocumentWriteOptions writeOptions, CancellationToken cancellationToken = default) =>
        SerializeDocumentsAsync(items, stream, options, writeOptions, cancellationToken);

    /// <summary>Renamed to <see cref="DeserializeDocumentsAsync{T}(string, ToonSerializerOptions, CancellationToken)"/>.</summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="filePath">The file path to read from.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable of deserialized objects.</returns>
    [Obsolete(RenamedToDeserializeDocuments)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static IAsyncEnumerable<T?> DeserializeStreamAsync<T>(string filePath, ToonSerializerOptions? options = null,
                                                                CancellationToken cancellationToken = default) =>
        DeserializeDocumentsAsync<T>(filePath, options, cancellationToken);

    /// <summary>Renamed to <see cref="DeserializeDocumentsAsync{T}(string, ToonSerializerOptions, ToonMultiDocumentReadOptions, CancellationToken)"/>.</summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="filePath">The file path to read from.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="multiDocumentOptions">Options that control how multiple TOON documents are delimited.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable of deserialized objects.</returns>
    [Obsolete(RenamedToDeserializeDocuments)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static IAsyncEnumerable<T?> DeserializeStreamAsync<T>(string filePath, ToonSerializerOptions? options,
                                                                ToonMultiDocumentReadOptions multiDocumentOptions,
                                                                CancellationToken cancellationToken = default) =>
        DeserializeDocumentsAsync<T>(filePath, options, multiDocumentOptions, cancellationToken);

    /// <summary>Renamed to <see cref="DeserializeDocumentsAsync{T}(StreamReader, ToonSerializerOptions, CancellationToken)"/>.</summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="reader">The StreamReader to read from.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable of deserialized objects.</returns>
    [Obsolete(RenamedToDeserializeDocuments)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static IAsyncEnumerable<T?> DeserializeStreamAsync<T>(StreamReader reader, ToonSerializerOptions? options = null,
                                                                CancellationToken cancellationToken = default) =>
        DeserializeDocumentsAsync<T>(reader, options, cancellationToken);

    /// <summary>Renamed to <see cref="DeserializeDocumentsAsync{T}(StreamReader, ToonSerializerOptions, ToonMultiDocumentReadOptions, CancellationToken)"/>.</summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="reader">The StreamReader to read from.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="multiDocumentOptions">Options that control how multiple TOON documents are delimited.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable of deserialized objects.</returns>
    [Obsolete(RenamedToDeserializeDocuments)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static IAsyncEnumerable<T?> DeserializeStreamAsync<T>(StreamReader reader, ToonSerializerOptions? options,
                                                                ToonMultiDocumentReadOptions multiDocumentOptions,
                                                                CancellationToken cancellationToken = default) =>
        DeserializeDocumentsAsync<T>(reader, options, multiDocumentOptions, cancellationToken);

    #endregion
}
