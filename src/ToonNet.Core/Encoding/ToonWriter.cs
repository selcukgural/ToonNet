using System.Globalization;
using System.Runtime.CompilerServices;
using ToonNet.Core.Models;

namespace ToonNet.Core.Encoding;

/// <summary>
///     Where an array is written, which decides how an empty array is represented and whether the tabular form may be
///     used (spec §9.1-§9.3).
/// </summary>
internal enum ToonArrayPosition
{
    Root,
    Field,
    ListItem
}

/// <summary>
///     Writes the pieces of TOON text into a <see cref="ToonTextBuffer"/>: lines and indentation, keys, primitive
///     values and array headers, with the quoting, escaping and number rules of the spec (§2, §6, §7, §12).
/// </summary>
/// <remarks>
///     It holds no document structure: <see cref="ToonDomWriter"/> drives it from a <see cref="ToonValue"/> tree, and
///     the serializer drives it directly from .NET objects, so both produce the same text. It also tracks the nesting
///     depth for <see cref="ToonOptions.MaxDepth"/>. An instance writes one document and is not thread-safe.
/// </remarks>
internal sealed class ToonWriter(ToonTextBuffer buffer, ToonOptions options)
{
    private const string HexDigits = "0123456789abcdef";

    private readonly ToonTextBuffer _buffer = buffer;
    private bool _hasLine;

    public ToonOptions Options { get; } = options;

    public char Delimiter => Options.Delimiter;

    /// <summary>
    ///     The nesting depth of the current object or array; the root value is at depth 0 (a root array starts at -1
    ///     before it is entered).
    /// </summary>
    public int Nesting { get; set; }

    #region Structure

    /// <summary>
    ///     Starts a new line at the given depth. Lines are separated by LF and the document has no trailing newline
    ///     (spec §12).
    /// </summary>
    public void StartLine(int depth)
    {
        if (_hasLine)
        {
            _buffer.Append('\n');
        }

        _hasLine = true;
        _buffer.Append(' ', depth * Options.IndentSize);
    }

