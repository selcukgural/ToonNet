using System.Runtime.CompilerServices;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.Core.Serialization;

/// <summary>
///     Serialization code for a type that needs no reflection: generated for a <see cref="ToonSerializableAttribute"/>
///     type (its static <c>ToonTypeInfo</c> property, or <see cref="GetGenerated{T}"/>), or composed for a list, array or
///     dictionary of such values with <see cref="CreateList{T}"/>, <see cref="CreateArray{T}"/> and
///     <see cref="CreateDictionary{TValue}"/>.
/// </summary>
/// <remarks>
///     The <see cref="ToonSerializer"/> overloads that take a type info write and read straight from text like the other
///     overloads, with the same output, but are compatible with trimming and NativeAOT: values are written and read by
///     generated code, and a shape that only reflection would handle directly (rare: a property typed
///     <see cref="object"/>, a value of a derived type) makes the whole document go through the generated
///     <c>Serialize</c>/<c>Deserialize</c> methods instead. To keep reflection out entirely, mark the types with
///     <c>[ToonSerializable(AllowReflectionFallback = false)]</c>; options with converters are not supported, because
///     generated code hands converters to the reflection-based serializer.
/// </remarks>
public abstract class ToonTypeInfo
{
    private protected ToonTypeInfo() { }

    /// <summary>The type the info serializes.</summary>
    public abstract Type Type { get; }

    /// <summary>
    ///     The generated type info of <typeparamref name="T"/>, the same as its static <c>ToonTypeInfo</c> property; for
    ///     generic code, or a type whose own member takes that name.
    /// </summary>
    /// <typeparam name="T">A non-generic <see cref="ToonSerializableAttribute"/> type.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="T"/> has no generated code.</exception>
    public static ToonTypeInfo<T> GetGenerated<T>()
    {
        if (ToonTypeInfo<T>.Generated is { } info)
        {
            return info;
        }

        // Generated types register from a module initializer, which getting a Type does not run
        RuntimeHelpers.RunModuleConstructor(typeof(T).Module.ModuleHandle);

        return ToonTypeInfo<T>.Generated ?? throw new InvalidOperationException(
            $"{typeof(T).Name} has no generated serialization code; mark it [ToonSerializable], make it partial and reference ToonNet.SourceGenerators.");
    }

    /// <summary>Creates the type info of a <see cref="List{T}"/> of values with <paramref name="elementInfo"/>.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="elementInfo"/> is null.</exception>
    public static ToonTypeInfo<List<T>> CreateList<T>(ToonTypeInfo<T> elementInfo)
    {
        ArgumentNullException.ThrowIfNull(elementInfo);

        return new ToonTypeInfo<List<T>>(
            (list, options, depth) => SerializeItems(list, list?.Count ?? 0, elementInfo, options, depth),
            elementInfo.Deserialize == null ? null : (value, options, depth) => DeserializeItems(value, elementInfo, options, depth)!,
            elementInfo.Read == null ? null : (ToonReader reader, in ToonReaderValue value, int depth) => ReadItems(reader, value, elementInfo, depth)!,
            elementInfo.AppliesReadingOptions);
    }

    /// <summary>Creates the type info of an array of values with <paramref name="elementInfo"/>.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="elementInfo"/> is null.</exception>
    public static ToonTypeInfo<T[]> CreateArray<T>(ToonTypeInfo<T> elementInfo)
    {
        ArgumentNullException.ThrowIfNull(elementInfo);

        return new ToonTypeInfo<T[]>(
            (array, options, depth) => SerializeItems(array, array?.Length ?? 0, elementInfo, options, depth),
            elementInfo.Deserialize == null ? null : (value, options, depth) => DeserializeItems(value, elementInfo, options, depth)?.ToArray()!,
            elementInfo.Read == null ? null : (ToonReader reader, in ToonReaderValue value, int depth) => ReadItems(reader, value, elementInfo, depth)?.ToArray()!,
            elementInfo.AppliesReadingOptions);
    }

    /// <summary>
    ///     Creates the type info of a <see cref="Dictionary{TKey,TValue}"/> with string keys and values with
    ///     <paramref name="valueInfo"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="valueInfo"/> is null.</exception>
    public static ToonTypeInfo<Dictionary<string, TValue>> CreateDictionary<TValue>(ToonTypeInfo<TValue> valueInfo)
    {
        ArgumentNullException.ThrowIfNull(valueInfo);

        return new ToonTypeInfo<Dictionary<string, TValue>>(
            (dictionary, options, depth) => SerializeEntries(dictionary, valueInfo, options, depth),
            valueInfo.Deserialize == null ? null : (value, options, depth) => DeserializeEntries(value, valueInfo, options, depth)!,
            valueInfo.Read == null ? null : (ToonReader reader, in ToonReaderValue value, int depth) => ReadEntries(reader, value, valueInfo, depth)!,
            valueInfo.AppliesReadingOptions);
    }

    #region Composed conversions

    // These follow the generated code for collection and dictionary properties, which follows ToonSerializer.

    private static ToonValue SerializeItems<T>(IEnumerable<T>? items, int count, ToonTypeInfo<T> elementInfo, ToonSerializerOptions options,
                                               int depth)
    {
        if (items is null)
        {
            return ToonNull.Instance;
        }

        ToonSourceGenerationHelpers.CheckSerializationDepth(options, depth);
        var array = new ToonArray(new List<ToonValue>(count));

        foreach (var item in items)
        {
            array.Items.Add(item is null ? ToonNull.Instance : elementInfo.Serialize(item, options, depth + 1));
        }

        return array;
    }

