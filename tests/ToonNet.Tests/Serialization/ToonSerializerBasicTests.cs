using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.Tests.Serialization;

public class ToonSerializerBasicTests
{
    [Fact]
    public void Serialize_SimpleClass_ProducesCorrectToon()
    {
        // Arrange
        var person = new SimplePerson
        {
            Name = "Alice",
            Age = 30,
            IsActive = true
        };

        // Act
        var toon = ToonSerializer.Serialize(person);

        // Assert
        Assert.Contains("Name: Alice", toon);
        Assert.Contains("Age: 30", toon);
        Assert.Contains("IsActive: true", toon);
    }

    [Fact]
    public void Deserialize_SimpleClass_ReconstructsObject()
    {
        // Arrange
        var toon = @"Name: Alice
Age: 30
IsActive: true";

        // Act
        var person = ToonSerializer.Deserialize<SimplePerson>(toon);

        // Assert
        Assert.NotNull(person);
        Assert.Equal("Alice", person.Name);
        Assert.Equal(30, person.Age);
        Assert.True(person.IsActive);
    }

    [Fact]
    public void RoundTrip_SimpleClass_PreservesData()
    {
        // Arrange
        var original = new SimplePerson
        {
            Name = "Bob",
            Age = 25,
            IsActive = false
        };

        // Act
        var toon = ToonSerializer.Serialize(original);
        var deserialized = ToonSerializer.Deserialize<SimplePerson>(toon);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.Age, deserialized.Age);
        Assert.Equal(original.IsActive, deserialized.IsActive);
    }

    [Fact]
    public void Serialize_WithNullValues_HandlesNullCorrectly()
    {
        // Arrange
        var person = new PersonWithNullable
        {
            Name = "Charlie",
            Age = null,
            Email = null
        };

        // Act
        var toon = ToonSerializer.Serialize(person);

        // Assert
        Assert.Contains("Name: Charlie", toon);
        Assert.Contains("Age: null", toon);
        Assert.Contains("Email: null", toon);
    }

    [Fact]
    public void Serialize_WithIgnoreNullValues_OmitsNulls()
    {
        // Arrange
        var person = new PersonWithNullable
        {
            Name = "Charlie",
            Age = null,
            Email = null
        };

        var options = new ToonSerializerOptions
        {
            IgnoreNullValues = true
        };

        // Act
        var toon = ToonSerializer.Serialize(person, options);

        // Assert
        Assert.Contains("Name: Charlie", toon);
        Assert.DoesNotContain("Age:", toon);
        Assert.DoesNotContain("Email:", toon);
    }

    [Fact]
    public void Serialize_NestedObject_ProducesNestedStructure()
    {
        // Arrange
        var person = new PersonWithAddress
        {
            Name = "Diana",
            Age = 28,
            Address = new SimpleAddress
            {
                Street = "123 Main St",
                City = "Boston",
                ZipCode = "02101"
            }
        };

        // Act
        var toon = ToonSerializer.Serialize(person);

        // Assert
        Assert.Contains("Name: Diana", toon);
        Assert.Contains("Address:", toon);
        Assert.Contains("  Street: \"123 Main St\"", toon);
        Assert.Contains("  City: Boston", toon);
    }

    [Fact]
    public void RoundTrip_NestedObject_PreservesStructure()
    {
        // Arrange
        var original = new PersonWithAddress
        {
            Name = "Eve",
            Age = 35,
            Address = new SimpleAddress
            {
                Street = "456 Oak Ave",
                City = "Seattle",
                ZipCode = "98101"
            }
        };

        // Act
        var toon = ToonSerializer.Serialize(original);
        var deserialized = ToonSerializer.Deserialize<PersonWithAddress>(toon);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.NotNull(deserialized.Address);
        Assert.Equal(original.Address.Street, deserialized.Address.Street);
        Assert.Equal(original.Address.City, deserialized.Address.City);
    }

    [Fact]
    public void Serialize_WithToonPropertyAttribute_UsesCustomName()
    {
        // Arrange
        var person = new PersonWithCustomNames
        {
            FirstName = "Frank",
            LastName = "Smith",
            YearsOld = 40
        };

        // Act
        var toon = ToonSerializer.Serialize(person);

        // Assert
        Assert.Contains("first_name: Frank", toon);
        Assert.Contains("last_name: Smith", toon);
        Assert.Contains("age: 40", toon);
    }

    [Fact]
    public void Serialize_WithToonIgnoreAttribute_OmitsProperty()
    {
        // Arrange
        var person = new PersonWithIgnored
        {
            Name = "Grace",
            Age = 30,
            Password = "secret123"
        };

        // Act
        var toon = ToonSerializer.Serialize(person);

        // Assert
        Assert.Contains("Name: Grace", toon);
        Assert.Contains("Age: 30", toon);
        Assert.DoesNotContain("Password", toon);
        Assert.DoesNotContain("secret123", toon);
    }

    [Fact]
    public void Serialize_CamelCaseNaming_ConvertsPascalToCamel()
    {
        // Arrange
        var person = new SimplePerson
        {
            Name = "Henry",
            Age = 45,
            IsActive = true
        };

        var options = new ToonSerializerOptions
        {
            PropertyNamingPolicy = PropertyNamingPolicy.CamelCase
        };

        // Act
        var toon = ToonSerializer.Serialize(person, options);

        // Assert
        Assert.Contains("name: Henry", toon);
        Assert.Contains("age: 45", toon);
        Assert.Contains("isActive: true", toon);
    }

    [Fact]
    public void Serialize_SnakeCaseNaming_ConvertsPascalToSnake()
    {
        // Arrange
        var person = new SimplePerson
        {
            Name = "Iris",
            Age = 22,
            IsActive = false
        };

        var options = new ToonSerializerOptions
        {
            PropertyNamingPolicy = PropertyNamingPolicy.SnakeCase
        };

        // Act
        var toon = ToonSerializer.Serialize(person, options);

        // Assert
        Assert.Contains("name: Iris", toon);
        Assert.Contains("age: 22", toon);
        Assert.Contains("is_active: false", toon);
    }
}

#region Test Models

public class SimplePerson
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
}

public class PersonWithNullable
{
    public string Name { get; set; } = "";
    public int? Age { get; set; }
    public string? Email { get; set; }
}

public class SimpleAddress
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string ZipCode { get; set; } = "";
}

public class PersonWithAddress
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public SimpleAddress? Address { get; set; }
}

public class PersonWithCustomNames
{
    [ToonProperty("first_name")]
    public string FirstName { get; set; } = "";

    [ToonProperty("last_name")]
    public string LastName { get; set; } = "";

    [ToonProperty("age")]
    public int YearsOld { get; set; }
}

public class PersonWithIgnored
{
    public string Name { get; set; } = "";
    public int Age { get; set; }

    [ToonIgnore]
    public string Password { get; set; } = "";
}

#endregion
