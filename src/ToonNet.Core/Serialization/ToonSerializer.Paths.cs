using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;
using ToonNet.Core.Paths;

namespace ToonNet.Core.Serialization;

// Deserialization of the value at a path. In strict mode the parser's cursors walk to the value, skipping what comes
// before it without building it, read the value straight from the text and stop: what follows the value is not read.
// Non-strict mode lets a repeated key replace an earlier one (§14.4), which is only known at the end of an object, so
// the document is parsed into a tree there and the path followed in the tree.
public static partial class ToonSerializer
{
    /// <summary>
    ///     Deserializes the value at a path of a document, such as the list inside a response envelope, without building
    ///     or converting the rest: <c>DeserializeAt&lt;List&lt;Order&gt;&gt;(toon, "$.data.orders")</c>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="toon">The TOON text.</param>
    /// <param name="path">A singular JSONPath: member names and indexes only (<c>$.data.orders</c>, <c>$.items[0]</c>, <c>$.items[-1]</c>).</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized value, or <c>default</c> when the document has no value at the path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toon"/> or <paramref name="path"/> is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="ArgumentException">Thrown when the path is not singular.</exception>
    /// <exception cref="ToonParseException">Thrown when the text up to the end of the value is not valid TOON.</exception>
    /// <exception cref="ToonSerializationException">Thrown when the value cannot be converted (its path in the document is in the error).</exception>
    /// <remarks>
    ///     In strict mode (the default), reading stops at the end of the value: the text after it is not parsed, so errors
    ///     there are not reported, and a value near the start of a large document is read in a fraction of the time the
    ///     whole document takes. Non-strict mode parses the whole document.
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static T? DeserializeAt<T>(string toon, string path, ToonSerializerOptions? options = null)
    {
        return Unbox<T>(DeserializeAt(toon, path, typeof(T), options));
    }

    /// <summary>
    ///     Deserializes the value at a path of a document to <paramref name="type"/>; see
    ///     <see cref="DeserializeAt{T}(string, string, ToonSerializerOptions)"/>.
    /// </summary>
    /// <param name="toon">The TOON text.</param>
    /// <param name="path">A singular JSONPath: member names and indexes only.</param>
    /// <param name="type">The type to deserialize to.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized value, or null when the document has no value at the path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="ArgumentException">Thrown when the path is not singular.</exception>
    /// <exception cref="ToonParseException">Thrown when the text up to the end of the value is not valid TOON.</exception>
    /// <exception cref="ToonSerializationException">Thrown when the value cannot be converted.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static object? DeserializeAt(string toon, string path, Type type, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(toon);
        ArgumentNullException.ThrowIfNull(type);

