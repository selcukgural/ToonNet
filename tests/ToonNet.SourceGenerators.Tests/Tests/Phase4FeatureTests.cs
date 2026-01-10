using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.SourceGenerators.Tests.Models;

namespace ToonNet.SourceGenerators.Tests.Tests;

/// <summary>
/// Tests for Phase 4 features: custom converters, custom constructors, and nested [ToonSerializable] classes.
/// </summary>
public class Phase4FeatureTests
{
    /// <summary>
    /// Test that nested [ToonSerializable] classes serialize and deserialize correctly.
    /// </summary>
    [Fact]
    public void NestedSerializable_RoundTrip_Succeeds()
    {
        // Arrange
        var address = new Address
        {
            Street = "123 Main St",
            City = "Springfield",
            ZipCode = "12345"
        };
        var person = new Person
        {
            Name = "John Doe",
            Age = 35,
            Address = address
        };

        // Act
        var doc = Person.Serialize(person);
        var deserialized = Person.Deserialize(doc);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(person.Name, deserialized.Name);
        Assert.Equal(person.Age, deserialized.Age);
        Assert.NotNull(deserialized.Address);
        Assert.Equal(address.Street, deserialized.Address.Street);
        Assert.Equal(address.City, deserialized.Address.City);
        Assert.Equal(address.ZipCode, deserialized.Address.ZipCode);
    }

    /// <summary>
    /// Test that nested [ToonSerializable] with null values are handled correctly.
    /// </summary>
    [Fact]
    public void NestedSerializable_WithNullValue_Succeeds()
    {
        // Arrange
        var person = new Person
        {
            Name = "Jane Doe",
            Age = 28,
            Address = null
        };

        // Act
        var doc = Person.Serialize(person);
        var deserialized = Person.Deserialize(doc);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(person.Name, deserialized.Name);
        Assert.Equal(person.Age, deserialized.Age);
        Assert.Null(deserialized.Address);
    }

    /// <summary>
    /// Test that custom converters work for properties.
    /// </summary>
    [Fact]
    public void CustomConverter_OnProperty_Works()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var evt = new Event
        {
            Name = "Conference",
            Timestamp = timestamp
        };

