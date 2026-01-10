using System.Text;

namespace ToonNet.SourceGenerators.Utilities;

/// <summary>
/// Helper class for building generated C# code with proper indentation.
/// </summary>
internal sealed class CodeBuilder
{
    private readonly StringBuilder _sb = new();
    private int _indentLevel = 0;
    private const int IndentSize = 4;

    /// <summary>
    /// Appends a line to the code with proper indentation.
    /// If text is null or empty, adds a blank line.
    /// </summary>
    public void AppendLine(string? text = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            _sb.AppendLine();
        }
        else
        {
            _sb.Append(new string(' ', _indentLevel * IndentSize));
            _sb.AppendLine(text);
        }
    }

    /// <summary>
    /// Appends text without a line ending.
    /// </summary>
    public void Append(string text)
    {
        _sb.Append(text);
    }

    /// <summary>
    /// Increases the indentation level.
    /// </summary>
    public void IncrementIndent() => _indentLevel++;

    /// <summary>
    /// Decreases the indentation level (does not go below 0).
    /// </summary>
    public void DecrementIndent() => _indentLevel = Math.Max(0, _indentLevel - 1);

    /// <summary>
    /// Appends a line with header text and opens a block with '{'.
    /// </summary>
    public void BeginBlock(string header)
    {
        AppendLine(header);
        AppendLine("{");
        IncrementIndent();
    }

    /// <summary>
    /// Closes a block with '}' and decreases indentation.
    /// </summary>
    public void EndBlock()
    {
        DecrementIndent();
        AppendLine("}");
    }

    /// <summary>
    /// Returns the complete generated code as a string.
    /// </summary>
    public override string ToString() => _sb.ToString();
}
