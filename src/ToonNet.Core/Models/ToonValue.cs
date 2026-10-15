using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace ToonNet.Core.Models;

/// <summary>
///     Represents a value in TOON format.
/// </summary>
/// <remarks>
///     This is the base class for all TOON value types, such as null, boolean, number, string, object, and array.
///     It provides a common interface for accessing the type of the value.
/// </remarks>
public abstract partial class ToonValue
{
    /// <summary>
    ///     Gets the type of this value.
    /// </summary>
    /// <value>
    ///     A <see cref="ToonValueType"/> enumeration value indicating the specific type of this TOON value.
    /// </value>
    public abstract ToonValueType ValueType { get; }

    /// <summary>
    ///     Implicitly converts a boolean value to a ToonValue.
    /// </summary>
    /// <param name="value">The boolean value to convert.</param>
    public static implicit operator ToonValue(bool value) => new ToonBoolean(value);

    /// <summary>
    ///     Implicitly converts a double value to a ToonValue.
    /// </summary>
    /// <param name="value">The double value to convert.</param>
    public static implicit operator ToonValue(double value) => new ToonNumber(value);

    /// <summary>
    ///     Implicitly converts an integer value to a ToonValue.
    /// </summary>
    /// <param name="value">The integer value to convert.</param>
    public static implicit operator ToonValue(int value) => new ToonNumber((long)value);

    /// <summary>
    ///     Implicitly converts a long value to a ToonValue.
    /// </summary>
    /// <param name="value">The long value to convert.</param>
    public static implicit operator ToonValue(long value) => new ToonNumber(value);

    /// <summary>
    ///     Implicitly converts a float value to a ToonValue.
    /// </summary>
    /// <param name="value">The float value to convert.</param>
    public static implicit operator ToonValue(float value) => new ToonNumber(value);

    /// <summary>
    ///     Implicitly converts a decimal value to a ToonValue.
    /// </summary>
    /// <param name="value">The decimal value to convert.</param>
    public static implicit operator ToonValue(decimal value) => new ToonNumber(value);

    /// <summary>
    ///     Implicitly converts a string value to a ToonValue.
    /// </summary>
    /// <param name="value">The string value to convert. Null values are converted to ToonNull.</param>
    public static implicit operator ToonValue(string? value) => value == null ? ToonNull.Instance : new ToonString(value);
}

/// <summary>
///     Specifies the type of TOON value.
/// </summary>
/// <remarks>
///     This enumeration defines the possible types of values that can exist in the TOON format.
/// </remarks>
public enum ToonValueType
{
    /// <summary>
    ///     Represents a null value.
    /// </summary>
    Null,

    /// <summary>
    ///     Represents a boolean value (true/false).
    /// </summary>
    Boolean,

    /// <summary>
    ///     Represents a numeric value.
    /// </summary>
    Number,

    /// <summary>
    ///     Represents a string value.
    /// </summary>
    String,

    /// <summary>
    ///     Represents an object (key-value pairs).
    /// </summary>
    Object,

    /// <summary>
    ///     Represents an array of values.
    /// </summary>
    Array
}

/// <summary>
///     Represents a null value in TOON format.
/// </summary>
/// <remarks>
///     This class is a singleton, meaning only one instance of it exists.
/// </remarks>
public sealed class ToonNull : ToonValue
{
    /// <summary>
    ///     The singleton instance of ToonNull.
    /// </summary>
    public static readonly ToonNull Instance = new();

    private ToonNull() { }

    /// <summary>
    ///     Gets the type of this value.
    /// </summary>
    public override ToonValueType ValueType => ToonValueType.Null;

    /// <summary>
    ///     Returns a string representation of this null value.
    /// </summary>
    /// <returns>The string "null".</returns>
    public override string ToString()
    {
        return "null";
    }
}

/// <summary>
///     Represents a boolean value in TOON format.
/// </summary>
/// <param name="value">The boolean value to be represented.</param>
public sealed class ToonBoolean(bool value) : ToonValue
{
    /// <summary>Shared <c>true</c> value used by the parser and serializer (the type is immutable).</summary>
    internal static readonly ToonBoolean True = new(true);

