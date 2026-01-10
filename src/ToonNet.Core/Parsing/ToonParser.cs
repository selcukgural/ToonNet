using ToonNet.Core.Models;

namespace ToonNet.Core.Parsing;

/// <summary>
/// Parses TOON tokens into a document structure.
/// </summary>
public sealed class ToonParser(ToonOptions? options = null)
{
    private readonly List<ToonToken> _tokens = [];
    private readonly ToonOptions _options = options ?? ToonOptions.Default;
    private int _position;

    public ToonDocument Parse(string input)
    {
        var lexer = new ToonLexer(input);
        _tokens.Clear();
        _tokens.AddRange(lexer.Tokenize());
        _position = 0;

        var root = ParseValue(0);
        return new ToonDocument(root);
    }

    public ToonDocument Parse(List<ToonToken> tokens)
    {
        _tokens.Clear();
        _tokens.AddRange(tokens);
        _position = 0;

        var root = ParseValue(0);
        return new ToonDocument(root);
    }

    private ToonValue ParseValue(int indentLevel)
    {
        SkipNewlines();

        if (IsAtEnd())
            return new ToonObject();

        var token = Peek();

        return token.Type switch
        {
            // Check if this is an object (has key-value pairs)
            ToonTokenType.Key or ToonTokenType.Indent => ParseObject(indentLevel),
            // List item
            ToonTokenType.ListItem => ParseList(indentLevel),
            // Quoted string - always return as string
            ToonTokenType.QuotedString => new ToonString(Advance().Value.ToString()),
            // Simple value
            ToonTokenType.Value => ParsePrimitiveValue(Advance().Value),
            _                   => throw new ToonParseException($"Unexpected token: {token.Type}", token.Line, token.Column)
        };
    }

    private ToonObject ParseObject(int indentLevel)
    {
        var obj = new ToonObject();

        while (!IsAtEnd())
        {
            SkipNewlines();

            if (IsAtEnd())
            {
                break;
            }

            // Get and consume indent if present
            var currentIndent = 0;

            if (Peek().Type == ToonTokenType.Indent)
            {
                currentIndent = Peek().Value.Length;
                Advance(); // consume indent
            }

            // If we've decreased indent, we're done with this object
            if (currentIndent < indentLevel)
            {
                break;
            }

            // Skip if not at our indent level (shouldn't happen with proper input)
            if (currentIndent > indentLevel && indentLevel > 0)
            {
                throw new ToonParseException($"Unexpected indentation", Peek().Line, Peek().Column);
            }

            var keyToken = Peek();

            if (keyToken.Type != ToonTokenType.Key)
            {
                break;
            }

            Advance(); // consume key

            var key = keyToken.Value.ToString();

            // Check for array notation
            int? arrayLength = null;
            string[]? fieldNames = null;

            if (Peek().Type == ToonTokenType.ArrayLength)
            {
                var lengthToken = Advance();
                var lengthStr = lengthToken.Value.ToString().Trim('[', ']');

                if (int.TryParse(lengthStr, out var len))
                {
                    arrayLength = len;
                }
            }

            if (Peek().Type == ToonTokenType.ArrayFields)
            {
                var fieldsToken = Advance();
                var fieldsStr = fieldsToken.Value.ToString().Trim('{', '}');
                fieldNames = fieldsStr.Split(',').Select(f => f.Trim()).ToArray();
            }

            // Expect colon
            if (Peek().Type != ToonTokenType.Colon)
            {
                throw new ToonParseException($"Expected ':' after key '{key}'", Peek().Line, Peek().Column);
            }

            Advance(); // consume colon

            // Parse the value
            ToonValue value;

            SkipWhitespace();

            // If newline after colon, it's a nested object or array
            if (Peek().Type == ToonTokenType.Newline || IsAtEnd())
            {
                Advance(); // consume newline

                if (arrayLength.HasValue || fieldNames != null)
                {
                    // Tabular array
                    value = ParseTabularArray(indentLevel + _options.IndentSize, arrayLength, fieldNames);
                }
                else
                {
                    // Nested object
                    value = ParseValue(indentLevel + _options.IndentSize);
                }
            }
            else if (IsValueToken(Peek().Type))
            {
                // Inline values or primitive array
                if (arrayLength.HasValue)
                {
                    // Primitive array: tags[3]: a,b,c (tokenized as multiple Value and Comma tokens)
                    value = ParseInlinePrimitiveArray(arrayLength.Value);
                }
                else
                {
                    // Simple value
                    var valueToken = Advance();
                    if (valueToken.Type == ToonTokenType.QuotedString)
                    {
                        value = new ToonString(valueToken.Value.ToString());
                    }
                    else
                    {
                        value = ParsePrimitiveValue(valueToken.Value);
                    }
                }
            }
            else
            {
                throw new ToonParseException($"Expected value after ':'", Peek().Line, Peek().Column);
            }

            obj.Properties[key] = value;

            // Consume optional trailing newline
            if (Peek().Type == ToonTokenType.Newline)
            {
                Advance();
            }
        }

        return obj;
    }

