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

    /// <summary>
    /// Parses a TOON format string into a document.
    /// </summary>
    /// <param name="input">The TOON format string to parse.</param>
    /// <returns>A ToonDocument representing the parsed input.</returns>
    /// <exception cref="ToonParseException">Thrown when the input is invalid.</exception>
    public ToonDocument Parse(string input)
    {
        var lexer = new ToonLexer(input);
        _tokens.Clear();
        _tokens.AddRange(lexer.Tokenize());
        _position = 0;

        var root = ParseValue(0);
        return new ToonDocument(root);
    }

    /// <summary>
    /// Parses a list of TOON tokens into a document.
    /// </summary>
    /// <param name="tokens">The tokens to parse.</param>
    /// <returns>A ToonDocument representing the parsed tokens.</returns>
    /// <exception cref="ToonParseException">Thrown when the tokens are invalid.</exception>
    internal ToonDocument Parse(List<ToonToken> tokens)
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
        {
            return new ToonObject();
        }

        var token = Peek();
        
        // STEP 1.2: Detect list items (Indent followed by ListItem)
        // This handles: key:\n  - item1\n  - item2
        if (token.Type == ToonTokenType.Indent)
        {
            var nextIdx = _position + 1;
            var hasListItems = nextIdx < _tokens.Count && _tokens[nextIdx].Type == ToonTokenType.ListItem;
            
            if (hasListItems)
            {
                // It's a list of items - parse as array
                return ParseList(indentLevel);
            }
        }

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

    /// <summary>
    /// Parses an object from the token stream.
    /// </summary>
    /// <param name="indentLevel">The current indentation level.</param>
    /// <returns>A ToonObject containing the parsed key-value pairs.</returns>
    /// <exception cref="ToonParseException">Thrown when the object structure is invalid.</exception>
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

                // Check if this is actually a list by peeking ahead for list items
                var isListArray = false;
                if (arrayLength.HasValue || fieldNames != null)
                {
                    // Peek to see if next content is a list (Indent followed by ListItem)
                    var peekPos = _position;
                    while (peekPos < _tokens.Count && _tokens[peekPos].Type == ToonTokenType.Newline)
                    {
                        peekPos++;
                    }
                    
                    if (peekPos < _tokens.Count && _tokens[peekPos].Type == ToonTokenType.Indent)
                    {
                        var nextPos = peekPos + 1;
                        if (nextPos < _tokens.Count && _tokens[nextPos].Type == ToonTokenType.ListItem)
                        {
                            isListArray = true;
                        }
                    }
                }

                if ((arrayLength.HasValue || fieldNames != null) && !isListArray)
                {
                    // Tabular array
                    value = ParseTabularArray(indentLevel + _options.IndentSize, arrayLength, fieldNames);
                }
                else
                {
                    // Nested object or list array
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
                    value = ParseValueToken(valueToken);
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
                rowValues.Add(ParseValueToken(firstToken));

                // Read the remaining values
                while (Peek().Type == ToonTokenType.Comma)
                {
                    Advance(); // consume comma

                    if (IsValueToken(Peek().Type))
                    {
                        var token = Advance();
                        rowValues.Add(ParseValueToken(token));
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
            values.Add(ParseValueToken(token));
        }

        // Read the remaining values (separated by commas)
        while (Peek().Type == ToonTokenType.Comma)
        {
            Advance(); // consume comma

            if (IsValueToken(Peek().Type))
            {
                var token = Advance();
                values.Add(ParseValueToken(token));
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
            values.Add(ParseValueToken(token));
        }

        // Read the remaining values (separated by commas)
        while (Peek().Type == ToonTokenType.Comma)
        {
            Advance(); // consume comma

            if (IsValueToken(Peek().Type))
            {
                var token = Advance();
                values.Add(ParseValueToken(token));
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

            // Check for indentation
            var currentIndent = GetCurrentIndent();

            if (currentIndent < indentLevel)
            {
                // Dedented - end of list
                break;
            }

            // Consume indent token if present
            if (Peek().Type == ToonTokenType.Indent)
            {
                Advance();
            }

            // Now expect a list item (- marker)
            if (Peek().Type == ToonTokenType.ListItem)
            {
                Advance(); // consume list marker (-)

                // STEP 1.4: Parse the item (scalar value, inline first field, or nested object)
                if (IsValueToken(Peek().Type))
                {
                    // Scalar list item: - value
                    var valueToken = Advance();
                    array.Items.Add(ParseValueToken(valueToken));
                }
                else if (Peek().Type == ToonTokenType.Key)
                {
                    // Inline first field: - key: value
                    // Parse as object with first field inline, rest indented
                    var itemObject = new ToonObject();
                    
                    // Parse inline first field
                    var firstKey = Advance().Value.ToString();
                    
                    if (Peek().Type != ToonTokenType.Colon)
                    {
                        throw new ToonParseException($"Expected ':' after key '{firstKey}'", Peek().Line, Peek().Column);
                    }
                    
                    Advance(); // consume colon
                    SkipWhitespace();
                    
                    // Parse first value
                    if (IsValueToken(Peek().Type))
                    {
                        var valueToken = Advance();
                        itemObject.Properties[firstKey] = ParseValueToken(valueToken);
                    }
                    else
                    {
                        throw new ToonParseException($"Expected value after ':' for key '{firstKey}'", Peek().Line, Peek().Column);
                    }
                    
                    // Consume trailing newline
                    if (Peek().Type == ToonTokenType.Newline)
                    {
                        Advance();
                    }
                    
                    // Parse remaining fields at higher indentation
                    while (!IsAtEnd())
                    {
                        SkipNewlines();
                        
                        if (IsAtEnd())
                            break;
                        
                        // Check indentation
                        if (Peek().Type != ToonTokenType.Indent)
                        {
                            // No indent token - we're back at list level or less
                            break;
                        }
                        
                        var propIndent = Peek().Value.Length;
                        
                        // If dedented to list level or below, stop parsing this object
                        if (propIndent <= indentLevel)
                        {
                            break;
                        }
                        
                        Advance(); // consume indent
                        
                        // Parse key-value pair
                        if (Peek().Type == ToonTokenType.Key)
                        {
                            var key = Advance().Value.ToString();
                            
                            // Check for array notation (same as ParseObject)
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
                            
                            if (Peek().Type != ToonTokenType.Colon)
                            {
                                throw new ToonParseException($"Expected ':' after key '{key}'", Peek().Line, Peek().Column);
                            }
                            
                            Advance(); // consume colon
                            SkipWhitespace();
                            
                            // Parse value
                            ToonValue value;
                            
                            // Check if value is on next line (nested object/array)
                            if (Peek().Type == ToonTokenType.Newline)
                            {
                                Advance(); // consume newline
                                
                                // Determine if this is a tabular array or nested structure
                                if ((arrayLength.HasValue || fieldNames != null))
                                {
                                    // Check if it's actually a list array by peeking ahead
                                    var isListArray = false;
                                    var peekPos = _position;
                                    while (peekPos < _tokens.Count && _tokens[peekPos].Type == ToonTokenType.Newline)
                                    {
                                        peekPos++;
                                    }
                                    
                                    if (peekPos < _tokens.Count && _tokens[peekPos].Type == ToonTokenType.Indent)
                                    {
                                        var nextPos = peekPos + 1;
                                        if (nextPos < _tokens.Count && _tokens[nextPos].Type == ToonTokenType.ListItem)
                                        {
                                            isListArray = true;
                                        }
                                    }
                                    
                                    if (!isListArray)
                                    {
                                        // Tabular array
                                        value = ParseTabularArray(propIndent + _options.IndentSize, arrayLength, fieldNames);
                                    }
                                    else
                                    {
                                        // List array
                                        value = ParseValue(propIndent + _options.IndentSize);
                                    }
                                }
                                else
                                {
                                    // Nested object or array
                                    value = ParseValue(propIndent + _options.IndentSize);
                                }
                            }
                            else if (IsValueToken(Peek().Type))
                            {
                                // Inline value or inline array
                                if (arrayLength.HasValue)
                                {
                                    // Inline primitive array: roles[2]: admin, user
                                    value = ParseInlinePrimitiveArray(arrayLength.Value);
                                }
                                else
                                {
                                    var valueToken = Advance();
                                    value = ParseValueToken(valueToken);
                                }
                            }
                            else
                            {
                                throw new ToonParseException($"Expected value after ':'", Peek().Line, Peek().Column);
                            }
                            
                            itemObject.Properties[key] = value;
                            
                            if (Peek().Type == ToonTokenType.Newline)
                            {
                                Advance();
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    
                    array.Items.Add(itemObject);
                }
                else if (Peek().Type == ToonTokenType.Newline)
                {
                    // Object list item: - \n properties
                    Advance(); // consume newline
                    
                    // Parse nested object properties at higher indentation
                    var itemObject = new ToonObject();
                    
                    while (!IsAtEnd())
                    {
                        SkipNewlines();
                        
                        if (IsAtEnd())
                            break;
                        
                        // Check indentation
                        if (Peek().Type != ToonTokenType.Indent)
                        {
                            // No indent token - we're back at list level or less
                            break;
                        }
                        
                        var propIndent = Peek().Value.Length;
                        
                        // If dedented to list level or below, stop parsing this object
                        if (propIndent <= indentLevel)
                        {
                            break;
                        }
                        
                        Advance(); // consume indent
                        
                        // Parse key-value pair for this object
                        if (Peek().Type == ToonTokenType.Key)
                        {
                            var key = Advance().Value.ToString();
                            
                            if (Peek().Type != ToonTokenType.Colon)
                            {
                                throw new ToonParseException($"Expected ':' after key '{key}'", Peek().Line, Peek().Column);
                            }
                            
                            Advance(); // consume colon
                            SkipWhitespace();
                            
                            // Parse value
                            ToonValue value;
                            if (IsValueToken(Peek().Type))
                            {
                                var valueToken = Advance();
                                value = ParseValueToken(valueToken);
                            }
                            else
                            {
                                throw new ToonParseException($"Expected value after ':'", Peek().Line, Peek().Column);
                            }
                            
                            itemObject.Properties[key] = value;
                            
                            if (Peek().Type == ToonTokenType.Newline)
                            {
                                Advance();
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    
                    array.Items.Add(itemObject);
                }
            }
            else
            {
                // Not a list item - end of list
                break;
            }

            // Consume trailing newline
            if (Peek().Type == ToonTokenType.Newline)
            {
                Advance();
            }
        }

        return array;
    }

    /// <summary>
    /// Parses a value token (either quoted string or primitive).
    /// </summary>
    /// <param name="token">The token to parse.</param>
    /// <returns>A ToonValue representing the token.</returns>
    private static ToonValue ParseValueToken(ToonToken token)
    {
        return token.Type == ToonTokenType.QuotedString
            ? new ToonString(token.Value.ToString())
            : ParsePrimitiveValue(token.Value);
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