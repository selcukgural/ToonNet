# Phase 4: Advanced Features Guide

**Date:** January 10, 2026  
**Status:** Production Ready ✅  
**Tests:** 12/12 passing

## Overview

Phase 4 adds three powerful advanced features to ToonNet's source generator:

1. **Custom Converters** - Type-specific serialization logic
2. **Custom Constructors** - Custom deserialization instantiation
3. **Nested [ToonSerializable] Classes** - Recursive object serialization

All three features are **zero-reflection**, **compile-time**, and **production-ready**.

---

## Feature 1: Custom Converters

### Purpose

Custom converters let you handle special types with custom serialization logic without modifying the class itself.

### Common Use Cases

- **Special Date/Time Formats** (ISO 8601, custom formats)
- **Enum Mapping** (custom string representations)
- **Complex Type Serialization** (transform before/after serialization)
- **Protocol Compatibility** (encode/decode for specific formats)
- **Legacy System Integration** (adapt types for compatibility)

### Implementation

#### Step 1: Create Converter Class

```csharp
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

public sealed class DateTimeOffsetConverter : ToonConverter<DateTimeOffset>
{
    public override ToonValue? Write(DateTimeOffset value, ToonSerializerOptions options)
    {
        // Serialize to ISO 8601 format
        return new ToonString(value.ToString("O"));
    }

    public override DateTimeOffset Read(ToonValue value, ToonSerializerOptions options)
    {
        // Deserialize from ISO 8601 format
        if (value is ToonNull)
            return default;

        if (value is not ToonString str)
            throw new InvalidOperationException("Expected string for DateTimeOffset");

        if (DateTimeOffset.TryParse(str.Value, null, 
            System.Globalization.DateTimeStyles.RoundtripKind, out var result))
            return result;

        throw new InvalidOperationException($"Could not parse '{str.Value}' as DateTimeOffset");
    }
}
```

#### Step 2: Mark Property with Attribute

```csharp
[ToonSerializable]
public partial class Event
{
    public string Title { get; set; } = string.Empty;
    
    [ToonConverter(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset ScheduledTime { get; set; }
    
    public string? Location { get; set; }
}
```

#### Step 3: Use Normally

```csharp
// Create instance
var evt = new Event
{
    Title = "Team Standup",
    ScheduledTime = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero),
    Location = "Conference Room A"
};

// Serialize (uses custom converter for Timestamp)
var doc = Event.Serialize(evt);

// Deserialize (uses custom converter for Timestamp)
var restored = Event.Deserialize(doc);

// Custom converter automatically handled!
Assert.Equal(evt.ScheduledTime, restored.ScheduledTime);
```

### Built-in Converter Pattern

```csharp
// Base class provides default implementations
public abstract class ToonConverter<T> : IToonConverter<T>
{
    public virtual bool CanConvert(Type type) => typeof(T).IsAssignableFrom(type);
    public abstract ToonValue? Write(T? value, ToonSerializerOptions options);
    public abstract T? Read(ToonValue value, ToonSerializerOptions options);
}
```

### Real-World Examples

#### Enum Converter

```csharp
public sealed class EnumConverter<T> : ToonConverter<T> where T : struct, Enum
{
    public override ToonValue? Write(T? value, ToonSerializerOptions options)
    {
        return value == null ? null : new ToonString(value.Value.ToString());
    }

    public override T? Read(ToonValue value, ToonSerializerOptions options)
    {
        if (value is ToonNull)
            return null;
        
        var str = ((ToonString)value).Value;
        return Enum.TryParse<T>(str, out var result) ? result : null;
    }
}
```

#### Guid Converter

```csharp
public sealed class GuidConverter : ToonConverter<Guid>
{
    public override ToonValue? Write(Guid value, ToonSerializerOptions options)
    {
        return new ToonString(value.ToString("D"));
    }

    public override Guid Read(ToonValue value, ToonSerializerOptions options)
    {
        if (Guid.TryParse(((ToonString)value).Value, out var result))
            return result;
        return Guid.Empty;
    }
}
```

---

## Feature 2: Custom Constructors

### Purpose

Specify which constructor to use during deserialization. Useful for:
- Immutable objects with constructor-based initialization
- Objects with validation logic in constructors
- Objects requiring specific initialization order

### Implementation

#### Step 1: Define Constructor

