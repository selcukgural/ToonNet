using System.Buffers;
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
    public static string Serialize(object? value, Type type, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(type);
        options ??= ToonSerializerOptions.Default;

        var toonValue = SerializeValue(value, type, options, 0, new SerializerState());
        var document = new ToonDocument(toonValue ?? ToonNull.Instance);
        var encoder = new ToonEncoder(options.ToonOptions);

        return encoder.Encode(document);
    }

    /// <summary>
    ///     Converts an object to a <see cref="ToonValue"/> without encoding it to text.
    /// </summary>
    /// <typeparam name="T">The declared type of the value.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <returns>The TOON value; <see cref="ToonNull.Instance"/> when <paramref name="value"/> is null.</returns>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
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
    ///     For I/O-bound operations, use <see cref="SerializeToFileAsync{T}"/> or <see cref="SerializeToStreamAsync{T}"/>.
    /// </remarks>
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
    /// This method uses ArrayPool&lt;byte&gt; for efficient memory usage.
    /// Suitable for high-throughput scenarios with minimal GC pressure.
    /// </remarks>
    public static async ValueTask SerializeToStreamAsync<T>(T? value, Stream stream, ToonSerializerOptions? options = null,
                                                            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var toonString = await SerializeAsync(value, options, cancellationToken).ConfigureAwait(false);
        
        // Use ArrayPool to avoid allocating byte arrays
        var encoding = System.Text.Encoding.UTF8;
        var maxByteCount = encoding.GetMaxByteCount(toonString.Length);
        var buffer = ArrayPool<byte>.Shared.Rent(maxByteCount);
        
        try
        {
            var bytesWritten = encoding.GetBytes(toonString, 0, toonString.Length, buffer, 0);
            await stream.WriteAsync(buffer.AsMemory(0, bytesWritten), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
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
    public static async ValueTask SerializeToFileAsync<T>(T? value, string filePath, ToonSerializerOptions? options = null,
                                                          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        var toonString = await SerializeAsync(value, options, cancellationToken).ConfigureAwait(false);

        await File.WriteAllTextAsync(filePath, toonString, ToonTextEncoding.Utf8NoBom, cancellationToken).ConfigureAwait(false);
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
    /// This method uses ArrayPool&lt;byte&gt; for efficient memory usage.
    /// Suitable for high-throughput scenarios with minimal GC pressure.
    /// </remarks>
    public static async ValueTask SerializeToStreamAsync(Type type, object? value, Stream stream, ToonSerializerOptions? options = null,
                                                         CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(stream);

        cancellationToken.ThrowIfCancellationRequested();
        var toonString = Serialize(value, type, options);
        
        // Use ArrayPool to avoid allocating byte arrays
        var encoding = System.Text.Encoding.UTF8;
        var maxByteCount = encoding.GetMaxByteCount(toonString.Length);
        var buffer = ArrayPool<byte>.Shared.Rent(maxByteCount);
        
        try
        {
            var bytesWritten = encoding.GetBytes(toonString, 0, toonString.Length, buffer, 0);
            await stream.WriteAsync(buffer.AsMemory(0, bytesWritten), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
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
    ///     This format is compatible with DeserializeStreamAsync for reading back multiple objects.
    /// </remarks>
    public static async ValueTask SerializeCollectionToFileAsync<T>(IEnumerable<T> values, string filePath, ToonSerializerOptions? options = null,
                                                                    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(filePath);

        var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);

        await using var fileStreamDisposal = fileStream.ConfigureAwait(false);
        var writer = new StreamWriter(fileStream, ToonTextEncoding.Utf8NoBom);
        await using var writerDisposal = writer.ConfigureAwait(false);

        var isFirst = true;

        foreach (var value in values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Add a blank line separator between objects (but not before the first)
            if (!isFirst)
            {
                await writer.WriteAsync(BlankLineDocumentSeparator.AsMemory(), cancellationToken).ConfigureAwait(false);
            }

            var toonString = await SerializeAsync(value, options, cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync(toonString.AsMemory(), cancellationToken).ConfigureAwait(false);

            isFirst = false;
        }
    }


    /// <summary>
    ///     Asynchronously serializes a collection of objects to a stream with each object separated by a blank line.
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
    ///     This format is compatible with DeserializeStreamAsync for reading back multiple objects.
    /// </remarks>
    public static async ValueTask SerializeCollectionToStreamAsync<T>(IEnumerable<T> values, Stream stream, ToonSerializerOptions? options = null,
                                                                      CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(stream);

        var writer = new StreamWriter(stream, ToonTextEncoding.Utf8NoBom, leaveOpen: true);

        await using var writerDisposal = writer.ConfigureAwait(false);

        var isFirst = true;

        foreach (var value in values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Add a blank line separator between objects (but not before the first)
            if (!isFirst)
            {
                await writer.WriteAsync(BlankLineDocumentSeparator.AsMemory(), cancellationToken).ConfigureAwait(false);
            }

            var toonString = await SerializeAsync(value, options, cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync(toonString.AsMemory(), cancellationToken).ConfigureAwait(false);

            isFirst = false;
        }
    }

    /// <summary>
    ///     Asynchronously serializes a stream of objects to a file with memory-efficient incremental serialization.
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
    ///     Uses blank-line separation by default. Compatible with <see cref="DeserializeStreamAsync{T}(string,ToonSerializerOptions,CancellationToken)"/>.
    /// </remarks>
    /// <example>
    ///     <code>
    ///     // Stream large dataset from database
    ///     await ToonSerializer.SerializeStreamAsync(
    ///         items: dbContext.Users.AsAsyncEnumerable(),
    ///         filePath: "users.toon",
    ///         cancellationToken: cts.Token
    ///     );
    ///     </code>
    /// </example>
    public static async ValueTask SerializeStreamAsync<T>(IAsyncEnumerable<T> items, string filePath, ToonSerializerOptions? options = null,
                                                          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(filePath);

        var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);

        await using var fileStreamDisposal = fileStream.ConfigureAwait(false);
        await SerializeStreamAsync(items, fileStream, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Asynchronously serializes a stream of objects to a file with memory-efficient incremental serialization and custom write options.
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
    public static async ValueTask SerializeStreamAsync<T>(IAsyncEnumerable<T> items, string filePath, ToonSerializerOptions? options,
                                                          ToonMultiDocumentWriteOptions writeOptions,
                                                          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(writeOptions);

        var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);

        await using var fileStreamDisposal = fileStream.ConfigureAwait(false);
        await SerializeStreamAsync(items, fileStream, options, writeOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Asynchronously serializes a stream of objects to a stream with memory-efficient incremental serialization.
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
    ///     Uses blank-line separation by default. Compatible with <see cref="DeserializeStreamAsync{T}(StreamReader,ToonSerializerOptions,CancellationToken)"/>.
    /// </remarks>
    public static async ValueTask SerializeStreamAsync<T>(IAsyncEnumerable<T> items, Stream stream, ToonSerializerOptions? options = null,
                                                          CancellationToken cancellationToken = default)
    {
        await SerializeStreamAsync(items, stream, options, ToonMultiDocumentWriteOptions.BlankLine, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Asynchronously serializes a stream of objects to a stream with memory-efficient incremental serialization and custom write options.
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
    /// </remarks>
    public static async ValueTask SerializeStreamAsync<T>(IAsyncEnumerable<T> items, Stream stream, ToonSerializerOptions? options,
                                                          ToonMultiDocumentWriteOptions writeOptions,
                                                          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(writeOptions);

        var writer = new StreamWriter(stream, ToonTextEncoding.Utf8NoBom, leaveOpen: true);

        await using var writerDisposal = writer.ConfigureAwait(false);

        var isFirst = true;
        var batch = new StringBuilder();
        var itemCount = 0;
        var batchSize = Math.Max(1, writeOptions.BatchSize); // Ensure at least 1

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

            // Serialize item
            var toonString = await SerializeAsync(item, options, cancellationToken).ConfigureAwait(false);
            batch.Append(toonString);

            isFirst = false;
            itemCount++;

            // Flush batch when a size threshold reached
            if (itemCount < batchSize)
            {
                continue;
            }

            await writer.WriteAsync(batch.ToString().AsMemory(), cancellationToken).ConfigureAwait(false);
            batch.Clear();
            itemCount = 0;
        }

        // Flush remaining items
        if (batch.Length > 0)
        {
            await writer.WriteAsync(batch.ToString().AsMemory(), cancellationToken).ConfigureAwait(false);
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
    /// <returns>The deserialized object, or null if the input is null.</returns>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    public static T? Deserialize<T>(string toonString, ToonSerializerOptions? options = null)
    {
        return (T?)Deserialize(toonString, typeof(T), options);
    }

    /// <summary>
    ///     Deserializes a TOON format string to an object of the specified type.
    /// </summary>
    /// <param name="toonString">The TOON format string to deserialize.</param>
    /// <param name="type">The target type to deserialize to.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized object, or null if the input is null.</returns>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted to the target type.</exception>
    public static object? Deserialize(string toonString, Type type, ToonSerializerOptions? options = null)
    {
        options ??= ToonSerializerOptions.Default;

        var parser = new ToonParser(options.ToonOptions);
        var document = parser.Parse(toonString);

        return DeserializeValue(document.Root, type, options, 0, new SerializerState());
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
    ///     For I/O-bound operations, use <see cref="DeserializeFromFileAsync{T}"/> or <see cref="DeserializeFromStreamAsync{T}"/>.
    /// </remarks>
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
    public static async ValueTask<T?> DeserializeFromFileAsync<T>(string filePath, ToonSerializerOptions? options = null,
                                                                   CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        var toonString = await File.ReadAllTextAsync(filePath, System.Text.Encoding.UTF8, cancellationToken).ConfigureAwait(false);

        return await DeserializeAsync<T>(toonString, options, cancellationToken).ConfigureAwait(false);
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
    public static async ValueTask<T?> DeserializeFromStreamAsync<T>(Stream stream, ToonSerializerOptions? options = null,
                                                                     CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var toonString = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        return await DeserializeAsync<T>(toonString, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Asynchronously deserializes a stream of TOON objects from a file.
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
    public static async IAsyncEnumerable<T?> DeserializeStreamAsync<T>(string filePath, ToonSerializerOptions? options = null,
                                                                       [EnumeratorCancellation]
                                                                       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);

        await using var fileStreamDisposal = fileStream.ConfigureAwait(false);
        using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8);

        await foreach (var item in DeserializeStreamAsync<T>(reader, options, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <summary>
    ///     Asynchronously deserializes a stream of TOON objects from a file using multi-document options.
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
    public static async IAsyncEnumerable<T?> DeserializeStreamAsync<T>(string filePath, ToonSerializerOptions? options,
                                                                       ToonMultiDocumentReadOptions multiDocumentOptions,
                                                                       [EnumeratorCancellation]
                                                                       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(multiDocumentOptions);

        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);

        await using var fileStreamDisposal = fileStream.ConfigureAwait(false);
        using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8);

        await foreach (var item in DeserializeStreamAsync<T>(reader, options, multiDocumentOptions, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <summary>
    ///     Asynchronously deserializes a stream of TOON objects from a StreamReader.
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
    public static async IAsyncEnumerable<T?> DeserializeStreamAsync<T>(StreamReader reader, ToonSerializerOptions? options = null,
                                                                       [EnumeratorCancellation]
                                                                       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);

        await foreach (var item in DeserializeStreamAsync<T>(reader, options, ToonMultiDocumentReadOptions.BlankLine, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <summary>
    ///     Asynchronously deserializes a stream of TOON objects from a StreamReader using multi-document options.
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
    public static async IAsyncEnumerable<T?> DeserializeStreamAsync<T>(StreamReader reader, ToonSerializerOptions? options,
                                                                       ToonMultiDocumentReadOptions multiDocumentOptions,
                                                                       [EnumeratorCancellation]
                                                                       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(multiDocumentOptions);

        var currentObject = new StringBuilder();

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isBoundary = multiDocumentOptions.Mode switch
            {
                ToonMultiDocumentSeparatorMode.BlankLine         => string.IsNullOrWhiteSpace(line),
                ToonMultiDocumentSeparatorMode.ExplicitSeparator => line == multiDocumentOptions.DocumentSeparator,
                _                                                => string.IsNullOrWhiteSpace(line)
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

            currentObject.AppendLine(line);
        }

        if (currentObject.Length <= 0)
        {
            yield break;
        }

        var lastToonString = currentObject.ToString();
        var lastObj = await DeserializeAsync<T>(lastToonString, options, cancellationToken).ConfigureAwait(false);
        yield return lastObj;
    }

    #endregion

}
