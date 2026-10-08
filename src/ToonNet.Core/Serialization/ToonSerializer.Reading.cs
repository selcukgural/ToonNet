using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using ToonNet.Core.Models;

namespace ToonNet.Core.Serialization;

public static partial class ToonSerializer
{
    /// <summary>
    ///     Converts a TOON value to an instance of <paramref name="targetType"/>.
    /// </summary>
    /// <exception cref="ToonParseException">Thrown when the maximum depth is exceeded.</exception>
    /// <exception cref="ToonSerializationException">Thrown when the value cannot be converted to the target type.</exception>
    private static object? DeserializeValue(ToonValue value, Type targetType, ToonSerializerOptions options, int depth, SerializerState state,
                                            IToonConverter? propertyConverter = null)
    {
        if (depth > options.MaxDepth)
        {
            throw new ToonParseException($"Maximum depth of {options.MaxDepth} exceeded during deserialization", 0, 0);
        }

        if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            throw new ToonParseException("Document is nested too deeply to deserialize with the available stack space", 0, 0);
        }

        var converter = propertyConverter ?? options.GetConverter(targetType);

        if (converter != null)
        {
            return converter.Read(value, targetType, options);
        }

        if (targetType.IsInstanceOfType(value) && typeof(ToonValue).IsAssignableFrom(targetType))
        {
            return value;
        }

        var underlyingType = Nullable.GetUnderlyingType(targetType);

        if (value is ToonNull)
        {
            if (targetType.IsValueType && underlyingType == null)
            {
                throw Error($"Cannot convert null to the non-nullable type {targetType.Name}", targetType, value, state);
            }

            return null;
        }

        targetType = underlyingType ?? targetType;

        // A converter for T also reads T? (it is used for T? values when writing, through their runtime type)
        if (underlyingType != null && propertyConverter == null && options.GetConverter(underlyingType) is { } underlyingConverter)
        {
            return underlyingConverter.Read(value, underlyingType, options);
        }

        if (targetType == typeof(object))
        {
            return ToUntyped(value);
        }

        if (TryDeserializePrimitive(value, targetType, state, out var primitiveResult))
        {
            return primitiveResult;
        }

        if (GetTypeMetadata(targetType).TypeConverter is { } typeConverter)
        {
            return typeConverter.Read(value, targetType, options);
        }

        if (TryDeserializeDictionary(value, targetType, options, depth, state, out var dictionaryResult))
        {
            return dictionaryResult;
        }

        if (TryDeserializeCollection(value, targetType, options, depth, state, out var collectionResult))
        {
            return collectionResult;
        }

