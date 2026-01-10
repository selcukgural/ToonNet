using System.Globalization;

namespace ToonNet.Core.Models;

/// <summary>
/// Represents a value in TOON format.
/// </summary>
public abstract class ToonValue
{
    /// <summary>
    /// Gets the type of this value.
    /// </summary>
    public abstract ToonValueType ValueType { get; }
}

/// <summary>
/// Specifies the type of a TOON value.
/// </summary>
public enum ToonValueType
{
    /// <summary>
    /// Represents a null value.
    /// </summary>
    Null,
    
    /// <summary>
    /// Represents a boolean value (true/false).
    /// </summary>
    Boolean,
    
    /// <summary>
    /// Represents a numeric value (double).
    /// </summary>
    Number,
    
    /// <summary>
    /// Represents a string value.
    /// </summary>
    String,
    
    /// <summary>
    /// Represents an object (key-value pairs).
    /// </summary>
    Object,
    
    /// <summary>
    /// Represents an array of values.
    /// </summary>
    Array
}

/// <summary>
/// Represents a null value in TOON format.
/// </summary>
public sealed class ToonNull : ToonValue
{
    /// <summary>
    /// The singleton instance of ToonNull.
    /// </summary>
    public static readonly ToonNull Instance = new();
    
    private ToonNull() { }
    
    public override ToonValueType ValueType => ToonValueType.Null;
    
    public override string ToString() => "null";
}

/// <summary>
/// Represents a boolean value in TOON format.
/// </summary>
public sealed class ToonBoolean(bool value) : ToonValue
{
    /// <summary>
    /// Gets the boolean value.
    /// </summary>
    public bool Value { get; } = value;
    
    public override ToonValueType ValueType => ToonValueType.Boolean;
    
    public override string ToString() => Value ? "true" : "false";
}

/// <summary>
/// Represents a numeric value in TOON format.
/// </summary>
public sealed class ToonNumber(double value) : ToonValue
{
    /// <summary>
    /// Gets the numeric value.
    /// </summary>
    public double Value { get; } = value;
    
    public override ToonValueType ValueType => ToonValueType.Number;
    
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>
/// Represents a string value in TOON format.
/// </summary>
public sealed class ToonString(string value) : ToonValue
{
    /// <summary>
    /// Gets the string value.
    /// </summary>
    public string Value { get; } = value;
    
    public override ToonValueType ValueType => ToonValueType.String;
    
    public override string ToString() => Value;
}

/// <summary>
/// Represents an object (key-value pairs) in TOON format.
/// </summary>
public sealed class ToonObject(Dictionary<string, ToonValue> properties) : ToonValue
{
    /// <summary>
    /// Gets the properties dictionary.
    /// </summary>
    public Dictionary<string, ToonValue> Properties { get; } = properties;

    /// <summary>
    /// Creates a new empty ToonObject.
    /// </summary>
    public ToonObject() : this(new Dictionary<string, ToonValue>()) { }

    public override ToonValueType ValueType => ToonValueType.Object;
    
    /// <summary>
    /// Gets or sets a property value by key.
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
/// Represents an array of values in TOON format.
/// </summary>
public sealed class ToonArray(List<ToonValue> items, string[]? fieldNames = null) : ToonValue
{
    /// <summary>
    /// Gets the array items.
    /// </summary>
    public List<ToonValue> Items { get; } = items;
    
    /// <summary>
    /// Gets the optional field names for tabular arrays.
    /// </summary>
    public string[]? FieldNames { get; init; } = fieldNames;
    
    /// <summary>
    /// Gets a value indicating whether this is a tabular array (array of objects with named fields).
    /// </summary>
    public bool IsTabular => FieldNames is { Length: > 0 };
    
    /// <summary>
    /// Creates a new empty ToonArray.
    /// </summary>
    public ToonArray() : this([]) { }

    public override ToonValueType ValueType => ToonValueType.Array;
    
    /// <summary>
    /// Gets the number of items in this array.
    /// </summary>
    public int Count => Items.Count;
    
    /// <summary>
    /// Gets or sets an array item by index.
    /// </summary>
    /// <param name="index">The item index.</param>
    /// <returns>The item at the specified index.</returns>
    public ToonValue this[int index]
    {
        get => Items[index];
        set => Items[index] = value;
    }
}