    /// <summary>Shared <c>false</c> value used by the parser and serializer (the type is immutable).</summary>
    internal static readonly ToonBoolean False = new(false);

    /// <summary>
    ///     Gets the boolean value.
    /// </summary>
    public bool Value { get; } = value;

    /// <summary>
    ///     Gets the type of this value.
    /// </summary>
    public override ToonValueType ValueType => ToonValueType.Boolean;

    /// <summary>The shared instance for <paramref name="value"/>.</summary>
    internal static ToonBoolean From(bool value) => value ? True : False;

    /// <summary>
    ///     Returns a string representation of this boolean value.
    /// </summary>
    /// <returns>The string "true" or "false".</returns>
    public override string ToString()
    {
        return Value ? "true" : "false";
    }

    /// <summary>
    ///     Implicitly converts a boolean value to a ToonBoolean.
    /// </summary>
    /// <param name="value">The boolean value to convert.</param>
    public static implicit operator ToonBoolean(bool value) => new(value);

    /// <summary>
    ///     Implicitly converts a ToonBoolean to a boolean value.
    /// </summary>
    /// <param name="toonBoolean">The ToonBoolean to convert.</param>
    public static implicit operator bool(ToonBoolean toonBoolean) => toonBoolean.Value;
}

/// <summary>
///     Represents a numeric value in TOON format.
/// </summary>
/// <remarks>
///     Every number exposes a <see cref="double"/> <see cref="Value"/>. Numbers created from integral or
///     <see cref="decimal"/> values, and numbers parsed from TOON text that fit in a <see cref="decimal"/>,
///     also keep their exact value in <see cref="DecimalValue"/>, so <see cref="long"/> and <see cref="decimal"/>
///     values round-trip without the precision loss of <see cref="double"/>.
/// </remarks>
public sealed class ToonNumber : ToonValue
{
    /// <summary>
    ///     Initializes a new number from a <see cref="double"/>.
    /// </summary>
    /// <param name="value">The numeric value to be represented.</param>
    public ToonNumber(double value)
    {
        Value = value;
    }

    /// <summary>
    ///     Initializes a new number from a <see cref="float"/>, keeping its shortest decimal form (0.1f is 0.1, not 0.10000000149011612).
    /// </summary>
    /// <param name="value">The numeric value to be represented.</param>
    public ToonNumber(float value)
    {
        Value = SingleToDouble(value);
    }

