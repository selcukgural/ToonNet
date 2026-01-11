using System.Text;
using ToonNet.Core.Models;

namespace ToonNet.Core.Parsing;

/// <summary>
/// Tokenizes TOON format input into a stream of tokens.
/// </summary>
/// <remarks>
/// This is an internal implementation detail. Users should use <see cref="ToonParser" /> instead.
/// </remarks>
internal sealed class ToonLexer
{
    private readonly ReadOnlyMemory<char> _input;
    private int _column = 1;
    private int _line = 1;
    private int _position;

    /// <summary>
    /// Creates a new lexer for the specified input string.
    /// </summary>
    /// <param name="input">The TOON format string to tokenize.</param>
    /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
    public ToonLexer(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        _input = input.AsMemory();
    }

    /// <summary>
    /// Creates a new lexer for processing TOON format input.
    /// </summary>
    /// <remarks>
    /// Represents a low-level implementation for tokenizing TOON format data.
    /// For higher-level parsing, refer to <see cref="ToonParser" />.
    /// </remarks>
    public ToonLexer(ReadOnlyMemory<char> input)
    {
        _input = input;
    }

    /// <summary>
    /// Tokenizes the input into a list of TOON tokens.
    /// </summary>
    /// <returns>A list of tokens representing the parsed input, including an end-of-input token.</returns>
    public List<ToonToken> Tokenize()
    {
        var tokens = new List<ToonToken>();

        while (!IsAtEnd())
        {
            var token = NextToken();

            if (token.Type != ToonTokenType.EndOfInput)
            {
                tokens.Add(token);
            }
        }

        tokens.Add(new ToonToken(ToonTokenType.EndOfInput, ReadOnlyMemory<char>.Empty, _line, _column));
        return tokens;
    }

    /// <summary>
    /// Reads the next token from the input stream.
    /// </summary>
    /// <returns>An instance of <see cref="ToonToken"/> representing the next token in the stream.</returns>
    /// <exception cref="ToonParseException">Thrown when an unexpected character or invalid sequence is encountered during parsing.</exception>
    private ToonToken NextToken()
    {
        while (true)
        {
            if (IsAtEnd())
            {
                return new ToonToken(ToonTokenType.EndOfInput, ReadOnlyMemory<char>.Empty, _line, _column);
            }

            var startLine = _line;
            var startColumn = _column;
            var current = Peek();

            switch (current)
            {
                // Newline
                case '\n':
                    Advance();
                    _line++;
                    _column = 1;
                    return new ToonToken(ToonTokenType.Newline, "\n".AsMemory(), startLine, startColumn);
                // Skip \r (handle \r\n as just \n)
                case '\r':
                {
                    Advance();

                    if (Peek() != '\n')
                    {
                        continue;
                    }

                    Advance();

                    _line++;
                    _column = 1;

                    return new ToonToken(ToonTokenType.Newline, "\n".AsMemory(), startLine, startColumn);
                }
                // Indentation (spaces at start of line or after newline)
                case ' ' when _column == 1 || PreviousWasNewline():
                    return ReadIndentation();
                // List item marker (- followed by space)
                case '-' when PeekNext() == ' ':
                    Advance(); // -
                    Advance(); // space
                    return new ToonToken(ToonTokenType.ListItem, "- ".AsMemory(), startLine, startColumn);
                // Colon
                case ':':
                    Advance();
                    return new ToonToken(ToonTokenType.Colon, ":".AsMemory(), startLine, startColumn);
                // Comma
                case ',':
                    Advance();
                    return new ToonToken(ToonTokenType.Comma, ",".AsMemory(), startLine, startColumn);
                // Array length or fields (after a key)
                case '[':
                    return ReadArrayLength();
                case '{':
                    return ReadArrayFields();
                // Quoted string - could be key or value
                case '"':
                    return ReadQuotedStringToken();
                default:
                    // Key or value
                    return ReadKeyOrValue();
            }
        }
    }

