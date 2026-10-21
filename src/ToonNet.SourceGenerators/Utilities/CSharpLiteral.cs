using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace ToonNet.SourceGenerators.Utilities;

/// <summary>
/// C# literals of strings and characters from user code (property names, default values) for generated source: quotes,
/// backslashes and line breaks escaped by Roslyn, and format characters (bidirectional controls, zero-width characters,
/// byte order marks) as <c>\uXXXX</c>, so the generated file cannot read differently from what it compiles to.
/// </summary>
internal static class CSharpLiteral
{
    public static string Of(string value) => EscapeFormatCharacters(SymbolDisplay.FormatLiteral(value, true));

    public static string Of(char value) => EscapeFormatCharacters(SymbolDisplay.FormatLiteral(value, true));

    private static string EscapeFormatCharacters(string literal)
    {
        var first = 0;

        while (first < literal.Length && CharUnicodeInfo.GetUnicodeCategory(literal[first]) != UnicodeCategory.Format)
        {
            first++;
        }

        if (first == literal.Length)
        {
            return literal;
        }

        var text = new StringBuilder(literal.Length + 16).Append(literal, 0, first);

        for (var i = first; i < literal.Length; i++)
        {
            var c = literal[i];

            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Format)
            {
                text.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
            }
            else
            {
                text.Append(c);
            }
        }

        return text.ToString();
    }
}
