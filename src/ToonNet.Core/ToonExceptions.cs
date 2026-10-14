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
    ///     Puts <paramref name="prefix"/> (a path such as <c>$.Items[2]</c>) in front of a path that starts with <c>$</c>,
    ///     or makes it the path when there is none.
    /// </summary>
    internal static string PrependPath(string prefix, string? path) => path is { Length: > 1 } ? prefix + path[1..] : prefix;

    /// <summary>The path of a property or dictionary key, as the serializer writes it: <c>$.Name</c>.</summary>
    internal static string PathOf(string name) => ToonErrorText.AppendMember(new System.Text.StringBuilder("$"), name).ToString();

    /// <summary>The path of an item: <c>$[2]</c>.</summary>
    internal static string PathOf(int index) => "$[" + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";

    /// <summary>
    ///     Puts a path in front of the path of an error the serializer raised, as the error leaves the value the path
    ///     leads to; other errors are left as they are.
    /// </summary>
    internal virtual void PrependPath(string prefix) { }

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

    /// <summary>The message without details, for an error the serializer raised (its path can still be completed).</summary>
    private string? _reason;

    private string? _propertyPath;

    /// <summary>
    ///     Gets the path to the problematic property.
    /// </summary>
    public string? PropertyPath { get => _propertyPath; init => _propertyPath = value; }

    /// <inheritdoc />
    public override string Message => _reason == null ? base.Message : WithDetails(_reason, ("property", _propertyPath));

    /// <summary>An error the serializer raised at a path (relative to where it was raised, if it is completed later).</summary>
    internal static ToonEncodingException AtPath(string message, string path, string? suggestion = null) =>
        new(WithDetails(message, ("property", path))) { _reason = message, PropertyPath = path, Suggestion = suggestion };

    internal override void PrependPath(string prefix)
    {
        if (_reason != null)
        {
            _propertyPath = PrependPath(prefix, _propertyPath);
        }
    }
    
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
    
    /// <summary>The message without details, for an error the serializer raised (its path can still be completed).</summary>
    private string? _reason;

    /// <summary>Whether the message names the target type.</summary>
    private bool _namesTargetType;

    private string? _path;

    /// <summary>
    ///     Gets where the error occurred: a path such as <c>$.Items[2].Price</c> for errors raised by
    ///     <see cref="Serialization.ToonSerializer"/>, or the property name passed to <see cref="Create"/>.
    /// </summary>
    public string? Path { get => _path; init => _path = value; }

    /// <inheritdoc />
    public override string Message => _reason == null
        ? base.Message
        : WithDetails(_reason, ("path", _path), ("target type", _namesTargetType ? TargetType?.Name : null));

    /// <summary>
    ///     An error the serializer raised at a path (relative to where it was raised, if it is completed later); the
    ///     message names the path and, with <paramref name="namesTargetType"/>, the target type.
    /// </summary>
    internal static ToonSerializationException AtPath(string message, string? path, Type targetType, bool namesTargetType, object? value = null,
                                                      Exception? inner = null)
    {
        var text = WithDetails(message, ("path", path), ("target type", namesTargetType ? targetType.Name : null));

        return inner == null
            ? new ToonSerializationException(text) { _reason = message, _namesTargetType = namesTargetType, Path = path, TargetType = targetType, Value = value }
            : new ToonSerializationException(text, inner)
            {
                _reason = message, _namesTargetType = namesTargetType, Path = path, TargetType = targetType, Value = value
            };
    }

    internal override void PrependPath(string prefix)
    {
        if (_reason != null)
        {
            _path = PrependPath(prefix, _path);
        }
    }

    /// <summary>
    ///     For an error in an item of a batch of a streamed array (<c>$[3].Id</c>): the path in the whole document, with
    ///     the items of the earlier batches counted and the path of the array in front (<c>$.data.orders[1003].Id</c>).
    /// </summary>
    internal void RebaseItemPath(long itemsBefore, string arrayPath)
    {
        if (_reason == null || _path is not { } path || !path.StartsWith("$[", StringComparison.Ordinal))
        {
            return;
        }

        var close = path.IndexOf(']');

        if (close > 2 && long.TryParse(path.AsSpan(2, close - 2), System.Globalization.NumberStyles.None,
                                       System.Globalization.CultureInfo.InvariantCulture, out var index))
        {
            _path = arrayPath + "[" + (index + itemsBefore).ToString(System.Globalization.CultureInfo.InvariantCulture) + "]" + path[(close + 1)..];
        }
    }

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
/// <summary>
///     Exception thrown when a path (<see cref="Paths.ToonPath"/>) is not valid.
/// </summary>
public sealed class ToonPathException : ToonException
{
    /// <summary>Initializes a new instance of the ToonPathException class.</summary>
    /// <param name="reason">What is wrong with the path.</param>
    /// <param name="path">The path text.</param>
    /// <param name="position">The 0-based position of the error in the path.</param>
    public ToonPathException(string reason, string path, int position)
        : base(WithDetails(reason, ("position", position.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                           ("path", ToonErrorText.EscapeControls(path.Length <= 200 ? path : path[..200] + "..."))))
    {
        Reason = reason;
        Path = path;
        Position = position;
    }

    /// <summary>What is wrong with the path, without the details.</summary>
    public string Reason { get; }

    /// <summary>The path text.</summary>
    public string Path { get; }

    /// <summary>The 0-based position of the error in the path.</summary>
    public int Position { get; }
}

/// <summary>
///     Exception thrown when a document breaks the rules of a <see cref="Validation.ToonRuleSet"/>
///     (<see cref="Validation.ToonRuleResult.ThrowIfInvalid"/>).
/// </summary>
public sealed class ToonRuleException : ToonException
{
    /// <summary>Initializes a new instance of the ToonRuleException class.</summary>
    /// <param name="violations">The violations of rules of severity <see cref="Validation.ToonRuleSeverity.Error"/>.</param>
    public ToonRuleException(IReadOnlyList<Validation.ToonRuleViolation> violations)
        : base(Describe(violations))
    {
        Violations = violations;
    }

    /// <summary>The violations.</summary>
    public IReadOnlyList<Validation.ToonRuleViolation> Violations { get; }

    /// <summary>One line: the number of violations, then the first ten of them.</summary>
    private static string Describe(IReadOnlyList<Validation.ToonRuleViolation> violations)
    {
        ArgumentNullException.ThrowIfNull(violations);
        var text = new System.Text.StringBuilder();
        text.Append("The document has ").Append(violations.Count).Append(violations.Count == 1 ? " rule violation: " : " rule violations: ");
        text.AppendJoin("; ", violations.Take(10));

        if (violations.Count > 10)
        {
            text.Append("; ...");
        }

        return text.ToString();
    }
}

/// <summary>
///     Exception thrown when evaluating a path, or validating rules, goes beyond a limit of its
///     <see cref="Paths.ToonPathOptions"/>: the step budget or the timeout.
/// </summary>
public sealed class ToonPathLimitException : ToonException
{
    /// <summary>Initializes a new instance of the ToonPathLimitException class.</summary>
    /// <param name="message">The message.</param>
    /// <param name="isTimeout">Whether the timeout was reached (otherwise the step budget).</param>
    public ToonPathLimitException(string message, bool isTimeout) : base(message)
    {
        IsTimeout = isTimeout;
    }

    /// <summary>Whether the evaluation took longer than <see cref="Paths.ToonPathOptions.Timeout"/>; false for the step budget.</summary>
    public bool IsTimeout { get; }
}
