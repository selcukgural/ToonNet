using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Core.Serialization;

// Typed access to primitive properties for the reflection-based serializer, so writing and reading an int, double,
// bool, ... property does not box the value through Func<object, object?> / Action<object, object?>.
public static partial class ToonSerializer
{
    /// <summary>
    ///     Reads and writes a primitive property of a class through typed delegates. Used only when neither the
    ///     property nor the options have a converter; the results match <see cref="SerializeValue"/> and
    ///     <see cref="DeserializeValue"/>.
    /// </summary>
    private abstract class PrimitiveAccessor
    {
        /// <summary>
        ///     Converts the property value of <paramref name="instance"/>; null when the value is null.
        /// </summary>
        public abstract ToonValue? Write(object instance);

        /// <summary>
        ///     Sets the property from <paramref name="value"/> when it is an exact match (a string for a string, an
        ///     integral number in range for an integer, ...). Returns <c>false</c> otherwise, and the caller takes the
        ///     general path, which also reports errors.
        /// </summary>
        public abstract bool TryRead(object instance, ToonValue value);

        /// <summary>
        ///     Sets the property from a primitive token when it is an exact match, like <see cref="TryRead"/>.
        /// </summary>
        public abstract bool TryReadToken(object instance, ToonParser parser, in ToonParser.ValueRef value);

        /// <summary>Whether the property value of <paramref name="instance"/> is null.</summary>
        public abstract bool IsNull(object instance);

        /// <summary>Writes the property value of <paramref name="instance"/> as TOON text (<c>null</c> when it is null).</summary>
        public abstract void WriteText(ToonWriter writer, object instance);

