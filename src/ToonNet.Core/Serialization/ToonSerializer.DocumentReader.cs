using System.Diagnostics.CodeAnalysis;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Core.Serialization;

// Deserialization of the value a ToonDocumentReader is on, as System.Text.Json reads from a Utf8JsonReader: the value is
// read straight from the text, and when that gives up (or fails, to report the error as the tree-based path does) the
// parser returns to the start of the value and the value is built as a tree and converted.
public static partial class ToonSerializer
{
    /// <summary>
    ///     Reads the value a <see cref="ToonDocumentReader"/> is on and converts it to <typeparamref name="T"/>, straight
    ///     from the text, so a large document can be read partly by hand and partly into objects.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="reader">
    ///     The reader, on a property name (its value is read), on the start of an object or array, or on a primitive; a
    ///     reader that has not read yet reads its first token. Afterwards it is on the value's last token, as after
    ///     <see cref="ToonDocumentReader.ReadValue"/>.
    /// </param>
    /// <param name="options">Optional deserialization options; the reader's own options decide how the text is parsed.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reader"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the reader is not on a property name or the start of a value.</exception>
    /// <exception cref="ToonParseException">Thrown when the value is not valid TOON; the reader cannot be used afterwards.</exception>
    /// <exception cref="ToonSerializationException">Thrown when the value cannot be converted; the reader cannot be used afterwards.</exception>
    /// <example>
    ///     <code>
    ///     using var reader = new ToonDocumentReader(utf8);
    ///     while (reader.Read())
    ///     {
    ///         if (reader.TokenType == ToonTokenType.PropertyName &amp;&amp; reader.GetPropertyName() == "orders")
    ///         {
    ///             var orders = ToonSerializer.Deserialize&lt;List&lt;Order&gt;&gt;(reader);
    ///         }
    ///     }
    ///     </code>
    /// </example>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static T? Deserialize<T>(ToonDocumentReader reader, ToonSerializerOptions? options = null)
    {
        return (T?)Deserialize(reader, typeof(T), options);
    }

    /// <summary>
    ///     Reads the value a <see cref="ToonDocumentReader"/> is on and converts it to <paramref name="type"/>; see
    ///     <see cref="Deserialize{T}(ToonDocumentReader, ToonSerializerOptions)"/>.
    /// </summary>
    /// <param name="reader">The reader, on a property name or the start of a value.</param>
    /// <param name="type">The type to convert to.</param>
    /// <param name="options">Optional deserialization options; the reader's own options decide how the text is parsed.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reader"/> or <paramref name="type"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the reader is not on a property name or the start of a value.</exception>
    /// <exception cref="ToonParseException">Thrown when the value is not valid TOON; the reader cannot be used afterwards.</exception>
    /// <exception cref="ToonSerializationException">Thrown when the value cannot be converted; the reader cannot be used afterwards.</exception>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static object? Deserialize(ToonDocumentReader reader, Type type, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(type);
        options ??= ToonSerializerOptions.Default;

        var value = reader.BeginValue(nameof(Deserialize));
        var result = ReadAt(reader.Parser, value, type, options);
        reader.EndValue();
        return result;
    }

    /// <summary>
    ///     Reads the value a <see cref="ToonDocumentReader"/> is on and converts it with the type info of its type,
    ///     without reflection; see <see cref="Deserialize{T}(ToonDocumentReader, ToonSerializerOptions)"/>.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="reader">The reader, on a property name or the start of a value.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options; the reader's own options decide how the text is parsed.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reader"/> or <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when generated code cannot create <typeparamref name="T"/>, or the options need a newer generator.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the reader is not on a property name or the start of a value.</exception>
    /// <exception cref="ToonParseException">Thrown when the value is not valid TOON; the reader cannot be used afterwards.</exception>
    /// <exception cref="ToonSerializationException">Thrown when the value cannot be converted; the reader cannot be used afterwards.</exception>
    public static T? Deserialize<T>(ToonDocumentReader reader, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(reader);
        options = CheckTypeInfo(typeInfo, options);
        CheckGeneratedReading(typeInfo, options);

        var value = reader.BeginValue(nameof(Deserialize));
        var result = ReadAt(reader.Parser, value, typeInfo, options);
        reader.EndValue();
        return result;
    }

    /// <summary>Reads one value of a document being read, straight from the text where possible.</summary>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    private static object? ReadAt(ToonParser parser, in ToonParser.ValueRef value, Type type, ToonSerializerOptions options)
    {
        var checkpoint = parser.Save(value);

        try
        {
            return new DirectReader(parser, options).ReadValue(value, type, 0);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Report the error exactly as the tree-based path does (or read what direct reading gave up on)
            DirectReadFallbacks++;
        }

        parser.Restore(checkpoint);
        return DeserializeValue(parser.BuildValue(value), type, options, 0, new SerializerState());
    }

    /// <summary>Reads one value of a document being read with a type info, straight from the text where generated code can.</summary>
    private static T? ReadAt<T>(ToonParser parser, in ToonParser.ValueRef value, ToonTypeInfo<T> typeInfo, ToonSerializerOptions options)
    {
        var deserialize = typeInfo.Deserialize ?? throw CannotCreate<T>();

        if (typeInfo.Read != null && (GetConverterCheck(options) is not { } check || typeInfo.ConvertsDirectly(check)))
        {
            var checkpoint = parser.Save(value);

            try
            {
                return typeInfo.Read(new DirectReader(parser, options, generatedOnly: true).Reader, new ToonReaderValue(value), 0);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                DirectReadFallbacks++;
            }

            parser.Restore(checkpoint);
        }

        var tree = parser.BuildValue(value);

        return options.Converters.Count != 0 && TryReadConverted<T>(tree, options, out var converted)
            ? converted
            : deserialize(tree, options, 0);
    }
}
