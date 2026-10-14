using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Core.Serialization;

/// <summary>
/// Provides serialization and deserialization functionality for converting
/// between C# objects and the TOON format. This class includes synchronous
/// and asynchronous methods supporting various input and output options such as
/// strings, streams, and files.
/// </summary>
/// <remarks>
/// This type is thread-safe for concurrent use. Shared metadata caches are
/// backed by <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/> and are safe for
/// multithreaded access. For correctness, do not mutate a single
/// <see cref="ToonSerializerOptions"/> instance concurrently across threads.
/// Cache entries are retained for the lifetime of the process.
/// </remarks>
public static partial class ToonSerializer
{
    /// <summary>Why the serializer's methods are not compatible with trimming and NativeAOT.</summary>
    internal const string ReflectionMessage =
        "This overload converts .NET types with reflection, which trimming and NativeAOT cannot analyze. For " +
        "[ToonSerializable] types, use the overloads that take a ToonTypeInfo, such as Serialize(value, MyType.ToonTypeInfo).";

    /// <summary>
    ///     Why the reflection-based code may be trimmed and compiled ahead of time: it runs only under the methods above
    ///     (annotated as such), or from generated code for derived types and types without generated code, which need
    ///     reflection.
    /// </summary>
    internal const string ReflectionJustification =
        "Reached only from ToonSerializer methods annotated with RequiresUnreferencedCode and RequiresDynamicCode, or from " +
        "generated code when an object is of a derived type or a type without generated code, which need the reflection-based serializer.";

    /// <summary>Ends the previous document's last line and inserts one blank line.</summary>
    private const string BlankLineDocumentSeparator = "\n\n";

    #region public serialization methods

    /// <summary>
    ///     Serializes an object to TOON format string.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <returns>The TOON format string representation of the object.</returns>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static string Serialize<T>(T? value, ToonSerializerOptions? options = null)
    {
        return Serialize(value, typeof(T), options);
    }

    /// <summary>
    ///     Serializes an object to TOON format string (non-generic overload).
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="type">The type of object to serialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <returns>The TOON format string representation of the object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the type is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static string Serialize(object? value, Type type, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(type);
        options ??= ToonSerializerOptions.Default;

        using var buffer = new ToonTextBuffer();
        SerializeTo(buffer, value, type, options);
        return buffer.ToString();
    }

