# Source Generators

Use ToonNet source generators for compile-time performance optimization.

## Overview

`ToonNet.SourceGenerators` generates serialization code at compile-time using Roslyn source generators, eliminating runtime reflection overhead.

## Installation

```bash
dotnet add package ToonNet.SourceGenerators
```

## Usage

### Mark Classes with Attributes

```csharp
using ToonNet.Core.Attributes;

[ToonSerializable]
public partial class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }
}
```

### Generated Code

The source generator creates optimized serialization methods:

```csharp
// Generated code (simplified)
partial class Person
{
    public static ToonValue ToToon(Person instance)
    {
        return new ToonObject
        {
            ["Name"] = instance.Name,
            ["Age"] = instance.Age,
            ["Email"] = instance.Email
        };
    }
    
    public static Person FromToon(ToonValue value)
    {
        var obj = (ToonObject)value;
        return new Person
        {
            Name = (string)obj["Name"],
            Age = (int)obj["Age"],
            Email = (string)obj["Email"]
        };
    }
}
```

## Benefits

1. **Zero Runtime Reflection**: Code generated at compile-time
2. **Type Safety**: Compile-time type checking
3. **Performance**: 10-100x faster than reflection-based serialization
4. **Debugging**: View generated code in IDE
5. **Trimming-Friendly**: Works with IL trimming/AOT

## Attributes

### ToonSerializable

Mark classes for source generation:

```csharp
[ToonSerializable]
public partial class User { }
```

### ToonIgnore

Exclude properties from serialization:

```csharp
[ToonSerializable]
public partial class User
{
    public string Username { get; set; }
    
    [ToonIgnore]
    public string PasswordHash { get; set; }  // Not serialized
}
```

### ToonPropertyName

Customize serialized property name:

```csharp
[ToonSerializable]
public partial class Person
{
    [ToonPropertyName("full_name")]
    public string Name { get; set; }
}
```

## Viewing Generated Code

In Visual Studio/Rider:
1. Build project
2. Navigate to **Dependencies → Analyzers → ToonNet.SourceGenerators**
3. View generated files

Or use `EmitCompilerGeneratedFiles`:

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

Generated files will be in `obj/generated/`.

## Limitations

- Classes must be `partial`
- Only public properties are serialized
- Requires .NET 8+ and C# 12+

## See Also

- **[Performance Tuning](performance-tuning)**: Optimization strategies
- **[Serialization](../core-features/serialization)**: Serialization API
