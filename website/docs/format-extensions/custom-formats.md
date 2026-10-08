# Custom Formats

Create custom converters to handle specific types or implement custom serialization logic.

## IToonConverter Interface

Base interface for custom converters (namespace `ToonNet.Core.Serialization`). Usually you derive from
`ToonConverter<T>`, which implements the non-generic members:

```csharp
public interface IToonConverter
{
    bool CanConvert(Type type);
    ToonValue? Write(object? value, ToonSerializerOptions options);
    object? Read(ToonValue value, Type targetType, ToonSerializerOptions options);
}

public interface IToonConverter<T> : IToonConverter
{
    ToonValue? Write(T? value, ToonSerializerOptions options);
    T? Read(ToonValue value, ToonSerializerOptions options);
}
```

`Write` is never called with `null`; `Read` receives a `ToonNull` for null values. See
[Custom Converters](../advanced/custom-converters) for details and the `[ToonConverter]` attribute.

## Creating a Custom Converter

### Example: Custom DateTime Converter

```csharp
using System.Globalization;
using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

public class CustomDateTimeConverter : ToonConverter<DateTime>
{
    public override ToonValue? Write(DateTime value, ToonSerializerOptions options)
    {
        // Custom format: "YYYY-MM-DD HH:MM:SS"
        string formatted = value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        return new ToonString(formatted);
    }

    public override DateTime Read(ToonValue value, ToonSerializerOptions options)
    {
        if (value is not ToonString str)
            throw new ToonSerializationException("Expected string value");

        return DateTime.ParseExact(str.Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }
}
```

### Example: Guid Converter

`Guid` is supported out of the box; a converter lets you pick another format:

```csharp
public class GuidConverter : ToonConverter<Guid>
{
    public override ToonValue? Write(Guid value, ToonSerializerOptions options)
    {
        return new ToonString(value.ToString("N")); // 32 digits, no dashes
    }

    public override Guid Read(ToonValue value, ToonSerializerOptions options)
    {
        if (value is not ToonString str)
            throw new ToonSerializationException("Expected string for Guid");
        
        return Guid.Parse(str.Value);
    }
}
```

## Registering Converters

```csharp
var options = new ToonSerializerOptions();
options.AddConverter(new CustomDateTimeConverter());
options.AddConverter(new GuidConverter());

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
    public override ToonValue? Write(Address? value, ToonSerializerOptions options)
    {
        return new ToonObject
        {
            ["street"] = value!.Street,
            ["city"] = value.City,
            ["zip"] = value.ZipCode
        };
    }

    public override Address? Read(ToonValue value, ToonSerializerOptions options)
    {
        if (value is ToonNull)
            return null;

        if (value is not ToonObject obj)
            throw new ToonSerializationException("Expected object");

        return new Address
        {
            Street = (obj["street"] as ToonString)?.Value ?? "",
            City = (obj["city"] as ToonString)?.Value ?? "",
            ZipCode = (obj["zip"] as ToonString)?.Value ?? ""
        };
    }
}
```

### Generic Collection Converter

```csharp
public class CustomListConverter<T> : ToonConverter<List<T>>
{
    public override ToonValue? Write(List<T>? value, ToonSerializerOptions options)
    {
        var array = new ToonArray();
        foreach (var item in value!)
        {
            array.Add(ToonSerializer.SerializeToValue(item, options));
        }
        return array;
    }

    public override List<T>? Read(ToonValue value, ToonSerializerOptions options)
    {
        if (value is ToonNull)
            return null;

        if (value is not ToonArray arr)
            throw new ToonSerializationException("Expected array");

        var list = new List<T>();
        foreach (var item in arr.Items)
        {
            list.Add(ToonSerializer.DeserializeFromValue<T>(item, options)!);
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
