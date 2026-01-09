using System.Globalization;

namespace ToonNet.Core.Models;

/// <summary>
/// Represents a value in TOON format.
/// </summary>
public abstract class ToonValue
{
    public abstract ToonValueType ValueType { get; }
}

public enum ToonValueType
{
    Null,
    Boolean,
    Number,
    String,
    Object,
    Array
}

public sealed class ToonNull : ToonValue
{
    public static readonly ToonNull Instance = new();
    private ToonNull() { }
    public override ToonValueType ValueType => ToonValueType.Null;
    public override string ToString() => "null";
}

public sealed class ToonBoolean(bool value) : ToonValue
{
    public bool Value { get; } = value;
    public override ToonValueType ValueType => ToonValueType.Boolean;
    public override string ToString() => Value ? "true" : "false";
}

public sealed class ToonNumber(double value) : ToonValue
{
    public double Value { get; } = value;
    public override ToonValueType ValueType => ToonValueType.Number;
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

public sealed class ToonString(string value) : ToonValue
{
    public string Value { get; } = value;
    public override ToonValueType ValueType => ToonValueType.String;
    public override string ToString() => Value;
}

public sealed class ToonObject(Dictionary<string, ToonValue> properties) : ToonValue
{
    public Dictionary<string, ToonValue> Properties { get; } = properties;

    public ToonObject() : this(new Dictionary<string, ToonValue>()) { }

    public override ToonValueType ValueType => ToonValueType.Object;
    
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

public sealed class ToonArray(List<ToonValue> items, string[]? fieldNames = null) : ToonValue
{
    public List<ToonValue> Items { get; } = items;
    public string[]? FieldNames { get; init; } = fieldNames;
    public bool IsTabular => FieldNames is { Length: > 0 };
    
    public ToonArray() : this([]) { }

    public override ToonValueType ValueType => ToonValueType.Array;
    
    public int Count => Items.Count;
    
    public ToonValue this[int index]
    {
        get => Items[index];
        set => Items[index] = value;
    }
}
