# Custom Converters

Create custom type converters for specialized serialization/deserialization logic.

## IToonConverter Interface

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

## ToonConverter Base Class

Extend `ToonConverter<T>` for type-specific converters:

```csharp
using ToonNet.Core;

public abstract class ToonConverter<T> : IToonConverter<T>
{
    public bool CanConvert(Type type) => type == typeof(T);
    
    public abstract ToonValue Write(T value);
    public abstract T Read(ToonValue toonValue);
}
```

## Example: DateTime Converter

```csharp
public class CustomDateTimeConverter : ToonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";
    
    public override ToonValue Write(DateTime value)
    {
        return new ToonString(value.ToString(Format));
    }
    
    public override DateTime Read(ToonValue toonValue)
    {
        if (toonValue is not ToonString str)
            throw new ToonSerializationException("Expected string for DateTime");
        
        if (!DateTime.TryParseExact(str.Value, Format, null, 
            DateTimeStyles.None, out DateTime result))
            throw new ToonSerializationException($"Invalid DateTime format: {str.Value}");
        
        return result;
    }
}
```

## Example: URI Converter

```csharp
public class UriConverter : ToonConverter<Uri>
{
    public override ToonValue Write(Uri value)
    {
        return new ToonString(value.ToString());
    }
    
    public override Uri Read(ToonValue toonValue)
    {
        if (toonValue is not ToonString str)
            throw new ToonSerializationException("Expected string for Uri");
        
        if (!Uri.TryCreate(str.Value, UriKind.Absolute, out Uri? uri))
            throw new ToonSerializationException($"Invalid URI: {str.Value}");
        
        return uri;
    }
}
```

## Registering Converters

```csharp
var options = new ToonSerializerOptions();
options.Converters.Add(new CustomDateTimeConverter());
options.Converters.Add(new UriConverter());

string toon = ToonSerializer.Serialize(obj, options);
var result = ToonSerializer.Deserialize<MyType>(toon, options);
```

## Complex Example: Custom Object Converter

```csharp
public class AddressConverter : ToonConverter<Address>
{
    public override ToonValue Write(Address value)
    {
        // Custom format: "Street, City, ZIP"
        string formatted = $"{value.Street}, {value.City}, {value.ZipCode}";
        return new ToonString(formatted);
    }
    
    public override Address Read(ToonValue toonValue)
    {
        if (toonValue is not ToonString str)
            throw new ToonSerializationException("Expected string");
        
        string[] parts = str.Value.Split(", ");
        if (parts.Length != 3)
            throw new ToonSerializationException("Invalid address format");
        
        return new Address
        {
            Street = parts[0],
            City = parts[1],
            ZipCode = parts[2]
        };
    }
}
```

## Error Handling

```csharp
public class SafeConverter<T> : ToonConverter<T> where T : new()
{
    public override ToonValue Write(T value)
    {
        try
        {
            // Serialization logic
            return new ToonString(value.ToString());
        }
        catch (Exception ex)
        {
            throw new ToonEncodingException(
                $"Failed to serialize {typeof(T).Name}", 
                typeof(T).Name, 
                value, 
                ex);
        }
    }
    
    public override T Read(ToonValue toonValue)
    {
        try
        {
            // Deserialization logic
            return new T();
        }
        catch (Exception ex)
        {
            throw new ToonSerializationException(
                $"Failed to deserialize {typeof(T).Name}", 
                ex)
            {
                TargetType = typeof(T)
            };
        }
    }
}
```

## See Also

- **[Type System](../core-features/type-system)**: ToonValue types
- **[Custom Formats](../format-extensions/custom-formats)**: Format converters
