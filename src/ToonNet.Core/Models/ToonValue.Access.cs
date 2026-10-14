using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using ToonNet.Core.Paths;
using ToonNet.Core.Serialization;

namespace ToonNet.Core.Models;

// Navigation, conversion, copying and comparison of values, in the manner of System.Text.Json.Nodes.JsonNode
public abstract partial class ToonValue
{
    /// <summary>
    ///     Gets or sets a property of this object by key, so lookups can be chained: <c>doc.Root["users"]![0]["name"]</c>.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <returns>The property value, or null when the object has no such property.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this value is not an object.</exception>
    /// <remarks>Setting <c>null</c> stores <see cref="ToonNull.Instance"/>, a TOON <c>null</c> value.</remarks>
    public ToonValue? this[string key]
    {
        get => AsObject()[key];
        set => AsObject()[key] = value;
    }

    /// <summary>Gets or sets an item of this array by index.</summary>
    /// <param name="index">The item index.</param>
    /// <returns>The item at the index.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this value is not an array.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is outside the array.</exception>
    /// <remarks>Setting <c>null</c> stores <see cref="ToonNull.Instance"/>, a TOON <c>null</c> value.</remarks>
    public ToonValue this[int index]
    {
        get => AsArray()[index];
        set => AsArray()[index] = value;
    }

    /// <summary>Returns this value as an object.</summary>
    /// <exception cref="InvalidOperationException">Thrown when this value is not an object.</exception>
    public ToonObject AsObject() => this as ToonObject ?? throw NotA("an object");

    /// <summary>Returns this value as an array.</summary>
    /// <exception cref="InvalidOperationException">Thrown when this value is not an array.</exception>
    public ToonArray AsArray() => this as ToonArray ?? throw NotA("an array");

    private InvalidOperationException NotA(string kind) => new($"The value is {DescribeKind(ValueType)}, not {kind}.");

    internal static string DescribeKind(ToonValueType type) => type switch
    {
        ToonValueType.Null => "null",
        ToonValueType.Boolean => "a boolean",
        ToonValueType.Number => "a number",
        ToonValueType.String => "a string",
        ToonValueType.Object => "an object",
        _ => "an array"
    };

    /// <summary>
    ///     Converts this primitive to a string, boolean, number, date/time, <see cref="Guid"/>, <see cref="Uri"/> or enum
    ///     (or its nullable form), with the parsing rules of <see cref="ToonSerializer"/>: <c>doc.Root["id"]!.GetValue&lt;int&gt;()</c>.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <returns>The converted value; <c>default</c> for <see cref="ToonNull"/> and a nullable or reference type.</returns>
    /// <exception cref="ToonSerializationException">
    ///     Thrown when the value cannot be converted (an object or array, a number out of range, text that does not parse).
    /// </exception>
    /// <remarks>Use <see cref="Deserialize{T}(ToonSerializerOptions)"/> for objects and collections.</remarks>
    public T GetValue<T>() => ToonSerializer.ReadPrimitiveForGeneratedCode<T>(this);

    /// <summary>Converts this primitive like <see cref="GetValue{T}"/>, without throwing when it cannot be converted.</summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="value">The converted value, or <c>default</c>.</param>
    /// <returns>Whether the value was converted.</returns>
    public bool TryGetValue<T>([MaybeNullWhen(false)] out T value)
    {
        if (this is ToonObject or ToonArray)
        {
            value = default;
            return false;
        }

        try
        {
            value = GetValue<T>();
            return true;
        }
        catch (ToonSerializationException)
        {
            value = default;
            return false;
        }
    }

    /// <summary>Converts this value to <typeparamref name="T"/> with the serializer, like <see cref="ToonSerializer.DeserializeFromValue{T}(ToonValue, ToonSerializerOptions)"/>.</summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The converted object, or null for <see cref="ToonNull"/> and nullable targets.</returns>
    /// <exception cref="ToonSerializationException">Thrown when the value cannot be converted to the target type.</exception>
    [RequiresUnreferencedCode(ToonSerializer.ReflectionMessage)]
    [RequiresDynamicCode(ToonSerializer.ReflectionMessage)]
    public T? Deserialize<T>(ToonSerializerOptions? options = null) => ToonSerializer.DeserializeFromValue<T>(this, options);

    /// <summary>
    ///     Converts this value to <typeparamref name="T"/> with its type info, without reflection, like
    ///     <see cref="ToonSerializer.DeserializeFromValue{T}(ToonValue, ToonTypeInfo{T}, ToonSerializerOptions)"/>.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The converted object, or null for <see cref="ToonNull"/> and nullable targets.</returns>
    /// <exception cref="ToonSerializationException">Thrown when the value cannot be converted to the target type.</exception>
    public T? Deserialize<T>(ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null) =>
        ToonSerializer.DeserializeFromValue(this, typeInfo, options);

    /// <summary>
    ///     Selects the values a path leads to, with this value as the root <c>$</c>: <c>root.Select("$.orders[?@.total &gt; 100]")</c>.
    /// </summary>
    /// <param name="path">A JSONPath (RFC 9535); see <see cref="ToonPath"/>.</param>
    /// <returns>The selected values, in document order; empty when there are none.</returns>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    public IReadOnlyList<ToonValue> Select(string path) => ToonPath.Parse(path).Select(this);

