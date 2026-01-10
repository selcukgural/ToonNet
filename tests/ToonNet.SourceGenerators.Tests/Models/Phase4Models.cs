using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.SourceGenerators.Tests.Models;

/// <summary>
/// Nested model for testing nested [ToonSerializable] support.
/// </summary>
[ToonSerializable]
public partial class Address
{
    /// <summary>Gets or sets the street address.</summary>
    public string Street { get; set; } = string.Empty;

    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the zip code.</summary>
    public string ZipCode { get; set; } = string.Empty;
}

/// <summary>
/// Model with nested [ToonSerializable] property.
/// </summary>
[ToonSerializable]
public partial class Person
{
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the age.</summary>
    public int Age { get; set; }

    /// <summary>Gets or sets the nested address (nested [ToonSerializable] class).</summary>
    public Address? Address { get; set; }
}

/// <summary>
/// Custom converter for special types.
/// </summary>
public sealed class DateTimeOffsetConverter : ToonConverter<DateTimeOffset>
{
    public override ToonValue? Write(DateTimeOffset value, ToonSerializerOptions options)
    {
        // Format as ISO 8601 string
        return new ToonString(value.ToString("O"));
    }

    public override DateTimeOffset Read(ToonValue value, ToonSerializerOptions options)
    {
        if (value is ToonNull)
            return default;

        if (value is not ToonString str)
            throw new InvalidOperationException("Expected string value for DateTimeOffset");

        if (DateTimeOffset.TryParse(str.Value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var result))
            return result;

        throw new InvalidOperationException($"Could not parse '{str.Value}' as DateTimeOffset");
    }
}

/// <summary>
/// Model with a custom converter on a property.
/// </summary>
[ToonSerializable]
public partial class Event
{
    /// <summary>Gets or sets the event name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp with custom converter.</summary>
    [ToonConverter(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Model with a custom constructor.
/// </summary>
[ToonSerializable]
public partial class Point
{
    /// <summary>Gets or sets the X coordinate.</summary>
    public int X { get; set; }

    /// <summary>Gets or sets the Y coordinate.</summary>
    public int Y { get; set; }

    /// <summary>
    /// Default parameterless constructor.
    /// </summary>
    public Point()
    {
        X = 0;
        Y = 0;
    }

    /// <summary>
    /// Constructor that initializes with X and Y values.
    /// </summary>
    [ToonConstructor]
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
}

/// <summary>
/// Model with multiple nested [ToonSerializable] classes.
/// </summary>
[ToonSerializable]
public partial class Company
{
    /// <summary>Gets or sets the company name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the company headquarters address.</summary>
    public Address? Headquarters { get; set; }

    /// <summary>Gets or sets the CEO information.</summary>
    public Person? Ceo { get; set; }
}

/// <summary>
/// Model combining nested serializable and custom converter.
/// </summary>
[ToonSerializable]
public partial class Meeting
{
    /// <summary>Gets or sets the meeting title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the meeting location (nested [ToonSerializable]).</summary>
    public Address? Location { get; set; }

    /// <summary>Gets or sets the scheduled time with custom converter.</summary>
    [ToonConverter(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset ScheduledTime { get; set; }
}
