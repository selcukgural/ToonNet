using System.Globalization;

namespace ToonNet.Core.Models;

/// <summary>
///     Represents a value in TOON format.
/// </summary>
/// <remarks>
///     This is the base class for all TOON value types, such as null, boolean, number, string, object, and array.
///     It provides a common interface for accessing the type of the value.
/// </remarks>
public abstract class ToonValue
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
    public static implicit operator ToonValue(int value) => new ToonNumber(value);

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
    public static implicit operator ToonValue(decimal value) => new ToonNumber((double)value);

    /// <summary>
    ///     Implicitly converts a string value to a ToonValue.
    /// </summary>
    /// <param name="value">The string value to convert. Null values are converted to ToonNull.</param>
    public static implicit operator ToonValue?(string? value) => value == null ? ToonNull.Instance : new ToonString(value);
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
    ///     Represents a numeric value (double).
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
    /// <summary>
    ///     Gets the boolean value.
    /// </summary>
    public bool Value { get; } = value;

    /// <summary>
    ///     Gets the type of this value.
    /// </summary>
    public override ToonValueType ValueType => ToonValueType.Boolean;

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
/// <param name="value">The numeric value to be represented.</param>
public sealed class ToonNumber(double value) : ToonValue
{
    /// <summary>
    ///     Gets the numeric value.
    /// </summary>
    public double Value { get; } = value;

    /// <summary>
    ///     Gets the type of this value.
    /// </summary>
    public override ToonValueType ValueType => ToonValueType.Number;

    /// <summary>
    ///     Returns a string representation of this numeric value.
    /// </summary>
    /// <returns>The number as a string in invariant culture format.</returns>
    public override string ToString()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
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
    public static implicit operator ToonNumber(int value) => new(value);

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
    public static implicit operator ToonNumber(decimal value) => new((double)value);

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
/// <param name="properties">The dictionary of properties to initialize the object with.</param>
public sealed class ToonObject(Dictionary<string, ToonValue> properties) : ToonValue
{
    /// <summary>
    ///     Creates a new empty ToonObject.
    /// </summary>
    public ToonObject() : this(new Dictionary<string, ToonValue>()) { }

    /// <summary>
    ///     Gets the properties dictionary.
    /// </summary>
    public Dictionary<string, ToonValue> Properties { get; } = properties;

    /// <summary>
    ///     Gets the type of this value.
    /// </summary>
    public override ToonValueType ValueType => ToonValueType.Object;

    /// <summary>
    ///     Gets or sets a property value by key.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <returns>The property value, or null if not found.</returns>
    public ToonValue? this[string key]
    {
        get => Properties.GetValueOrDefault(key);
        set
        {
            if (value != null)
            {
                Properties[key] = value;
            }
        }
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
    ///     Gets the array items.
    /// </summary>
    public List<ToonValue> Items { get; } = items;

    /// <summary>
    ///     Gets the optional field names for tabular arrays.
    /// </summary>
    public string[]? FieldNames { get; init; } = fieldNames;

    /// <summary>
    ///     Gets a value indicating whether this is a tabular array (array of objects with named fields).
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
    public ToonValue this[int index]
    {
        get => Items[index];
        set => Items[index] = value;
    }

    /// <summary>
    ///     Adds a value to the end of the array.
    /// </summary>
    /// <param name="value">The value to add.</param>
    public void Add(ToonValue value) => Items.Add(value);
}