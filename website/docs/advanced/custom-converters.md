# Custom Converters

Create custom type converters for specialized serialization/deserialization logic.

## IToonConverter Interface

The converter types live in the `ToonNet.Core.Serialization` namespace; the `ToonValue` types
(`ToonString`, `ToonNumber`, `ToonObject`, ...) live in `ToonNet.Core.Models`.

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

## ToonConverter Base Class

Extend `ToonConverter<T>` for type-specific converters. It implements the non-generic members for you:

```csharp
public abstract class ToonConverter<T> : IToonConverter<T>
{
    // Matches T and types derived from T. Override to narrow it.
    public virtual bool CanConvert(Type type) => typeof(T).IsAssignableFrom(type);

    public abstract ToonValue? Write(T? value, ToonSerializerOptions options);
    public abstract T? Read(ToonValue value, ToonSerializerOptions options);
}
```

How the serializer calls a converter:

- `Write` is not called for `null`; the serializer writes `null` itself (or skips the property when
  `IgnoreNullValues` is set).
- `Read` **is** called for a `null` value, with a `ToonNull`, so handle it for reference types.
- The `options` argument lets a converter serialize nested values with `ToonSerializer.SerializeToValue`
  and `ToonSerializer.DeserializeFromValue`.

## Example: DateTime Converter

```csharp
using System.Globalization;
using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

public class CustomDateTimeConverter : ToonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";

    public override ToonValue? Write(DateTime value, ToonSerializerOptions options)
    {
        return new ToonString(value.ToString(Format, CultureInfo.InvariantCulture));
    }

    public override DateTime Read(ToonValue value, ToonSerializerOptions options)
    {
        if (value is not ToonString str)
            throw new ToonSerializationException("Expected string for DateTime");

        if (!DateTime.TryParseExact(str.Value, Format, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime result))
            throw new ToonSerializationException($"Invalid DateTime format: {str.Value}");

        return result;
    }
}
```

Output for a `DateTime` property: `When: "2026-06-15 09:00:00"` (quoted because the value contains a `:`).

## Example: URI Converter

`Uri` is supported out of the box; a converter is only needed for different rules, such as accepting
absolute URIs only:

```csharp
public class AbsoluteUriConverter : ToonConverter<Uri>
{
    public override ToonValue? Write(Uri? value, ToonSerializerOptions options)
    {
        return new ToonString(value!.ToString());  // never called with null
    }

    public override Uri? Read(ToonValue value, ToonSerializerOptions options)
    {
        if (value is ToonNull)
            return null;

        if (value is not ToonString str)
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
options.AddConverter(new CustomDateTimeConverter());   // same as options.Converters.Add(...), with a null check
options.AddConverter(new AbsoluteUriConverter());

string toon = ToonSerializer.Serialize(obj, options);
var result = ToonSerializer.Deserialize<MyType>(toon, options);
```

The first converter whose `CanConvert` returns `true` is used.

### With the `[ToonConverter]` Attribute

Apply `[ToonConverter]` (namespace `ToonNet.Core.Serialization.Attributes`) to a property, or to a class
or struct, to use a converter without registering it in the options. The converter type needs a public
parameterless constructor.

```csharp
using ToonNet.Core.Serialization.Attributes;

public class Order
{
    [ToonConverter(typeof(CustomDateTimeConverter))]
    public DateTime PlacedAt { get; set; }
}
```

A converter on a property takes precedence over the converters in the options, which take precedence over a
converter on the type.

## Complex Example: Custom Object Converter

```csharp
public class AddressConverter : ToonConverter<Address>
{
    public override ToonValue? Write(Address? value, ToonSerializerOptions options)
    {
        // Custom format: "Street, City, ZIP"
        string formatted = $"{value!.Street}, {value.City}, {value.ZipCode}";
        return new ToonString(formatted);
    }

    public override Address? Read(ToonValue value, ToonSerializerOptions options)
    {
        if (value is ToonNull)
            return null;

        if (value is not ToonString str)
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

An `Address` property is then written as `Address: "1 Main St, New York, 10001"`.

## Error Handling

```csharp
public class SafeConverter<T> : ToonConverter<T> where T : new()
{
    public override ToonValue? Write(T? value, ToonSerializerOptions options)
    {
        try
        {
            // Serialization logic
            return new ToonString(value!.ToString() ?? "");
        }
        catch (Exception ex)
        {
            throw new ToonEncodingException($"Failed to serialize {typeof(T).Name}", ex)
            {
                ProblematicValue = value
            };
        }
    }

    public override T? Read(ToonValue value, ToonSerializerOptions options)
    {
        try
        {
            // Deserialization logic
            return new T();
        }
        catch (Exception ex)
        {
            throw new ToonSerializationException($"Failed to deserialize {typeof(T).Name}", ex)
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
