using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Core.Serialization;

public static partial class ToonSerializer
{
    /// <summary>
    ///     Converts a TOON value to an instance of <paramref name="targetType"/>.
    /// </summary>
    /// <exception cref="ToonSerializationException">
    ///     Thrown when the value cannot be converted to the target type or the maximum depth is exceeded.
    /// </exception>
    private static object? DeserializeValue(ToonValue value, Type targetType, ToonSerializerOptions options, int depth, SerializerState state,
                                            IToonConverter? propertyConverter = null)
    {
        var generatedAllowed = !state.FromGeneratedCode;
        state.FromGeneratedCode = false;

        var converter = propertyConverter ?? options.GetConverter(targetType);

        if (converter != null)
        {
            return converter.Read(value, targetType, options);
        }

        if (targetType.IsInstanceOfType(value) && typeof(ToonValue).IsAssignableFrom(targetType))
        {
            return value;
        }

        // A ToonDocument, JsonElement or JsonNode is created from the value (null included)
        if (ToonJsonInterop.TryFromToon(value, targetType, out var documentValue))
        {
            return documentValue;
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
            return ToUntyped(value, options, depth);
        }

        // Generated code reads objects; other values are reported by the checks below
        if (generatedAllowed && value is ToonObject && !options.PropertyNameCaseInsensitive &&
            GetGeneratedSerializer(targetType, options) is { Deserialize: { } generated })
        {
            return generated(value, options, depth);
        }

        if (TryDeserializePrimitive(value, targetType, state, out var primitiveResult))
        {
            return primitiveResult;
        }

        if (GetTypeMetadata(targetType).TypeConverter is { } typeConverter)
        {
            return typeConverter.Read(value, targetType, options);
        }

        if (IsScalarType(targetType))
        {
            throw Error($"Cannot convert {value.ValueType} to {targetType.Name}", targetType, value, state);
        }

        // Only objects, collections and dictionaries count towards the depth (the root is at depth 0)
        EnsureDepth(options, depth, targetType, state);

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

    /// <exception cref="ToonSerializationException">Thrown when the maximum depth or the available stack space is exceeded.</exception>
    private static void EnsureDepth(ToonSerializerOptions options, int depth, Type targetType, SerializerState? state)
    {
        if (depth > options.ValidatedMaxDepth)
        {
            throw DepthError($"Maximum depth of {options.MaxDepth} exceeded during deserialization", targetType, state?.Path);
        }

        if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            throw DepthError("Document is nested too deeply to deserialize with the available stack space", targetType, state?.Path);
        }
    }

    internal static ToonSerializationException DepthError(string message, Type targetType, string? path)
    {
        return new ToonSerializationException(path == null ? message : $"{message}\n  📍 Path: {path}")
        {
            TargetType = targetType,
            Path = path
        };
    }

    /// <summary>The error for a value of the wrong kind (an array for an object, ...), for composed type infos.</summary>
    internal static ToonSerializationException ShapeError(string message, Type targetType, ToonValue value) =>
        Error(message, targetType, value, SerializerState.Root);

    /// <remarks>
    ///     The message names the path and the type but not the value, which may be personal data and ends up in logs and
    ///     (through model state) in HTTP responses; the value is available as <see cref="ToonSerializationException.Value"/>.
    /// </remarks>
    private static ToonSerializationException Error(string message, Type targetType, ToonValue? value, SerializerState state, Exception? inner = null)
    {
        var path = state.Path;
        var text = $"{message}\n  📍 Path: {path}\n  🎯 Target Type: {targetType.Name}";

        return inner == null
            ? new ToonSerializationException(text) { TargetType = targetType, Path = path, Value = value }
            : new ToonSerializationException(text, inner) { TargetType = targetType, Path = path, Value = value };
    }