    /// <summary>
    /// Reads an indentation (sequence of spaces) from the input string at the current position.
    /// </summary>
    /// <returns>A token representing the indentation, including the number of spaces and their position in the input.</returns>
    private ToonToken ReadIndentation()
    {
        var startColumn = _column;
        var count = 0;

        while (Peek() == ' ')
        {
            Advance();
            count++;
        }

        return new ToonToken(ToonTokenType.Indent, new string(' ', count).AsMemory(), _line, startColumn);
    }

    /// <summary>
    /// Reads an array length token (e.g., "[3]") from the input.
    /// </summary>
    /// <returns>A token representing the array length.</returns>
    /// <exception cref="ToonParseException">
    /// Thrown when the array length is not properly terminated with a closing bracket.
    /// </exception>
    private ToonToken ReadArrayLength()
    {
        var startLine = _line;
        var startColumn = _column;
        var startPos = _position;

        Advance(); // [

        while (!IsAtEnd() && Peek() != ']')
        {
            Advance();
        }

        if (IsAtEnd())
        {
            throw new ToonParseException("Unterminated array length", _line, _column);
        }

        Advance(); // ]

        var length = _position - startPos;
        var value = _input.Slice(startPos, length);

        return new ToonToken(ToonTokenType.ArrayLength, value, startLine, startColumn);
    }

    /// <summary>
    /// Reads the fields of an array from the input and returns a token representing the content.
    /// </summary>
    /// <returns>
    /// A <see cref="ToonToken"/> of type <c>ArrayFields</c> containing the array fields content
    /// as well as the starting line and column of the token in the input.
    /// </returns>
    /// <exception cref="ToonParseException">
    /// Thrown when the array fields are not properly terminated in the input.
    /// </exception>
    private ToonToken ReadArrayFields()
    {
        var startLine = _line;
        var startColumn = _column;
        var startPos = _position;

        Advance(); // {

        while (!IsAtEnd() && Peek() != '}')
        {
            Advance();
        }

        if (IsAtEnd())
        {
            throw new ToonParseException("Unterminated array fields", _line, _column);
        }

        Advance(); // }

        var length = _position - startPos;
        var value = _input.Slice(startPos, length);

        return new ToonToken(ToonTokenType.ArrayFields, value, startLine, startColumn);
    }

    /// <summary>
    /// Reads a quoted string token and determines if it represents a key or value based on the subsequent content.
    /// </summary>
    /// <returns>A token of type Key or QuotedString, depending on the context of the parsed content.</returns>
    private ToonToken ReadQuotedStringToken()
    {
        var startLine = _line;
        var startColumn = _column;
        var stringValue = ReadQuotedStringValue();

        // Check what comes after the quoted string to determine if it's a key
        SkipWhitespace();
        var next = Peek();
        var isKey = next is ':' or '[' or '{';

        return new ToonToken(isKey ? ToonTokenType.Key : ToonTokenType.QuotedString, stringValue.AsMemory(), startLine, startColumn);
    }

    /// <summary>
    /// Reads the content of a quoted string from the input while handling escape sequences.
    /// </summary>
    /// <returns>The unescaped content of the quoted string.</returns>
    /// <exception cref="ToonParseException">
    /// Thrown when a quoted string is not properly terminated in the input.
    /// </exception>
    private string ReadQuotedStringValue()
    {
        var sb = new StringBuilder();

        Advance(); // opening "

        while (!IsAtEnd() && Peek() != '"')
        {
            var ch = Peek();

            if (ch == '\\')
            {
                Advance();

                if (IsAtEnd())
                {
                    throw new ToonParseException("Unterminated string", _line, _column);
                }

                var escaped = Peek();

                sb.Append(escaped switch
                {
                    'n'  => '\n',
                    't'  => '\t',
                    'r'  => '\r',
                    '"'  => '"',
                    '\\' => '\\',
                    _    => escaped
                });
            }
            else
            {
                sb.Append(ch);
            }

            Advance();
        }

        if (IsAtEnd())
        {
            throw new ToonParseException("Unterminated string", _line, _column);
        }

        Advance(); // closing "

        return sb.ToString();
    }


