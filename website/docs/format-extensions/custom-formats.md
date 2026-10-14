# Converter Examples

More converters for types that need another text form than the default one. The interface, the base class, the
order in which converters are chosen and the `[ToonConverter]` attribute are described in
[Custom Converters](../advanced/custom-converters), which also has a `DateTime` converter with a fixed format.

`Write` is never called with `null`; `Read` receives a `ToonNull` for null values.

## Value Converters

### Guid Without Hyphens

`Guid` is supported out of the box; a converter lets you pick another format:

```csharp
using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

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
options.AddConverter(new CustomDateTimeConverter());   // from Custom Converters
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

- **[Custom Converters](../advanced/custom-converters)**: The converter interface and how converters are chosen
- **[JSON Integration](json-integration)**: Converting JSON documents to and from TOON
- **[YAML Integration](yaml-integration)**: Converting YAML documents to and from TOON
- **[Type System](../core-features/type-system)**: ToonValue types
