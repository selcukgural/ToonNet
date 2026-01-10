using System.Text;
using ToonNet.Core.Models;

namespace ToonNet.Core.Parsing;

/// <summary>
/// Tokenizes TOON format input into a stream of tokens.
/// </summary>
/// <remarks>
/// This is an internal implementation detail. Users should use <see cref="ToonParser"/> instead.
/// </remarks>
internal sealed class ToonLexer
{
    private readonly ReadOnlyMemory<char> _input;
    private int _position;
    private int _line = 1;
    private int _column = 1;

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
    /// Creates a new lexer for the specified input memory.
    /// </summary>
    /// <param name="input">The TOON format memory to tokenize.</param>
    public ToonLexer(ReadOnlyMemory<char> input)
    {
        _input = input;
    }

    /// <summary>
    /// Tokenizes the input into a list of TOON tokens.
    /// </summary>
    /// <returns>A list of tokens representing the input.</returns>
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

    private ToonToken ReadIndentation()
    {
        var startColumn = _column;
        var count = 0;
        
        while (Peek() == ' ')
        {
            Advance();
            count++;
        }
        
        return new ToonToken(
            ToonTokenType.Indent, 
            new string(' ', count).AsMemory(), 
            _line, 
            startColumn);
    }

    /// <summary>
    /// Reads an array length token (e.g., "[3]").
    /// </summary>
    /// <returns>An array length token.</returns>
    /// <exception cref="ToonParseException">Thrown when array length is not properly terminated.</exception>
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
    /// Reads a quoted string token and determines if it's a key or value.
    /// </summary>
    /// <returns>A key or quoted string token depending on what follows.</returns>
    private ToonToken ReadQuotedStringToken()
    {
        var startLine = _line;
        var startColumn = _column;
        var stringValue = ReadQuotedStringValue();
        
        // Check what comes after the quoted string to determine if it's a key
        SkipWhitespace();
        var next = Peek();
        var isKey = next is ':' or '[' or '{';
        
        return new ToonToken(
            isKey ? ToonTokenType.Key : ToonTokenType.QuotedString,
            stringValue.AsMemory(),
            startLine,
            startColumn);
    }

    /// <summary>
    /// Reads the content of a quoted string (without determining token type).
    /// </summary>
    /// <returns>The unescaped string content.</returns>
    /// <exception cref="ToonParseException">Thrown when string is not properly terminated.</exception>
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
                    throw new ToonParseException("Unterminated string", _line, _column);
                
                var escaped = Peek();
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    '"' => '"',
                    '\\' => '\\',
                    _ => escaped
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
        
        return new ToonToken(
            isKey ? ToonTokenType.Key : ToonTokenType.Value, 
            value, 
            startLine, 
            startColumn);
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd() && Peek() == ' ')
        {
            Advance();
        }
    }

    private bool PreviousWasNewline()
    {
        if (_position == 0)
        {
            return true;
        }
        
        var prev = _input.Span[_position - 1];
        return prev is '\n' or '\r';
    }

    private char Peek()
    {
        return IsAtEnd() ? '\0' : _input.Span[_position];
    }

    private char PeekNext()
    {
        return _position + 1 >= _input.Length ? '\0' : _input.Span[_position + 1];
    }

    private void Advance()
    {
        if (IsAtEnd())
        {
            return;
        }

        _position++;
        _column++;
    }

    private bool IsAtEnd()
    {
        return _position >= _input.Length;
    }
}