    /// <summary>
    ///     Serializes a value and writes it to <paramref name="stream"/> as UTF-8 in pooled chunks.
    /// </summary>
    private static async ValueTask SerializeToUtf8StreamAsync(object? value, Type type, Stream stream, ToonSerializerOptions? options,
                                                             CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var buffer = new ToonTextBuffer();
        SerializeTo(buffer, value, type, options ?? ToonSerializerOptions.Default);
        await buffer.WriteUtf8Async(stream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Converts an object to a <see cref="ToonValue"/> without encoding it to text.
    /// </summary>
    /// <typeparam name="T">The declared type of the value.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <returns>The TOON value; <see cref="ToonNull.Instance"/> when <paramref name="value"/> is null.</returns>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static ToonValue SerializeToValue<T>(T? value, ToonSerializerOptions? options = null)
    {
        return SerializeToValue(value, typeof(T), options);
    }

    /// <summary>
    ///     Converts an object to a <see cref="ToonValue"/> without encoding it to text (non-generic overload).
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="type">The declared type of the value.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <returns>The TOON value; <see cref="ToonNull.Instance"/> when <paramref name="value"/> is null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the type is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static ToonValue SerializeToValue(object? value, Type type, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(type);
        options ??= ToonSerializerOptions.Default;

        return SerializeValue(value, type, options, 0, new SerializerState()) ?? ToonNull.Instance;
    }

    /// <summary>
    ///     Asynchronously serializes an object to TOON format string.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous serialization operation.</returns>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    ///     This method performs CPU-bound synchronous serialization with an async signature for API consistency
    ///     and cancellation support. The ValueTask optimization ensures zero allocation in the common case.
    ///     For I/O-bound operations, use <see cref="SerializeToFileAsync{T}"/> or <see cref="SerializeToStreamAsync{T}(T, Stream, ToonSerializerOptions, CancellationToken)"/>.
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static ValueTask<string> SerializeAsync<T>(T? value, ToonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = Serialize(value, options);
        return new ValueTask<string>(result);
    }


    /// <summary>
    ///     Asynchronously serializes an object and writes it to a stream.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous serialization and write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="IOException">Thrown when stream I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    /// The text is encoded to UTF-8 in chunks through pooled buffers, without an intermediate string or a
    /// full-size byte array.
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async ValueTask SerializeToStreamAsync<T>(T? value, Stream stream, ToonSerializerOptions? options = null,
                                                            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        await SerializeToUtf8StreamAsync(value, typeof(T), stream, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Asynchronously serializes an object and writes it to a file.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="filePath">The file path to write to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous serialization and write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="IOException">Thrown when file I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async ValueTask SerializeToFileAsync<T>(T? value, string filePath, ToonSerializerOptions? options = null,
                                                          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        cancellationToken.ThrowIfCancellationRequested();

        // Serialize before creating the file, so a failure does not leave an empty or truncated file behind
        using var buffer = new ToonTextBuffer();
        SerializeTo(buffer, value, typeof(T), options ?? ToonSerializerOptions.Default);

        var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize: 1, useAsync: true);

        await using (fileStream.ConfigureAwait(false))
        {
            await buffer.WriteUtf8Async(fileStream, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Asynchronously serializes an object and writes it to a stream (non-generic overload).
    /// </summary>
    /// <param name="type">The type of object to serialize.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="stream">The stream to write the serialized data to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous serialization and write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="type"/> or <paramref name="stream"/> is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="IOException">Thrown when stream I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    /// The text is encoded to UTF-8 in chunks through pooled buffers, without an intermediate string or a
    /// full-size byte array.
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async ValueTask SerializeToStreamAsync(Type type, object? value, Stream stream, ToonSerializerOptions? options = null,
                                                         CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(stream);

        await SerializeToUtf8StreamAsync(value, type, stream, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Asynchronously serializes a collection of objects to a file with each object separated by a blank line.
    /// </summary>
    /// <typeparam name="T">The type of objects to serialize.</typeparam>
    /// <param name="values">The collection of values to serialize.</param>
    /// <param name="filePath">The file path to write to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous serialization and write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when values or filePath is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="IOException">Thrown when file I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    ///     This method writes each object as a separate TOON document, separated by blank lines.
    ///     This format is compatible with DeserializeDocumentsAsync for reading back multiple objects.
    ///     An object that encodes to an empty document would be lost on reading, so it throws a
    ///     <see cref="ToonSerializationException"/>.
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async ValueTask SerializeCollectionToFileAsync<T>(IEnumerable<T> values, string filePath, ToonSerializerOptions? options = null,
                                                                    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(filePath);

        await WriteFileAsync(filePath, async fileStream =>
        {
            var writer = new StreamWriter(fileStream, ToonTextEncoding.Utf8NoBom);
            await using var writerDisposal = writer.ConfigureAwait(false);

            var isFirst = true;
            options ??= ToonSerializerOptions.Default;
            using var buffer = new ToonTextBuffer();

            foreach (var value in values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Add a blank line separator between objects (but not before the first)
                if (!isFirst)
                {
                    await writer.WriteAsync(BlankLineDocumentSeparator.AsMemory(), cancellationToken).ConfigureAwait(false);
                }

                // Each item is encoded into the same pooled buffer instead of a string per item
                buffer.Clear();
                SerializeTo(buffer, value, typeof(T), options);
                EnsureSeparableByBlankLines(buffer.Length, typeof(T));
                await buffer.WriteToAsync(writer, cancellationToken).ConfigureAwait(false);

                isFirst = false;
            }
        }, cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    ///     Asynchronously writes each item of a collection to a stream as a separate TOON document, separated by blank lines.
    /// </summary>
    /// <typeparam name="T">The type of objects to serialize.</typeparam>
    /// <param name="values">The collection of values to serialize.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous serialization and write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when values or stream is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="IOException">Thrown when stream I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    ///     This method writes each object as a separate TOON document, separated by blank lines.
    ///     This format is compatible with DeserializeDocumentsAsync for reading back multiple objects.
    ///     An object that encodes to an empty document would be lost on reading, so it throws a
    ///     <see cref="ToonSerializationException"/>.
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async ValueTask SerializeDocumentsAsync<T>(IEnumerable<T> values, Stream stream, ToonSerializerOptions? options = null,
                                                                      CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(stream);

        var writer = new StreamWriter(stream, ToonTextEncoding.Utf8NoBom, leaveOpen: true);

        await using var writerDisposal = writer.ConfigureAwait(false);

        var isFirst = true;
        options ??= ToonSerializerOptions.Default;
        using var buffer = new ToonTextBuffer();

        foreach (var value in values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Add a blank line separator between objects (but not before the first)
            if (!isFirst)
            {
                await writer.WriteAsync(BlankLineDocumentSeparator.AsMemory(), cancellationToken).ConfigureAwait(false);
            }

            // Each item is encoded into the same pooled buffer instead of a string per item
            buffer.Clear();
            SerializeTo(buffer, value, typeof(T), options);
            EnsureSeparableByBlankLines(buffer.Length, typeof(T));
            await buffer.WriteToAsync(writer, cancellationToken).ConfigureAwait(false);

            isFirst = false;
        }
    }

    /// <summary>
    ///     Asynchronously writes each item of an async sequence to a file as a separate TOON document, as the items arrive.
    /// </summary>
    /// <typeparam name="T">The type of objects to serialize.</typeparam>
    /// <param name="items">The async enumerable stream of items to serialize.</param>
    /// <param name="filePath">The file path to write to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous serialization and write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when items or filePath is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="IOException">Thrown when file I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    ///     This method is designed for large datasets that do not fit in memory (e.g., database queries, ETL pipelines).
    ///     Items are serialized incrementally as they are enumerated, with minimal memory overhead.
    ///     Uses blank-line separation by default. Compatible with <see cref="DeserializeDocumentsAsync{T}(string,ToonSerializerOptions,CancellationToken)"/>.
    /// </remarks>
    /// <example>
    ///     <code>
    ///     // Stream large dataset from database
    ///     await ToonSerializer.SerializeDocumentsAsync(
    ///         items: dbContext.Users.AsAsyncEnumerable(),
    ///         filePath: "users.toon",
    ///         cancellationToken: cts.Token
    ///     );
    ///     </code>
    /// </example>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async ValueTask SerializeDocumentsAsync<T>(IAsyncEnumerable<T> items, string filePath, ToonSerializerOptions? options = null,
                                                          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(filePath);

        await WriteFileAsync(filePath, fileStream => SerializeDocumentsAsync(items, fileStream, options, cancellationToken), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Asynchronously writes each item of an async sequence to a file as a separate TOON document, as the items arrive, separated as the write options say.
    /// </summary>
    /// <typeparam name="T">The type of objects to serialize.</typeparam>
    /// <param name="items">The async enumerable stream of items to serialize.</param>
    /// <param name="filePath">The file path to write to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="writeOptions">Options controlling multi-document separation and batching behavior.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous serialization and write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when items, filePath, or writeOptions is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="IOException">Thrown when file I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    ///     This overload allows configuring the separator mode (blank-line vs explicit separator) and batch size for optimized throughput.
    ///     Higher batch sizes reduce I/O overhead but increase memory usage. The default batch size is 50.
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async ValueTask SerializeDocumentsAsync<T>(IAsyncEnumerable<T> items, string filePath, ToonSerializerOptions? options,
                                                          ToonMultiDocumentWriteOptions writeOptions,
                                                          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(writeOptions);

        await WriteFileAsync(filePath, fileStream => SerializeDocumentsAsync(items, fileStream, options, writeOptions, cancellationToken), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Asynchronously writes each item of an async sequence to a stream as a separate TOON document, as the items arrive.
    /// </summary>
    /// <typeparam name="T">The type of objects to serialize.</typeparam>
    /// <param name="items">The async enumerable stream of items to serialize.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous serialization and write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when items or stream is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="IOException">Thrown when stream I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    ///     This method is designed for large datasets that do not fit in memory (e.g., database queries, ETL pipelines).
    ///     Items are serialized incrementally as they are enumerated, with minimal memory overhead.
    ///     Uses blank-line separation by default. Compatible with <see cref="DeserializeDocumentsAsync{T}(StreamReader,ToonSerializerOptions,CancellationToken)"/>.
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async ValueTask SerializeDocumentsAsync<T>(IAsyncEnumerable<T> items, Stream stream, ToonSerializerOptions? options = null,
                                                          CancellationToken cancellationToken = default)
    {
        await SerializeDocumentsAsync(items, stream, options, ToonMultiDocumentWriteOptions.BlankLine, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Asynchronously writes each item of an async sequence to a stream as a separate TOON document, as the items arrive, separated as the write options say.
    /// </summary>
    /// <typeparam name="T">The type of objects to serialize.</typeparam>
    /// <param name="items">The async enumerable stream of items to serialize.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="writeOptions">Options controlling multi-document separation and batching behavior.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous serialization and write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when items, stream, or writeOptions is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="IOException">Thrown when stream I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    ///     This method uses batched writes for improved throughput. Items are accumulated up to the configured batch size 
    ///     before writing to the stream, reducing I/O overhead. Memory usage is proportional to batch size × item size.
    ///     For very large items (MB+), consider using a smaller batch size.
    ///     An item that encodes to an empty document throws a <see cref="ToonSerializationException"/> in blank-line mode,
    ///     where it could not be read back; <see cref="ToonMultiDocumentSeparatorMode.ExplicitSeparator"/> keeps it.
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async ValueTask SerializeDocumentsAsync<T>(IAsyncEnumerable<T> items, Stream stream, ToonSerializerOptions? options,
                                                          ToonMultiDocumentWriteOptions writeOptions,
                                                          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(writeOptions);

        var writer = new StreamWriter(stream, ToonTextEncoding.Utf8NoBom, leaveOpen: true);

        await using var writerDisposal = writer.ConfigureAwait(false);

        ValidateSeparator(writeOptions.Mode, writeOptions.DocumentSeparator);

        var isFirst = true;
        var lastIsEmpty = false;
        using var batch = new ToonTextBuffer();
        var itemCount = 0;
        var batchSize = Math.Max(1, writeOptions.BatchSize); // Ensure at least 1
        options ??= ToonSerializerOptions.Default;

        await foreach (var item in items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Add a separator between documents. Encoded documents carry no trailing newline,
            // so the separator has to terminate the previous document's last line itself.
            if (!isFirst)
            {
                if (writeOptions.Mode == ToonMultiDocumentSeparatorMode.ExplicitSeparator)
                {
                    batch.Append('\n').Append(writeOptions.DocumentSeparator).Append('\n');
                }
                else
                {
                    batch.Append(BlankLineDocumentSeparator);
                }
            }

            // Serialize the item straight into the batch
            var start = batch.Length;
            SerializeTo(batch, item, typeof(T), options);
            var length = batch.Length - start;

            if (writeOptions.Mode != ToonMultiDocumentSeparatorMode.ExplicitSeparator)
            {
                EnsureSeparableByBlankLines(length, typeof(T));
            }
            else if (batch.HasLine(start, writeOptions.DocumentSeparator))
            {
                // It would end the document there when read (a root string "===" with the separator "===")
                throw new ToonSerializationException(
                    $"A document has a line that is the document separator, so it could not be read back as one document; choose a separator that cannot be a line of {typeof(T).Name}, such as the default \"---\".")
                {
                    TargetType = typeof(T)
                };
            }

            isFirst = false;
            lastIsEmpty = length == 0;
            itemCount++;

            // Flush batch when a size threshold reached
            if (itemCount < batchSize)
            {
                continue;
            }

            await batch.WriteToAsync(writer, cancellationToken).ConfigureAwait(false);
            batch.Clear();
            itemCount = 0;
        }

        // An empty last document needs a line of its own, or the reader would see nothing after the last separator
        if (lastIsEmpty)
        {
            batch.Append('\n');
        }

        // Flush remaining items
        if (batch.Length > 0)
        {
            await batch.WriteToAsync(writer, cancellationToken).ConfigureAwait(false);
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion


    /// <summary>
    ///     Deserializes a TOON format string to an object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="toonString">The TOON format string to deserialize.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized object, or null when the document is <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toonString"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static T? Deserialize<T>(string toonString, ToonSerializerOptions? options = null)
    {
        return (T?)Deserialize(toonString, typeof(T), options);
    }

    /// <summary>
    ///     Writes a file in place (as before 4.0: a temporary file and a move would lose the file's permissions, owner and
    ///     links). An item that fails to serialize leaves the items written before it.
    /// </summary>
    private static async ValueTask WriteFileAsync(string filePath, Func<FileStream, ValueTask> write, CancellationToken cancellationToken)
    {
        // A canceled write does not empty the file it would replace
        cancellationToken.ThrowIfCancellationRequested();

        var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);

        await using (fileStream.ConfigureAwait(false))
        {
            await write(fileStream).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Deserializes a TOON format string to an object of the specified type.
    /// </summary>
    /// <param name="toonString">The TOON format string to deserialize.</param>
    /// <param name="type">The target type to deserialize to.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized object, or null when the document is <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toonString"/> or <paramref name="type"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static object? Deserialize(string toonString, Type type, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(toonString);
        ArgumentNullException.ThrowIfNull(type);
        options ??= ToonSerializerOptions.Default;

        return DeserializeText(toonString, type, options);
    }

    /// <summary>
    ///     Converts a <see cref="ToonValue"/> to an object, for example a value taken from a parsed <see cref="ToonDocument"/>.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="value">The TOON value to convert.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The converted object, or null for <see cref="ToonNull"/> and nullable targets.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
    /// <exception cref="ToonSerializationException">Thrown when the value cannot be converted to the target type.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static T? DeserializeFromValue<T>(ToonValue value, ToonSerializerOptions? options = null)
    {
        return (T?)DeserializeFromValue(value, typeof(T), options);
    }

    /// <summary>
    ///     Converts a <see cref="ToonValue"/> to an object of the specified type.
    /// </summary>
    /// <param name="value">The TOON value to convert.</param>
    /// <param name="type">The type to convert to.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The converted object, or null for <see cref="ToonNull"/> and nullable targets.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the value or type is null.</exception>
    /// <exception cref="ToonSerializationException">Thrown when the value cannot be converted to the target type.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static object? DeserializeFromValue(ToonValue value, Type type, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(type);
        options ??= ToonSerializerOptions.Default;

        return DeserializeValue(value, type, options, 0, new SerializerState());
    }

    #region Async APIs

    /// <summary>
    ///     Asynchronously deserializes a TOON format string to an object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="toonString">The TOON format string to deserialize.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous deserialization operation.</returns>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    ///     This method performs CPU-bound synchronous deserialization with an async signature for API consistency
    ///     and cancellation support. The ValueTask optimization ensures zero allocation in the common case.
    ///     For I/O-bound operations, use <see cref="DeserializeFromFileAsync{T}"/> or <see cref="DeserializeFromStreamAsync{T}(Stream, ToonSerializerOptions, CancellationToken)"/>.
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static ValueTask<T?> DeserializeAsync<T>(string toonString, ToonSerializerOptions? options = null,
                                                     CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = Deserialize<T>(toonString, options);
        return new ValueTask<T?>(result);
    }


    /// <summary>
    ///     Asynchronously reads a file and deserializes its contents to an object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="filePath">The file path to read from.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read and deserialization operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async ValueTask<T?> DeserializeFromFileAsync<T>(string filePath, ToonSerializerOptions? options = null,
                                                                   CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        using var text = await ToonTextEncoding.ReadFileAsync(filePath, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return (T?)DeserializeText(text.Text, typeof(T), options ?? ToonSerializerOptions.Default);
    }


    /// <summary>
    ///     Asynchronously reads from a stream and deserializes the content to an object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read and deserialization operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async ValueTask<T?> DeserializeFromStreamAsync<T>(Stream stream, ToonSerializerOptions? options = null,
                                                                     CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return (T?)await DeserializeFromStreamAsync(stream, typeof(T), options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Asynchronously reads TOON documents separated by blank lines from a file, one item per document.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="filePath">The file path to read from.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable of deserialized objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    ///     This method assumes the file contains multiple TOON objects separated by blank lines.
    ///     Each object is parsed and yielded individually, making it memory-efficient for large files.
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async IAsyncEnumerable<T?> DeserializeDocumentsAsync<T>(string filePath, ToonSerializerOptions? options = null,
                                                                       [EnumeratorCancellation]
                                                                       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);

        await using var fileStreamDisposal = fileStream.ConfigureAwait(false);
        using var reader = new StreamReader(fileStream, ToonTextEncoding.Utf8StrictReader, detectEncodingFromByteOrderMarks: false);

        await foreach (var item in DeserializeDocumentsAsync<T>(reader, options, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <summary>
    ///     Asynchronously reads TOON documents from a file, one item per document, separated as the multi-document options say.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="filePath">The file path to read from.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="multiDocumentOptions">Options that control how multiple TOON documents are delimited.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable of deserialized objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath or multiDocumentOptions is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    ///     This overload supports both legacy blank-line separation and deterministic explicit separator lines (for example: <c>---</c>).
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async IAsyncEnumerable<T?> DeserializeDocumentsAsync<T>(string filePath, ToonSerializerOptions? options,
                                                                       ToonMultiDocumentReadOptions multiDocumentOptions,
                                                                       [EnumeratorCancellation]
                                                                       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(multiDocumentOptions);

        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);

        await using var fileStreamDisposal = fileStream.ConfigureAwait(false);
        using var reader = new StreamReader(fileStream, ToonTextEncoding.Utf8StrictReader, detectEncodingFromByteOrderMarks: false);

        await foreach (var item in DeserializeDocumentsAsync<T>(reader, options, multiDocumentOptions, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <summary>
    ///     Asynchronously reads TOON documents separated by blank lines from a StreamReader, one item per document.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="reader">The StreamReader to read from.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable of deserialized objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the reader is null.</exception>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    ///     This method assumes the stream contains multiple TOON objects separated by blank lines.
    ///     Each object is parsed and yielded individually, making it memory-efficient for large streams.
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async IAsyncEnumerable<T?> DeserializeDocumentsAsync<T>(StreamReader reader, ToonSerializerOptions? options = null,
                                                                       [EnumeratorCancellation]
                                                                       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);

        await foreach (var item in DeserializeDocumentsAsync<T>(reader, options, ToonMultiDocumentReadOptions.BlankLine, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <summary>
    ///     Asynchronously reads TOON documents from a StreamReader, one item per document, separated as the multi-document options say.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="reader">The StreamReader to read from.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="multiDocumentOptions">Options that control how multiple TOON documents are delimited.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable of deserialized objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the reader or multiDocumentOptions is null.</exception>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    ///     When using <see cref="ToonMultiDocumentSeparatorMode.ExplicitSeparator"/>, a separator line is recognized only when the line matches exactly.
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async IAsyncEnumerable<T?> DeserializeDocumentsAsync<T>(StreamReader reader, ToonSerializerOptions? options,
                                                                       ToonMultiDocumentReadOptions multiDocumentOptions,
                                                                       [EnumeratorCancellation]
                                                                       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(multiDocumentOptions);
        ValidateSeparator(multiDocumentOptions.Mode, multiDocumentOptions.DocumentSeparator);

        var currentObject = new StringBuilder();
        var lines = new ToonLineReader(reader);

        while (await lines.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // A blank line has nothing but spaces (spec §12); tabs and other characters are content
            var isBoundary = multiDocumentOptions.Mode switch
            {
                ToonMultiDocumentSeparatorMode.ExplicitSeparator => line == multiDocumentOptions.DocumentSeparator,
                _                                                => line.AsSpan().TrimEnd(' ').IsEmpty
            };

            if (isBoundary)
            {
                if (currentObject.Length <= 0)
                {
                    continue;
                }

                var toonString = currentObject.ToString();
                currentObject.Clear();

                var obj = await DeserializeAsync<T>(toonString, options, cancellationToken).ConfigureAwait(false);
                yield return obj;

                continue;
            }

            currentObject.Append(line).Append('\n');
        }

        if (currentObject.Length <= 0)
        {
            yield break;
        }

        var lastToonString = currentObject.ToString();
        var lastObj = await DeserializeAsync<T>(lastToonString, options, cancellationToken).ConfigureAwait(false);
        yield return lastObj;
    }

    /// <summary>
    ///     Reads TOON lines from a <see cref="StreamReader"/>: lines end at LF only and a CR before it is removed, so a
    ///     CR anywhere else is content, as spec §12 says (<see cref="StreamReader.ReadLineAsync()"/> also splits at a
    ///     lone CR).
    /// </summary>
    private sealed class ToonLineReader(StreamReader reader)
    {
        private readonly char[] _buffer = new char[4096];
        private readonly StringBuilder _line = new();
        private int _start;
        private int _end;

        /// <summary>Returns the next line, or null at the end of the input.</summary>
        /// <exception cref="ToonParseException">Thrown when the input is not valid UTF-8.</exception>
        public async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        {
            _line.Clear();

            while (true)
            {
                if (_start == _end)
                {
                    int read;

                    try
                    {
                        read = await reader.ReadAsync(_buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                    }
                    catch (DecoderFallbackException)
                    {
                        throw ToonTextEncoding.InvalidUtf8();
                    }

                    if (read == 0)
                    {
                        return _line.Length > 0 ? Finish() : null;
                    }

                    _start = 0;
                    _end = read;
                }

                if (TakeLine())
                {
                    return Finish();
                }
            }
        }

        /// <summary>Moves buffered text to the line; returns whether it reached the end of the line.</summary>
        private bool TakeLine()
        {
            var text = _buffer.AsSpan(_start, _end - _start);
            var newline = text.IndexOf('\n');

            if (newline < 0)
            {
                _line.Append(text);
                _start = _end;
                return false;
            }

            _line.Append(text[..newline]);
            _start += newline + 1;
            return true;
        }

        private string Finish()
        {
            if (_line.Length > 0 && _line[^1] == '\r')
            {
                _line.Length--;
            }

            return _line.ToString();
        }
    }

    /// <summary>
    ///     Rejects a document that encodes to nothing (an object without written properties) in blank-line separated
    ///     output, where it would vanish: the reader cannot tell it from the separator.
    /// </summary>
    /// <exception cref="ToonSerializationException">Thrown when <paramref name="documentLength"/> is 0.</exception>
    private static void EnsureSeparableByBlankLines(int documentLength, Type type)
    {
        if (documentLength == 0)
        {
            throw new ToonSerializationException($"An item of type {type.Name} encodes to an empty TOON document, which cannot be " +
                                                 "written to blank-line separated output without being lost when it is read back. " +
                                                 "Use ToonMultiDocumentWriteOptions.ExplicitSeparator.") { TargetType = type };
        }
    }

    /// <exception cref="ArgumentException">Thrown when an explicit separator is empty, blank or spans several lines.</exception>
    private static void ValidateSeparator(ToonMultiDocumentSeparatorMode mode, string? separator)
    {
        if (mode == ToonMultiDocumentSeparatorMode.ExplicitSeparator &&
            (string.IsNullOrWhiteSpace(separator) || separator.AsSpan().IndexOfAny('\r', '\n') >= 0))
        {
            throw new ArgumentException("DocumentSeparator must be a single, non-blank line", nameof(separator));
        }
    }

    #endregion

}
