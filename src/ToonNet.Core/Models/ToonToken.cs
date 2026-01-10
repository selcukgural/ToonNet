namespace ToonNet.Core.Models;

/// <summary>
///     Represents a token in TOON format with its type, value, and position.
/// </summary>
/// <remarks>
///     This is an internal implementation detail used by the lexer and parser.
///     Users should use <see cref="ToonDocument" /> and <see cref="ToonValue" /> instead.
/// </remarks>
internal readonly struct ToonToken(ToonTokenType type, ReadOnlyMemory<char> value, int line, int column)
{
    /// <summary>
    ///     Gets the type of this token.
    /// </summary>
    public ToonTokenType Type { get; } = type;
    
    /// <summary>
    ///     Gets the value of this token.
    /// </summary>
    public ReadOnlyMemory<char> Value { get; } = value;
    
    /// <summary>
    ///     Gets the line number where this token appears.
    /// </summary>
    public int Line { get; } = line;
    
    /// <summary>
    ///     Gets the column number where this token appears.
    /// </summary>
    public int Column { get; } = column;

    /// <summary>
    ///     Returns a string representation of this token.
    /// </summary>
    /// <returns>A string containing the token type, value, and position.</returns>
    public override string ToString()
    {
        return $"{Type} '{Value}' at {Line}:{Column}";
    }
}