# Custom Formats

Create custom converters to handle specific types or implement custom serialization logic.

## IToonConverter Interface

Base interface for custom converters:

```csharp
public interface IToonConverter
{
    bool CanConvert(Type type);
}

public interface IToonConverter<T> : IToonConverter
{
    ToonValue Write(T value);
    T Read(ToonValue toonValue);
}
```

## Creating a Custom Converter

### Example: Custom DateTime Converter

```csharp
using ToonNet.Core;

public class CustomDateTimeConverter : ToonConverter<DateTime>
{
    public override ToonValue Write(DateTime value)
    {
        // Custom format: "YYYY-MM-DD HH:MM:SS"
        string formatted = value.ToString("yyyy-MM-dd HH:mm:ss");
        return new ToonString(formatted);
    }
    
    public override DateTime Read(ToonValue toonValue)
    {
        if (toonValue is not ToonString str)
            throw new ToonSerializationException("Expected string value");
        
        return DateTime.ParseExact(str.Value, "yyyy-MM-dd HH:mm:ss", null);
    }
}
```

### Example: Guid Converter

```csharp
public class GuidConverter : ToonConverter<Guid>
{
    public override ToonValue Write(Guid value)
    {
        return new ToonString(value.ToString("D")); // Format with dashes
    }
    
    public override Guid Read(ToonValue toonValue)
    {
        if (toonValue is not ToonString str)
            throw new ToonSerializationException("Expected string for Guid");
        
        return Guid.Parse(str.Value);
    }
}
```

## Registering Converters

```csharp
var options = new ToonSerializerOptions();
options.Converters.Add(new CustomDateTimeConverter());
options.Converters.Add(new GuidConverter());

// Use for serialization
string toon = ToonSerializer.Serialize(obj, options);

// Use for deserialization
var result = ToonSerializer.Deserialize<MyData>(toon, options);
```

## Advanced Examples

### Complex Type Converter

```csharp
public class AddressConverter : ToonConverter<Address>
{
    public override ToonValue Write(Address value)
    {
        return new ToonObject
        {
            ["street"] = value.Street,
            ["city"] = value.City,
            ["zip"] = value.ZipCode
        };
    }
    
    public override Address Read(ToonValue toonValue)
    {
        if (toonValue is not ToonObject obj)
            throw new ToonSerializationException("Expected object");
        
        return new Address
        {
            Street = (string)obj["street"],
            City = (string)obj["city"],
            ZipCode = (string)obj["zip"]
        };
    }
}
```

### Generic Collection Converter

```csharp
public class CustomListConverter<T> : ToonConverter<List<T>>
{
    public override ToonValue Write(List<T> value)
    {
        var array = new ToonArray();
        foreach (var item in value)
        {
            array.Add(ToonSerializer.Serialize(item));
        }
        return array;
    }
    
    public override List<T> Read(ToonValue toonValue)
    {
        if (toonValue is not ToonArray arr)
            throw new ToonSerializationException("Expected array");
        
        var list = new List<T>();
        foreach (var item in arr)
        {
            list.Add(ToonSerializer.Deserialize<T>(item.ToString()));
        }
        return list;
    }
}
```

## Best Practices

1. **Handle nulls**: Check for `ToonNull` in Read methods
2. **Type validation**: Validate `ToonValue` types before casting
3. **Error messages**: Provide clear exception messages
4. **Immutability**: Don't modify input `ToonValue` objects
5. **Performance**: Cache expensive operations

## See Also

- **[JSON Integration](json-integration)**: JSON converter implementation
- **[YAML Integration](yaml-integration)**: YAML converter implementation
- **[Type System](../core-features/type-system)**: ToonValue types
