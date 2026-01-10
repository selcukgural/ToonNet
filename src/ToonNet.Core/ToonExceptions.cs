namespace ToonNet.Core;

/// <summary>
/// Base exception for TOON-related errors.
/// </summary>
public class ToonException : Exception
{
    public ToonException(string message) : base(message) { }
    public ToonException(string message, Exception innerException) : base(message, innerException) { }
    
    /// <summary>
    /// Optional suggestion for fixing the error.
    /// </summary>
    public string? Suggestion { get; init; }
    
    /// <summary>
    /// Optional code snippet showing the problematic area.
    /// </summary>
    public string? CodeSnippet { get; init; }

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
/// Exception thrown during TOON parsing.
/// </summary>
public sealed class ToonParseException : ToonException
{
    public int Line { get; }
    public int Column { get; }
    public string? ActualToken { get; init; }
    public string? ExpectedToken { get; init; }
    
    public ToonParseException(string message, int line, int column) 
        : base(FormatMessage(message, line, column))
    {
        Line = line;
        Column = column;
    }
    
    private static string FormatMessage(string message, int line, int column)
    {
        if (line == 0 && column == 0)
            return message;
            
        return $"{message}\n  📍 Position: Line {line}, Column {column}";
    }
    
    /// <summary>
    /// Creates a parse exception with detailed context.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The line number where the error occurred.</param>
    /// <param name="column">The column number where the error occurred.</param>
    /// <param name="actual">Optional actual token that was encountered.</param>
    /// <param name="expected">Optional expected token type.</param>
    /// <param name="suggestion">Optional suggestion for fixing the error.</param>
    /// <param name="codeSnippet">Optional code snippet showing the problematic area.</param>
    /// <returns>A new ToonParseException instance.</returns>
    public static ToonParseException Create(
        string message, 
        int line, 
        int column, 
        string? actual = null, 
        string? expected = null,
        string? suggestion = null,
        string? codeSnippet = null)
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
/// Exception thrown during TOON encoding.
/// </summary>
public sealed class ToonEncodingException : ToonException
{
    public string? PropertyPath { get; init; }
    public object? ProblematicValue { get; init; }
    
    public ToonEncodingException(string message) : base(message) { }
    
    public ToonEncodingException(string message, Exception innerException) 
        : base(message, innerException) { }
    
    /// <summary>
    /// Creates an encoding exception with context.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="propertyPath">The optional path to the problematic property.</param>
    /// <param name="problematicValue">The value that caused the encoding error.</param>
    /// <param name="suggestion">Optional suggestion for fixing the error.</param>
    /// <returns>A new ToonEncodingException instance.</returns>
    public static ToonEncodingException Create(
        string message,
        string? propertyPath = null,
        object? problematicValue = null,
        string? suggestion = null)
    {
        var fullMessage = message;
        
        if (!string.IsNullOrEmpty(propertyPath))
            fullMessage += $"\n  📍 Property: {propertyPath}";
            
        if (problematicValue != null)
            fullMessage += $"\n  ⚠️  Value: {problematicValue} ({problematicValue.GetType().Name})";
        
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
/// Exception thrown during TOON serialization/deserialization.
/// </summary>
public sealed class ToonSerializationException : ToonException
{
    public Type? TargetType { get; init; }
    public string? PropertyName { get; init; }
    public object? Value { get; init; }
    
    public ToonSerializationException(string message) : base(message) { }
    
    public ToonSerializationException(string message, Exception innerException) 
        : base(message, innerException) { }
    
    /// <summary>
    /// Creates a serialization exception with context.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="targetType">The target type being serialized/deserialized.</param>
    /// <param name="propertyName">The property name, if applicable.</param>
    /// <param name="value">The value that caused the error.</param>
    /// <param name="suggestion">Optional suggestion for fixing the error.</param>
    /// <returns>A new ToonSerializationException instance.</returns>
    public static ToonSerializationException Create(
        string message,
        Type? targetType = null,
        string? propertyName = null,
        object? value = null,
        string? suggestion = null)
    {
        var fullMessage = message;
        
        if (targetType != null)
            fullMessage += $"\n  🎯 Target Type: {targetType.Name}";
            
        if (!string.IsNullOrEmpty(propertyName))
            fullMessage += $"\n  📍 Property: {propertyName}";
            
        if (value != null)
            fullMessage += $"\n  ⚠️  Value: {value} ({value.GetType().Name})";
        
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