        /// <summary>
        ///     Creates an accessor for a property of a class or struct with a primitive or enum type; null for other
        ///     properties.
        /// </summary>
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ToonSerializer.ReflectionJustification)]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = ToonSerializer.ReflectionJustification)]
        [UnconditionalSuppressMessage("Trimming", "IL2076", Justification = ToonSerializer.ReflectionJustification)]
        public static PrimitiveAccessor? Create(PropertyInfo property)
        {
            var declaringType = property.DeclaringType!;
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (!(IsTypedPrimitive(type) || type.IsEnum) || property.GetGetMethod() is not { } getter)
            {
                return null;
            }

            // Struct properties are reached through a reference into the boxed instance, as the compiled setters do
            var underlying = Nullable.GetUnderlyingType(property.PropertyType);
            var accessorType = (declaringType.IsValueType, underlying != null) switch
            {
                (false, false) => typeof(PrimitiveAccessor<,>).MakeGenericType(declaringType, property.PropertyType),
                (false, true) => typeof(NullablePrimitiveAccessor<,>).MakeGenericType(declaringType, underlying!),
                (true, false) => typeof(StructPrimitiveAccessor<,>).MakeGenericType(declaringType, property.PropertyType),
                (true, true) => typeof(StructNullablePrimitiveAccessor<,>).MakeGenericType(declaringType, underlying!)
            };
            return (PrimitiveAccessor)Activator.CreateInstance(accessorType, getter, property.GetSetMethod(nonPublic: true))!;
        }

        public static bool IsTypedPrimitive(Type type)
        {
            return type == typeof(string) || type == typeof(bool) || type == typeof(char) ||
                   type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
                   type == typeof(sbyte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong) ||
                   type == typeof(double) || type == typeof(float) || type == typeof(decimal) ||
                   type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(Guid) ||
                   type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(TimeSpan);
        }
    }

    private sealed class PrimitiveAccessor<TDeclaring, TValue> : PrimitiveAccessor where TDeclaring : class
    {
        private readonly Func<TDeclaring, TValue> _get;
        private readonly Action<TDeclaring, TValue>? _set;

        public PrimitiveAccessor(MethodInfo getter, MethodInfo? setter)
        {
            _get = getter.CreateDelegate<Func<TDeclaring, TValue>>();
            _set = setter?.CreateDelegate<Action<TDeclaring, TValue>>();
        }

        public override ToonValue? Write(object instance) => WriteTyped(_get((TDeclaring)instance));

        public override bool TryRead(object instance, ToonValue value)
        {
            if (_set == null || !TryReadTyped(value, out TValue result))
            {
                return false;
            }

            _set((TDeclaring)instance, result);
            return true;
        }

        public override bool TryReadToken(object instance, ToonParser parser, in ToonParser.ValueRef value)
        {
            if (_set == null || !ToonSerializer.TryReadToken(parser, value, out TValue result))
            {
                return false;
            }

            _set((TDeclaring)instance, result);
            return true;
        }

        public override bool IsNull(object instance) => _get((TDeclaring)instance) is null;

        public override void WriteText(ToonWriter writer, object instance) => WriteTypedText(writer, _get((TDeclaring)instance));
    }

    /// <summary>A <see cref="PrimitiveAccessor"/> for a <see cref="Nullable{T}"/> property, which unwraps without boxing.</summary>
    private sealed class NullablePrimitiveAccessor<TDeclaring, TValue> : PrimitiveAccessor where TDeclaring : class where TValue : struct
    {
        private readonly Func<TDeclaring, TValue?> _get;
        private readonly Action<TDeclaring, TValue?>? _set;

        public NullablePrimitiveAccessor(MethodInfo getter, MethodInfo? setter)
        {
            _get = getter.CreateDelegate<Func<TDeclaring, TValue?>>();
            _set = setter?.CreateDelegate<Action<TDeclaring, TValue?>>();
        }

        public override ToonValue? Write(object instance) => _get((TDeclaring)instance) is { } value ? WriteTyped(value) : null;

        public override bool TryRead(object instance, ToonValue value)
        {
            if (_set == null || !TryReadTyped<TValue?>(value, out var result))
            {
                return false;
            }

            _set((TDeclaring)instance, result);
            return true;
        }

        public override bool TryReadToken(object instance, ToonParser parser, in ToonParser.ValueRef value)
        {
            if (_set == null || !ToonSerializer.TryReadToken<TValue?>(parser, value, out var result))
            {
                return false;
            }

            _set((TDeclaring)instance, result);
            return true;
        }

        public override bool IsNull(object instance) => !_get((TDeclaring)instance).HasValue;

        public override void WriteText(ToonWriter writer, object instance)
        {
            if (_get((TDeclaring)instance) is { } value)
            {
                WriteTypedText(writer, value);
            }
            else
            {
                writer.WriteNull();
            }
        }
    }

    private delegate TValue RefGetter<TDeclaring, out TValue>(ref TDeclaring instance);

    private delegate void RefSetter<TDeclaring, in TValue>(ref TDeclaring instance, TValue value);

    /// <summary>A <see cref="PrimitiveAccessor"/> for a property of a struct, which reads and writes the boxed instance in place.</summary>
    private sealed class StructPrimitiveAccessor<TDeclaring, TValue> : PrimitiveAccessor where TDeclaring : struct
    {
        private readonly RefGetter<TDeclaring, TValue> _get;
        private readonly RefSetter<TDeclaring, TValue>? _set;

        public StructPrimitiveAccessor(MethodInfo getter, MethodInfo? setter)
        {
            _get = getter.CreateDelegate<RefGetter<TDeclaring, TValue>>();
            _set = setter?.CreateDelegate<RefSetter<TDeclaring, TValue>>();
        }

        private TValue Get(object instance) => _get(ref Unsafe.Unbox<TDeclaring>(instance));

        public override ToonValue? Write(object instance) => WriteTyped(Get(instance));

        public override bool TryRead(object instance, ToonValue value)
        {
            if (_set == null || !TryReadTyped(value, out TValue result))
            {
                return false;
            }

            _set(ref Unsafe.Unbox<TDeclaring>(instance), result);
            return true;
        }

        public override bool TryReadToken(object instance, ToonParser parser, in ToonParser.ValueRef value)
        {
            if (_set == null || !ToonSerializer.TryReadToken(parser, value, out TValue result))
            {
                return false;
            }

            _set(ref Unsafe.Unbox<TDeclaring>(instance), result);
            return true;
        }

        public override bool IsNull(object instance) => Get(instance) is null;

        public override void WriteText(ToonWriter writer, object instance) => WriteTypedText(writer, Get(instance));
    }

    /// <summary>A <see cref="StructPrimitiveAccessor{TDeclaring,TValue}"/> for a <see cref="Nullable{T}"/> property.</summary>
    private sealed class StructNullablePrimitiveAccessor<TDeclaring, TValue> : PrimitiveAccessor where TDeclaring : struct where TValue : struct
    {
        private readonly RefGetter<TDeclaring, TValue?> _get;
        private readonly RefSetter<TDeclaring, TValue?>? _set;

        public StructNullablePrimitiveAccessor(MethodInfo getter, MethodInfo? setter)
        {
            _get = getter.CreateDelegate<RefGetter<TDeclaring, TValue?>>();
            _set = setter?.CreateDelegate<RefSetter<TDeclaring, TValue?>>();
        }

        private TValue? Get(object instance) => _get(ref Unsafe.Unbox<TDeclaring>(instance));

        public override ToonValue? Write(object instance) => Get(instance) is { } value ? WriteTyped(value) : null;

        public override bool TryRead(object instance, ToonValue value)
        {
            if (_set == null || !TryReadTyped<TValue?>(value, out var result))
            {
                return false;
            }

            _set(ref Unsafe.Unbox<TDeclaring>(instance), result);
            return true;
        }

        public override bool TryReadToken(object instance, ToonParser parser, in ToonParser.ValueRef value)
        {
            if (_set == null || !ToonSerializer.TryReadToken<TValue?>(parser, value, out var result))
            {
                return false;
            }

            _set(ref Unsafe.Unbox<TDeclaring>(instance), result);
            return true;
        }

        public override bool IsNull(object instance) => !Get(instance).HasValue;

        public override void WriteText(ToonWriter writer, object instance)
        {
            if (Get(instance) is { } value)
            {
                WriteTypedText(writer, value);
            }
            else
            {
                writer.WriteNull();
            }
        }
    }

    /// <summary>
    ///     Writes a primitive as TOON text, exactly as <see cref="WriteTyped{T}"/> followed by the encoder would, without
    ///     creating a <see cref="ToonValue"/>.
    /// </summary>
    internal static void WriteTypedText<T>(ToonWriter writer, T value)
    {
        if (value is null)
        {
            writer.WriteNull();
        }
        else if (typeof(T) == typeof(string))
        {
            writer.WriteString(Unsafe.As<T, string>(ref value));
        }
        else if (typeof(T) == typeof(int))
        {
            writer.WriteInteger(Unsafe.As<T, int>(ref value));
        }
        else if (typeof(T) == typeof(long))
        {
            writer.WriteInteger(Unsafe.As<T, long>(ref value));
        }
        else if (typeof(T) == typeof(double))
        {
            writer.WriteDouble(Unsafe.As<T, double>(ref value));
        }
        else if (typeof(T) == typeof(bool))
        {
            writer.WriteBoolean(Unsafe.As<T, bool>(ref value));
        }
        else if (typeof(T) == typeof(decimal))
        {
            writer.WriteDecimal(Unsafe.As<T, decimal>(ref value));
        }
        else if (typeof(T) == typeof(float))
        {
            writer.WriteDouble(ToonNumber.SingleToDouble(Unsafe.As<T, float>(ref value)));
        }
        else if (typeof(T) == typeof(short))
        {
            writer.WriteInteger(Unsafe.As<T, short>(ref value));
        }
        else if (typeof(T) == typeof(byte))
        {
            writer.WriteInteger(Unsafe.As<T, byte>(ref value));
        }
        else if (typeof(T) == typeof(sbyte))
        {
            writer.WriteInteger(Unsafe.As<T, sbyte>(ref value));
        }
        else if (typeof(T) == typeof(ushort))
        {
            writer.WriteInteger(Unsafe.As<T, ushort>(ref value));
        }
        else if (typeof(T) == typeof(uint))
        {
            writer.WriteInteger(Unsafe.As<T, uint>(ref value));
        }
        else if (typeof(T) == typeof(ulong))
        {
            writer.WriteDecimal(Unsafe.As<T, ulong>(ref value));
        }
        else if (typeof(T) == typeof(char))
        {
            writer.WriteString(new ReadOnlySpan<char>(in Unsafe.As<T, char>(ref value)));
        }
        else if (typeof(T) == typeof(DateTime))
        {
            Span<char> text = stackalloc char[ToonDateFormat.MaxLength];
            writer.WriteString(text[..ToonDateFormat.Format(Unsafe.As<T, DateTime>(ref value), text)]);
        }
        else if (typeof(T) == typeof(DateTimeOffset))
        {
            Span<char> text = stackalloc char[ToonDateFormat.MaxLength];
            writer.WriteString(text[..ToonDateFormat.Format(Unsafe.As<T, DateTimeOffset>(ref value), text)]);
        }
        else if (typeof(T) == typeof(Guid))
        {
            Span<char> text = stackalloc char[36];
            Unsafe.As<T, Guid>(ref value).TryFormat(text, out var length, "D");
            writer.WriteString(text[..length]);
        }
        // The formats of TrySerializePrimitive, without the string and ToonString it creates
        else if (typeof(T) == typeof(DateOnly))
        {
            Span<char> text = stackalloc char[16];
            Unsafe.As<T, DateOnly>(ref value).TryFormat(text, out var length, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            writer.WriteString(text[..length]);
        }
        else if (typeof(T) == typeof(TimeOnly))
        {
            Span<char> text = stackalloc char[24];
            Unsafe.As<T, TimeOnly>(ref value).TryFormat(text, out var length, "HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);
            writer.WriteString(text[..length].TrimEnd('.'));
        }
        else if (typeof(T) == typeof(TimeSpan))
        {
            Span<char> text = stackalloc char[32];
            Unsafe.As<T, TimeSpan>(ref value).TryFormat(text, out var length, "c", CultureInfo.InvariantCulture);
            writer.WriteString(text[..length]);
        }
        else if (typeof(T).IsEnum)
        {
            writer.WriteString(EnumText<T>.GetName(value) ?? value.ToString()!);
        }
        else
        {
            writer.WritePrimitive(WriteTyped(value));
        }
    }

    /// <summary>
    ///     Converts a primitive like <see cref="TrySerializePrimitive"/> without boxing it; the type checks are
    ///     resolved when the method is compiled for <typeparamref name="T"/>.
    /// </summary>
    /// <returns>The value, or null when <paramref name="value"/> is null.</returns>
    private static ToonValue? WriteTyped<T>(T value)
    {
        if (typeof(T) == typeof(string))
        {
            return value is null ? null : new ToonString(Unsafe.As<T, string>(ref value));
        }

        if (default(T) == null)
        {
            // Nullable<TPrimitive>: unwrap through the non-nullable branches below
            return value is null ? null : WriteBoxedPrimitive(value);
        }

        if (typeof(T).IsEnum)
        {
            return new ToonString(EnumText<T>.GetName(value) ?? value!.ToString()!);
        }

        if (typeof(T) == typeof(int))
        {
            return new ToonNumber((long)Unsafe.As<T, int>(ref value));
        }

        if (typeof(T) == typeof(long))
        {
            return new ToonNumber(Unsafe.As<T, long>(ref value));
        }

        if (typeof(T) == typeof(double))
        {
            return new ToonNumber(Unsafe.As<T, double>(ref value));
        }

        if (typeof(T) == typeof(bool))
        {
            return ToonBoolean.From(Unsafe.As<T, bool>(ref value));
        }

        if (typeof(T) == typeof(decimal))
        {
            return new ToonNumber(Unsafe.As<T, decimal>(ref value));
        }

        if (typeof(T) == typeof(float))
        {
            return new ToonNumber(Unsafe.As<T, float>(ref value));
        }

        if (typeof(T) == typeof(short))
        {
            return new ToonNumber((long)Unsafe.As<T, short>(ref value));
        }

        if (typeof(T) == typeof(byte))
        {
            return new ToonNumber((long)Unsafe.As<T, byte>(ref value));
        }

        if (typeof(T) == typeof(sbyte))
        {
            return new ToonNumber((long)Unsafe.As<T, sbyte>(ref value));
        }

        if (typeof(T) == typeof(ushort))
        {
            return new ToonNumber((long)Unsafe.As<T, ushort>(ref value));
        }

        if (typeof(T) == typeof(uint))
        {
            return new ToonNumber((long)Unsafe.As<T, uint>(ref value));
        }

        if (typeof(T) == typeof(ulong))
        {
            return new ToonNumber(Unsafe.As<T, ulong>(ref value));
        }

        if (typeof(T) == typeof(char))
        {
            return new ToonString(Unsafe.As<T, char>(ref value).ToString());
        }

        if (typeof(T) == typeof(DateTime))
        {
            return new ToonString(ToonDateFormat.ToString(Unsafe.As<T, DateTime>(ref value)));
        }

        if (typeof(T) == typeof(DateTimeOffset))
        {
            return new ToonString(ToonDateFormat.ToString(Unsafe.As<T, DateTimeOffset>(ref value)));
        }

        if (typeof(T) == typeof(Guid))
        {
            return new ToonString(Unsafe.As<T, Guid>(ref value).ToString("D"));
        }

        return WriteBoxedPrimitive(value!);

        static ToonValue WriteBoxedPrimitive(object value) =>
            TrySerializePrimitive(value, out var result) ? result! : throw new InvalidOperationException($"{value.GetType().Name} is not a primitive");
    }

    /// <summary>
    ///     Reads an exact match for <typeparamref name="T"/> (or its nullable form) without boxing: the same matches
    ///     generated code converts inline. Everything else returns <c>false</c> for the general path.
    /// </summary>
    private static bool TryReadTyped<T>(ToonValue value, out T result)
    {
        result = default!;

        if (typeof(T).IsEnum)
        {
            // Exact names only; other names, numbers and errors take the general path
            return value is ToonString name && EnumText<T>.TryParse(name.Value, out result);
        }

        if (typeof(T) == typeof(string))
        {
            switch (value)
            {
                case ToonString s:
                    var text = s.Value;
                    result = Unsafe.As<string, T>(ref text);
                    return true;
                case ToonNull:
                    return true; // null for a reference type
                default:
                    return false;
            }
        }

        if (default(T) == null && value is ToonNull)
        {
            return true; // null for a Nullable<TPrimitive>
        }

        if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
        {
            return value is ToonBoolean b && Set(b.Value, out result);
        }

        if (value is not ToonNumber number)
        {
            return false;
        }

        if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
        {
            return Set(number.Value, out result);
        }

        if (typeof(T) == typeof(float) || typeof(T) == typeof(float?))
        {
            var single = (float)number.Value;
            return float.IsFinite(single) && Set(single, out result);
        }

        if (number.DecimalValue is not { } exact)
        {
            return false;
        }

        if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
        {
            return Set(exact, out result);
        }

        if (exact != decimal.Truncate(exact))
        {
            return false;
        }

        if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
        {
            return exact is >= int.MinValue and <= int.MaxValue && Set((int)exact, out result);
        }

        if (typeof(T) == typeof(long) || typeof(T) == typeof(long?))
        {
            return exact is >= long.MinValue and <= long.MaxValue && Set((long)exact, out result);
        }

        if (typeof(T) == typeof(short) || typeof(T) == typeof(short?))
        {
            return exact is >= short.MinValue and <= short.MaxValue && Set((short)exact, out result);
        }

        if (typeof(T) == typeof(byte) || typeof(T) == typeof(byte?))
        {
            return exact is >= byte.MinValue and <= byte.MaxValue && Set((byte)exact, out result);
        }

        if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(sbyte?))
        {
            return exact is >= sbyte.MinValue and <= sbyte.MaxValue && Set((sbyte)exact, out result);
        }

        if (typeof(T) == typeof(ushort) || typeof(T) == typeof(ushort?))
        {
            return exact is >= ushort.MinValue and <= ushort.MaxValue && Set((ushort)exact, out result);
        }

        if (typeof(T) == typeof(uint) || typeof(T) == typeof(uint?))
        {
            return exact is >= uint.MinValue and <= uint.MaxValue && Set((uint)exact, out result);
        }

        if (typeof(T) == typeof(ulong) || typeof(T) == typeof(ulong?))
        {
            return exact is >= ulong.MinValue and <= ulong.MaxValue && Set((ulong)exact, out result);
        }

        return false;

        // Stores a primitive in T, which is that primitive or its Nullable<>
        static bool Set<TPrimitive>(TPrimitive primitive, out T target) where TPrimitive : struct
        {
            if (typeof(T) == typeof(TPrimitive))
            {
                target = Unsafe.As<TPrimitive, T>(ref primitive);
            }
            else
            {
                TPrimitive? nullable = primitive;
                target = Unsafe.As<TPrimitive?, T>(ref nullable);
            }

            return true;
        }
    }

    /// <summary>
    ///     The names of an enum type's values, to write and read them without boxing. Values without a single name of
    ///     their own (flag combinations, undefined values, aliases) and names that differ only in case leave the work
    ///     to <see cref="Enum"/>, so the results are those of <c>ToString</c> and <c>Enum.Parse</c>.
    /// </summary>
    private static class EnumText<T>
    {
        private static readonly ulong[] Values;
        private static readonly string[] Names;

        /// <summary>Whether reading exact names gives what <c>Enum.Parse</c> with <c>ignoreCase</c> gives.</summary>
        private static readonly bool ReadNames;

        static EnumText()
        {
            // Both are sorted by value (GetValuesAsUnderlyingType needs no generated code for the enum type)
            var names = Enum.GetNames(typeof(T));
            var values = Enum.GetValuesAsUnderlyingType(typeof(T));
            var bits = new ulong[values.Length];

            for (var i = 0; i < bits.Length; i++)
            {
                if (ToBits(values.GetValue(i)) is not { } valueBits)
                {
                    Values = [];
                    Names = [];
                    return;
                }

                bits[i] = valueBits;
            }

            if (bits.Distinct().Count() != bits.Length)
            {
                // ToString picks one of the names of an alias; leave the choice to it
                Values = [];
                Names = [];
                return;
            }

            Values = bits;
            Names = names;
            ReadNames = names.Distinct(StringComparer.OrdinalIgnoreCase).Count() == names.Length;
        }

        /// <summary>The name of a value with one of its own, or null.</summary>
        public static string? GetName(T value)
        {
            var bits = Bits(value);
            var values = Values;

            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] == bits)
                {
                    return Names[i];
                }
            }

            return null;
        }

        /// <summary>Reads an exact name; false for anything else.</summary>
        public static bool TryParse(ReadOnlySpan<char> text, out T value)
        {
            if (ReadNames)
            {
                var names = Names;

                for (var i = 0; i < names.Length; i++)
                {
                    if (text.SequenceEqual(names[i]))
                    {
                        value = FromBits(Values[i]);
                        return true;
                    }
                }
            }

            value = default!;
            return false;
        }

        private static ulong Bits(T value) => Unsafe.SizeOf<T>() switch
        {
            1 => Unsafe.As<T, byte>(ref value),
            2 => Unsafe.As<T, ushort>(ref value),
            4 => Unsafe.As<T, uint>(ref value),
            _ => Unsafe.As<T, ulong>(ref value)
        };

        private static T FromBits(ulong bits)
        {
            switch (Unsafe.SizeOf<T>())
            {
                case 1:
                {
                    var narrow = (byte)bits;
                    return Unsafe.As<byte, T>(ref narrow);
                }
                case 2:
                {
                    var narrow = (ushort)bits;
                    return Unsafe.As<ushort, T>(ref narrow);
                }
                case 4:
                {
                    var narrow = (uint)bits;
                    return Unsafe.As<uint, T>(ref narrow);
                }
                default:
                    return Unsafe.As<ulong, T>(ref bits);
            }
        }

        /// <summary>The bits of an underlying value as <see cref="Bits"/> reads them from the enum, or null for other types.</summary>
        private static ulong? ToBits(object? value) => value switch
        {
            byte v => v,
            sbyte v => (byte)v,
            short v => (ushort)v,
            ushort v => v,
            int v => (uint)v,
            uint v => v,
            long v => (ulong)v,
            ulong v => v,
            _ => null
        };
    }
}
