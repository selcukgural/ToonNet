using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ToonNet.SourceGenerators.Utilities;

/// <summary>
/// Utility class designed to assist in constructing structured C# code with easy
/// management of indentation, formatting, and code block structures.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class CodeBuilder
{
    /// <summary>
    /// Represents the number of spaces used for a single level of indentation
    /// when formatting code in the <c>CodeBuilder</c>.
    /// </summary>
    private const int IndentSize = 4;

    /// <summary>
    /// Represents an internal <see cref="StringBuilder"/> instance used to construct and
    /// store the generated code as text. This field is utilized to manage and accumulate
    /// the output of various code-building operations, ensuring efficient string manipulations.
    /// </summary>
    private readonly StringBuilder _sb = new();

    /// <summary>
    /// Represents the current level of indentation applied to the generated code.
    /// Used internally to determine the number of spaces to prepend for each line
    /// based on the defined indent size.
    /// </summary>
    private int _indentLevel;

    /// <summary>
    /// Appends a line to the code with proper indentation.
    /// If the provided text is null or empty, a blank line is added instead.
    /// </summary>
    /// <param name="text">
    /// The text to append as a new line. If null or empty, a blank line is added.
    /// </param>
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
    /// <param name="text">
    /// The text to append to the code without adding a new line.
    /// </param>
    public void Append(string text)
    {
        _sb.Append(text);
    }

    /// <summary>
    /// Increases the current indentation level by one.
    /// </summary>
    public void IncrementIndent()
    {
        _indentLevel++;
    }

    /// <summary>
    /// Decreases the current indentation level, ensuring it does not go below zero.
    /// </summary>
    public void DecrementIndent()
    {
        _indentLevel = Math.Max(0, _indentLevel - 1);
    }

    /// <summary>
    /// Appends a line with the specified header text, opens a block by adding a '{',
    /// and adjusts the indentation for the subsequent lines inside the block.
    /// </summary>
    /// <param name="header">
    /// The text to be used as the block's header. This header is included
    /// at the beginning of the block and followed by an opening '{'.
    /// </param>
    public void BeginBlock(string header)
    {
        AppendLine(header);
        AppendLine("{");
        IncrementIndent();
    }

    /// <summary>
    /// Closes the current code block by appending the closing brace ('}')
    /// and decreases the indentation level.
    /// </summary>
    public void EndBlock()
    {
        DecrementIndent();
        AppendLine("}");
    }

    /// <summary>
    /// Returns the string representation of the current state of the code builder.
    /// </summary>
    /// <returns>
    /// A string containing the complete generated code.
    /// </returns>
    public override string ToString()
    {
        return _sb.ToString();
    }
}