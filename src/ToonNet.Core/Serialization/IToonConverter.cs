using ToonNet.Core.Models;

namespace ToonNet.Core.Serialization;

/// <summary>
///     Base interface for type converters.
/// </summary>
public interface IToonConverter
{
    /// <summary>
    ///     Determines whether this converter can handle the specified type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if this converter can handle the type; otherwise, false.</returns>
    bool CanConvert(Type type);
    
    /// <summary>
    ///     Writes an object to its TOON representation.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>The TOON representation of the value.</returns>
    ToonValue? Write(object? value, ToonSerializerOptions options);
    
    /// <summary>
    ///     Reads a TOON value and converts it to a C# object.
    /// </summary>
    /// <param name="value">The TOON value to read.</param>
    /// <param name="targetType">The target type to convert to.</param>
    /// <param name="options">The deserialization options.</param>
    /// <returns>The deserialized object.</returns>
    object? Read(ToonValue value, Type targetType, ToonSerializerOptions options);
}

/// <summary>
///     Generic converter interface for strongly typed conversions.
/// </summary>
/// <typeparam name="T">The type this converter handles.</typeparam>
public interface IToonConverter<T> : IToonConverter
{
    /// <summary>
    ///     Writes a strongly typed value to its TOON representation.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>The TOON representation of the value.</returns>
    ToonValue? Write(T? value, ToonSerializerOptions options);
    
    /// <summary>
    ///     Reads a TOON value and converts it to a strongly typed object.
    /// </summary>
    /// <param name="value">The TOON value to read.</param>
    /// <param name="options">The deserialization options.</param>
    /// <returns>The deserialized object.</returns>
    T? Read(ToonValue value, ToonSerializerOptions options);
}

/// <summary>
///     Base class for implementing type converters.
/// </summary>
/// <typeparam name="T">The type this converter handles.</typeparam>
public abstract class ToonConverter<T> : IToonConverter<T>
{
    /// <summary>
    ///     Determines whether this converter can handle the specified type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is assignable to T; otherwise, false.</returns>
    public virtual bool CanConvert(Type type)
    {
        return typeof(T).IsAssignableFrom(type);
    }

    /// <summary>
    ///     Writes a strongly typed value to its TOON representation.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>The TOON representation of the value.</returns>
    public abstract ToonValue? Write(T? value, ToonSerializerOptions options);

    /// <summary>
    ///     Reads a TOON value and converts it to a strongly typed object.
    /// </summary>
    /// <param name="value">The TOON value to read.</param>
    /// <param name="options">The deserialization options.</param>
    /// <returns>The deserialized object.</returns>
    public abstract T? Read(ToonValue value, ToonSerializerOptions options);

    /// <summary>
    ///     Non-generic write implementation that delegates to the strongly typed version.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>The TOON representation of the value.</returns>
    ToonValue? IToonConverter.Write(object? value, ToonSerializerOptions options)
    {
        return Write((T?)value, options);
    }

    /// <summary>
    ///     Non-generic read implementation that delegates to the strongly typed version.
    /// </summary>
    /// <param name="value">The TOON value to read.</param>
    /// <param name="targetType">The target type (ignored, T is used).</param>
    /// <param name="options">The deserialization options.</param>
    /// <returns>The deserialized object.</returns>
    object? IToonConverter.Read(ToonValue value, Type targetType, ToonSerializerOptions options)
    {
        return Read(value, options);
    }
}