    /// <summary>
    ///     The double with the shortest decimal form of <paramref name="value"/> (0.1f becomes 0.1), as stored by the
    ///     <see cref="float"/> constructor.
    /// </summary>
    internal static double SingleToDouble(float value)
    {
        if (!float.IsFinite(value))
        {
            return value;
        }

        Span<char> text = stackalloc char[32];
        value.TryFormat(text, out var length, "R", CultureInfo.InvariantCulture);
        return double.Parse(text[..length], CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Initializes a new number from a <see cref="long"/>, keeping its exact value.
    /// </summary>
    /// <param name="value">The numeric value to be represented.</param>
    public ToonNumber(long value)
    {
        Value = value;
        DecimalValue = value;
    }

    /// <summary>
    ///     Initializes a new number from a <see cref="ulong"/>, keeping its exact value.
    /// </summary>
    /// <param name="value">The numeric value to be represented.</param>
    public ToonNumber(ulong value)
    {
        Value = value;
        DecimalValue = value;
    }

    /// <summary>
    ///     Initializes a new number from a <see cref="decimal"/>, keeping its exact value.
    /// </summary>
    /// <param name="value">The numeric value to be represented.</param>
    public ToonNumber(decimal value)
    {
        Value = (double)value;
        DecimalValue = value;
    }

    /// <summary>
    ///     A number parsed from text: <paramref name="value"/> is the double closest to the text (the cast from
    ///     <see cref="decimal"/> is not correctly rounded, so it is not derived from <paramref name="exact"/>).
    /// </summary>
    internal ToonNumber(decimal exact, double value)
    {
        Value = value;
        DecimalValue = exact;
    }

    /// <summary>
    ///     Gets the numeric value as a <see cref="double"/>.
    /// </summary>
    public double Value { get; }

    /// <summary>
    ///     Gets the exact value when it is known, or <c>null</c> when the number only exists as a <see cref="double"/>.
    /// </summary>
    public decimal? DecimalValue { get; }

    /// <summary>
    ///     The number token as parsed, kept when the value does not fit in a <see cref="decimal"/>, so integers beyond
    ///     that range can still be read exactly (for example into <see cref="System.Numerics.BigInteger"/>).
    /// </summary>
    internal string? ParsedText { get; init; }

    /// <summary>
    ///     Gets the type of this value.
    /// </summary>
    public override ToonValueType ValueType => ToonValueType.Number;

    /// <summary>
    ///     Gets whether the number is finite. NaN and infinities have no TOON number form and are written as <c>null</c>.
    /// </summary>
    public bool IsFinite => DecimalValue != null || double.IsFinite(Value);

    /// <summary>
    ///     Parses a TOON number token: the JSON number grammar without leading zeros (<c>05</c> is not a number), keeping
    ///     the exact value in <see cref="DecimalValue"/> when it fits in a <see cref="decimal"/>.
    /// </summary>
    /// <param name="text">The token to parse.</param>
    /// <param name="number">The number, when <paramref name="text"/> is a finite TOON number.</param>
    /// <returns><c>true</c> when <paramref name="text"/> is a number that is finite as a <see cref="double"/>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? text, [NotNullWhen(true)] out ToonNumber? number)
    {
        number = text == null ? null : Parsing.ToonParser.TryParseNumber(text);
        return number != null;
    }

    /// <summary>
    ///     Returns the canonical TOON representation of this number (spec §2).
    /// </summary>
    /// <returns>The number in canonical form, or "null" for NaN and infinities.</returns>
    public override string ToString()
    {
        return Encoding.ToonNumberFormatter.Format(this) ?? "null";
    }

    /// <summary>
    ///     Implicitly converts a double value to a ToonNumber.
    /// </summary>
    /// <param name="value">The double value to convert.</param>
    public static implicit operator ToonNumber(double value) => new(value);

    /// <summary>
    ///     Implicitly converts an integer value to a ToonNumber.
    /// </summary>
    /// <param name="value">The integer value to convert.</param>
    public static implicit operator ToonNumber(int value) => new((long)value);

    /// <summary>
    ///     Implicitly converts a long value to a ToonNumber.
    /// </summary>
    /// <param name="value">The long value to convert.</param>
    public static implicit operator ToonNumber(long value) => new(value);

    /// <summary>
    ///     Implicitly converts a float value to a ToonNumber.
    /// </summary>
    /// <param name="value">The float value to convert.</param>
    public static implicit operator ToonNumber(float value) => new(value);

    /// <summary>
    ///     Implicitly converts a decimal value to a ToonNumber.
    /// </summary>
    /// <param name="value">The decimal value to convert.</param>
    public static implicit operator ToonNumber(decimal value) => new(value);

    /// <summary>
    ///     Implicitly converts a ToonNumber to a double value.
    /// </summary>
    /// <param name="toonNumber">The ToonNumber to convert.</param>
    public static implicit operator double(ToonNumber toonNumber) => toonNumber.Value;
}

/// <summary>
///     Represents a string value in TOON format.
/// </summary>
/// <param name="value">The string value to be represented.</param>
public sealed class ToonString(string value) : ToonValue
{
    /// <summary>
    ///     Gets the string value.
    /// </summary>
    public string Value { get; } = value;

    /// <summary>
    ///     Gets the type of this value.
    /// </summary>
    public override ToonValueType ValueType => ToonValueType.String;

    /// <summary>
    ///     Returns a string representation of this string value.
    /// </summary>
    /// <returns>The string value itself.</returns>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    ///     Implicitly converts a string value to a ToonString.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    [return: NotNullIfNotNull(nameof(value))]
    public static implicit operator ToonString?(string? value) => value == null ? null : new(value);

    /// <summary>
    ///     Implicitly converts a ToonString to a string value.
    /// </summary>
    /// <param name="toonString">The ToonString to convert.</param>
    public static implicit operator string(ToonString toonString) => toonString.Value;
}

/// <summary>
///     Represents an object (key-value pairs) in TOON format.
/// </summary>
public sealed class ToonObject : ToonValue
{
    /// <summary>
    ///     Creates a new empty ToonObject.
    /// </summary>
    public ToonObject()
    {
        Properties = new ToonPropertyCollection();
    }

    /// <summary>
    ///     Creates an empty ToonObject with room for <paramref name="capacity"/> properties, so adding up to that many
    ///     does not resize its storage.
    /// </summary>
    /// <param name="capacity">The number of properties to reserve room for; 0 or less reserves none.</param>
    public ToonObject(int capacity)
    {
        Properties = new ToonPropertyCollection(capacity);
    }

    /// <summary>
    ///     Creates a ToonObject with the given properties, in their enumeration order.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when a key occurs more than once.</exception>
    public ToonObject(IEnumerable<KeyValuePair<string, ToonValue>> properties)
    {
        Properties = new ToonPropertyCollection(properties);
    }

    /// <summary>
    ///     Gets the properties, in insertion order (key order is significant in TOON).
    /// </summary>
    public ToonPropertyCollection Properties { get; }

    /// <summary>
    ///     Gets the type of this value.
    /// </summary>
    public override ToonValueType ValueType => ToonValueType.Object;

    /// <summary>
    ///     Gets or sets a property value by key.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <returns>The property value, or null if not found.</returns>
    /// <remarks>Setting <c>null</c> stores <see cref="ToonNull.Instance"/>, a TOON <c>null</c> value.</remarks>
    public new ToonValue? this[string key]
    {
        get => Properties.GetValueOrDefault(key);
        set => Properties[key] = value ?? ToonNull.Instance;
    }
}

/// <summary>
///     Represents an array of values in TOON format.
/// </summary>
/// <param name="items">The list of items to initialize the array with.</param>
/// <param name="fieldNames">Optional field names for tabular arrays.</param>
public sealed class ToonArray(List<ToonValue> items, string[]? fieldNames = null) : ToonValue
{
    /// <summary>
    ///     Creates a new empty ToonArray.
    /// </summary>
    public ToonArray() : this([]) { }

    /// <summary>
    ///     Gets the array items. A null put into the list is a <see cref="ToonNull"/> item to every reader of the array
    ///     (the indexer, paths, the serializer, cloning and comparing).
    /// </summary>
    public List<ToonValue> Items { get; } = items;

    /// <summary>
    ///     Gets the field names of the tabular header this array was parsed from, or the names passed when creating it.
    /// </summary>
    /// <remarks>
    ///     They describe the source text only: they are not updated when <see cref="Items"/> change, and the encoder does
    ///     not use them. It writes an array in tabular form whenever its items are uniform objects with primitive values.
    /// </remarks>
    public string[]? FieldNames { get; init; } = fieldNames;

    /// <summary>
    ///     Gets a value indicating whether <see cref="FieldNames"/> is set, i.e. the array was parsed from a tabular header.
    /// </summary>
    public bool IsTabular => FieldNames is { Length: > 0 };

    /// <summary>
    ///     Gets the type of this value.
    /// </summary>
    public override ToonValueType ValueType => ToonValueType.Array;

    /// <summary>
    ///     Gets the number of items in this array.
    /// </summary>
    public int Count => Items.Count;

    /// <summary>
    ///     Gets or sets an array item by index.
    /// </summary>
    /// <param name="index">The item index.</param>
    /// <returns>The item at the specified index.</returns>
    public new ToonValue this[int index]
    {
        get => Items[index] ?? ToonNull.Instance;
        set => Items[index] = value ?? ToonNull.Instance;
    }

    /// <summary>
    ///     Adds a value to the end of the array.
    /// </summary>
    /// <param name="value">The value to add; <c>null</c> adds <see cref="ToonNull.Instance"/>.</param>
    public void Add(ToonValue? value) => Items.Add(value ?? ToonNull.Instance);
}