```csharp
[ToonSerializable]
public partial class Point
{
    public int X { get; set; }
    public int Y { get; set; }

    // Default parameterless constructor
    public Point()
    {
        X = 0;
        Y = 0;
    }

    // Mark this constructor for deserialization
    [ToonConstructor]
    public Point(int x, int y)
    {
        if (x < 0 || y < 0)
            throw new ArgumentException("Coordinates must be non-negative");
        
        X = x;
        Y = y;
    }
}
```

#### Step 2: Use Normally

```csharp
// Create instance (uses custom constructor)
var point = new Point(10, 20);

// Serialize
var doc = Point.Serialize(point);

// Deserialize (uses [ToonConstructor] for instantiation)
var restored = Point.Deserialize(doc);

// Properties preserved and constructor logic applied
Assert.Equal(10, restored.X);
Assert.Equal(20, restored.Y);
```

### Parameter Mapping

Constructor parameters are matched to properties by **name (case-insensitive)**:

```csharp
[ToonSerializable]
public partial class Rectangle
{
    public int Width { get; set; }
    public int Height { get; set; }

    [ToonConstructor]
    public Rectangle(int width, int height)
    {
        Width = width;
        Height = height;
    }
}

// Parameter names (width, height) matched to property names (Width, Height)
```

### Real-World Example: Immutable Record

```csharp
[ToonSerializable]
public partial class ImmutableUser
{
    public string Name { get; }
    public int Age { get; }
    public string Email { get; }

    [ToonConstructor]
    public ImmutableUser(string name, int age, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name required", nameof(name));
        
        Name = name;
        Age = age;
        Email = email;
    }
}

// Usage
var user = new ImmutableUser("Alice", 30, "alice@example.com");
var doc = ImmutableUser.Serialize(user);
var restored = ImmutableUser.Deserialize(doc);
```

---

## Feature 3: Nested [ToonSerializable] Classes

### Purpose

Support nested objects marked with [ToonSerializable]. Enables:
- Complex object hierarchies
- Recursive structures
- Natural object composition
- Multiple nesting levels

### Implementation

#### Step 1: Define Nested Type

```csharp
[ToonSerializable]
public partial class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}
```

#### Step 2: Use in Parent Type

```csharp
[ToonSerializable]
public partial class Person
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    
    // Just use it - automatic detection!
    public Address? HomeAddress { get; set; }
    public Address? WorkAddress { get; set; }
}
```

#### Step 3: Use Normally

```csharp
var person = new Person
{
    Name = "John Doe",
    Age = 35,
    HomeAddress = new Address
    {
        Street = "123 Main St",
        City = "Springfield",
        State = "IL",
        ZipCode = "62701"
    },
    WorkAddress = new Address
    {
        Street = "456 Business Ave",
        City = "Chicago",
        State = "IL",
        ZipCode = "60601"
    }
};

// Serialize (handles nested addresses automatically)
var doc = Person.Serialize(person);

// Deserialize (handles nested addresses automatically)
var restored = Person.Deserialize(doc);

// All nested objects preserved!
Assert.Equal("John Doe", restored.Name);
Assert.Equal("Springfield", restored.HomeAddress?.City);
Assert.Equal("Chicago", restored.WorkAddress?.City);
```

### Multiple Nesting Levels

```csharp
[ToonSerializable]
public partial class Company
{
    public string Name { get; set; } = string.Empty;
    public Address? Headquarters { get; set; }
    public Person? CEO { get; set; }
}

// Usage
var company = new Company
{
    Name = "TechCorp",
    Headquarters = new Address { City = "San Francisco" },
    CEO = new Person 
    { 
        Name = "Jane Smith",
        HomeAddress = new Address { City = "Seattle" }
    }
};

// All levels handled automatically
var doc = Company.Serialize(company);
var restored = Company.Deserialize(doc);

Assert.Equal("TechCorp", restored.Name);
Assert.Equal("San Francisco", restored.Headquarters?.City);
Assert.Equal("Jane Smith", restored.CEO?.Name);
Assert.Equal("Seattle", restored.CEO?.HomeAddress?.City);
```

### Null Handling

```csharp
var person = new Person
{
    Name = "Jane Doe",
    Age = 28,
    HomeAddress = null,  // Null is fine
    WorkAddress = null   // Null is fine
};

// Serializes with nulls
var doc = Person.Serialize(person);

// Deserializes correctly
var restored = Person.Deserialize(doc);

Assert.Null(restored.HomeAddress);
Assert.Null(restored.WorkAddress);
```

