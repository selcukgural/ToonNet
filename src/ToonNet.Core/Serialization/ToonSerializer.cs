using ToonNet.Core.Models;
using ToonNet.Core.Parsing;
using ToonNet.Core.Encoding;
using System.Reflection;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.Core.Serialization;

/// <summary>
/// Main serializer for converting between C# objects and TOON format.
/// </summary>
public static class ToonSerializer
{
    /// <summary>
    /// Serializes an object to TOON string.
    /// </summary>
    public static string Serialize<T>(T? value, ToonSerializerOptions? options = null)
    {
        options ??= ToonSerializerOptions.Default;

        var toonValue = SerializeValue(value, typeof(T), options, 0);
        var document = new ToonDocument(toonValue ?? ToonNull.Instance);
        var encoder = new ToonEncoder(options.ToonOptions);

        return encoder.Encode(document);
    }

    /// <summary>
    /// Deserializes TOON string to an object.
    /// </summary>
    public static T? Deserialize<T>(string toonString, ToonSerializerOptions? options = null)
    {
        return (T?)Deserialize(toonString, typeof(T), options);
    }

    /// <summary>
    /// Deserializes TOON string to an object of the specified type.
    /// </summary>
    public static object? Deserialize(string toonString, Type type, ToonSerializerOptions? options = null)
    {
        options ??= ToonSerializerOptions.Default;

        var parser = new ToonParser(options.ToonOptions);
        var document = parser.Parse(toonString);

        return DeserializeValue(document.Root, type, options, 0);
    }

    #region Serialization

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

    private static bool TrySerializeCollection(object value, ToonSerializerOptions options, int depth, out ToonValue? result)
    {
        result = null;

        if (value is not System.Collections.IEnumerable enumerable)
        {
            return false;
        }
        
        if (value is string) // String is IEnumerable but shouldn't be treated as a collection
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

    private static bool TrySerializeDictionary(object value, ToonSerializerOptions options, int depth, out ToonValue? result)
    {
        result = null;

        if (value is not System.Collections.IDictionary dictionary)
        {
            return false;
        }

        var obj = new ToonObject();

        foreach (System.Collections.DictionaryEntry entry in dictionary)
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

    private static ToonValue SerializeObject(object value, Type type, ToonSerializerOptions options, int depth)
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

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
        {
            return name;
        }

        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var result = new System.Text.StringBuilder();
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

            if (genericDef == typeof(List<>)
                || genericDef == typeof(IList<>)
                || genericDef == typeof(ICollection<>)
                || genericDef == typeof(IEnumerable<>))
            {
                elementType = targetType.GetGenericArguments()[0];
            }
        }

        if (elementType == null)
        {
            return false;
        }
        var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;

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
        
        var dict = (System.Collections.IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType))!;

        foreach (var (key, toonValue) in obj.Properties)
        {
            var deserializedValue = DeserializeValue(toonValue, valueType, options, depth + 1);
            dict[key] = deserializedValue;
        }

        result = dict;
        return true;
    }

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
}