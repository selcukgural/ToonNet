# Quick Start

Get started with ToonNet in 5 minutes. This guide will walk you through your first TOON serialization.

## Step 1: Install ToonNet

```bash
dotnet add package ToonNet.Core
```

## Step 2: Create a Model

Create a simple C# class:

```csharp
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }
}
```

## Step 3: Serialize to TOON

Use `ToonSerializer` to convert your object to TOON format:

```csharp
using ToonNet.Core;

var person = new Person
{
    Name = "Alice Smith",
    Age = 30,
    Email = "alice@example.com"
};

string toonString = ToonSerializer.Serialize(person);
Console.WriteLine(toonString);
```

**Output:**
```toon
Name: Alice Smith
Age: 30
Email: alice@example.com
```

## Step 4: Deserialize from TOON

Convert the TOON string back to your object:

```csharp
string toonInput = """
Name: Bob Johnson
Age: 25
Email: bob@example.com
""";

Person restoredPerson = ToonSerializer.Deserialize<Person>(toonInput);

Console.WriteLine($"{restoredPerson.Name} is {restoredPerson.Age} years old");
// Output: Bob Johnson is 25 years old
```

## Complete Example

Here's a complete working example:

```csharp
using ToonNet.Core;

// Define your model
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }
}

class Program
{
    static void Main()
    {
        // Create an object
        var person = new Person
        {
            Name = "Alice Smith",
            Age = 30,
            Email = "alice@example.com"
        };

        // Serialize to TOON
        string toon = ToonSerializer.Serialize(person);
        Console.WriteLine("Serialized:");
        Console.WriteLine(toon);

        // Deserialize back to object
        var restored = ToonSerializer.Deserialize<Person>(toon);
        Console.WriteLine($"\nDeserialized: {restored.Name}, {restored.Age}");
    }
}
```

## TOON vs JSON Comparison

For the same data, compare the output formats:

**TOON Format** (73 characters):
```toon
Name: Alice Smith
Age: 30
Email: alice@example.com
```

**JSON Format** (97 characters):
```json
{
  "Name": "Alice Smith",
  "Age": 30,
  "Email": "alice@example.com"
}
```

**Token Savings**: ~25% fewer tokens with TOON! 🚀

## What Makes TOON Different?

1. **Human-Readable**: Clean syntax without brackets and quotes
2. **Token-Efficient**: Up to 40% fewer tokens than JSON (ideal for AI/LLM)
3. **Type-Safe**: Strongly typed serialization/deserialization
4. **Performance**: Uses expression trees, not reflection

## Next Steps

Now that you've completed your first serialization:

- **[Basic Serialization](basic-serialization)**: Learn more about serializing different types
- **[Core Features](../core-features/serialization)**: Explore all serialization options
- **[Configuration](../core-features/configuration)**: Customize serialization behavior