---

## Combining Features

All three features can be combined for powerful, flexible serialization:

```csharp
[ToonSerializable]
public partial class Meeting
{
    public string Title { get; set; } = string.Empty;
    
    // Nested [ToonSerializable]
    public Address? Location { get; set; }
    
    // Custom converter
    [ToonConverter(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset StartTime { get; set; }
    
    // Custom converter
    [ToonConverter(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset EndTime { get; set; }
    
    // Another nested type
    public List<Person>? Attendees { get; set; }
}

// Usage - everything works together!
var meeting = new Meeting
{
    Title = "Product Planning",
    Location = new Address { City = "Boston" },
    StartTime = DateTimeOffset.UtcNow,
    EndTime = DateTimeOffset.UtcNow.AddHours(1),
    Attendees = new List<Person> { /* ... */ }
};

var doc = Meeting.Serialize(meeting);
var restored = Meeting.Deserialize(doc);
```

---

## Performance Characteristics

### Zero Reflection
- All three features use **compile-time code generation**
- No runtime type checking or reflection
- Custom converters instantiated at generation time
- Constructor parameters resolved at generation time

### Compile-Time Validation
- Invalid converter types detected early
- Missing custom constructor validation
- Type mismatches caught before runtime

### Memory Efficiency
- No extra allocations for converter caching
- Nested objects handled efficiently
- Constructor parameters optimized at compile-time

---

## Best Practices

### Custom Converters

✅ **DO:**
- Create separate converter classes (reusable, testable)
- Implement both Write and Read properly
- Handle null values gracefully
- Use meaningful error messages
- Test round-trip serialization

❌ **DON'T:**
- Put conversion logic in the class itself
- Assume non-null values
- Create stateful converters
- Throw without messages

### Custom Constructors

✅ **DO:**
- Use [ToonConstructor] only on one constructor
- Match parameter names to properties
- Add validation logic in constructor
- Use for immutable objects
- Document why custom constructor is needed

❌ **DON'T:**
- Mark multiple constructors with [ToonConstructor]
- Use parameter names that don't match properties
- Create side effects in constructor
- Use for performance optimization

### Nested Serializable Classes

✅ **DO:**
- Use [ToonSerializable] on nested classes
- Support null nested objects
- Test multiple nesting levels
- Document the object hierarchy
- Keep nesting reasonable (3-4 levels typical)

❌ **DON'T:**
- Create circular references (will cause stack overflow)
- Nest too deeply (affects readability)
- Mix with reflection-based serialization
- Assume nested objects are always present

---

## Troubleshooting

### Converter Not Being Used

**Problem:** Converter specified but serialization not using it

**Solutions:**
1. Ensure [ToonConverter(typeof(...))] attribute is on the property
2. Check converter type inherits from ToonConverter<T>
3. Verify converter has parameterless constructor
4. Rebuild to regenerate code

### Custom Constructor Not Being Called

**Problem:** Parameterless constructor used instead of custom constructor

**Solutions:**
1. Check [ToonConstructor] attribute is present
2. Ensure parameter names match property names (case-insensitive)
3. Only one constructor should have [ToonConstructor]
4. Rebuild to regenerate code

### Nested Class Not Recognized

**Problem:** Nested object treated as complex type (reflection fallback)

**Solutions:**
1. Ensure nested class has [ToonSerializable] attribute
2. Verify class is marked `public partial`
3. Check class is in same namespace as parent
4. Rebuild to regenerate code

---

## Examples Repository

See **tests/ToonNet.SourceGenerators.Tests/Models/Phase4Models.cs** for complete working examples of all three features.

See **tests/ToonNet.SourceGenerators.Tests/Tests/Phase4FeatureTests.cs** for 12 comprehensive test cases.

---

## Summary

Phase 4 provides three powerful, zero-reflection features:

1. **Custom Converters** - Type-specific serialization logic
2. **Custom Constructors** - Custom deserialization instantiation
3. **Nested [ToonSerializable] Classes** - Recursive object serialization

All are production-ready, fully tested, and work together seamlessly.

**Next:** See PHASE_4_COMPLETION_REPORT.md for technical details.
