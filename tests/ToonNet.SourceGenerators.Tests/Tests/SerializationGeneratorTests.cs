using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.SourceGenerators.Tests.Models;

namespace ToonNet.SourceGenerators.Tests.Tests;

/// <summary>
/// Tests for source-generated serialization methods.
/// </summary>
public class SerializationGeneratorTests
{
    /// <summary>
    /// Test that simple model serializes and deserializes correctly.
    /// </summary>
    [Fact]
    public void SimpleModel_RoundTrip_Succeeds()
    {
        // Arrange
        var model = new SimpleModel
        {
            Name = "John",
            Age = 30,
            Balance = 1234.56m,
            IsActive = true
        };

        // Act
        var doc = SimpleModel.Serialize(model);
        var deserialized = SimpleModel.Deserialize(doc);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(model.Name, deserialized.Name);
        Assert.Equal(model.Age, deserialized.Age);
        Assert.Equal(model.Balance, deserialized.Balance);
        Assert.Equal(model.IsActive, deserialized.IsActive);
    }

    /// <summary>
    /// Test that nullable properties are handled correctly when null.
    /// </summary>
    [Fact]
    public void NullableModel_WithNullValues_SerializesCorrectly()
    {
        // Arrange
        var model = new NullableModel
        {
            OptionalName = null,
            OptionalAge = null,
            Score = 95.5
        };

        // Act
        var doc = NullableModel.Serialize(model);
        var deserialized = NullableModel.Deserialize(doc);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Null(deserialized.OptionalName);
        Assert.Null(deserialized.OptionalAge);
        Assert.Equal(95.5, deserialized.Score);
    }

    /// <summary>
    /// Test camelCase naming policy is applied during serialization.
    /// </summary>
    [Fact]
    public void CamelCaseModel_AppliesNamingPolicy()
    {
        // Arrange
        var model = new CamelCaseModel
        {
            FirstName = "Jane",
            LastName = "Doe",
            EmailAddress = "jane@example.com"
        };

        // Act
        var doc = CamelCaseModel.Serialize(model);
        var deserialized = CamelCaseModel.Deserialize(doc);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(model.FirstName, deserialized.FirstName);
        Assert.Equal(model.LastName, deserialized.LastName);
        Assert.Equal(model.EmailAddress, deserialized.EmailAddress);

        // Verify camelCase is used in serialized form
        var obj = (global::ToonNet.Core.Models.ToonObject)doc.Root;
        var keys = obj.Properties.Keys.ToList();
        Assert.Contains("firstName", keys);
        Assert.Contains("lastName", keys);
        Assert.Contains("emailAddress", keys);
    }

    /// <summary>
    /// Test snake_case naming policy is applied during serialization.
    /// </summary>
    [Fact]
    public void SnakeCaseModel_AppliesNamingPolicy()
    {
        // Arrange
        var model = new SnakeCaseModel
        {
            FirstName = "Bob",
            PhoneNumber = "555-1234",
            IsVerified = true
        };

        // Act
        var doc = SnakeCaseModel.Serialize(model);
        var deserialized = SnakeCaseModel.Deserialize(doc);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(model.FirstName, deserialized.FirstName);
        Assert.Equal(model.PhoneNumber, deserialized.PhoneNumber);
        Assert.Equal(model.IsVerified, deserialized.IsVerified);

        // Verify snake_case is used in serialized form
        var obj = (global::ToonNet.Core.Models.ToonObject)doc.Root;
        var keys = obj.Properties.Keys.ToList();
        Assert.Contains("first_name", keys);
        Assert.Contains("phone_number", keys);
        Assert.Contains("is_verified", keys);
    }

    /// <summary>
    /// Test that numeric types preserve precision during round-trip.
    /// </summary>
    [Fact]
    public void SimpleModel_NumericPrecision_PreservedInRoundTrip()
    {
        // Arrange
        var model = new SimpleModel
        {
            Name = "Precision Test",
            Age = 42,
            Balance = 123.456789m,
            IsActive = false
        };

        // Act
        var doc = SimpleModel.Serialize(model);
        var deserialized = SimpleModel.Deserialize(doc);

        // Assert
        Assert.Equal(model.Balance, deserialized.Balance);
        Assert.Equal(model.Age, deserialized.Age);
    }
}