    /// <summary>
    ///     Maps a TOON value to plain .NET values for <see cref="object"/> targets: objects become
    ///     <see cref="Dictionary{TKey,TValue}"/>, arrays <see cref="List{T}"/>, integers <see cref="long"/>,
    ///     other exact numbers <see cref="decimal"/> and the rest <see cref="double"/>.
    /// </summary>
    private static object? ToUntyped(ToonValue value, ToonSerializerOptions options, int depth)
    {
        if (value is ToonArray or ToonObject)
        {
            EnsureDepth(options, depth, typeof(object), null);
        }

        return value switch
        {
            ToonNull      => null,
            ToonBoolean b => b.Value,
            ToonString s  => s.Value,
            ToonNumber { DecimalValue: { } exact } when exact == decimal.Truncate(exact) && exact is >= long.MinValue and <= long.MaxValue
                => (long)exact,
            ToonNumber { DecimalValue: { } exact } => exact,
            ToonNumber n  => n.Value,
            ToonArray a   => ToUntypedList(a, options, depth),
            ToonObject o  => ToUntypedDictionary(o, options, depth),
            _             => value
        };
    }

    private static List<object?> ToUntypedList(ToonArray array, ToonSerializerOptions options, int depth)
    {
        var list = new List<object?>(array.Count);

        foreach (var item in array.Items)
        {
            list.Add(ToUntyped(item, options, depth + 1));
        }

        return list;
    }

    private static Dictionary<string, object?> ToUntypedDictionary(ToonObject obj, ToonSerializerOptions options, int depth)
    {
        var dictionary = new Dictionary<string, object?>(obj.Properties.Count);

        foreach (var (key, value) in obj.Properties)
        {
            dictionary.Add(key, ToUntyped(value, options, depth + 1));
        }

        return dictionary;
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
                // Integers beyond the decimal range are written as strings (lossless). Each branch is boxed on its own: a
                // conditional expression would convert Int128 and UInt128 to BigInteger.
                if (targetType == typeof(Int128))
                {
                    result = Int128.Parse(bigText.Value, CultureInfo.InvariantCulture);
                }
                else if (targetType == typeof(UInt128))
                {
                    result = UInt128.Parse(bigText.Value, CultureInfo.InvariantCulture);
                }
                else
                {
                    result = BigInteger.Parse(bigText.Value, CultureInfo.InvariantCulture);
                }

                return true;
            }

            if (IsNumericType(targetType))
            {
                var number = value switch
                {
                    ToonNumber n => n,
                    ToonString s => Parsing.ToonParser.TryParseNumber(s.Value.Trim())
                                    ?? throw Error("The string is not a number", targetType, value, state),
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
            // The inner message may quote the value (Enum.Parse does), so it is not repeated here
            throw Error($"Cannot convert the {value.ValueType} value to {targetType.Name} ({ex.GetType().Name})", targetType, value, state, ex);
        }
    }