    /// <summary>Selects the values a path leads to with their normalized paths (<c>$['orders'][2]</c>), with this value as the root <c>$</c>.</summary>
    /// <param name="path">A JSONPath (RFC 9535); see <see cref="ToonPath"/>.</param>
    /// <returns>The selected values and where they are, in document order.</returns>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    public IReadOnlyList<ToonPathMatch> SelectMatches(string path) => ToonPath.Parse(path).SelectMatches(this);

    /// <summary>Selects the one value a path leads to, with this value as the root <c>$</c>.</summary>
    /// <param name="path">A JSONPath (RFC 9535); see <see cref="ToonPath"/>.</param>
    /// <returns>The value, or null when the path selects none.</returns>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the path selects more than one value.</exception>
    public ToonValue? SelectSingle(string path) => ToonPath.Parse(path).SelectSingle(this);

    /// <summary>
    ///     Sets the value at a path of member names and indexes (<c>$.meta.tags[0]</c>), with this value as the root
    ///     <c>$</c>, creating missing objects and arrays on the way; see <see cref="ToonPath.SetValue"/>.
    /// </summary>
    /// <param name="path">A singular JSONPath.</param>
    /// <param name="value">The value to set; null sets <see cref="ToonNull.Instance"/>.</param>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the path is not singular or does not fit the values on the way.</exception>
    public void SetValue(string path, ToonValue? value) => ToonPath.Parse(path).SetValue(this, value);

    /// <summary>Removes every value a path selects (<c>$.users[?@.active == false]</c>), with this value as the root <c>$</c>.</summary>
    /// <param name="path">A JSONPath (RFC 9535); see <see cref="ToonPath"/>.</param>
    /// <returns>The number of values removed.</returns>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the path selects the root itself.</exception>
    public int RemoveAll(string path) => ToonPath.Parse(path).RemoveAll(this);

    /// <summary>
    ///     Creates a deep copy: objects and arrays are copied with all their contents, so changing the copy does not change
    ///     this value. Primitives cannot change and are shared.
    /// </summary>
    /// <returns>The copy.</returns>
    /// <exception cref="InsufficientExecutionStackException">Thrown when the value is nested too deeply to copy.</exception>
    public ToonValue DeepClone()
    {
        switch (this)
        {
            case ToonObject obj:
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
                var copy = new ToonObject(obj.Properties.Count);

                foreach (var (key, value) in obj.Properties)
                {
                    copy.Properties.Add(key, value.DeepClone());
                }

                return copy;
            }
            case ToonArray array:
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
                var items = new List<ToonValue>(array.Items.Count);

                foreach (var item in array.Items)
                {
                    items.Add(item.DeepClone());
                }

                return new ToonArray(items, array.FieldNames is { } names ? (string[])names.Clone() : null);
            }
            default:
                return this;
        }
    }

    /// <summary>
    ///     Compares two values by their data: objects with the same keys and equal values (in any order), arrays with
    ///     equal items in the same order, and equal primitives (numbers by value, so <c>1</c> equals <c>1.0</c>).
    /// </summary>
    /// <param name="left">The first value, or null.</param>
    /// <param name="right">The second value, or null.</param>
    /// <returns>Whether the values are equal; two nulls are equal, and null does not equal <see cref="ToonNull"/>.</returns>
    /// <exception cref="InsufficientExecutionStackException">Thrown when the values are nested too deeply to compare.</exception>
    /// <remarks><see cref="ToonArray.FieldNames"/> describe the source text and are not compared.</remarks>
    public static bool DeepEquals(ToonValue? left, ToonValue? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.ValueType != right.ValueType)
        {
            return false;
        }

        switch (left)
        {
            case ToonBoolean boolean:
                return boolean.Value == ((ToonBoolean)right).Value;
            case ToonString text:
                return text.Value == ((ToonString)right).Value;
            case ToonNumber number:
                return NumbersEqual(number, (ToonNumber)right);
            case ToonObject obj:
            {
                var other = (ToonObject)right;

                if (obj.Properties.Count != other.Properties.Count)
                {
                    return false;
                }

                RuntimeHelpers.EnsureSufficientExecutionStack();

                foreach (var (key, value) in obj.Properties)
                {
                    if (!other.Properties.TryGetValue(key, out var otherValue) || !DeepEquals(value, otherValue))
                    {
                        return false;
                    }
                }

                return true;
            }
            case ToonArray array:
            {
                var items = array.Items;
                var otherItems = ((ToonArray)right).Items;

                if (items.Count != otherItems.Count)
                {
                    return false;
                }

                RuntimeHelpers.EnsureSufficientExecutionStack();

                for (var i = 0; i < items.Count; i++)
                {
                    if (!DeepEquals(items[i], otherItems[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
            default:
                return true; // both null
        }
    }

    /// <summary>
    ///     Compares numbers exactly when both are known exactly (as decimals, or as integers parsed beyond the range of
    ///     <see cref="decimal"/>), otherwise by their canonical text.
    /// </summary>
    internal static bool NumbersEqual(ToonNumber left, ToonNumber right)
    {
        if (left.DecimalValue is { } a && right.DecimalValue is { } b)
        {
            return a == b;
        }

        if (left.ParsedText is { } leftText && right.ParsedText is { } rightText &&
            BigInteger.TryParse(leftText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var leftInteger) &&
            BigInteger.TryParse(rightText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var rightInteger))
        {
            return leftInteger == rightInteger;
        }

        return left.ToString() == right.ToString();
    }
}