    /// <summary>
    /// Reads the next token from the input, identifying it as either a key or a value.
    /// </summary>
    /// <returns>
    /// A <see cref="ToonToken"/> representing either a key or a value, based on the parsed input.
    /// </returns>
    private ToonToken ReadKeyOrValue()
    {
        var startLine = _line;
        var startColumn = _column;

        // Skip leading whitespace
        while (!IsAtEnd() && Peek() == ' ')
        {
            Advance();
            startColumn = _column;
        }

        // If we hit a quoted string after skipping whitespace, parse it as a quoted string token
        if (Peek() == '"')
        {
            return ReadQuotedStringToken();
        }

        var startPos = _position;

        while (!IsAtEnd())
        {
            var ch = Peek();

            // Stop at structural characters
            if (ch is ':' or ',' or '\n' or '\r' or '[' or '{' or ']' or '}' or '"')
            {
                break;
            }

            Advance();
        }

        var length = _position - startPos;
        var value = _input.Slice(startPos, length);

        // Trim leading and trailing spaces
        var span = value.Span;
        var trimStart = 0;

        while (trimStart < span.Length && span[trimStart] == ' ')
        {
            trimStart++;
        }

        var trimEnd = span.Length;

        while (trimEnd > trimStart && span[trimEnd - 1] == ' ')
        {
            trimEnd--;
        }

        value = value.Slice(trimStart, trimEnd - trimStart);

        // Determine if this is a key (will be followed by: or [ or {)
        SkipWhitespace();
        var next = Peek();
        var isKey = next is ':' or '[' or '{';

        return new ToonToken(isKey ? ToonTokenType.Key : ToonTokenType.Value, value, startLine, startColumn);
    }

    /// <summary>
    /// Skips whitespace characters (spaces only, not including newlines) in the input.
    /// </summary>
    /// <remarks>
    /// Moves the position forward until a non-whitespace character is encountered or the end of input is reached.
    /// </remarks>
    private void SkipWhitespace()
    {
        while (!IsAtEnd() && Peek() == ' ')
        {
            Advance();
        }
    }

    /// <summary>
    /// Checks if the previous character was a newline.
    /// </summary>
    /// <returns>
    /// True if the previous character was a newline, or if the current position is at the start of the input;
    /// otherwise, false.
    /// </returns>
    private bool PreviousWasNewline()
    {
        if (_position == 0)
        {
            return true;
        }

        var prev = _input.Span[_position - 1];
        return prev is '\n' or '\r';
    }

    /// <summary>
    /// Peeks at the current character in the input stream without advancing the position.
    /// </summary>
    /// <returns>The current character, or '\0' if the end of the input stream is reached.</returns>
    private char Peek()
    {
        return IsAtEnd() ? '\0' : _input.Span[_position];
    }

    /// <summary>
    /// Peeks at the character following the current position without advancing the lexer.
    /// </summary>
    /// <returns>The character after the current position, or '\0' if beyond the end of the input.</returns>
    private char PeekNext()
    {
        return _position + 1 >= _input.Length ? '\0' : _input.Span[_position + 1];
    }

    /// <summary>
    /// Advances the current position within the input string by one character.
    /// </summary>
    /// <remarks>
    /// Updates the internal position and column counters.
    /// No operation is performed if the end of the input has been reached.
    /// </remarks>
    private void Advance()
    {
        if (IsAtEnd())
        {
            return;
        }

        _position++;
        _column++;
    }

    /// <summary>
    /// Checks if the lexer has reached the end of the input.
    /// </summary>
    /// <returns>True if the current position is at or beyond the end of the input; otherwise, false.</returns>
    private bool IsAtEnd()
    {
        return _position >= _input.Length;
    }
}