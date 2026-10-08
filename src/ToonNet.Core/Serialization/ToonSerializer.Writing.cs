using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using ToonNet.Core.Models;

namespace ToonNet.Core.Serialization;

public static partial class ToonSerializer
{
    /// <summary>
    ///     Converts a .NET value to its TOON representation (the host-type mapping of spec §3).
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="declaredType">The declared type; the runtime type of <paramref name="value"/> is used for objects.</param>
    /// <param name="options">Serialization options.</param>
    /// <param name="depth">The current serialization depth.</param>
    /// <param name="state">Path and cycle-detection state.</param>
    /// <param name="propertyConverter">A converter from <c>[ToonConverter]</c> on the property being written.</param>
    /// <returns>The TOON value, or null when a null value is skipped because of <see cref="ToonSerializerOptions.IgnoreNullValues"/>.</returns>
    /// <exception cref="ToonEncodingException">Thrown when the depth is exceeded or a circular reference is found.</exception>
    private static ToonValue? SerializeValue(object? value, Type declaredType, ToonSerializerOptions options, int depth, SerializerState state,
                                             IToonConverter? propertyConverter = null)
    {
        if (depth > options.MaxDepth)
        {
            throw ToonEncodingException.Create($"Maximum depth of {options.MaxDepth} exceeded during serialization", state.Path);
        }

        if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            throw ToonEncodingException.Create("Object graph is nested too deeply to serialize with the available stack space", state.Path);
        }

        if (value == null)
        {
            return options.IgnoreNullValues ? null : ToonNull.Instance;
        }

        var runtimeType = value.GetType();
        var converter = propertyConverter ?? options.GetConverter(runtimeType) ?? options.GetConverter(declaredType);

        if (converter != null)
        {
            return converter.Write(value, options);
        }

        if (value is ToonValue toonValue)
        {
            return toonValue;
        }

        if (TrySerializePrimitive(value, out var primitiveValue))
        {
            return primitiveValue;
        }

        if (GetTypeMetadata(runtimeType).TypeConverter is { } typeConverter)
        {
            return typeConverter.Write(value, options);
        }

        if (!state.Visiting.Add(value))
        {
            throw ToonEncodingException.Create("A circular reference was detected", state.Path, suggestion:
                                               "Break the cycle or mark the back-reference with [ToonIgnore].");
        }