        return DeserializeAtText(toon, SingularPath(path, options), type, options ?? ToonSerializerOptions.Default);
    }

    /// <summary>
    ///     Deserializes the value at a path of UTF-8 encoded TOON text, without creating an intermediate string; see
    ///     <see cref="DeserializeAt{T}(string, string, ToonSerializerOptions)"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Toon">The UTF-8 encoded TOON text; a byte order mark at the start is ignored.</param>
    /// <param name="path">A singular JSONPath: member names and indexes only.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized value, or <c>default</c> when the document has no value at the path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="ArgumentException">Thrown when the path is not singular.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8, or not valid TOON up to the end of the value.</exception>
    /// <exception cref="ToonSerializationException">Thrown when the value cannot be converted.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static T? DeserializeAt<T>(ReadOnlySpan<byte> utf8Toon, string path, ToonSerializerOptions? options = null)
    {
        var compiled = SingularPath(path, options);
        using var text = ToonTextEncoding.Decode(utf8Toon);
        return Unbox<T>(DeserializeAtText(text.Text, compiled, typeof(T), options ?? ToonSerializerOptions.Default));
    }

    /// <summary>
    ///     Reads UTF-8 encoded TOON text from a stream to its end and deserializes the value at a path; see
    ///     <see cref="DeserializeAt{T}(string, string, ToonSerializerOptions)"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Toon">The stream; it is read to the end and left open.</param>
    /// <param name="path">A singular JSONPath: member names and indexes only.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The deserialized value, or <c>default</c> when the document has no value at the path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Toon"/> or <paramref name="path"/> is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="ArgumentException">Thrown when the path is not singular.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8, or not valid TOON up to the end of the value.</exception>
    /// <exception cref="ToonSerializationException">Thrown when the value cannot be converted.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async ValueTask<T?> DeserializeAtAsync<T>(Stream utf8Toon, string path, ToonSerializerOptions? options = null,
                                                            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(utf8Toon);
        var compiled = SingularPath(path, options);

        using var text = await ToonTextEncoding.ReadAsync(utf8Toon, cancellationToken).ConfigureAwait(false);
        return Unbox<T>(DeserializeAtText(text.Text, compiled, typeof(T), options ?? ToonSerializerOptions.Default));
    }

    /// <summary>
    ///     Deserializes the value at a path of a document with the type info of its type, without reflection; see
    ///     <see cref="DeserializeAt{T}(string, string, ToonSerializerOptions)"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="toon">The TOON text.</param>
    /// <param name="path">A singular JSONPath: member names and indexes only.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized value, or <c>default</c> when the document has no value at the path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="ArgumentException">Thrown when the path is not singular.</exception>
    /// <exception cref="NotSupportedException">Thrown when generated code cannot create <typeparamref name="T"/>, or the options need a newer generator.</exception>
    /// <exception cref="ToonParseException">Thrown when the text up to the end of the value is not valid TOON.</exception>
    /// <exception cref="ToonSerializationException">Thrown when the value cannot be converted.</exception>
    public static T? DeserializeAt<T>(string toon, string path, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(toon);
        return DeserializeAtText(toon, SingularPath(path, options), typeInfo, options);
    }

    /// <summary>
    ///     Deserializes the value at a path of UTF-8 encoded TOON text with the type info of its type, without reflection;
    ///     see <see cref="DeserializeAt{T}(string, string, ToonSerializerOptions)"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Toon">The UTF-8 encoded TOON text; a byte order mark at the start is ignored.</param>
    /// <param name="path">A singular JSONPath: member names and indexes only.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized value, or <c>default</c> when the document has no value at the path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> or <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="ArgumentException">Thrown when the path is not singular.</exception>
    /// <exception cref="NotSupportedException">Thrown when generated code cannot create <typeparamref name="T"/>, or the options need a newer generator.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8, or not valid TOON up to the end of the value.</exception>
    /// <exception cref="ToonSerializationException">Thrown when the value cannot be converted.</exception>
    public static T? DeserializeAt<T>(ReadOnlySpan<byte> utf8Toon, string path, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null)
    {
        var compiled = SingularPath(path, options);
        using var text = ToonTextEncoding.Decode(utf8Toon);
        return DeserializeAtText(text.Text, compiled, typeInfo, options);
    }

    /// <summary>
    ///     Reads UTF-8 encoded TOON text from a stream to its end and deserializes the value at a path with the type info
    ///     of its type, without reflection; see <see cref="DeserializeAt{T}(string, string, ToonSerializerOptions)"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Toon">The stream; it is read to the end and left open.</param>
    /// <param name="path">A singular JSONPath: member names and indexes only.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The deserialized value, or <c>default</c> when the document has no value at the path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Toon"/>, <paramref name="path"/> or <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="ArgumentException">Thrown when the path is not singular.</exception>
    /// <exception cref="NotSupportedException">Thrown when generated code cannot create <typeparamref name="T"/>, or the options need a newer generator.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8, or not valid TOON up to the end of the value.</exception>
    /// <exception cref="ToonSerializationException">Thrown when the value cannot be converted.</exception>
    public static async ValueTask<T?> DeserializeAtAsync<T>(Stream utf8Toon, string path, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null,
                                                            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(utf8Toon);
        var compiled = SingularPath(path, options);
        ArgumentNullException.ThrowIfNull(typeInfo);

        using var text = await ToonTextEncoding.ReadAsync(utf8Toon, cancellationToken).ConfigureAwait(false);
        return DeserializeAtText(text.Text, compiled, typeInfo, options);
    }

    /// <summary>
    ///     Deserializes every value a path selects, such as the rows that pass a filter, straight from the text:
    ///     <c>DeserializeAll&lt;Order&gt;(toon, "$.orders[?@.total &gt; 100]")</c>. What the path does not select is skipped
    ///     without being built or converted.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="toon">The TOON text.</param>
    /// <param name="path">A JSONPath (RFC 9535); see <see cref="ToonPath"/>.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The converted values, in the order <see cref="ToonPath.Select"/> gives them; empty when there are none.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toon"/> or <paramref name="path"/> is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid TOON.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted (its path in the document is in the error).</exception>
    /// <remarks>
    ///     The whole document is read and checked; a filter that only compares members reads just those members of each
    ///     child, and only the selected values are converted. See <see cref="ToonPath.SelectFrom(string, ToonOptions)"/>.
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static List<T?> DeserializeAll<T>(string toon, string path, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(toon);
        return DeserializeAllText<T>(toon, ToonPath.Parse(path, options?.PathLimits), options ?? ToonSerializerOptions.Default);
    }

    /// <summary>
    ///     Deserializes every value a path selects in UTF-8 encoded TOON text; see
    ///     <see cref="DeserializeAll{T}(string, string, ToonSerializerOptions)"/>.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="utf8Toon">The UTF-8 encoded TOON text; a byte order mark at the start is ignored.</param>
    /// <param name="path">A JSONPath (RFC 9535).</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The converted values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8 or not valid TOON.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static List<T?> DeserializeAll<T>(ReadOnlySpan<byte> utf8Toon, string path, ToonSerializerOptions? options = null)
    {
        var compiled = ToonPath.Parse(path, options?.PathLimits);
        using var text = ToonTextEncoding.Decode(utf8Toon);
        return DeserializeAllText<T>(text.Text, compiled, options ?? ToonSerializerOptions.Default);
    }

    /// <summary>
    ///     Reads UTF-8 encoded TOON text from a stream to its end and deserializes every value a path selects; see
    ///     <see cref="DeserializeAll{T}(string, string, ToonSerializerOptions)"/>.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="utf8Toon">The stream; it is read to the end and left open.</param>
    /// <param name="path">A JSONPath (RFC 9535).</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The converted values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Toon"/> or <paramref name="path"/> is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8 or not valid TOON.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static async ValueTask<List<T?>> DeserializeAllAsync<T>(Stream utf8Toon, string path, ToonSerializerOptions? options = null,
                                                                  CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(utf8Toon);
        var compiled = ToonPath.Parse(path, options?.PathLimits);

        using var text = await ToonTextEncoding.ReadAsync(utf8Toon, cancellationToken).ConfigureAwait(false);
        return DeserializeAllText<T>(text.Text, compiled, options ?? ToonSerializerOptions.Default);
    }

    /// <summary>
    ///     Deserializes every value a path selects with the type info of their type, without reflection; see
    ///     <see cref="DeserializeAll{T}(string, string, ToonSerializerOptions)"/>.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="toon">The TOON text.</param>
    /// <param name="path">A JSONPath (RFC 9535).</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The converted values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="NotSupportedException">Thrown when generated code cannot create <typeparamref name="T"/>, or the options need a newer generator.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid TOON.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted.</exception>
    public static List<T?> DeserializeAll<T>(string toon, string path, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(toon);
        return DeserializeAllText(toon, ToonPath.Parse(path, options?.PathLimits), typeInfo, options);
    }

    /// <summary>
    ///     Deserializes every value a path selects in UTF-8 encoded TOON text with the type info of their type, without
    ///     reflection; see <see cref="DeserializeAll{T}(string, string, ToonSerializerOptions)"/>.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="utf8Toon">The UTF-8 encoded TOON text; a byte order mark at the start is ignored.</param>
    /// <param name="path">A JSONPath (RFC 9535).</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The converted values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> or <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="NotSupportedException">Thrown when generated code cannot create <typeparamref name="T"/>, or the options need a newer generator.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8 or not valid TOON.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted.</exception>
    public static List<T?> DeserializeAll<T>(ReadOnlySpan<byte> utf8Toon, string path, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null)
    {
        var compiled = ToonPath.Parse(path, options?.PathLimits);
        using var text = ToonTextEncoding.Decode(utf8Toon);
        return DeserializeAllText(text.Text, compiled, typeInfo, options);
    }

    /// <summary>
    ///     Reads UTF-8 encoded TOON text from a stream to its end and deserializes every value a path selects with the type
    ///     info of their type, without reflection; see <see cref="DeserializeAll{T}(string, string, ToonSerializerOptions)"/>.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="utf8Toon">The stream; it is read to the end and left open.</param>
    /// <param name="path">A JSONPath (RFC 9535).</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The converted values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Toon"/>, <paramref name="path"/> or <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="NotSupportedException">Thrown when generated code cannot create <typeparamref name="T"/>, or the options need a newer generator.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8 or not valid TOON.</exception>
    /// <exception cref="ToonSerializationException">Thrown when a value cannot be converted.</exception>
    public static async ValueTask<List<T?>> DeserializeAllAsync<T>(Stream utf8Toon, string path, ToonTypeInfo<T> typeInfo,
                                                                  ToonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(utf8Toon);
        var compiled = ToonPath.Parse(path, options?.PathLimits);
        ArgumentNullException.ThrowIfNull(typeInfo);

        using var text = await ToonTextEncoding.ReadAsync(utf8Toon, cancellationToken).ConfigureAwait(false);
        return DeserializeAllText(text.Text, compiled, typeInfo, options);
    }

    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    private static List<T?> DeserializeAllText<T>(ToonText text, ToonPath path, ToonSerializerOptions options)
    {
        var sink = new ObjectSink<T>(options);
        path.Evaluate(text, options.ToonOptions, sink);
        return sink.Items;
    }

    private static List<T?> DeserializeAllText<T>(ToonText text, ToonPath path, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options)
    {
        options = CheckTypeInfo(typeInfo, options);
        CheckGeneratedReading(typeInfo, options);

        var sink = new TypeInfoSink<T>(typeInfo, options);
        path.Evaluate(text, options.ToonOptions, sink);
        return sink.Items;
    }

    /// <summary>Converts the selected values with reflection; errors get the value's path in the document.</summary>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    private sealed class ObjectSink<T>(ToonSerializerOptions options) : ToonPathTextEvaluator.ISink
    {
        public readonly List<T?> Items = [];

        public bool WantsLocations => true;

        public void Add(ToonParser parser, in ToonParser.ValueRef value, ToonPathTextEvaluator.LocationStack location)
        {
            try
            {
                Items.Add(Unbox<T>(ReadAt(parser, value, typeof(T), options)));
            }
            catch (ToonException ex)
            {
                ex.PrependPath(location.Format());
                throw;
            }
        }

        public void Add(ToonValue tree, ToonPathTextEvaluator.LocationStack location, ToonPathEvaluator.Location? relative)
        {
            try
            {
                Items.Add(Unbox<T>(DeserializeValue(tree, typeof(T), options, 0, new SerializerState())));
            }
            catch (ToonException ex)
            {
                ex.PrependPath(location.Format(relative));
                throw;
            }
        }
    }

    /// <summary>Converts the selected values with a type info; errors get the value's path in the document.</summary>
    private sealed class TypeInfoSink<T>(ToonTypeInfo<T> typeInfo, ToonSerializerOptions options) : ToonPathTextEvaluator.ISink
    {
        public readonly List<T?> Items = [];

        public bool WantsLocations => true;

        public void Add(ToonParser parser, in ToonParser.ValueRef value, ToonPathTextEvaluator.LocationStack location)
        {
            try
            {
                Items.Add(ReadAt(parser, value, typeInfo, options));
            }
            catch (ToonException ex)
            {
                ex.PrependPath(location.Format());
                throw;
            }
        }

        public void Add(ToonValue tree, ToonPathTextEvaluator.LocationStack location, ToonPathEvaluator.Location? relative)
        {
            try
            {
                Items.Add(DeserializeFromValue(tree, typeInfo, options));
            }
            catch (ToonException ex)
            {
                ex.PrependPath(location.Format(relative));
                throw;
            }
        }
    }

    /// <summary>The value as <typeparamref name="T"/>; null (no value at the path) is <c>default</c>, also for value types.</summary>
    private static T? Unbox<T>(object? value) => value is null ? default : (T)value;

    /// <summary>Compiles a path that must select at most one value.</summary>
    private static ToonPath SingularPath(string path, ToonSerializerOptions? options)
    {
        ArgumentNullException.ThrowIfNull(path);
        var compiled = ToonPath.Parse(path, options?.PathLimits);

        return compiled.IsSingular
            ? compiled
            : throw new ArgumentException(
                $"The path '{path}' can select several values; DeserializeAt needs member names and indexes only. Use ToonPath.Select for queries.",
                nameof(path));
    }

    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    private static object? DeserializeAtText(ToonText text, ToonPath path, Type type, ToonSerializerOptions options)
    {
        if (!options.ToonOptions.StrictMode)
        {
            var root = new ToonParser(options.ToonOptions).Parse(text).Root;

            return FindInTree(root, path.Query, out var tree, out var treePrefix)
                ? WithPathPrefix(treePrefix, () => DeserializeValue(tree, type, options, 0, new SerializerState()))
                : null;
        }

        var parser = new ToonParser(options.ToonOptions);
        parser.BeginDocument(text);

        try
        {
            if (!FindInText(parser, path.Query, out var value, out var prefix))
            {
                return null;
            }

            try
            {
                return ReadAt(parser, value, type, options);
            }
            catch (ToonException ex)
            {
                ex.PrependPath(prefix);
                throw;
            }
        }
        finally
        {
            parser.ReleaseDocument();
        }
    }

    private static T? DeserializeAtText<T>(ToonText text, ToonPath path, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options)
    {
        options = CheckTypeInfo(typeInfo, options);
        CheckGeneratedReading(typeInfo, options);

        if (!options.ToonOptions.StrictMode)
        {
            var root = new ToonParser(options.ToonOptions).Parse(text).Root;

            return FindInTree(root, path.Query, out var tree, out var treePrefix)
                ? WithPathPrefix(treePrefix, () => DeserializeFromValue(tree, typeInfo, options))
                : default;
        }

        var parser = new ToonParser(options.ToonOptions);
        parser.BeginDocument(text);

        try
        {
            if (!FindInText(parser, path.Query, out var value, out var prefix))
            {
                return default;
            }

            try
            {
                return ReadAt(parser, value, typeInfo, options);
            }
            catch (ToonException ex)
            {
                ex.PrependPath(prefix);
                throw;
            }
        }
        finally
        {
            parser.ReleaseDocument();
        }
    }

    private static TResult WithPathPrefix<TResult>(string prefix, Func<TResult> read)
    {
        try
        {
            return read();
        }
        catch (ToonException ex)
        {
            ex.PrependPath(prefix);
            throw;
        }
    }

    /// <summary>
    ///     Walks a singular path with the parser's cursors, skipping the members and items before each step, and returns
    ///     the value it leads to, with its path as errors give it (<c>$.data.orders[2]</c>).
    /// </summary>
    private static bool FindInText(ToonParser parser, Query query, out ToonParser.ValueRef value, out string prefix)
    {
        value = parser.ReadRoot();
        var path = new StringBuilder("$");

        foreach (var segment in query.Segments)
        {
            var selector = segment.Selectors[0];

            if (selector.Kind == SelectorKind.Name)
            {
                if (value.NodeKind != ToonNodeKind.Object || !TryFindMember(parser, value, selector.Name!, out value))
                {
                    prefix = "";
                    return false;
                }

                AppendName(path, selector.Name!);
            }
            else
            {
                if (value.NodeKind != ToonNodeKind.Array || !TryFindItem(parser, value, selector.Index, out value, out var index))
                {
                    prefix = "";
                    return false;
                }

                path.Append('[').Append(index.ToString(CultureInfo.InvariantCulture)).Append(']');
            }
        }

        prefix = path.ToString();
        return true;
    }

    private static bool TryFindMember(ToonParser parser, in ToonParser.ValueRef obj, string name, out ToonParser.ValueRef member)
    {
        var cursor = parser.BeginObject(obj);

        while (parser.NextProperty(ref cursor, out var key, out member))
        {
            if (key == name)
            {
                return true;
            }

            parser.Skip(member);
        }

        member = default;
        return false;
    }

    private static bool TryFindItem(ToonParser parser, in ToonParser.ValueRef array, long index, out ToonParser.ValueRef item, out int position)
    {
        // A negative index counts from the declared length, which strict mode holds the array to
        var length = array.Kind == ToonParser.RefKind.HeaderArray ? parser.HeaderOf(array.HeaderIndex).Length : 0;
        position = ToonPathEvaluator.ResolveIndex(index, length);

        if (position < 0)
        {
            item = default;
            return false;
        }

        var cursor = parser.BeginArray(array);

        for (var i = 0; parser.NextItem(ref cursor, out item); i++)
        {
            if (i == position)
            {
                return true;
            }

            parser.Skip(item);
        }

        item = default;
        return false;
    }

    /// <summary>Follows a singular path in a tree.</summary>
    private static bool FindInTree(ToonValue root, Query query, [NotNullWhen(true)] out ToonValue? value, out string prefix)
    {
        value = root;
        var path = new StringBuilder("$");

        foreach (var segment in query.Segments)
        {
            var selector = segment.Selectors[0];

            if (selector.Kind == SelectorKind.Name)
            {
                if (value is not ToonObject obj || !obj.Properties.TryGetValue(selector.Name!, out value))
                {
                    prefix = "";
                    return false;
                }

                AppendName(path, selector.Name!);
            }
            else
            {
                if (value is not ToonArray array || ToonPathEvaluator.ResolveIndex(selector.Index, array.Items.Count) is not (var index and >= 0))
                {
                    value = null;
                    prefix = "";
                    return false;
                }

                value = array.Items[index];
                path.Append('[').Append(index.ToString(CultureInfo.InvariantCulture)).Append(']');
            }
        }

        prefix = path.ToString();
        return true;
    }

    private static void AppendName(StringBuilder path, string name) => path.Append('.').Append(name);
}