        // Act
        var doc = Event.Serialize(evt);
        var deserialized = Event.Deserialize(doc);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(evt.Name, deserialized.Name);
        Assert.Equal(timestamp.ToString("O"), deserialized.Timestamp.ToString("O"));
    }

    /// <summary>
    /// Test that custom converters preserve value through round-trip.
    /// </summary>
    [Fact]
    public void CustomConverter_PreservesValue_InRoundTrip()
    {
        // Arrange
        var originalTimestamp = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(-5));
        var evt = new Event
        {
            Name = "Meeting",
            Timestamp = originalTimestamp
        };

        // Act
        var doc = Event.Serialize(evt);
        var deserialized = Event.Deserialize(doc);

        // Assert
        Assert.Equal(originalTimestamp, deserialized.Timestamp);
    }

    /// <summary>
    /// Test that custom constructors are used during deserialization.
    /// </summary>
    [Fact]
    public void CustomConstructor_IsUsed_WhenDeserializing()
    {
        // Arrange
        var point = new Point(10, 20);

        // Act
        var doc = Point.Serialize(point);
        var deserialized = Point.Deserialize(doc);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(point.X, deserialized.X);
        Assert.Equal(point.Y, deserialized.Y);
    }

    /// <summary>
    /// Test that custom constructor preserves constructor logic.
    /// </summary>
    [Fact]
    public void CustomConstructor_PreservesConstructorLogic()
    {
        // Arrange - Create point with parameterized constructor
        var originalPoint = new Point(15, 25);
        Assert.Equal(15, originalPoint.X);
        Assert.Equal(25, originalPoint.Y);

        // Act - Serialize and deserialize
        var doc = Point.Serialize(originalPoint);
        var deserialized = Point.Deserialize(doc);

        // Assert - Values preserved
        Assert.Equal(15, deserialized.X);
        Assert.Equal(25, deserialized.Y);
    }

    /// <summary>
    /// Test multiple levels of nesting with [ToonSerializable] classes.
    /// </summary>
    [Fact]
    public void MultipleNestedSerializable_RoundTrip_Succeeds()
    {
        // Arrange
        var ceoAddress = new Address
        {
            Street = "456 Executive Blvd",
            City = "Metropolis",
            ZipCode = "54321"
        };
        var ceo = new Person
        {
            Name = "Alice Smith",
            Age = 50,
            Address = ceoAddress
        };
        var company = new Company
        {
            Name = "TechCorp",
            Headquarters = new Address
            {
                Street = "789 Innovation Way",
                City = "Silicon Valley",
                ZipCode = "94025"
            },
            Ceo = ceo
        };

        // Act
        var doc = Company.Serialize(company);
        var deserialized = Company.Deserialize(doc);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(company.Name, deserialized.Name);
        Assert.NotNull(deserialized.Headquarters);
        Assert.Equal(company.Headquarters.Street, deserialized.Headquarters.Street);
        Assert.NotNull(deserialized.Ceo);
        Assert.Equal(company.Ceo.Name, deserialized.Ceo.Name);
        Assert.NotNull(deserialized.Ceo.Address);
        Assert.Equal(company.Ceo.Address.Street, deserialized.Ceo.Address.Street);
    }

    /// <summary>
    /// Test combination of nested [ToonSerializable] and custom converter.
    /// </summary>
    [Fact]
    public void NestedSerializableWithCustomConverter_RoundTrip_Succeeds()
    {
        // Arrange
        var location = new Address
        {
            Street = "100 Conference Center",
            City = "Boston",
            ZipCode = "02101"
        };
        var scheduledTime = new DateTimeOffset(2024, 3, 15, 10, 0, 0, TimeSpan.Zero);
        var meeting = new Meeting
        {
            Title = "Annual Strategy",
            Location = location,
            ScheduledTime = scheduledTime
        };

        // Act
        var doc = Meeting.Serialize(meeting);
        var deserialized = Meeting.Deserialize(doc);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(meeting.Title, deserialized.Title);
        Assert.NotNull(deserialized.Location);
        Assert.Equal(location.Street, deserialized.Location.Street);
        Assert.Equal(scheduledTime, deserialized.ScheduledTime);
    }

    /// <summary>
    /// Test that nested [ToonSerializable] with null still generates correct TOON.
    /// </summary>
    [Fact]
    public void NestedSerializable_SerializedForm_IsCorrect()
    {
        // Arrange
        var person = new Person
        {
            Name = "Bob",
            Age = 40,
            Address = new Address
            {
                Street = "Somewhere",
                City = "Nowhere",
                ZipCode = "00000"
            }
        };

        // Act
        var doc = Person.Serialize(person);

        // Assert
        Assert.NotNull(doc);
        var obj = (ToonObject)doc.Root;
        Assert.Contains("Name", obj.Properties.Keys);
        Assert.Contains("Age", obj.Properties.Keys);
        Assert.Contains("Address", obj.Properties.Keys);

        // Address should be an object
        var addressValue = obj.Properties["Address"];
        Assert.IsType<ToonObject>(addressValue);
        var addressObj = (ToonObject)addressValue;
        Assert.Contains("Street", addressObj.Properties.Keys);
        Assert.Contains("City", addressObj.Properties.Keys);
        Assert.Contains("ZipCode", addressObj.Properties.Keys);
    }

    /// <summary>
    /// Test custom converter produces correct serialized form.
    /// </summary>
    [Fact]
    public void CustomConverter_SerializedForm_IsCorrect()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var evt = new Event
        {
            Name = "Event",
            Timestamp = timestamp
        };

        // Act
        var doc = Event.Serialize(evt);

        // Assert
        Assert.NotNull(doc);
        var obj = (ToonObject)doc.Root;
        var timestampValue = obj.Properties["Timestamp"];
        
        // Should be serialized as string via custom converter
        Assert.IsType<ToonString>(timestampValue);
        var timestampString = (ToonString)timestampValue;
        Assert.Contains("2024-01-01", timestampString.Value);
    }

    /// <summary>
    /// Test Point model with custom constructor and zero values.
    /// </summary>
    [Fact]
    public void CustomConstructor_WithZeroValues_WorksCorrectly()
    {
        // Arrange
        var point = new Point(0, 0);

        // Act
        var doc = Point.Serialize(point);
        var deserialized = Point.Deserialize(doc);

        // Assert
        Assert.Equal(0, deserialized.X);
        Assert.Equal(0, deserialized.Y);
    }

    /// <summary>
    /// Test negative values with custom constructor.
    /// </summary>
    [Fact]
    public void CustomConstructor_WithNegativeValues_WorksCorrectly()
    {
        // Arrange
        var point = new Point(-10, -20);

        // Act
        var doc = Point.Serialize(point);
        var deserialized = Point.Deserialize(doc);

        // Assert
        Assert.Equal(-10, deserialized.X);
        Assert.Equal(-20, deserialized.Y);
    }
}