        try
        {
            if (TrySerializeDictionary(value, options, depth, state, out var dictionaryValue))
            {
                return dictionaryValue;
            }

            if (value is IEnumerable enumerable)
            {
                return SerializeCollection(enumerable, options, depth, state);
            }

            return SerializeObject(value, runtimeType, options, depth, state);
        }
        finally
        {
            state.Visiting.Remove(value);
        }
    }

    /// <summary>
    ///     Serializes primitive and well-known value types.
    /// </summary>
    /// <remarks>
    ///     Integers and decimals keep their exact value; dates use ISO 8601 (<c>O</c>, <c>yyyy-MM-dd</c>),
    ///     <see cref="TimeSpan"/> the constant format <c>[-][d.]hh:mm:ss[.fffffff]</c>; enums use their names.
    ///     Integers outside the <see cref="decimal"/> range are written as strings (lossless, spec §2).
    /// </remarks>
    private static bool TrySerializePrimitive(object value, out ToonValue? result)
    {
        result = value switch
        {
            string s          => new ToonString(s),
            char c            => new ToonString(c.ToString()),
            bool b            => new ToonBoolean(b),
            byte n            => new ToonNumber((long)n),
            sbyte n           => new ToonNumber((long)n),
            short n           => new ToonNumber((long)n),
            ushort n          => new ToonNumber((long)n),
            int n             => new ToonNumber((long)n),
            uint n            => new ToonNumber((long)n),
            long n            => new ToonNumber(n),
            ulong n           => new ToonNumber(n),
            float n           => new ToonNumber(n),
            double n          => new ToonNumber(n),
            decimal n         => new ToonNumber(n),
            Half n            => new ToonNumber((float)n),
            BigInteger n      => FromBigIntegerText(n.ToString(CultureInfo.InvariantCulture)),
            Int128 n          => FromBigIntegerText(n.ToString(CultureInfo.InvariantCulture)),
            UInt128 n         => FromBigIntegerText(n.ToString(CultureInfo.InvariantCulture)),
            DateTime dt       => new ToonString(dt.ToString("O", CultureInfo.InvariantCulture)),
            DateTimeOffset dt => new ToonString(dt.ToString("O", CultureInfo.InvariantCulture)),
            DateOnly d        => new ToonString(d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            TimeOnly t        => new ToonString(t.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture).TrimEnd('.')),
            TimeSpan ts       => new ToonString(ts.ToString("c", CultureInfo.InvariantCulture)),
            Guid guid         => new ToonString(guid.ToString("D")),
            Uri uri           => new ToonString(uri.OriginalString),
            Enum e            => new ToonString(e.ToString()),
            _                 => null
        };

        return result != null;

        static ToonValue FromBigIntegerText(string text)
        {
            return decimal.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var exact)
                ? new ToonNumber(exact)
                : new ToonString(text);
        }
    }

    /// <summary>
    ///     Serializes a sequence as an array. Null items are always kept so that indexes are preserved.
    /// </summary>
    private static ToonArray SerializeCollection(IEnumerable enumerable, ToonSerializerOptions options, int depth, SerializerState state)
    {
        var array = new ToonArray();
        var index = 0;

        foreach (var item in enumerable)
        {
            state.Push($"[{index++}]");
            array.Items.Add(SerializeValue(item, item?.GetType() ?? typeof(object), options, depth + 1, state) ?? ToonNull.Instance);
            state.Pop();
        }

        return array;
    }

    /// <summary>
    ///     Serializes dictionaries (non-generic <see cref="IDictionary"/>, <see cref="IDictionary{TKey,TValue}"/> or
    ///     <see cref="IReadOnlyDictionary{TKey,TValue}"/>) as objects with invariant-formatted keys.
    /// </summary>
    private static bool TrySerializeDictionary(object value, ToonSerializerOptions options, int depth, SerializerState state, out ToonValue? result)
    {
        IEnumerable<(object Key, object? Value)>? entries = value switch
        {
            IDictionary dictionary => EnumerateEntries(dictionary),
            _                      => GetGenericDictionaryEntries(value)
        };

        if (entries == null)
        {
            result = null;
            return false;
        }

        var obj = new ToonObject();

        foreach (var (rawKey, entryValue) in entries)
        {
            var key = FormatDictionaryKey(rawKey);
            state.Push(key);
            var itemValue = SerializeValue(entryValue, entryValue?.GetType() ?? typeof(object), options, depth + 1, state);
            state.Pop();

            if (itemValue != null)
            {
                obj.Properties[key] = itemValue;
            }
        }

        result = obj;
        return true;
    }

    private static IEnumerable<(object Key, object? Value)> EnumerateEntries(IDictionary dictionary)
    {
        var enumerator = dictionary.GetEnumerator();

        while (enumerator.MoveNext())
        {
            yield return (enumerator.Key, enumerator.Value);
        }
    }

    /// <summary>
    ///     Enumerates key/value pairs of a type that only implements the generic dictionary interfaces.
    /// </summary>
    private static IEnumerable<(object Key, object? Value)>? GetGenericDictionaryEntries(object value)
    {
        var dictionaryInterface = value.GetType().GetInterfaces().FirstOrDefault(i => i.IsGenericType &&
            (i.GetGenericTypeDefinition() == typeof(IDictionary<,>) || i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));

        if (dictionaryInterface == null)
        {
            return null;
        }

        var pairType = typeof(KeyValuePair<,>).MakeGenericType(dictionaryInterface.GetGenericArguments());
        var keyProperty = pairType.GetProperty("Key")!;
        var valueProperty = pairType.GetProperty("Value")!;

        return ((IEnumerable)value).Cast<object>().Select(pair => (keyProperty.GetValue(pair)!, valueProperty.GetValue(pair)));
    }

    private static string FormatDictionaryKey(object key)
    {
        return key switch
        {
            string s          => s,
            Enum e            => e.ToString(),
            DateTime dt       => dt.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dt => dt.ToString("O", CultureInfo.InvariantCulture),
            IFormattable f    => f.ToString(null, CultureInfo.InvariantCulture),
            _                 => key.ToString() ?? string.Empty
        };
    }

    /// <summary>
    ///     Serializes an object's properties using its runtime type.
    /// </summary>
    private static ToonObject SerializeObject(object value, Type type, ToonSerializerOptions options, int depth, SerializerState state)
    {
        var obj = new ToonObject();
        var metadata = GetTypeMetadata(type);

        foreach (var property in metadata.Properties)
        {
            if (!options.IncludeReadOnlyProperties && !property.HasSetter)
            {
                continue;
            }

            var name = GetPropertyName(property, options);
            state.Push(name);
            var toonValue = SerializeValue(property.Getter(value), property.Property.PropertyType, options, depth + 1, state, property.Converter);
            state.Pop();

            if (toonValue != null)
            {
                obj.Properties[name] = toonValue;
            }
        }

        return obj;
    }
}