        return DeserializeObject(value, targetType, options, depth, state);
    }

    private static ToonSerializationException Error(string message, Type targetType, ToonValue? value, SerializerState state, Exception? inner = null)
    {
        var path = state.Path;
        var text = $"{message}\n  📍 Path: {path}\n  🎯 Target Type: {targetType.Name}";

        if (value is ToonString or ToonNumber or ToonBoolean)
        {
            text += $"\n  ⚠️  Value: {value}";
        }

        return inner == null
            ? new ToonSerializationException(text) { TargetType = targetType, PropertyName = path }
            : new ToonSerializationException(text, inner) { TargetType = targetType, PropertyName = path };
    }

    /// <summary>
    ///     Maps a TOON value to plain .NET values for <see cref="object"/> targets: objects become
    ///     <see cref="Dictionary{TKey,TValue}"/>, arrays <see cref="List{T}"/>, integers <see cref="long"/>,
    ///     other exact numbers <see cref="decimal"/> and the rest <see cref="double"/>.
    /// </summary>
    private static object? ToUntyped(ToonValue value)
    {
        return value switch
        {
            ToonNull      => null,
            ToonBoolean b => b.Value,
            ToonString s  => s.Value,
            ToonNumber { DecimalValue: { } exact } when exact == decimal.Truncate(exact) && exact is >= long.MinValue and <= long.MaxValue
                => (long)exact,
            ToonNumber { DecimalValue: { } exact } => exact,
            ToonNumber n  => n.Value,
            ToonArray a   => a.Items.Select(ToUntyped).ToList(),
            ToonObject o  => o.Properties.ToDictionary(p => p.Key, p => ToUntyped(p.Value)),
            _             => value
        };
    }

    #region Primitives

    private static bool TryDeserializePrimitive(ToonValue value, Type targetType, SerializerState state, out object? result)
    {
        result = null;

        if (value is ToonObject or ToonArray)
        {
            return false;
        }

        try
        {
            if (targetType == typeof(string))
            {
                // Unquoted numbers and booleans are accepted for string targets (e.g. `zip: 12345` in hand-written TOON)
                result = value is ToonString s ? s.Value : value.ToString();
                return true;
            }

            if (targetType == typeof(bool))
            {
                result = value switch
                {
                    ToonBoolean b => b.Value,
                    ToonString s  => bool.Parse(s.Value),
                    _             => throw Error($"Cannot convert {value.ValueType} to Boolean", targetType, value, state)
                };
                return true;
            }

            if (targetType.IsEnum)
            {
                result = value switch
                {
                    ToonString s => Enum.Parse(targetType, s.Value, ignoreCase: true),
                    ToonNumber n => Enum.ToObject(targetType, ToInt64(n, targetType, state)),
                    _            => throw Error($"Cannot convert {value.ValueType} to {targetType.Name}", targetType, value, state)
                };
                return true;
            }

            if (value is ToonString bigText && (targetType == typeof(BigInteger) || targetType == typeof(Int128) || targetType == typeof(UInt128)))
            {
                // Integers beyond the decimal range are written as strings (lossless)
                result = targetType == typeof(BigInteger) ? BigInteger.Parse(bigText.Value, CultureInfo.InvariantCulture)
                    : targetType == typeof(Int128) ? Int128.Parse(bigText.Value, CultureInfo.InvariantCulture)
                    : UInt128.Parse(bigText.Value, CultureInfo.InvariantCulture);
                return true;
            }

            if (IsNumericType(targetType))
            {
                var number = value switch
                {
                    ToonNumber n => n,
                    ToonString s => Parsing.ToonParser.TryParseNumber(s.Value.Trim())
                                    ?? throw Error($"'{s.Value}' is not a number", targetType, value, state),
                    _            => throw Error($"Cannot convert {value.ValueType} to {targetType.Name}", targetType, value, state)
                };

                result = ConvertNumber(number, targetType, state);
                return true;
            }

            if (value is not ToonString text)
            {
                return false;
            }

            var str = text.Value;
            result = targetType switch
            {
                _ when targetType == typeof(char) => str.Length == 1 ? str[0] : throw Error("Expected a single character", targetType, value, state),
                _ when targetType == typeof(DateTime) => DateTime.Parse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                _ when targetType == typeof(DateTimeOffset) => DateTimeOffset.Parse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                _ when targetType == typeof(DateOnly) => DateOnly.Parse(str, CultureInfo.InvariantCulture),
                _ when targetType == typeof(TimeOnly) => TimeOnly.Parse(str, CultureInfo.InvariantCulture),
                _ when targetType == typeof(TimeSpan) => TimeSpan.Parse(str, CultureInfo.InvariantCulture),
                _ when targetType == typeof(Guid) => Guid.Parse(str),
                _ when targetType == typeof(Uri) => new Uri(str, UriKind.RelativeOrAbsolute),
                _ => null
            };

            return result != null;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
        {
            throw Error($"Cannot convert the value to {targetType.Name}: {ex.Message}", targetType, value, state, ex);
        }
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) || type == typeof(decimal) || type == typeof(Half) ||
               type == typeof(BigInteger) || type == typeof(Int128) || type == typeof(UInt128);
    }

    /// <summary>
    ///     Converts a number to a numeric type. Integer targets reject fractions and out-of-range values instead of
    ///     truncating or wrapping; <see cref="long"/> and <see cref="decimal"/> use the exact value when available.
    /// </summary>
    private static object ConvertNumber(ToonNumber number, Type targetType, SerializerState state)
    {
        if (targetType == typeof(double))
        {
            return number.Value;
        }

        if (targetType == typeof(float))
        {
            return (float)number.Value;
        }

        if (targetType == typeof(Half))
        {
            return (Half)number.Value;
        }

        if (targetType == typeof(decimal))
        {
            return number.DecimalValue ?? checked((decimal)number.Value);
        }

        // Integral targets
        var integral = number.DecimalValue is { } exact
            ? exact == decimal.Truncate(exact) ? exact : throw Error($"{number} is not an integer", targetType, number, state)
            : ToIntegralDecimal(number, targetType, state);

        if (targetType == typeof(BigInteger))
        {
            return new BigInteger(integral);
        }

        if (targetType == typeof(Int128))
        {
            return (Int128)integral;
        }

        if (targetType == typeof(UInt128))
        {
            return (UInt128)integral;
        }

        try
        {
            return Convert.ChangeType(integral, targetType, CultureInfo.InvariantCulture);
        }
        catch (OverflowException ex)
        {
            throw Error($"{number} is outside the range of {targetType.Name}", targetType, number, state, ex);
        }
    }

    private static decimal ToIntegralDecimal(ToonNumber number, Type targetType, SerializerState state)
    {
        var value = number.Value;

        if (value != Math.Floor(value))
        {
            throw Error($"{number} is not an integer", targetType, number, state);
        }

        try
        {
            return checked((decimal)value);
        }
        catch (OverflowException ex)
        {
            throw Error($"{number} is outside the range of {targetType.Name}", targetType, number, state, ex);
        }
    }

    private static long ToInt64(ToonNumber number, Type targetType, SerializerState state)
    {
        return (long)ConvertNumber(number, typeof(long), state);
    }

    #endregion

    #region Collections and dictionaries

    /// <summary>
    ///     Deserializes arrays into arrays, interfaces such as <see cref="IReadOnlyList{T}"/> or <see cref="ISet{T}"/>,
    ///     concrete collections with a parameterless constructor, and System.Collections.Immutable types.
    /// </summary>
    private static bool TryDeserializeCollection(ToonValue value, Type targetType, ToonSerializerOptions options, int depth, SerializerState state,
                                                 out object? result)
    {
        result = null;
        var elementType = GetElementType(targetType);

        if (elementType == null)
        {
            return false;
        }

        if (value is not ToonArray array)
        {
            throw Error($"Expected an array but got {value.ValueType}", targetType, value, state);
        }

        var items = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType), array.Count)!;

        for (var i = 0; i < array.Count; i++)
        {
            state.Push($"[{i}]");
            items.Add(DeserializeValue(array[i], elementType, options, depth + 1, state));
            state.Pop();
        }

        result = CreateCollection(targetType, elementType, items)
                 ?? throw Error($"Cannot create a collection of type {targetType.Name}", targetType, value, state);
        return true;
    }

    private static Type? GetElementType(Type type)
    {
        if (type == typeof(string) || typeof(ToonValue).IsAssignableFrom(type))
        {
            return null;
        }

        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        return type.GetInterfaces()
                   .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                   ?.GetGenericArguments()[0];
    }

    private static object? CreateCollection(Type targetType, Type elementType, IList items)
    {
        if (targetType.IsArray)
        {
            var array = Array.CreateInstance(elementType, items.Count);
            items.CopyTo(array, 0);
            return array;
        }

        if (targetType.IsInstanceOfType(items))
        {
            return items; // List<T> and the interfaces it implements
        }

        if (targetType.IsInterface)
        {
            var set = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(elementType), items)!;
            return targetType.IsInstanceOfType(set) ? set : null;
        }

        if (targetType is { IsGenericType: true, Namespace: "System.Collections.Immutable" })
        {
            var factoryType = targetType.Assembly.GetType($"System.Collections.Immutable.{targetType.Name.Split('`')[0]}");
            var createRange = factoryType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                         .FirstOrDefault(m => m.Name == "CreateRange" && m.GetParameters().Length == 1);
            return createRange?.MakeGenericMethod(elementType).Invoke(null, [items]);
        }

        if (targetType.GetConstructor(Type.EmptyTypes) == null)
        {
            return null;
        }

        var collection = Activator.CreateInstance(targetType)!;
        var add = typeof(ICollection<>).MakeGenericType(elementType).GetMethod("Add")!;

        if (!add.DeclaringType!.IsInstanceOfType(collection))
        {
            return null;
        }

        foreach (var item in items)
        {
            add.Invoke(collection, [item]);
        }

        return collection;
    }

    /// <summary>
    ///     Deserializes objects into dictionaries with string, numeric, enum, <see cref="Guid"/> or other parsable keys.
    /// </summary>
    private static bool TryDeserializeDictionary(ToonValue value, Type targetType, ToonSerializerOptions options, int depth, SerializerState state,
                                                 out object? result)
    {
        result = null;
        var dictionaryInterface = targetType.IsGenericType && targetType.GetGenericTypeDefinition() is var definition &&
                                  (definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>))
            ? targetType
            : targetType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

        if (dictionaryInterface == null)
        {
            return false;
        }

        if (value is not ToonObject obj)
        {
            throw Error($"Expected an object but got {value.ValueType}", targetType, value, state);
        }

        var keyType = dictionaryInterface.GetGenericArguments()[0];
        var valueType = dictionaryInterface.GetGenericArguments()[1];
        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);

        var dictionary = targetType.IsInterface || targetType.IsAssignableFrom(dictionaryType)
            ? (IDictionary)Activator.CreateInstance(dictionaryType)!
            : Activator.CreateInstance(targetType) as IDictionary
              ?? throw Error($"Cannot create a dictionary of type {targetType.Name}", targetType, value, state);

        foreach (var (key, toonValue) in obj.Properties)
        {
            state.Push(key);

            var typedKey = keyType == typeof(string)
                ? key
                : DeserializeValue(new ToonString(key), keyType, options, depth + 1, state)!;

            dictionary[typedKey] = DeserializeValue(toonValue, valueType, options, depth + 1, state);
            state.Pop();
        }

        result = dictionary;
        return true;
    }

    #endregion

    #region Objects

    /// <summary>
    ///     Deserializes an object, using the constructor selected for the type (binding parameters by name, as for
    ///     positional records) and then setting the remaining writable properties. Unknown keys are ignored.
    /// </summary>
    private static object DeserializeObject(ToonValue value, Type targetType, ToonSerializerOptions options, int depth, SerializerState state)
    {
        if (value is not ToonObject obj)
        {
            throw Error($"Expected an object but got {value.ValueType}", targetType, value, state);
        }

        var metadata = GetTypeMetadata(targetType);

        if (targetType.IsAbstract || targetType.IsInterface)
        {
            throw Error($"Cannot create an instance of the abstract type or interface {targetType.Name}; register a converter for it",
                        targetType, value, state);
        }

        if (metadata.Constructor == null && !targetType.IsValueType)
        {
            throw Error($"{targetType.Name} has no public constructor", targetType, value, state);
        }

        var boundProperties = new HashSet<PropertyMetadata>();
        object instance;

        if (metadata.ConstructorParameters.Length == 0)
        {
            instance = metadata.Constructor != null ? metadata.Constructor.Invoke(null) : Activator.CreateInstance(targetType)!;
        }
        else
        {
            var arguments = new object?[metadata.ConstructorParameters.Length];

            for (var i = 0; i < arguments.Length; i++)
            {
                var parameter = metadata.ConstructorParameters[i];
                var property = metadata.Properties.FirstOrDefault(p => string.Equals(p.Property.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
                var name = property != null ? GetPropertyName(property, options) : parameter.Name!;

                if (property != null)
                {
                    boundProperties.Add(property);
                }

                if (obj.Properties.TryGetValue(name, out var argumentValue))
                {
                    state.Push(name);
                    arguments[i] = DeserializeValue(argumentValue, parameter.ParameterType, options, depth + 1, state, property?.Converter);
                    state.Pop();
                }
                else
                {
                    arguments[i] = parameter.HasDefaultValue ? parameter.DefaultValue
                        : parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
                }
            }

            try
            {
                instance = metadata.Constructor!.Invoke(arguments);
            }
            catch (TargetInvocationException ex)
            {
                throw Error($"The constructor of {targetType.Name} threw: {ex.InnerException?.Message}", targetType, value, state, ex.InnerException);
            }
        }

        foreach (var property in metadata.Properties)
        {
            if (property.Setter == null || boundProperties.Contains(property))
            {
                continue;
            }

            var name = GetPropertyName(property, options);

            if (!obj.Properties.TryGetValue(name, out var toonValue))
            {
                continue;
            }

            state.Push(name);
            property.Setter(instance, DeserializeValue(toonValue, property.Property.PropertyType, options, depth + 1, state, property.Converter));
            state.Pop();
        }

        return instance;
    }

    #endregion
}