    /// <summary>
    ///     Types that are read from a single TOON primitive; an object or array cannot be converted to them.
    /// </summary>
    private static bool IsScalarType(Type type)
    {
        return type == typeof(string) || type == typeof(bool) || type.IsEnum || IsNumericType(type) || type == typeof(char) ||
               type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly) ||
               type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(Uri);
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
            var single = (float)number.Value;
            return float.IsFinite(single) ? single : throw Error("The number is outside the range of Single", targetType, number, state);
        }

        if (targetType == typeof(Half))
        {
            var half = (Half)number.Value;
            return Half.IsFinite(half) ? half : throw Error("The number is outside the range of Half", targetType, number, state);
        }

        // Integers beyond the decimal range keep their parsed text, which the wide integer types read exactly
        if (number.DecimalValue == null && number.ParsedText is { } text &&
            (targetType == typeof(BigInteger) || targetType == typeof(Int128) || targetType == typeof(UInt128)))
        {
            return ParseWideInteger(text, number, targetType, state);
        }

        if (targetType == typeof(decimal))
        {
            return number.DecimalValue ?? checked((decimal)number.Value);
        }

        // Integral targets
        var integral = number.DecimalValue is { } exact
            ? exact == decimal.Truncate(exact) ? exact : throw Error("The number is not an integer", targetType, number, state)
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
            // Decimal conversions to integer types throw OverflowException when the value is out of range
            return Type.GetTypeCode(targetType) switch
            {
                TypeCode.Int32  => (int)integral,
                TypeCode.Int64  => (long)integral,
                TypeCode.Int16  => (short)integral,
                TypeCode.Byte   => (byte)integral,
                TypeCode.SByte  => (sbyte)integral,
                TypeCode.UInt16 => (ushort)integral,
                TypeCode.UInt32 => (uint)integral,
                TypeCode.UInt64 => (ulong)integral,
                _               => Convert.ChangeType(integral, targetType, CultureInfo.InvariantCulture)
            };
        }
        catch (OverflowException ex)
        {
            throw Error($"The number is outside the range of {targetType.Name}", targetType, number, state, ex);
        }
    }

    /// <summary>
    ///     Reads an integer token such as <c>123456789012345678901234567890123</c> or <c>1e40</c> exactly.
    /// </summary>
    private static object ParseWideInteger(string text, ToonNumber number, Type targetType, SerializerState state)
    {
        if (!BigInteger.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var integer))
        {
            throw Error("The number is not an integer", targetType, number, state);
        }

        try
        {
            // Box each branch separately; a conditional expression would convert them all back to BigInteger
            if (targetType == typeof(Int128))
            {
                return (Int128)integer;
            }

            if (targetType == typeof(UInt128))
            {
                return (UInt128)integer;
            }

            return integer;
        }
        catch (OverflowException ex)
        {
            throw Error($"The number is outside the range of {targetType.Name}", targetType, number, state, ex);
        }
    }

    private static decimal ToIntegralDecimal(ToonNumber number, Type targetType, SerializerState state)
    {
        var value = number.Value;

        if (value != Math.Floor(value))
        {
            throw Error("The number is not an integer", targetType, number, state);
        }

        try
        {
            return checked((decimal)value);
        }
        catch (OverflowException ex)
        {
            throw Error($"The number is outside the range of {targetType.Name}", targetType, number, state, ex);
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
            return TryDeserializeNonGenericList(value, targetType, options, depth, state, out result);
        }

        if (value is not ToonArray array)
        {
            throw Error($"Expected an array but got {value.ValueType}", targetType, value, state);
        }

        var factory = CollectionFactories.GetOrAdd(targetType, static (t, e) => CollectionFactory.Create(t, e), elementType);
        var items = factory.Helpers.CreateList(array.Count);
        var typedItems = factory.HasPrimitiveItems && options.Converters.Count == 0;

        for (var i = 0; i < array.Count; i++)
        {
            if (typedItems && factory.Helpers.TryAddTyped(items, array[i]))
            {
                continue;
            }

            state.Push(i);
            items.Add(DeserializeValue(array[i], elementType, options, depth + 1, state));
            state.Pop();
        }

        result = factory.Complete(items) ?? throw Error($"Cannot create a collection of type {targetType.Name}", targetType, value, state);
        return true;
    }

    /// <summary>
    ///     Compiles <c>items =&gt; (object)method((TParameter)items)</c> for a static factory method such as
    ///     <c>ImmutableList.CreateRange</c>, instead of calling it through <see cref="MethodBase.Invoke(object, object[])"/>.
    /// </summary>
    private static Func<TItems, object?> CompileCall<TItems>(MethodInfo method)
    {
        var items = Expression.Parameter(typeof(TItems), "items");
        var call = Expression.Call(method, Expression.Convert(items, method.GetParameters()[0].ParameterType));
        return Expression.Lambda<Func<TItems, object?>>(Expression.Convert(call, typeof(object)), items).Compile();
    }

    /// <summary>How to build each collection type read so far.</summary>
    private static readonly ConcurrentDictionary<Type, CollectionFactory> CollectionFactories = new();

    /// <summary>
    ///     Builds a collection type from a <see cref="List{T}"/> of its items, with delegates chosen once per type.
    /// </summary>
    private sealed class CollectionFactory
    {
        /// <summary>Creates the <see cref="List{T}"/> the items are read into and adds typed items to it.</summary>
        public required CollectionHelpers Helpers { get; init; }

        /// <summary>Whether items are primitives that <see cref="CollectionHelpers.TryAddTyped"/> can read without boxing.</summary>
        public required bool HasPrimitiveItems { get; init; }

        /// <summary>Creates the target from the list, or returns null when the target type cannot be created.</summary>
        public required Func<IList, object?> Complete { get; init; }

        /// <summary>
        ///     Supports arrays, types <see cref="List{T}"/> is assignable to, interfaces <see cref="HashSet{T}"/>
        ///     implements, System.Collections.Immutable types (through <c>CreateRange</c>) and concrete
        ///     <see cref="ICollection{T}"/> types with a parameterless constructor.
        /// </summary>
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ToonSerializer.ReflectionJustification)]
        public static CollectionFactory Create(Type targetType, Type elementType)
        {
            var helpers = (CollectionHelpers)Activator.CreateInstance(typeof(CollectionHelpers<>).MakeGenericType(elementType))!;

            return new CollectionFactory
            {
                Helpers = helpers,
                HasPrimitiveItems = PrimitiveAccessor.IsTypedPrimitive(Nullable.GetUnderlyingType(elementType) ?? elementType),
                Complete = SelectCompletion(targetType, elementType, helpers)
            };
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ToonSerializer.ReflectionJustification)]
        [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = ToonSerializer.ReflectionJustification)]
        [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = ToonSerializer.ReflectionJustification)]
        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = ToonSerializer.ReflectionJustification)]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = ToonSerializer.ReflectionJustification)]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ToonSerializer.ReflectionJustification)]
        private static Func<IList, object?> SelectCompletion(Type targetType, Type elementType, CollectionHelpers helpers)
        {
            if (targetType.IsArray)
            {
                return helpers.ToArray;
            }

            if (targetType.IsAssignableFrom(typeof(List<>).MakeGenericType(elementType)))
            {
                return static items => items; // List<T> and the interfaces it implements
            }

            if (targetType.IsInterface)
            {
                return targetType.IsAssignableFrom(typeof(HashSet<>).MakeGenericType(elementType)) ? helpers.ToHashSet : static _ => null;
            }

            if (targetType is { IsGenericType: true, Namespace: "System.Collections.Immutable" })
            {
                var factoryType = targetType.Assembly.GetType($"System.Collections.Immutable.{targetType.Name.Split('`')[0]}");
                var createRange = factoryType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                             .FirstOrDefault(m => m.Name == "CreateRange" && m.GetParameters().Length == 1)
                                             ?.MakeGenericMethod(elementType);
                return createRange == null ? static _ => null : CompileCall<IList>(createRange);
            }

            if (targetType.GetConstructor(Type.EmptyTypes) == null ||
                !typeof(ICollection<>).MakeGenericType(elementType).IsAssignableFrom(targetType))
            {
                return static _ => null;
            }

            var create = Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(targetType), typeof(object))).Compile();
            return items => helpers.AddAll(create(), items);
        }
    }

    /// <summary>
    ///     Typed operations on the item list, so building a collection needs no reflection per call or per item.
    /// </summary>
    private abstract class CollectionHelpers
    {
        public abstract IList CreateList(int capacity);

        /// <summary>Adds an exact primitive match for the item type without boxing; false for anything else.</summary>
        public abstract bool TryAddTyped(IList items, ToonValue value);

        /// <summary>Adds an exact primitive token match for the item type without boxing; false for anything else.</summary>
        public abstract bool TryAddToken(IList items, ToonParser parser, in ToonParser.ValueRef value);

        public abstract object ToArray(IList items);

        public abstract object ToHashSet(IList items);

        /// <summary>Adds the items to <paramref name="collection"/>, an <see cref="ICollection{T}"/>, and returns it.</summary>
        public abstract object AddAll(object collection, IList items);
    }

    private sealed class CollectionHelpers<T> : CollectionHelpers
    {
        public override IList CreateList(int capacity) => new List<T>(capacity);

        public override bool TryAddTyped(IList items, ToonValue value)
        {
            if (!TryReadTyped(value, out T item))
            {
                return false;
            }

            ((List<T>)items).Add(item);
            return true;
        }

        public override bool TryAddToken(IList items, ToonParser parser, in ToonParser.ValueRef value)
        {
            if (!TryReadToken(parser, value, out T item))
            {
                return false;
            }

            ((List<T>)items).Add(item);
            return true;
        }

        public override object ToArray(IList items) => ((List<T>)items).ToArray();

        public override object ToHashSet(IList items) => new HashSet<T>((List<T>)items);

        public override object AddAll(object collection, IList items)
        {
            var target = (ICollection<T>)collection;

            foreach (var item in (List<T>)items)
            {
                target.Add(item);
            }

            return collection;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = ToonSerializer.ReflectionJustification)]
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

        return ElementTypeCache.GetOrAdd(type, static t => t.GetInterfaces()
                                                         .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                                         ?.GetGenericArguments()[0]);
    }

    /// <summary>The <see cref="IEnumerable{T}"/> element type per collection type, or null.</summary>
    private static readonly ConcurrentDictionary<Type, Type?> ElementTypeCache = new();

    /// <summary>
    ///     Deserializes objects into dictionaries with string, numeric, enum, <see cref="Guid"/> or other parsable keys.
    /// </summary>
    private static bool TryDeserializeDictionary(ToonValue value, Type targetType, ToonSerializerOptions options, int depth, SerializerState state,
                                                 out object? result)
    {
        result = null;

        if (DictionaryFactories.GetOrAdd(targetType, static t => DictionaryFactory.Create(t)) is not { } factory)
        {
            return TryDeserializeNonGenericDictionary(value, targetType, options, depth, state, out result);
        }

        if (value is not ToonObject obj)
        {
            throw Error($"Expected an object but got {value.ValueType}", targetType, value, state);
        }

        var dictionary = factory.CreateEntries();
        var keyType = factory.KeyType;

        foreach (var (key, toonValue) in obj.Properties)
        {
            state.Push(key);

            var typedKey = keyType == typeof(string)
                ? key
                : DeserializeValue(new ToonString(key), keyType, options, depth + 1, state)!;

            dictionary[typedKey] = DeserializeValue(toonValue, factory.ValueType, options, depth + 1, state);
            state.Pop();
        }

        result = factory.Complete(dictionary) ?? throw Error($"Cannot create a dictionary of type {targetType.Name}", targetType, value, state);
        return true;
    }

    /// <summary>How to build each generic dictionary type read so far; null for types that are not one.</summary>
    private static readonly ConcurrentDictionary<Type, DictionaryFactory?> DictionaryFactories = new();

    /// <summary>
    ///     Builds a generic dictionary type from a <see cref="Dictionary{TKey,TValue}"/> of its entries, with the way
    ///     to create it chosen once per type.
    /// </summary>
    private sealed class DictionaryFactory
    {
        public required Type KeyType { get; init; }

        public required Type ValueType { get; init; }

        /// <summary>Creates the <see cref="Dictionary{TKey,TValue}"/> the entries are read into.</summary>
        public required Func<IDictionary> CreateEntries { get; init; }

        /// <summary>Creates the target from the entries, or returns null when the target type cannot be created.</summary>
        public required Func<IDictionary, object?> Complete { get; init; }

        /// <summary>
        ///     Returns null when <paramref name="targetType"/> is not <see cref="IDictionary{TKey,TValue}"/>,
        ///     <see cref="IReadOnlyDictionary{TKey,TValue}"/> or a type implementing <see cref="IDictionary{TKey,TValue}"/>.
        /// </summary>
        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = ToonSerializer.ReflectionJustification)]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ToonSerializer.ReflectionJustification)]
        public static DictionaryFactory? Create(Type targetType)
        {
            var dictionaryInterface = targetType.IsGenericType && targetType.GetGenericTypeDefinition() is var definition &&
                                      (definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>))
                ? targetType
                : targetType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

            if (dictionaryInterface == null)
            {
                return null;
            }

            var keyType = dictionaryInterface.GetGenericArguments()[0];
            var valueType = dictionaryInterface.GetGenericArguments()[1];
            var helpers = (DictionaryHelpers)Activator.CreateInstance(typeof(DictionaryHelpers<,>).MakeGenericType(keyType, valueType))!;

            return new DictionaryFactory
            {
                KeyType = keyType,
                ValueType = valueType,
                CreateEntries = helpers.CreateEntries,
                Complete = SelectCompletion(targetType, keyType, valueType, helpers)
            };
        }

        /// <summary>
        ///     The entries are used directly when the target accepts a <see cref="Dictionary{TKey,TValue}"/>;
        ///     System.Collections.Immutable types use <c>CreateRange</c>; other types a constructor taking the entries
        ///     (for example <see cref="System.Collections.ObjectModel.ReadOnlyDictionary{TKey,TValue}"/>), or a new
        ///     instance the entries are added to.
        /// </summary>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ToonSerializer.ReflectionJustification)]
        [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = ToonSerializer.ReflectionJustification)]
        [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = ToonSerializer.ReflectionJustification)]
        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = ToonSerializer.ReflectionJustification)]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = ToonSerializer.ReflectionJustification)]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ToonSerializer.ReflectionJustification)]
        private static Func<IDictionary, object?> SelectCompletion(Type targetType, Type keyType, Type valueType, DictionaryHelpers helpers)
        {
            if (targetType.IsAssignableFrom(typeof(Dictionary<,>).MakeGenericType(keyType, valueType)))
            {
                return static entries => entries;
            }

            if (targetType is { IsGenericType: true, Namespace: "System.Collections.Immutable" })
            {
                var factoryType = targetType.Assembly.GetType($"System.Collections.Immutable.{targetType.Name.Split('`')[0]}");
                var createRange = factoryType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                             .FirstOrDefault(m => m.Name == "CreateRange" && m.GetParameters().Length == 1 &&
                                                                  m.GetGenericArguments().Length == 2)
                                             ?.MakeGenericMethod(keyType, valueType);
                return createRange == null ? static _ => null : CompileCall<IDictionary>(createRange);
            }

            if (targetType.IsInterface || targetType.IsAbstract)
            {
                return static _ => null;
            }

            var genericDictionary = typeof(IDictionary<,>).MakeGenericType(keyType, valueType);
            var pairs = typeof(IEnumerable<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType));

            if ((targetType.GetConstructor([genericDictionary]) ?? targetType.GetConstructor([pairs])) is { } fromEntries)
            {
                var entries = Expression.Parameter(typeof(IDictionary), "entries");
                var creation = Expression.New(fromEntries, Expression.Convert(entries, fromEntries.GetParameters()[0].ParameterType));
                return Expression.Lambda<Func<IDictionary, object?>>(Expression.Convert(creation, typeof(object)), entries).Compile();
            }

            if (targetType.GetConstructor(Type.EmptyTypes) == null)
            {
                return static _ => null;
            }

            var create = Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(targetType), typeof(object))).Compile();

            if (typeof(IDictionary).IsAssignableFrom(targetType))
            {
                return entries => CopyEntries(entries, (IDictionary)create());
            }

            return genericDictionary.IsAssignableFrom(targetType) ? entries => helpers.AddAll(create(), entries) : static _ => null;
        }

        private static IDictionary CopyEntries(IDictionary entries, IDictionary target)
        {
            foreach (DictionaryEntry entry in entries)
            {
                target[entry.Key] = entry.Value;
            }

            return target;
        }
    }

    /// <summary>
    ///     Typed operations on the entry dictionary, so building a dictionary needs no reflection per call or per entry.
    /// </summary>
    private abstract class DictionaryHelpers
    {
        public abstract IDictionary CreateEntries();

        /// <summary>Adds the entries to <paramref name="dictionary"/>, an <see cref="IDictionary{TKey,TValue}"/>, and returns it.</summary>
        public abstract object AddAll(object dictionary, IDictionary entries);
    }

    private sealed class DictionaryHelpers<TKey, TValue> : DictionaryHelpers where TKey : notnull
    {
        public override IDictionary CreateEntries() => new Dictionary<TKey, TValue>();

        public override object AddAll(object dictionary, IDictionary entries)
        {
            var target = (IDictionary<TKey, TValue>)dictionary;

            foreach (var (key, value) in (Dictionary<TKey, TValue>)entries)
            {
                target.Add(key, value);
            }

            return dictionary;
        }
    }

    /// <summary>
    ///     Reads a TOON object into a non-generic dictionary (<see cref="Hashtable"/>, <see cref="IDictionary"/>, ...):
    ///     keys stay strings and values are read as for <see cref="object"/> targets.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = ToonSerializer.ReflectionJustification)]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = ToonSerializer.ReflectionJustification)]
    private static bool TryDeserializeNonGenericDictionary(ToonValue value, Type targetType, ToonSerializerOptions options, int depth,
                                                           SerializerState state, out object? result)
    {
        result = null;

        if (!typeof(IDictionary).IsAssignableFrom(targetType))
        {
            return false;
        }

        if (value is not ToonObject obj)
        {
            throw Error($"Expected an object but got {value.ValueType}", targetType, value, state);
        }

        var dictionary = targetType.IsInterface
            ? new Dictionary<string, object?>()
            : targetType.GetConstructor(Type.EmptyTypes) != null
                ? (IDictionary)Activator.CreateInstance(targetType)!
                : throw Error($"Cannot create a dictionary of type {targetType.Name}", targetType, value, state);

        foreach (var (key, toonValue) in obj.Properties)
        {
            dictionary[key] = ToUntyped(toonValue, options, depth + 1);
        }

        result = dictionary;
        return true;
    }

    /// <summary>
    ///     Reads a TOON array into a non-generic list (<see cref="ArrayList"/>, <see cref="IList"/>,
    ///     <see cref="IEnumerable"/>, ...); items are read as for <see cref="object"/> targets.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = ToonSerializer.ReflectionJustification)]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = ToonSerializer.ReflectionJustification)]
    private static bool TryDeserializeNonGenericList(ToonValue value, Type targetType, ToonSerializerOptions options, int depth,
                                                     SerializerState state, out object? result)
    {
        result = null;

        var isInterface = targetType == typeof(IEnumerable) || targetType == typeof(ICollection) || targetType == typeof(IList);

        if (!isInterface && !typeof(IList).IsAssignableFrom(targetType))
        {
            return false;
        }

        if (value is not ToonArray array)
        {
            throw Error($"Expected an array but got {value.ValueType}", targetType, value, state);
        }

        var list = isInterface
            ? new List<object?>()
            : targetType.GetConstructor(Type.EmptyTypes) != null && !targetType.IsArray
                ? (IList)Activator.CreateInstance(targetType)!
                : throw Error($"Cannot create a collection of type {targetType.Name}", targetType, value, state);

        foreach (var item in array.Items)
        {
            list.Add(ToUntyped(item, options, depth + 1));
        }

        result = list;
        return true;
    }

    #endregion

    #region Objects

    /// <summary>
    ///     Deserializes an object, using the constructor selected for the type (binding parameters by name, as for
    ///     positional records) and then setting the remaining writable properties. Unknown keys are ignored.
    /// </summary>
    /// <summary>
    ///     The value of a name's key: the exact key, or with case-insensitive reading (<paramref name="caseInsensitive"/>
    ///     holds the names that may match so) the last key that equals the name ignoring case, as direct reading sets a
    ///     property for each such key in order.
    /// </summary>
    private static bool TryGetKey(ToonObject obj, string name, HashSet<string>? caseInsensitive, out ToonValue value)
    {
        if (caseInsensitive == null || !caseInsensitive.Contains(name))
        {
            return obj.Properties.TryGetValue(name, out value!);
        }

        var found = false;
        value = null!;

        foreach (var (key, keyValue) in obj.Properties)
        {
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
            {
                value = keyValue;
                found = true;
            }
        }

        return found;
    }

    /// <summary>
    ///     The names of a type's properties and constructor parameters (without a property) that no other name equals
    ///     ignoring case: only these are matched ignoring case.
    /// </summary>
    private static HashSet<string> GetCaseInsensitiveNames(TypeMetadata metadata, ToonSerializerOptions options)
    {
        var policy = metadata.FixedNamingPolicy ?? options.PropertyNamingPolicy;

        if ((uint)policy < NamingPolicyCount && metadata.CaseInsensitiveNames[(int)policy] is { } cached)
        {
            return cached;
        }

        var names = metadata.Properties.Select(property => GetPropertyName(property, metadata, options))
                            .Concat(metadata.Activator.Arguments.Where(argument => argument.Property == null).Select(argument => argument.Parameter.Name!));

        var unambiguous = names.GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                               .Where(group => group.Distinct(StringComparer.Ordinal).Count() == 1)
                               .Select(group => group.Key)
                               .ToHashSet(StringComparer.Ordinal);

        if ((uint)policy < NamingPolicyCount)
        {
            metadata.CaseInsensitiveNames[(int)policy] = unambiguous;
        }

        return unambiguous;
    }

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

        var activator = metadata.Activator;
        var arguments = activator.Arguments.Length == 0 ? [] : new object?[activator.Arguments.Length];
        var caseInsensitive = options.PropertyNameCaseInsensitive ? GetCaseInsensitiveNames(metadata, options) : null;

        for (var i = 0; i < arguments.Length; i++)
        {
            var (parameter, property, missingValue) = activator.Arguments[i];
            var name = property != null ? GetPropertyName(property, metadata, options) : parameter.Name!;

            if (TryGetKey(obj, name, caseInsensitive, out var argumentValue))
            {
                state.Push(name);
                arguments[i] = DeserializeValue(argumentValue, parameter.ParameterType, options, depth + 1, state, property?.Converter);
                state.Pop();
            }
            else
            {
                arguments[i] = missingValue;
            }
        }

        object instance;

        try
        {
            instance = activator.New(arguments);
        }
        catch (Exception ex) when (ex is not ToonException)
        {
            var inner = ex is TargetInvocationException { InnerException: { } thrown } ? thrown : ex;
            throw Error($"The constructor of {targetType.Name} threw: {inner.Message}", targetType, value, state, inner);
        }

        for (var p = 0; p < metadata.Properties.Length; p++)
        {
            var property = metadata.Properties[p];

            if (property.Setter == null || activator.BoundToConstructor[p])
            {
                continue;
            }

            var name = GetPropertyName(property, metadata, options);

            if (!TryGetKey(obj, name, caseInsensitive, out var toonValue))
            {
                continue;
            }

            // Exact primitive matches are set without boxing; anything else (and every error) takes the general path
            if (property.Primitive is { } primitive && property.Converter == null && options.Converters.Count == 0 &&
                primitive.TryRead(instance, toonValue))
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
