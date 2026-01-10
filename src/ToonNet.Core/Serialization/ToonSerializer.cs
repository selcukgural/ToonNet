using System.Collections;
using System.Reflection;
using System.Text;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.Core.Serialization;

/// <summary>
///     Main serializer for converting between C# objects and TOON format.
/// </summary>
public static class ToonSerializer
{
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
        options ??= ToonSerializerOptions.Default;

        var toonValue = SerializeValue(value, typeof(T), options, 0);
        var document = new ToonDocument(toonValue ?? ToonNull.Instance);
        var encoder = new ToonEncoder(options.ToonOptions);

        return encoder.Encode(document);
    }

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
    public static object? Deserialize(string toonString, Type type, ToonSerializerOptions? options = null)
    {
        options ??= ToonSerializerOptions.Default;

        var parser = new ToonParser(options.ToonOptions);
        var document = parser.Parse(toonString);

        return DeserializeValue(document.Root, type, options, 0);
    }

    #region Serialization

    /// <summary>
    ///     Serializes a value to its TOON representation.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="type">The type of the value.</param>
    /// <param name="options">Serialization options.</param>
    /// <param name="depth">The current serialization depth.</param>
    /// <returns>A ToonValue representing the serialized value.</returns>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails or depth exceeded.</exception>
    private static ToonValue? SerializeValue(object? value, Type type, ToonSerializerOptions options, int depth)
    {
        if (depth > options.MaxDepth)
        {
            throw new ToonEncodingException($"Maximum depth of {options.MaxDepth} exceeded during serialization");
        }

        if (value == null)
        {
            return options.IgnoreNullValues ? null : ToonNull.Instance;
        }

        // Check for custom converter
        var converter = options.GetConverter(type);

        if (converter != null)
        {
            return converter.Write(value, options);
        }

        // Handle primitives
        if (TrySerializePrimitive(value, out var primitiveValue))
        {
            return primitiveValue;
        }

        // Handle collections
        if (TrySerializeCollection(value, options, depth, out var collectionValue))
        {
            return collectionValue;
        }

        return TrySerializeDictionary(value, options, depth, out var dictionaryValue)
                   // Handle dictionaries
                   ? dictionaryValue
                   : // Handle objects
                   SerializeObject(value, type, options, depth);
    }

    /// <summary>
    ///     Attempts to serialize a primitive value.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="result">The resulting ToonValue if successful.</param>
    /// <returns>True if the value was successfully serialized as a primitive; otherwise, false.</returns>
    private static bool TrySerializePrimitive(object value, out ToonValue? result)
    {
        result = null;

        switch (value)
        {
            case string s:
                result = new ToonString(s);
                return true;
            case bool b:
                result = new ToonBoolean(b);
                return true;
            case byte b:
                result = new ToonNumber(b);
                return true;
            case sbyte sb:
                result = new ToonNumber(sb);
                return true;
            case short s:
                result = new ToonNumber(s);
                return true;
            case ushort us:
                result = new ToonNumber(us);
                return true;
            case int i:
                result = new ToonNumber(i);
                return true;
            case uint ui:
                result = new ToonNumber(ui);
                return true;
            case long l:
                result = new ToonNumber(l);
                return true;
            case ulong ul:
                result = new ToonNumber(ul);
                return true;
            case float f:
                result = new ToonNumber(f);
                return true;
            case double d:
                result = new ToonNumber(d);
                return true;
            case decimal m:
                result = new ToonNumber((double)m);
                return true;
            case DateTime dt:
                result = new ToonString(dt.ToString("O")); // ISO 8601
                return true;
            case DateTimeOffset dto:
                result = new ToonString(dto.ToString("O"));
                return true;
            case Guid guid:
                result = new ToonString(guid.ToString());
                return true;
            case Enum e:
                result = new ToonString(e.ToString());
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Attempts to serialize a collection value.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Serialization options.</param>
    /// <param name="depth">The current serialization depth.</param>
    /// <param name="result">The resulting ToonValue if successful.</param>
    /// <returns>True if the value was successfully serialized as a collection; otherwise, false.</returns>
    private static bool TrySerializeCollection(object value, ToonSerializerOptions options, int depth, out ToonValue? result)
    {
        result = null;

        if (value is not IEnumerable enumerable)
        {
            return false;
        }

        if (value is string) // String is IEnumerable but shouldn't be treated as a collection
        {
            return false;
        }

        // IDictionary also implements IEnumerable. Dictionaries must be serialized as objects, not arrays.
        if (value is IDictionary)
        {
            return false;
        }

        var array = new ToonArray();

        foreach (var item in enumerable)
        {
            var itemValue = SerializeValue(item, item?.GetType() ?? typeof(object), options, depth + 1);

            if (itemValue != null)
            {
                array.Items.Add(itemValue);
            }
        }

        result = array;
        return true;
    }

    /// <summary>
    ///     Attempts to serialize a dictionary value.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Serialization options.</param>
    /// <param name="depth">The current serialization depth.</param>
    /// <param name="result">The resulting ToonValue if successful.</param>
    /// <returns>True if the value was successfully serialized as a dictionary; otherwise, false.</returns>
    private static bool TrySerializeDictionary(object value, ToonSerializerOptions options, int depth, out ToonValue? result)
    {
        result = null;

        if (value is not IDictionary dictionary)
        {
            return false;
        }

        var obj = new ToonObject();

        foreach (DictionaryEntry entry in dictionary)
        {
            var key = entry.Key.ToString();

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var itemValue = SerializeValue(entry.Value, entry.Value?.GetType() ?? typeof(object), options, depth + 1);

            if (itemValue != null || !options.IgnoreNullValues)
            {
                obj.Properties[key] = itemValue ?? ToonNull.Instance;
            }
        }

        result = obj;
        return true;
    }

    /// <summary>
    ///     Serializes an object by reflecting over its properties.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <param name="type">The type of the object.</param>
    /// <param name="options">Serialization options.</param>
    /// <param name="depth">The current serialization depth.</param>
    /// <returns>A ToonObject containing the serialized properties.</returns>
    private static ToonObject SerializeObject(object value, Type type, ToonSerializerOptions options, int depth)
    {
        var obj = new ToonObject();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            // Skip ignored properties
            if (prop.GetCustomAttribute<ToonIgnoreAttribute>() != null)
            {
                continue;
            }

            // Skip write-only properties
            if (!prop.CanRead)
            {
                continue;
            }

            // Skip read-only if configured
            if (!options.IncludeReadOnlyProperties && !prop.CanWrite)
            {
                continue;
            }

            var propName = GetPropertyName(prop, options);
            var propValue = prop.GetValue(value);

            var toonValue = SerializeValue(propValue, prop.PropertyType, options, depth + 1);

            if (toonValue != null || !options.IgnoreNullValues)
            {
                obj.Properties[propName] = toonValue ?? ToonNull.Instance;
            }
        }

        return obj;
    }

    /// <summary>
    ///     Gets the serialized name for a property based on naming policy and attributes.
    /// </summary>
    /// <param name="property">The property to get the name for.</param>
    /// <param name="options">Serialization options containing naming policy.</param>
    /// <returns>The name to use in TOON format.</returns>
    private static string GetPropertyName(PropertyInfo property, ToonSerializerOptions options)
    {
        // Check for custom name attribute
        var nameAttr = property.GetCustomAttribute<ToonPropertyAttribute>();

        if (nameAttr != null)
        {
            return nameAttr.Name;
        }

        var name = property.Name;

        return options.PropertyNamingPolicy switch
        {
            PropertyNamingPolicy.CamelCase => ToCamelCase(name),
            PropertyNamingPolicy.SnakeCase => ToSnakeCase(name),
            PropertyNamingPolicy.LowerCase => name.ToLowerInvariant(),
            _                              => name
        };
    }

    /// <summary>
    ///     Converts a property name to camelCase format.
    /// </summary>
    /// <param name="name">The property name to convert.</param>
    /// <returns>The property name in camelCase format.</returns>
    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
        {
            return name;
        }

        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    /// <summary>
    ///     Converts a property name to snake_case format.
    /// </summary>
    /// <param name="name">The property name to convert.</param>
    /// <returns>The property name in snake_case format.</returns>
    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var result = new StringBuilder();
        result.Append(char.ToLowerInvariant(name[0]));

        for (var i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(name[i]));
            }
            else
            {
                result.Append(name[i]);
            }
        }

        return result.ToString();
    }

    #endregion

    #region Deserialization

    /// <summary>
    ///     Deserializes a ToonValue to a C# object.
    /// </summary>
    /// <param name="value">The ToonValue to deserialize.</param>
    /// <param name="targetType">The target C# type.</param>
    /// <param name="options">Deserialization options.</param>
    /// <param name="depth">The current deserialization depth.</param>
    /// <returns>The deserialized object, or null if the value is null.</returns>
    /// <exception cref="ToonParseException">Thrown when deserialization fails or depth exceeded.</exception>
    private static object? DeserializeValue(ToonValue value, Type targetType, ToonSerializerOptions options, int depth)
    {
        while (true)
        {
            if (depth > options.MaxDepth)
            {
                throw new ToonParseException($"Maximum depth of {options.MaxDepth} exceeded during deserialization", 0, 0);
            }

            if (value is ToonNull)
            {
                return null;
            }

            // Check for custom converter
            var converter = options.GetConverter(targetType);

            if (converter != null)
            {
                return converter.Read(value, targetType, options);
            }

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType);

            if (underlyingType != null)
            {
                targetType = underlyingType;
                continue;
            }

            // Handle primitives
            if (TryDeserializePrimitive(value, targetType, out var primitiveResult))
            {
                return primitiveResult;
            }

            // Handle collections
            if (TryDeserializeCollection(value, targetType, options, depth, out var collectionResult))
            {
                return collectionResult;
            }

            return TryDeserializeDictionary(value, targetType, options, depth, out var dictionaryResult)
                       // Handle dictionaries
                       ? dictionaryResult
                       : // Handle objects
                       DeserializeObject(value, targetType, options, depth);
        }
    }

    /// <summary>
    /// Attempts to deserialize a primitive value from a <see cref="ToonValue"/> to the specified target type.
    /// </summary>
    /// <param name="value">
    /// The <see cref="ToonValue"/> to deserialize. This value must represent a primitive type such as a string, number, or boolean.
    /// </param>
    /// <param name="targetType">
    /// The target C# type to which the value should be deserialized. Supported types include primitive types, enums, 
    /// <see cref="DateTime"/>, <see cref="DateTimeOffset"/>, and <see cref="Guid"/>.
    /// </param>
    /// <param name="result">
    /// When this method returns, contains the deserialized object if the operation was successful; otherwise, null.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value was successfully deserialized as a primitive; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method supports deserialization of the following types:
    /// <list type="bullet">
    /// <item><description>String</description></item>
    /// <item><description>Boolean</description></item>
    /// <item><description>Numeric types (e.g., byte, int, long, float, double, decimal)</description></item>
    /// <item><description>DateTime and DateTimeOffset (parsed from ISO 8601 strings)</description></item>
    /// <item><description>Guid</description></item>
    /// <item><description>Enums (parsed from their string representation)</description></item>
    /// </list>
    /// If the <paramref name="value"/> does not match the expected type or cannot be converted, the method returns <c>false</c>.
    /// </remarks>
    private static bool TryDeserializePrimitive(ToonValue value, Type targetType, out object? result)
    {
        result = null;

        switch (value)
        {
            case ToonString strValue:
            {
                var str = strValue.Value;

                if (targetType == typeof(string))
                {
                    result = str;
                    return true;
                }

                if (targetType == typeof(DateTime))
                {
                    result = DateTime.Parse(str);
                    return true;
                }

                if (targetType == typeof(DateTimeOffset))
                {
                    result = DateTimeOffset.Parse(str);
                    return true;
                }

                if (targetType == typeof(Guid))
                {
                    result = Guid.Parse(str);
                    return true;
                }

                if (targetType.IsEnum)
                {
                    result = Enum.Parse(targetType, str);
                    return true;
                }

                break;
            }
            case ToonNumber numValue:
            {
                var num = numValue.Value;

                if (targetType == typeof(byte))
                {
                    result = (byte)num;
                    return true;
                }

                if (targetType == typeof(sbyte))
                {
                    result = (sbyte)num;
                    return true;
                }

                if (targetType == typeof(short))
                {
                    result = (short)num;
                    return true;
                }

                if (targetType == typeof(ushort))
                {
                    result = (ushort)num;
                    return true;
                }

                if (targetType == typeof(int))
                {
                    result = (int)num;
                    return true;
                }

                if (targetType == typeof(uint))
                {
                    result = (uint)num;
                    return true;
                }

                if (targetType == typeof(long))
                {
                    result = (long)num;
                    return true;
                }

                if (targetType == typeof(ulong))
                {
                    result = (ulong)num;
                    return true;
                }

                if (targetType == typeof(float))
                {
                    result = (float)num;
                    return true;
                }

                if (targetType == typeof(double))
                {
                    result = num;
                    return true;
                }

                if (targetType == typeof(decimal))
                {
                    result = (decimal)num;
                    return true;
                }

                break;
            }
            case ToonBoolean boolValue when targetType == typeof(bool):
                result = boolValue.Value;
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Attempts to deserialize a TOON array as a collection.
    /// </summary>
    /// <param name="value">The TOON value to deserialize.</param>
    /// <param name="targetType">The target collection type.</param>
    /// <param name="options">Deserialization options.</param>
    /// <param name="depth">The current deserialization depth.</param>
    /// <param name="result">The deserialized collection if successful.</param>
    /// <returns>True if the value was successfully deserialized as a collection; otherwise, false.</returns>
    private static bool TryDeserializeCollection(ToonValue value, Type targetType, ToonSerializerOptions options, int depth, out object? result)
    {
        result = null;

        if (value is not ToonArray array)
        {
            return false;
        }

        // Determine element type
        Type? elementType = null;

        if (targetType.IsArray)
        {
            elementType = targetType.GetElementType();
        }
        else if (targetType.IsGenericType)
        {
            var genericDef = targetType.GetGenericTypeDefinition();

            if (genericDef == typeof(List<>) || genericDef == typeof(IList<>) || genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IEnumerable<>))
            {
                elementType = targetType.GetGenericArguments()[0];
            }
        }

        if (elementType == null)
        {
            return false;
        }

        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;

        foreach (var item in array.Items)
        {
            var deserializedItem = DeserializeValue(item, elementType, options, depth + 1);
            list.Add(deserializedItem);
        }

        if (targetType.IsArray)
        {
            var arrayResult = Array.CreateInstance(elementType, list.Count);
            list.CopyTo(arrayResult, 0);
            result = arrayResult;
        }
        else
        {
            result = list;
        }

        return true;
    }

    /// <summary>
    ///     Attempts to deserialize a TOON object as a dictionary.
    /// </summary>
    /// <param name="value">The TOON value to deserialize.</param>
    /// <param name="targetType">The target dictionary type.</param>
    /// <param name="options">Deserialization options.</param>
    /// <param name="depth">The current deserialization depth.</param>
    /// <param name="result">The deserialized dictionary if successful.</param>
    /// <returns>True if the value was successfully deserialized as a dictionary; otherwise, false.</returns>
    private static bool TryDeserializeDictionary(ToonValue value, Type targetType, ToonSerializerOptions options, int depth, out object? result)
    {
        result = null;

        if (value is not ToonObject obj)
        {
            return false;
        }

        if (!targetType.IsGenericType)
        {
            return false;
        }

        var genericDef = targetType.GetGenericTypeDefinition();

        if (genericDef != typeof(Dictionary<,>) && genericDef != typeof(IDictionary<,>))
        {
            return false;
        }

        var keyType = targetType.GetGenericArguments()[0];
        var valueType = targetType.GetGenericArguments()[1];

        if (keyType != typeof(string))
        {
            return false; // TOON only supports string keys
        }

        var dict = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType))!;

        foreach (var (key, toonValue) in obj.Properties)
        {
            var deserializedValue = DeserializeValue(toonValue, valueType, options, depth + 1);
            dict[key] = deserializedValue;
        }

        result = dict;
        return true;
    }

    /// <summary>
    ///     Deserializes a TOON object to a C# object by reflecting over properties.
    /// </summary>
    /// <param name="value">The TOON object to deserialize.</param>
    /// <param name="targetType">The target C# type.</param>
    /// <param name="options">Deserialization options.</param>
    /// <param name="depth">The current deserialization depth.</param>
    /// <returns>The deserialized C# object.</returns>
    /// <exception cref="ToonParseException">Thrown when the value is not an object or instance creation fails.</exception>
    private static object DeserializeObject(ToonValue value, Type targetType, ToonSerializerOptions options, int depth)
    {
        if (value is not ToonObject obj)
        {
            throw new ToonParseException($"Expected object but got {value.ValueType}", 0, 0);
        }

        var instance = Activator.CreateInstance(targetType);

        if (instance == null)
        {
            throw new ToonParseException($"Cannot create instance of {targetType.Name}", 0, 0);
        }

        var properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            // Skip ignored properties
            if (prop.GetCustomAttribute<ToonIgnoreAttribute>() != null)
            {
                continue;
            }

            // Skip read-only properties
            if (!prop.CanWrite)
            {
                continue;
            }

            var propName = GetPropertyName(prop, options);

            if (!obj.Properties.TryGetValue(propName, out var toonValue))
            {
                continue;
            }

            var propValue = DeserializeValue(toonValue, prop.PropertyType, options, depth + 1);
            prop.SetValue(instance, propValue);
        }

        return instance;
    }

    #endregion

    #region Async APIs

    /// <summary>
    ///     Asynchronously serializes an object to TOON format string.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous serialization operation.</returns>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static async Task<string> SerializeAsync<T>(T? value, ToonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Serialize(value, options);
        }, cancellationToken);
    }

    /// <summary>
    ///     Asynchronously deserializes a TOON format string to an object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="toonString">The TOON format string to deserialize.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous deserialization operation.</returns>
    /// <exception cref="ToonParseException">Thrown when parsing fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static async Task<T?> DeserializeAsync<T>(string toonString, ToonSerializerOptions? options = null,
                                                     CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Deserialize<T>(toonString, options);
        }, cancellationToken);
    }

    /// <summary>
    ///     Asynchronously serializes an object and writes it to a file.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="filePath">The file path to write to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous serialization and write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="IOException">Thrown when file I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static async Task SerializeToFileAsync<T>(T? value, string filePath, ToonSerializerOptions? options = null,
                                                     CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        var toonString = await SerializeAsync(value, options, cancellationToken);

        await File.WriteAllTextAsync(filePath, toonString, System.Text.Encoding.UTF8, cancellationToken);
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
    public static async Task<T?> DeserializeFromFileAsync<T>(string filePath, ToonSerializerOptions? options = null,
                                                             CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        var toonString = await File.ReadAllTextAsync(filePath, System.Text.Encoding.UTF8, cancellationToken);

        return await DeserializeAsync<T>(toonString, options, cancellationToken);
    }

    /// <summary>
    ///     Asynchronously serializes an object and writes it to a stream.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous serialization and write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="IOException">Thrown when stream I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static async Task SerializeToStreamAsync<T>(T? value, Stream stream, ToonSerializerOptions? options = null,
                                                       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var toonString = await SerializeAsync(value, options, cancellationToken);
        var bytes = System.Text.Encoding.UTF8.GetBytes(toonString);

        await stream.WriteAsync(bytes, cancellationToken);
    }
    
    public static async Task SerializeToStreamAsync(Type type, Stream stream, ToonSerializerOptions? options = null,
                                                       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(stream);

        var toonString = await SerializeAsync(type, options, cancellationToken);
        var bytes = System.Text.Encoding.UTF8.GetBytes(toonString);

        await stream.WriteAsync(bytes, cancellationToken);
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
    public static async Task<T?> DeserializeFromStreamAsync<T>(Stream stream, ToonSerializerOptions? options = null,
                                                               CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var toonString = await reader.ReadToEndAsync(cancellationToken);

        return await DeserializeAsync<T>(toonString, options, cancellationToken);
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
                                                                       [System.Runtime.CompilerServices.EnumeratorCancellation]
                                                                       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8);

        await foreach (var item in DeserializeStreamAsync<T>(reader, options, cancellationToken))
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
                                                                       [System.Runtime.CompilerServices.EnumeratorCancellation]
                                                                       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(multiDocumentOptions);

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8);

        await foreach (var item in DeserializeStreamAsync<T>(reader, options, multiDocumentOptions, cancellationToken))
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
                                                                       [System.Runtime.CompilerServices.EnumeratorCancellation]
                                                                       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);

        await foreach (var item in DeserializeStreamAsync<T>(reader, options, ToonMultiDocumentReadOptions.BlankLine, cancellationToken))
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
                                                                       [System.Runtime.CompilerServices.EnumeratorCancellation]
                                                                       CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(multiDocumentOptions);

        var currentObject = new StringBuilder();

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isBoundary = multiDocumentOptions.Mode switch
            {
                ToonMultiDocumentSeparatorMode.BlankLine => string.IsNullOrWhiteSpace(line),
                ToonMultiDocumentSeparatorMode.ExplicitSeparator => line == multiDocumentOptions.DocumentSeparator,
                _ => string.IsNullOrWhiteSpace(line)
            };

            if (isBoundary)
            {
                if (currentObject.Length <= 0)
                {
                    continue;
                }

                var toonString = currentObject.ToString();
                currentObject.Clear();

                var obj = await DeserializeAsync<T>(toonString, options, cancellationToken);
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
        var lastObj = await DeserializeAsync<T>(lastToonString, options, cancellationToken);
        yield return lastObj;
    }

    /// <summary>
    ///     Asynchronously serializes a collection of objects to a file with each object separated by a blank line.
    /// </summary>
    /// <typeparam name="T">The type of objects to serialize.</typeparam>
    /// <param name="values">The collection of values to serialize.</param>
    /// <param name="filePath">The file path to write to.</param>
    /// <param name="options">Optional serialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous serialization and write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when values or filePath is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="IOException">Thrown when file I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    ///     This method writes each object as a separate TOON document, separated by blank lines.
    ///     This format is compatible with DeserializeStreamAsync for reading back multiple objects.
    /// </remarks>
    public static async Task SerializeCollectionToFileAsync<T>(IEnumerable<T> values, string filePath, ToonSerializerOptions? options = null,
                                                               CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(filePath);

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        await using var writer = new StreamWriter(fileStream, System.Text.Encoding.UTF8);

        var isFirst = true;

        foreach (var value in values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Add a blank line separator between objects (but not before the first)
            if (!isFirst)
            {
                await writer.WriteLineAsync(); // End previous object
                await writer.WriteLineAsync(); // Add a blank line
            }

            var toonString = await SerializeAsync(value, options, cancellationToken);
            await writer.WriteAsync(toonString);

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
    /// <returns>A task that represents the asynchronous serialization and write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when values or stream is null.</exception>
    /// <exception cref="ToonEncodingException">Thrown when serialization fails.</exception>
    /// <exception cref="IOException">Thrown when stream I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    ///     This method writes each object as a separate TOON document, separated by blank lines.
    ///     This format is compatible with DeserializeStreamAsync for reading back multiple objects.
    /// </remarks>
    public static async Task SerializeCollectionToStreamAsync<T>(IEnumerable<T> values, Stream stream, ToonSerializerOptions? options = null,
                                                                 CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(stream);

        await using var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        var isFirst = true;

        foreach (var value in values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Add a blank line separator between objects (but not before the first)
            if (!isFirst)
            {
                await writer.WriteLineAsync(); // End previous object
            }

            var toonString = await SerializeAsync(value, options, cancellationToken);
            await writer.WriteAsync(toonString);

            isFirst = false;
        }
    }

    #endregion
}