    private static List<T>? DeserializeItems<T>(ToonValue value, ToonTypeInfo<T> elementInfo, ToonSerializerOptions options, int depth)
    {
        ToonSourceGenerationHelpers.CheckDeserializationDepth(options, depth);

        if (value is not ToonArray array)
        {
            return value is ToonNull ? null : throw ToonSerializer.ShapeError($"Expected an array but got {value.ValueType}", typeof(List<T>), value);
        }

        var items = new List<T>(array.Count);

        for (var i = 0; i < array.Count; i++)
        {
            items.Add(elementInfo.Deserialize!(array[i], options, depth + 1));
        }

        return items;
    }

    private static List<T>? ReadItems<T>(ToonReader reader, in ToonReaderValue value, ToonTypeInfo<T> elementInfo, int depth)
    {
        ToonSourceGenerationHelpers.CheckDeserializationDepth(reader.Options, depth);

        if (!reader.IsArray(in value))
        {
            if (reader.IsNull(in value))
            {
                return null;
            }

            reader.Fallback(); // the tree-based path reports the error
        }

        var array = reader.BeginArray(in value);
        var items = new List<T>(reader.CapacityHint(in array));

        while (reader.NextItem(ref array, out var item))
        {
            items.Add(elementInfo.Read!(reader, in item, depth + 1));
        }

        return items;
    }

    private static ToonValue SerializeEntries<TValue>(Dictionary<string, TValue>? dictionary, ToonTypeInfo<TValue> valueInfo,
                                                      ToonSerializerOptions options, int depth)
    {
        if (dictionary is null)
        {
            return ToonNull.Instance;
        }

        ToonSourceGenerationHelpers.CheckSerializationDepth(options, depth);
        var result = new ToonObject(dictionary.Count);

        foreach (var (key, entryValue) in dictionary)
        {
            if (entryValue is null)
            {
                if (!options.IgnoreNullValues)
                {
                    result.Properties[key] = ToonNull.Instance;
                }
            }
            else
            {
                result.Properties[key] = valueInfo.Serialize(entryValue, options, depth + 1);
            }
        }

        return result;
    }

    private static Dictionary<string, TValue>? DeserializeEntries<TValue>(ToonValue value, ToonTypeInfo<TValue> valueInfo,
                                                                          ToonSerializerOptions options, int depth)
    {
        ToonSourceGenerationHelpers.CheckDeserializationDepth(options, depth);

        if (value is not ToonObject obj)
        {
            return value is ToonNull
                ? null
                : throw ToonSerializer.ShapeError($"Expected an object but got {value.ValueType}", typeof(Dictionary<string, TValue>), value);
        }

        var dictionary = new Dictionary<string, TValue>(obj.Properties.Count);

        foreach (var (key, entryValue) in obj.Properties)
        {
            dictionary[key] = valueInfo.Deserialize!(entryValue, options, depth + 1);
        }

        return dictionary;
    }

    private static Dictionary<string, TValue>? ReadEntries<TValue>(ToonReader reader, in ToonReaderValue value, ToonTypeInfo<TValue> valueInfo,
                                                                   int depth)
    {
        ToonSourceGenerationHelpers.CheckDeserializationDepth(reader.Options, depth);

        if (!reader.IsObject(in value))
        {
            if (reader.IsNull(in value))
            {
                return null;
            }

            reader.Fallback(); // the tree-based path reports the error
        }

        var dictionary = new Dictionary<string, TValue>();
        var fields = reader.BeginObject(in value);

        while (reader.NextProperty(ref fields, out var key, out var item))
        {
            dictionary[key] = valueInfo.Read!(reader, in item, depth + 1);
        }

        return dictionary;
    }

    #endregion
}

/// <summary>
///     Serialization code for <typeparamref name="T"/> that needs no reflection; see <see cref="ToonTypeInfo"/>.
/// </summary>
/// <typeparam name="T">The type the info serializes.</typeparam>
public sealed class ToonTypeInfo<T> : ToonTypeInfo
{
    /// <summary>Reads a value straight from text with a <see cref="ToonReader"/>.</summary>
    internal delegate T ReadMethod(ToonReader reader, in ToonReaderValue value, int depth);

    /// <summary>The generated type info, set when the generated code of <typeparamref name="T"/> registers.</summary>
    internal static ToonTypeInfo<T>? Generated;

    internal ToonTypeInfo(Func<T, ToonSerializerOptions, int, ToonValue> serialize, Func<ToonValue, ToonSerializerOptions, int, T>? deserialize,
                          ReadMethod? read, bool appliesReadingOptions)
    {
        Serialize = serialize;
        Deserialize = deserialize;
        Read = read;
        AppliesReadingOptions = appliesReadingOptions;
    }

    /// <inheritdoc />
    public override Type Type => typeof(T);

    /// <summary>Converts a value to a tree (null to <see cref="ToonNull"/>), like the generated <c>Serialize</c> method.</summary>
    internal Func<T, ToonSerializerOptions, int, ToonValue> Serialize { get; }

    /// <summary>Converts a tree to a value, like the generated <c>Deserialize</c>; null when the type cannot be created.</summary>
    internal Func<ToonValue, ToonSerializerOptions, int, T>? Deserialize { get; }

    /// <summary>Reads a value straight from text; null when the type cannot be created.</summary>
    internal ReadMethod? Read { get; }

    /// <summary>
    ///     Whether the generated code applies case-insensitive reading, unmapped key errors and required properties (code
    ///     from older generators leaves them to reflection).
    /// </summary>
    internal bool AppliesReadingOptions { get; }
}
