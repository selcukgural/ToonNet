namespace ToonNet.Core.Models;

/// <summary>
/// Represents a token in TOON format with its type, value, and position.
/// </summary>
/// <remarks>
/// This is an internal implementation detail used by the lexer and parser.
/// Users should use <see cref="ToonDocument"/> and <see cref="ToonValue"/> instead.
/// </remarks>
internal readonly struct ToonToken(ToonTokenType type, ReadOnlyMemory<char> value, int line, int column)
{
    public ToonTokenType Type { get; } = type;
    public ReadOnlyMemory<char> Value { get; } = value;
    public int Line { get; } = line;
    public int Column { get; } = column;

    public override string ToString() => $"{Type} '{Value}' at {Line}:{Column}";
}
