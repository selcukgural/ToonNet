namespace ToonNet.Core.Models;

/// <summary>
/// Represents a token in TOON format with its type, value, and position.
/// </summary>
public readonly struct ToonToken(ToonTokenType type, ReadOnlyMemory<char> value, int line, int column)
{
    public ToonTokenType Type { get; } = type;
    public ReadOnlyMemory<char> Value { get; } = value;
    public int Line { get; } = line;
    public int Column { get; } = column;

    public override string ToString() => $"{Type} '{Value}' at {Line}:{Column}";
}
