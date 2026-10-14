using System.Globalization;
using System.Text;

namespace ToonNet.Core;

/// <summary>
///     Puts text from a document (keys, rule values) into messages that reach logs and clients: escaped, so a line break
///     or a control character cannot forge a line or hide text, and shortened, so a long key cannot make a huge message.
/// </summary>
internal static class ToonErrorText
{
    /// <summary>The characters of a key or value a message shows; longer text ends with "...".</summary>
    public const int MaxLength = 64;

    /// <summary>Appends a path segment for a member: <c>.name</c> for a short identifier, else <c>['escaped']</c>.</summary>
    public static StringBuilder AppendMember(StringBuilder text, string name)
    {
        if (IsIdentifier(name))
        {
            return text.Append('.').Append(name);
        }

        text.Append("['");
        AppendEscaped(text, name, '\'');
        return text.Append("']");
    }

    /// <summary>The text in double quotes, escaped and shortened: <c>"a\nb"</c>.</summary>
    public static string Quote(string value)
    {
        var text = new StringBuilder(Math.Min(value.Length, MaxLength) + 8).Append('"');
        AppendEscaped(text, value, '"');
        return text.Append('"').ToString();
    }

    /// <summary>The text of a path from a caller (in an exception about it), with its control characters escaped.</summary>
    public static string EscapeControls(string text)
    {
        foreach (var c in text)
        {
            if (NeedsEscape(c))
            {
                var escaped = new StringBuilder(text.Length + 16);

                foreach (var ch in text)
                {
                    AppendChar(escaped, ch, quote: null);
                }

                return escaped.ToString();
            }
        }

        return text;
    }

    private static bool IsIdentifier(string name)
    {
        if (name.Length is 0 or > MaxLength || !(char.IsAsciiLetter(name[0]) || name[0] == '_'))
        {
            return false;
        }

        foreach (var c in name)
        {
            if (!(char.IsAsciiLetterOrDigit(c) || c == '_'))
            {
                return false;
            }
        }

        return true;
    }

    private static void AppendEscaped(StringBuilder text, string value, char quote)
    {
        var length = value.Length;

        if (length > MaxLength)
        {
            // Do not cut a surrogate pair in two
            length = char.IsHighSurrogate(value[MaxLength - 1]) ? MaxLength - 1 : MaxLength;
        }

        for (var i = 0; i < length; i++)
        {
            AppendChar(text, value[i], quote);
        }

        if (length < value.Length)
        {
            text.Append("...");
        }
    }

    /// <summary>
    ///     Control characters, line and paragraph separators, and the bidirectional controls that reorder how text is
    ///     shown.
    /// </summary>
    private static bool NeedsEscape(char c) =>
        c < 0x20 || c is (char)0x7F or (char)0x85 or (char)0x2028 or (char)0x2029 or (char)0x200E or (char)0x200F or (char)0x061C or
            (>= (char)0x202A and <= (char)0x202E) or (>= (char)0x2066 and <= (char)0x2069);

    private static void AppendChar(StringBuilder text, char c, char? quote)
    {
        switch (c)
        {
            case '\n':
                text.Append(@"\n");
                return;
            case '\r':
                text.Append(@"\r");
                return;
            case '\t':
                text.Append(@"\t");
                return;
            case '\\' when quote != null:
                text.Append(@"\\");
                return;
        }

        if (c == quote)
        {
            text.Append('\\').Append(c);
        }
        else if (NeedsEscape(c))
        {
            text.Append(@"\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
        }
        else
        {
            text.Append(c);
        }
    }
}
