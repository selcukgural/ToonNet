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
    ///     Appends the details that have a value to a message, on one line: <c>message (path: $.Age, target type: Int32)</c>.
    /// </summary>
    internal static string WithDetails(string message, params (string Name, string? Value)[] details)
    {
        var text = new System.Text.StringBuilder(message);
        var first = true;

        foreach (var (name, value) in details)
        {
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            text.Append(first ? " (" : ", ").Append(name).Append(": ").Append(value);
            first = false;
        }

        return first ? message : text.Append(')').ToString();
    }

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
            result += $"\n\nSuggestion: {Suggestion}";
        }

        if (!string.IsNullOrEmpty(CodeSnippet))
        {
            result += $"\n\nCode:\n{CodeSnippet}";
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
        Reason = message;
        Line = line;
        Column = column;
    }

    /// <summary>The message without the position.</summary>
    internal string Reason { get; }

    /// <summary>
    ///     Gets the line number where the error occurred.
    /// </summary>
    public int Line { get; }
    
    /// <summary>
    ///     Gets the column number where the error occurred.
    /// </summary>
    public int Column { get; }
    
    /// <summary>
    ///     Formats the error message with line and column information, on one line: <c>Duplicate key 'id' (line 3, column 1)</c>.
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

        return $"{message} (line {line}, column {column})";
    }

    /// <summary>
    ///     Creates a parse exception with detailed context.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The line number where the error occurred.</param>
    /// <param name="column">The column number where the error occurred.</param>
    /// <param name="suggestion">Optional suggestion for fixing the error.</param>
    /// <param name="codeSnippet">Optional code snippet showing the problematic area.</param>
    /// <returns>A new ToonParseException instance.</returns>
    public static ToonParseException Create(string message, int line, int column, string? suggestion = null, string? codeSnippet = null)
    {
        return new ToonParseException(message, line, column) { Suggestion = suggestion, CodeSnippet = codeSnippet };
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
    /// <param name="problematicValue">The value that caused the encoding error; only its type is written to the message.</param>
    /// <param name="suggestion">Optional suggestion for fixing the error.</param>
    /// <returns>A new ToonEncodingException instance.</returns>
    public static ToonEncodingException Create(string message, string? propertyPath = null, object? problematicValue = null,
                                               string? suggestion = null)
    {
        // The value itself stays out of the message (it may be personal data); it is available as ProblematicValue
        var fullMessage = WithDetails(message, ("property", propertyPath), ("value type", problematicValue?.GetType().Name));

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
    ///     Gets where the error occurred: a path such as <c>$.Items[2].Price</c> for errors raised by
    ///     <see cref="Serialization.ToonSerializer"/>, or the property name passed to <see cref="Create"/>.
    /// </summary>
    public string? Path { get; init; }

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
    /// <param name="value">The value that caused the error; only its type is written to the message.</param>
    /// <param name="suggestion">Optional suggestion for fixing the error.</param>
    /// <returns>A new ToonSerializationException instance.</returns>
    public static ToonSerializationException Create(string message, Type? targetType = null, string? propertyName = null, object? value = null,
                                                    string? suggestion = null)
    {
        // The value itself stays out of the message (it may be personal data); it is available as Value
        var fullMessage = WithDetails(message, ("target type", targetType?.Name), ("property", propertyName), ("value type", value?.GetType().Name));

        var ex = new ToonSerializationException(fullMessage)
        {
            TargetType = targetType,
            Path = propertyName,
            Value = value,
            Suggestion = suggestion
        };

        return ex;
    }
}