    /// <summary>
    ///     Enters the level of a nested object or array, enforcing <see cref="ToonOptions.MaxDepth"/> (the root value
    ///     is at depth 0, each object or array inside another one level deeper) and the available stack space.
    /// </summary>
    /// <exception cref="ToonEncodingException">Thrown when the document is nested too deeply.</exception>
    public void EnterNesting()
    {
        if (++Nesting > Options.ValidatedMaxDepth)
        {
            throw new ToonEncodingException($"Maximum depth of {Options.MaxDepth} exceeded");
        }

        if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            throw new ToonEncodingException("Document is nested too deeply to encode with the available stack space");
        }
    }

    public void ExitNesting() => Nesting--;

    /// <summary>
    ///     Checks the depth of an empty object or array, which has no content that would enter its level.
    /// </summary>
    public void CheckEmptyNesting()
    {
        EnterNesting();
        Nesting--;
    }

    public void Write(char value) => _buffer.Append(value);

    public void Write(string value) => _buffer.Append(value);

    public void WriteDelimiter() => _buffer.Append(Options.Delimiter);

    /// <summary>
    ///     Writes <c>key[N]</c> (the key is optional) including the delimiter symbol for tab and pipe (spec §6).
    /// </summary>
    public void WriteArrayHeader(string? key, int count)
    {
        if (key != null)
        {
            WriteKey(key);
        }

        _buffer.Append('[');
        count.TryFormat(_buffer.GetSpan(11), out var length, default, CultureInfo.InvariantCulture);
        _buffer.Advance(length);

        if (Options.Delimiter != ',')
        {
            _buffer.Append(Options.Delimiter);
        }

        _buffer.Append(']');
    }

    /// <summary>
    ///     Writes the field list of a tabular array header, <c>{a,b}:</c> (spec §9.3).
    /// </summary>
    public void WriteFieldList(ReadOnlySpan<string> fields)
    {
        _buffer.Append('{');

        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0)
            {
                _buffer.Append(Options.Delimiter);
            }

            WriteKey(fields[i]);
        }

        _buffer.Append("}:");
    }

    #endregion

    #region Keys and primitives

    /// <summary>
    ///     Writes a key or tabular field name, quoting it unless it matches <c>^[A-Za-z_][A-Za-z0-9_.]*$</c> (spec §7.3).
    /// </summary>
    public void WriteKey(string key)
    {
        if (IsSafeUnquotedKey(key))
        {
            _buffer.Append(key);
        }
        else
        {
            WriteQuoted(key);
        }
    }

    /// <summary>
    ///     Writes a primitive value. NaN and infinities are written as null (spec §3).
    /// </summary>
    /// <exception cref="ToonEncodingException">Thrown for objects, arrays and unknown <see cref="ToonValue"/> subclasses.</exception>
    public void WritePrimitive(ToonValue? value)
    {
        switch (value)
        {
            case null or ToonNull:
                WriteNull();
                break;
            case ToonBoolean b:
                WriteBoolean(b.Value);
                break;
            case ToonNumber n:
                WriteNumber(n);
                break;
            case ToonString s:
                WriteString(s.Value);
                break;
            default:
                throw new ToonEncodingException($"Unsupported TOON value type: {value.GetType().Name}");
        }
    }

    public void WriteNull() => _buffer.Append("null");

    public void WriteBoolean(bool value) => _buffer.Append(value ? "true" : "false");

    public void WriteNumber(ToonNumber number)
    {
        if (ToonNumberFormatter.TryFormat(number, _buffer.GetSpan(ToonNumberFormatter.MaxLength), out var written))
        {
            _buffer.Advance(written);
        }
        else
        {
            WriteNull();
        }
    }

    /// <summary>Writes an integer like a <see cref="ToonNumber"/> created from it.</summary>
    public void WriteInteger(long value)
    {
        value.TryFormat(_buffer.GetSpan(20), out var written, default, CultureInfo.InvariantCulture);
        _buffer.Advance(written);
    }

    /// <summary>Writes a decimal like a <see cref="ToonNumber"/> created from it.</summary>
    public void WriteDecimal(decimal value)
    {
        _buffer.Advance(ToonNumberFormatter.FormatDecimal(value, _buffer.GetSpan(ToonNumberFormatter.MaxLength)));
    }

    /// <summary>Writes a double like a <see cref="ToonNumber"/> created from it (NaN and infinities as null).</summary>
    public void WriteDouble(double value)
    {
        if (ToonNumberFormatter.TryFormatDouble(value, _buffer.GetSpan(ToonNumberFormatter.MaxLength), out var written))
        {
            _buffer.Advance(written);
        }
        else
        {
            WriteNull();
        }
    }

    /// <summary>
    ///     Writes a string value, quoted and escaped when spec §7.2 requires it.
    /// </summary>
    public void WriteString(ReadOnlySpan<char> value)
    {
        if (NeedsQuoting(value, Options.Delimiter))
        {
            WriteQuoted(value);
        }
        else
        {
            _buffer.Append(value);
        }
    }

    /// <summary>
    ///     Writes a quoted string, escaped per spec §7.1: backslash, quote, LF, CR and tab use short escapes,
    ///     other control characters U+0000–U+001F use lowercase <c>\uXXXX</c>. Runs of characters that need no escape
    ///     are written in one piece.
    /// </summary>
    private void WriteQuoted(ReadOnlySpan<char> text)
    {
        var sb = _buffer;
        sb.Append('"');
        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            if (ch is not ('\\' or '"') && ch >= ' ')
            {
                continue;
            }

            sb.Append(text[start..i]);
            start = i + 1;

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
                default:
                    sb.Append("\\u00").Append(HexDigits[ch >> 4]).Append(HexDigits[ch & 0xF]);
                    break;
            }
        }

        sb.Append(text[start..]).Append('"');
    }

    private static bool IsSafeUnquotedKey(string key)
    {
        if (key.Length == 0 || !(char.IsAsciiLetter(key[0]) || key[0] == '_'))
        {
            return false;
        }

        foreach (var ch in key.AsSpan(1))
        {
            if (!(char.IsAsciiLetterOrDigit(ch) || ch is '_' or '.'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Checks the quoting rules of spec §7.2 for a string value.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="delimiter">The delimiter in effect for the value.</param>
    /// <returns><c>true</c> if the string must be quoted.</returns>
    internal static bool NeedsQuoting(ReadOnlySpan<char> value, char delimiter)
    {
        if (value.Length == 0 || char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1]))
        {
            return true;
        }

        if (value.SequenceEqual("true") || value.SequenceEqual("false") || value.SequenceEqual("null") || value[0] == '-' ||
            IsNumericLike(value))
        {
            return true;
        }

        foreach (var ch in value)
        {
            if (ch is ':' or '"' or '\\' or '[' or ']' or '{' or '}' || ch < ' ' || ch == delimiter)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Matches <c>/^-?\d+(?:\.\d+)?(?:e[+-]?\d+)?$/i</c> (spec §7.2).
    /// </summary>
    private static bool IsNumericLike(ReadOnlySpan<char> value)
    {
        var i = 0;

        if (value[i] == '-')
        {
            i++;
        }

        if (!SkipDigits(value, ref i))
        {
            return false;
        }

        if (i < value.Length && value[i] == '.')
        {
            i++;

            if (!SkipDigits(value, ref i))
            {
                return false;
            }
        }

        if (i < value.Length && value[i] is 'e' or 'E')
        {
            i++;

            if (i < value.Length && value[i] is '+' or '-')
            {
                i++;
            }

            if (!SkipDigits(value, ref i))
            {
                return false;
            }
        }

        return i == value.Length;

        static bool SkipDigits(ReadOnlySpan<char> text, ref int index)
        {
            var start = index;

            while (index < text.Length && char.IsAsciiDigit(text[index]))
            {
                index++;
            }

            return index > start;
        }
    }

    #endregion
}
