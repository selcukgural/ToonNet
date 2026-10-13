using System.Text;

namespace ToonNet.Shared;

/// <summary>
///     The key quoting rules of TOON spec §7.3, compiled into ToonNet.Core and ToonNet.SourceGenerators so that the
///     encoder and generated code (which encodes keys at compile time) write the same keys.
/// </summary>
internal static class ToonKeys
{
    private const string HexDigits = "0123456789abcdef";

    /// <summary>
    ///     Whether <paramref name="text"/> holds a surrogate that is not part of a pair, which TOON cannot represent
    ///     (spec §3).
    /// </summary>
    public static bool HasUnpairedSurrogate(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                i++;
            }
            else if (char.IsSurrogate(text[i]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Whether a key can be written without quotes: it matches <c>^[A-Za-z_][A-Za-z0-9_.]*$</c>.
    /// </summary>
    public static bool IsSafeUnquoted(string key)
    {
        if (key.Length == 0 || !(IsAsciiLetter(key[0]) || key[0] == '_'))
        {
            return false;
        }

        for (var i = 1; i < key.Length; i++)
        {
            var ch = key[i];

            if (!(IsAsciiLetter(ch) || ch is >= '0' and <= '9' || ch is '_' or '.'))
            {
                return false;
            }
        }

        return true;

        static bool IsAsciiLetter(char ch) => ch is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
    }

    /// <summary>
    ///     The key as written in TOON text: unchanged when it is safe, otherwise quoted and escaped per spec §7.1
    ///     (backslash, quote, LF, CR and tab use short escapes, other control characters lowercase <c>\uXXXX</c>).
    /// </summary>
    public static string Encode(string key)
    {
        if (IsSafeUnquoted(key))
        {
            return key;
        }

        var sb = new StringBuilder(key.Length + 2).Append('"');

        foreach (var ch in key)
        {
            switch (ch)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case < ' ':
                    sb.Append("\\u00").Append(HexDigits[ch >> 4]).Append(HexDigits[ch & 0xF]);
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }

        return sb.Append('"').ToString();
    }
}
