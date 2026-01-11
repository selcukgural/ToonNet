namespace ToonNet.Core;

/// <summary>
///     Base exception for TOON-related errors.
/// </summary>
public class ToonException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the ToonException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ToonException(string message) : base(message) { }
    
    /// <summary>
    ///     Initializes a new instance of the ToonException class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ToonException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    ///     Optional suggestion for fixing the error.
    /// </summary>
    public string? Suggestion { get; init; }

    /// <summary>
    ///     Optional code snippet showing the problematic area.
    /// </summary>
    public string? CodeSnippet { get; init; }

    /// <summary>
    ///     Returns a string representation of the exception, including suggestions and code snippets.
    /// </summary>
    /// <returns>A formatted string containing exception details, suggestions, and code snippets.</returns>
    public override string ToString()
    {
        var result = base.ToString();

        if (!string.IsNullOrEmpty(Suggestion))
        {
            result += $"\n\n💡 Suggestion: {Suggestion}";
        }

        if (!string.IsNullOrEmpty(CodeSnippet))
        {
            result += $"\n\n📝 Code:\n{CodeSnippet}";
        }

        return result;
    }
}

/// <summary>
///     Exception thrown during TOON parsing.
/// </summary>
public sealed class ToonParseException : ToonException
{
    /// <summary>
    ///     Initializes a new instance of the ToonParseException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The line number where the error occurred.</param>
    /// <param name="column">The column number where the error occurred.</param>
    public ToonParseException(string message, int line, int column) : base(FormatMessage(message, line, column))
    {
        Line = line;
        Column = column;
    }

    /// <summary>
    ///     Gets the line number where the error occurred.
    /// </summary>
    public int Line { get; }
    
    /// <summary>
    ///     Gets the column number where the error occurred.
    /// </summary>
    public int Column { get; }
    
    /// <summary>
    ///     Gets the actual token that was encountered.
    /// </summary>
    public string? ActualToken { get; init; }
    
    /// <summary>
    ///     Gets the expected token type.
    /// </summary>
    public string? ExpectedToken { get; init; }

    /// <summary>
    ///     Formats the error message with line and column information.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The line number.</param>
    /// <param name="column">The column number.</param>
    /// <returns>A formatted error message.</returns>
    private static string FormatMessage(string message, int line, int column)
    {
        if (line == 0 && column == 0)
        {
            return message;
        }

        return $"{message}\n  📍 Position: Line {line}, Column {column}";
    }

    /// <summary>
    ///     Creates a parse exception with detailed context.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The line number where the error occurred.</param>
    /// <param name="column">The column number where the error occurred.</param>
    /// <param name="actual">Optional actual token that was encountered.</param>
    /// <param name="expected">Optional expected token type.</param>
    /// <param name="suggestion">Optional suggestion for fixing the error.</param>
    /// <param name="codeSnippet">Optional code snippet showing the problematic area.</param>
    /// <returns>A new ToonParseException instance.</returns>
    public static ToonParseException Create(string message, int line, int column, string? actual = null, string? expected = null,
                                            string? suggestion = null, string? codeSnippet = null)
    {
        var ex = new ToonParseException(message, line, column)
        {
            ActualToken = actual,
            ExpectedToken = expected,
            Suggestion = suggestion,
            CodeSnippet = codeSnippet
        };

        return ex;
    }
}

/// <summary>
///     Exception thrown during TOON encoding.
/// </summary>
public sealed class ToonEncodingException : ToonException
{
    /// <summary>
    ///     Initializes a new instance of the ToonEncodingException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ToonEncodingException(string message) : base(message) { }

    /// <summary>
    ///     Initializes a new instance of the ToonEncodingException class with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ToonEncodingException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    ///     Gets the path to the problematic property.
    /// </summary>
    public string? PropertyPath { get; init; }
    
    /// <summary>
    ///     Gets the value that caused the encoding error.
    /// </summary>
    public object? ProblematicValue { get; init; }

    /// <summary>
    ///     Creates an encoding exception with context.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="propertyPath">The optional path to the problematic property.</param>
    /// <param name="problematicValue">The value that caused the encoding error.</param>
    /// <param name="suggestion">Optional suggestion for fixing the error.</param>
    /// <returns>A new ToonEncodingException instance.</returns>
    public static ToonEncodingException Create(string message, string? propertyPath = null, object? problematicValue = null,
                                               string? suggestion = null)
    {
        var fullMessage = message;

        if (!string.IsNullOrEmpty(propertyPath))
        {
            fullMessage += $"\n  📍 Property: {propertyPath}";
        }

        if (problematicValue != null)
        {
            fullMessage += $"\n  ⚠️  Value: {problematicValue} ({problematicValue.GetType().Name})";
        }

        var ex = new ToonEncodingException(fullMessage)
        {
            PropertyPath = propertyPath,
            ProblematicValue = problematicValue,
            Suggestion = suggestion
        };

        return ex;
    }
}

/// <summary>
///     Exception thrown during TOON serialization/deserialization.
/// </summary>
public sealed class ToonSerializationException : ToonException
{
    /// <summary>
    ///     Initializes a new instance of the ToonSerializationException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ToonSerializationException(string message) : base(message) { }

    /// <summary>
    ///     Initializes a new instance of the ToonSerializationException class with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ToonSerializationException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    ///     Gets the target type being serialized or deserialized.
    /// </summary>
    public Type? TargetType { get; init; }
    
    /// <summary>
    ///     Gets the property name where the error occurred.
    /// </summary>
    public string? PropertyName { get; init; }
    
    /// <summary>
    ///     Gets the value that caused the error.
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    ///     Creates a serialization exception with context.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="targetType">The target type being serialized/deserialized.</param>
    /// <param name="propertyName">The property name, if applicable.</param>
    /// <param name="value">The value that caused the error.</param>
    /// <param name="suggestion">Optional suggestion for fixing the error.</param>
    /// <returns>A new ToonSerializationException instance.</returns>
    public static ToonSerializationException Create(string message, Type? targetType = null, string? propertyName = null, object? value = null,
                                                    string? suggestion = null)
    {
        var fullMessage = message;

        if (targetType != null)
        {
            fullMessage += $"\n  🎯 Target Type: {targetType.Name}";
        }

        if (!string.IsNullOrEmpty(propertyName))
        {
            fullMessage += $"\n  📍 Property: {propertyName}";
        }

        if (value != null)
        {
            fullMessage += $"\n  ⚠️  Value: {value} ({value.GetType().Name})";
        }

        var ex = new ToonSerializationException(fullMessage)
        {
            TargetType = targetType,
            PropertyName = propertyName,
            Value = value,
            Suggestion = suggestion
        };

        return ex;
    }
}