    private ToonArray ParseTabularArray(int indentLevel, int? expectedLength, string[]? fieldNames)
    {
        var array = new ToonArray { FieldNames = fieldNames };

        while (!IsAtEnd())
        {
            SkipNewlines();

            if (IsAtEnd())
            {
                break;
            }

            // Get and consume indent if present
            var currentIndent = 0;

            if (Peek().Type == ToonTokenType.Indent)
            {
                currentIndent = Peek().Value.Length;
                Advance(); // consume indent
            }

            if (currentIndent < indentLevel)
            {
                break;
            }

            if (currentIndent > indentLevel)
            {
                throw new ToonParseException("Unexpected indentation in tabular array", Peek().Line, Peek().Column);
            }

            // Read row values (inline, separated by commas)
            if (IsValueToken(Peek().Type))
            {
                var rowValues = new List<ToonValue>();
                
                // Read first value
                var firstToken = Advance();
                rowValues.Add(firstToken.Type == ToonTokenType.QuotedString 
                    ? new ToonString(firstToken.Value.ToString())
                    : ParsePrimitiveValue(firstToken.Value));

                // Read the remaining values
                while (Peek().Type == ToonTokenType.Comma)
                {
                    Advance(); // consume comma

                    if (IsValueToken(Peek().Type))
                    {
                        var token = Advance();
                        rowValues.Add(token.Type == ToonTokenType.QuotedString
                            ? new ToonString(token.Value.ToString())
                            : ParsePrimitiveValue(token.Value));
                    }
                }

                if (fieldNames != null && rowValues.Count != fieldNames.Length)
                {
                    throw new ToonParseException($"Row has {rowValues.Count} values but expected {fieldNames.Length}", 0, 0);
                }

                // Convert to object if field names provided
                if (fieldNames != null)
                {
                    var rowObject = new ToonObject();

                    for (var i = 0; i < rowValues.Count; i++)
                    {
                        rowObject.Properties[fieldNames[i]] = rowValues[i];
                    }

                    array.Items.Add(rowObject);
                }
                else
                {
                    // Add values directly
                    foreach (var val in rowValues)
                    {
                        array.Items.Add(val);
                    }
                }
            }
            else
            {
                break;
            }

            if (Peek().Type == ToonTokenType.Newline)
            {
                Advance();
            }
        }

        if (!expectedLength.HasValue || array.Count == expectedLength.Value)
        {
            return array;
        }

        return _options.StrictMode
                   ? throw new ToonParseException($"Array length mismatch: expected {expectedLength.Value}, got {array.Count}", 0, 0)
                   : array;
    }

