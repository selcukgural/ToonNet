using ToonNet.Core.Models;

namespace ToonNet.Core.Serialization;

/// <summary>
/// Base interface for type converters.
/// </summary>
public interface IToonConverter
{
    bool CanConvert(Type type);
    ToonValue? Write(object? value, ToonSerializerOptions options);
    object? Read(ToonValue value, Type targetType, ToonSerializerOptions options);
}

/// <summary>
/// Generic converter interface for strongly typed conversions.
/// </summary>
public interface IToonConverter<T> : IToonConverter
{
    ToonValue? Write(T? value, ToonSerializerOptions options);
    T? Read(ToonValue value, ToonSerializerOptions options);
}

/// <summary>
/// Base class for implementing type converters.
/// </summary>
public abstract class ToonConverter<T> : IToonConverter<T>
{
    public virtual bool CanConvert(Type type)
    {
        return typeof(T).IsAssignableFrom(type);
    }

    public abstract ToonValue? Write(T? value, ToonSerializerOptions options);
    
    public abstract T? Read(ToonValue value, ToonSerializerOptions options);

    // Non-generic interface implementations
    ToonValue? IToonConverter.Write(object? value, ToonSerializerOptions options)
    {
        return Write((T?)value, options);
    }

    object? IToonConverter.Read(ToonValue value, Type targetType, ToonSerializerOptions options)
    {
        return Read(value, options);
    }
}
