using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;

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

        /// <summary>Whether the property value of <paramref name="instance"/> is null.</summary>
        public abstract bool IsNull(object instance);

        /// <summary>Writes the property value of <paramref name="instance"/> as TOON text (<c>null</c> when it is null).</summary>
        public abstract void WriteText(ToonWriter writer, object instance);

        /// <summary>
        ///     Creates an accessor for a property of a class with a primitive type; null for other properties.
        /// </summary>
        public static PrimitiveAccessor? Create(PropertyInfo property)
        {
            var declaringType = property.DeclaringType!;
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (declaringType.IsValueType || !IsTypedPrimitive(type) || property.GetGetMethod() is not { } getter)
            {
                return null;
            }

            var accessorType = Nullable.GetUnderlyingType(property.PropertyType) is { } underlying
                ? typeof(NullablePrimitiveAccessor<,>).MakeGenericType(declaringType, underlying)
                : typeof(PrimitiveAccessor<,>).MakeGenericType(declaringType, property.PropertyType);
            return (PrimitiveAccessor)Activator.CreateInstance(accessorType, getter, property.GetSetMethod(nonPublic: true))!;
        }

        public static bool IsTypedPrimitive(Type type)
        {
            return type == typeof(string) || type == typeof(bool) || type == typeof(char) ||
                   type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
                   type == typeof(sbyte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong) ||
                   type == typeof(double) || type == typeof(float) || type == typeof(decimal) ||
                   type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(Guid);
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

    /// <summary>
    ///     Writes a primitive as TOON text, exactly as <see cref="WriteTyped{T}"/> followed by the encoder would, without
    ///     creating a <see cref="ToonValue"/>.
    /// </summary>
    private static void WriteTypedText<T>(ToonWriter writer, T value)
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
            Span<char> text = stackalloc char[64];
            Unsafe.As<T, DateTime>(ref value).TryFormat(text, out var length, "O", CultureInfo.InvariantCulture);
            writer.WriteString(text[..length]);
        }
        else if (typeof(T) == typeof(DateTimeOffset))
        {
            Span<char> text = stackalloc char[64];
            Unsafe.As<T, DateTimeOffset>(ref value).TryFormat(text, out var length, "O", CultureInfo.InvariantCulture);
            writer.WriteString(text[..length]);
        }
        else if (typeof(T) == typeof(Guid))
        {
            Span<char> text = stackalloc char[36];
            Unsafe.As<T, Guid>(ref value).TryFormat(text, out var length, "D");
            writer.WriteString(text[..length]);
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
            return new ToonString(Unsafe.As<T, DateTime>(ref value).ToString("O", CultureInfo.InvariantCulture));
        }

        if (typeof(T) == typeof(DateTimeOffset))
        {
            return new ToonString(Unsafe.As<T, DateTimeOffset>(ref value).ToString("O", CultureInfo.InvariantCulture));
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
}