    private ToonArray ParseInlinePrimitiveArray(int expectedLength)
    {
        var values = new List<ToonValue>();

        // Read first value
        if (IsValueToken(Peek().Type))
        {
            var token = Advance();
            values.Add(token.Type == ToonTokenType.QuotedString
                ? new ToonString(token.Value.ToString())
                : ParsePrimitiveValue(token.Value));
        }

        // Read the remaining values (separated by commas)
        while (Peek().Type == ToonTokenType.Comma)
        {
            Advance(); // consume comma

            if (IsValueToken(Peek().Type))
            {
                var token = Advance();
                values.Add(token.Type == ToonTokenType.QuotedString
                    ? new ToonString(token.Value.ToString())
                    : ParsePrimitiveValue(token.Value));
            }
            else
            {
                break;
            }
        }

        return _options.StrictMode && values.Count != expectedLength
                   ? throw new ToonParseException($"Array length mismatch: expected {expectedLength}, got {values.Count}", 0, 0)
                   : new ToonArray(values);
    }

    private ToonArray ParseTabularArrayInlineRow(string[]? fieldNames)
    {
        var values = new List<ToonValue>();

        // Read first value
        if (IsValueToken(Peek().Type))
        {
            var token = Advance();
            values.Add(token.Type == ToonTokenType.QuotedString
                ? new ToonString(token.Value.ToString())
                : ParsePrimitiveValue(token.Value));
        }

        // Read the remaining values (separated by commas)
        while (Peek().Type == ToonTokenType.Comma)
        {
            Advance(); // consume comma

            if (IsValueToken(Peek().Type))
            {
                var token = Advance();
                values.Add(token.Type == ToonTokenType.QuotedString
                    ? new ToonString(token.Value.ToString())
                    : ParsePrimitiveValue(token.Value));
            }
            else
            {
                break;
            }
        }

        return fieldNames != null && values.Count != fieldNames.Length
                   ? throw new ToonParseException($"Row has {values.Count} values but expected {fieldNames.Length}", 0, 0)
                   : new ToonArray(values);
    }

    private ToonArray ParseList(int indentLevel)
    {
        var array = new ToonArray();

        while (!IsAtEnd())
        {
            SkipNewlines();

            if (IsAtEnd())
            {
                break;
            }

            var currentIndent = GetCurrentIndent();

            if (currentIndent < indentLevel)
            {
                break;
            }

            if (Peek().Type == ToonTokenType.ListItem)
            {
                Advance(); // consume list marker

                if (IsValueToken(Peek().Type))
                {
                    var valueToken = Advance();
                    array.Items.Add(valueToken.Type == ToonTokenType.QuotedString
                        ? new ToonString(valueToken.Value.ToString())
                        : ParsePrimitiveValue(valueToken.Value));
                }
                else if (Peek().Type == ToonTokenType.Newline)
                {
                    // Nested object
                    Advance();
                    array.Items.Add(ParseValue(indentLevel + _options.IndentSize));
                }
            }
            else
            {
                break;
            }

            if (Peek().Type == ToonTokenType.Newline)
            {
                Advance();
            }
        }

        return array;
    }

    private static ToonValue ParsePrimitiveValue(ReadOnlyMemory<char> valueMemory)
    {
        var value = valueMemory.ToString().Trim();

        return value switch
        {
            "null"  => ToonNull.Instance,
            "true"  => new ToonBoolean(true),
            "false" => new ToonBoolean(false),
            _       => double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var number) ? new ToonNumber(number) : new ToonString(value)
        };
    }

    private int GetCurrentIndent()
    {
        return IsAtEnd() || Peek().Type != ToonTokenType.Indent ? 0 : Peek().Value.Length;
    }

    private void SkipNewlines()
    {
        while (!IsAtEnd() && Peek().Type == ToonTokenType.Newline)
        {
            Advance();
        }
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd() && Peek().Type == ToonTokenType.Indent)
        {
            Advance();
        }
    }

    private bool IsValueToken(ToonTokenType type)
    {
        return type is ToonTokenType.Value or ToonTokenType.QuotedString;
    }

    private ToonToken Peek()
    {
        return _position < _tokens.Count ? _tokens[_position] : new ToonToken(ToonTokenType.EndOfInput, ReadOnlyMemory<char>.Empty, 0, 0);
    }

    private ToonToken Advance()
    {
        var token = Peek();

        if (_position < _tokens.Count)
            _position++;
        return token;
    }

    private bool IsAtEnd()
    {
        return _position >= _tokens.Count || Peek().Type == ToonTokenType.EndOfInput;
    }
}