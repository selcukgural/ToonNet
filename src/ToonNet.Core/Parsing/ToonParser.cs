using System.Globalization;
using ToonNet.Core.Models;

namespace ToonNet.Core.Parsing;

/// <summary>
///     Parses TOON tokens into a document structure.
/// </summary>
public sealed class ToonParser(ToonOptions? options = null)
{
    // Cached EndOfInput token to avoid repeated allocations
    private static readonly ToonToken EndOfInputToken = new(ToonTokenType.EndOfInput, ReadOnlyMemory<char>.Empty, 0, 0);

    private readonly ToonOptions _options = options ?? ToonOptions.Default;
    private readonly List<ToonToken> _tokens = [];
    private int _position;
    
    // Current token cache to avoid repeated Peek() calls at same position
    private ToonToken _currentToken;
    private int _currentTokenPosition = -1;

    #region Public API

    /// <summary>
    ///     Parses a TOON format string into a document.
    /// </summary>
    /// <param name="input">The TOON format string to parse.</param>
    /// <returns>A ToonDocument representing the parsed input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the input is invalid.</exception>
    public ToonDocument Parse(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var lexer = new ToonLexer(input);
        _tokens.Clear();
        _tokens.AddRange(lexer.Tokenize());
        _position = 0;
        _currentTokenPosition = -1; // Reset token cache

        var root = ParseValue(0);
        return new ToonDocument(root);
    }

    /// <summary>
    ///     Parses a list of TOON tokens into a document.
    /// </summary>
    /// <param name="tokens">The tokens to parse.</param>
    /// <returns>A ToonDocument representing the parsed tokens.</returns>
    /// <exception cref="ToonParseException">Thrown when the tokens are invalid.</exception>
    internal ToonDocument Parse(List<ToonToken> tokens)
    {
        _tokens.Clear();
        _tokens.AddRange(tokens);
        _position = 0;
        _currentTokenPosition = -1; // Reset token cache

        var root = ParseValue(0);
        return new ToonDocument(root);
    }

    /// <summary>
    ///     Asynchronously parses a TOON format string into a document.
    /// </summary>
    /// <param name="input">The TOON format string to parse.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous parse operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the input is invalid.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public async Task<ToonDocument> ParseAsync(string input, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Parse(input);
        }, cancellationToken);
    }

    /// <summary>
    ///     Asynchronously parses a TOON document from a file.
    /// </summary>
    /// <param name="filePath">The file path to read from.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous parse operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ToonParseException">Thrown when the input is invalid.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public async Task<ToonDocument> ParseFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        
        var input = await File.ReadAllTextAsync(filePath, System.Text.Encoding.UTF8, cancellationToken);
        return await ParseAsync(input, cancellationToken);
    }

    /// <summary>
    ///     Asynchronously parses a TOON document from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous parse operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the input is invalid.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public async Task<ToonDocument> ParseFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var input = await reader.ReadToEndAsync(cancellationToken);
        return await ParseAsync(input, cancellationToken);
    }

    #endregion

    #region Core Parsing Methods

    /// <summary>
    ///     Parses a value at the specified indentation level.
    /// </summary>
    /// <param name="indentLevel">The current indentation level.</param>
    /// <returns>The parsed ToonValue.</returns>
    /// <exception cref="ToonParseException">Thrown when an unexpected token is encountered.</exception>
    private ToonValue ParseValue(int indentLevel)
    {
        SkipNewlines();

        if (IsAtEnd())
        {
            return new ToonObject();
        }

        var token = Peek();
        var tokenType = token.Type;

        // STEP 1.2: Detect list items (Indent followed by ListItem)
        // This handles: key:\n  - item1\n  - item2
        if (tokenType != ToonTokenType.Indent)
        {
            // Fast path: Use bitmask check for value types
            if (tokenType.IsActualValue())
            {
                return tokenType == ToonTokenType.QuotedString 
                    ? new ToonString(new string(Advance().Value.Span)) 
                    : ParsePrimitiveValue(Advance().Value);
            }

            return tokenType switch
            {
                // Check if this is an object (has key-value pairs)
                ToonTokenType.Key => ParseObject(indentLevel),
                // List item
                ToonTokenType.ListItem => ParseList(indentLevel),
                // End of input - return an empty object
                ToonTokenType.EndOfInput => new ToonObject(),
                _                        => throw new ToonParseException($"Unexpected token: {tokenType}", token.Line, token.Column)
            };
        }

        var nextIdx = _position + 1;
        var hasListItems = nextIdx < _tokens.Count && _tokens[nextIdx].Type == ToonTokenType.ListItem;

        if (hasListItems)
        {
            // It's a list of items - parse as array
            return ParseList(indentLevel);
        }

        return token.Type switch
        {
            // Check if this is an object (has key-value pairs)
            ToonTokenType.Key or ToonTokenType.Indent => ParseObject(indentLevel),
            // List item
            ToonTokenType.ListItem => ParseList(indentLevel),
            // Quoted string - always return as string
            ToonTokenType.QuotedString => new ToonString(new string(Advance().Value.Span)),
            // Simple value
            ToonTokenType.Value => ParsePrimitiveValue(Advance().Value),
            // End of input - return an empty object
            ToonTokenType.EndOfInput => new ToonObject(),
            _                        => throw new ToonParseException($"Unexpected token: {token.Type}", token.Line, token.Column)
        };
    }

    /// <summary>
    ///     Parses an object from the token stream.
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

            // Get indent if present (but don't consume yet)
            var currentIndent = 0;
            var hasIndent = false;

            if (ExpectToken(ToonTokenType.Indent))
            {
                currentIndent = Peek().Value.Length;
                hasIndent = true;
            }

            // If we've decreased indent, we're done with this object (don't consume the indent)
            if (currentIndent < indentLevel)
            {
                break;
            }

            // Now consume the indent if we're continuing
            if (hasIndent)
            {
                Advance();
            }

            // Skip if not at our indent level (shouldn't happen with proper input)
            if (currentIndent > indentLevel && indentLevel > 0)
            {
                throw new ToonParseException("Unexpected indentation", Peek().Line, Peek().Column);
            }

            var keyToken = Peek();

            if (keyToken.Type != ToonTokenType.Key)
            {
                break;
            }

            Advance(); // consume key

            var key = new string(keyToken.Value.Span);

            // Parse array notation (if present)
            var (arrayLength, fieldNames) = ParseArrayNotation();

            // Expect colon
            if (Peek().Type != ToonTokenType.Colon)
            {
                throw new ToonParseException($"Expected ':' after key '{key}'", Peek().Line, Peek().Column);
            }

            Advance(); // consume colon

            // Parse the value after colon
            var value = ParseValueAfterColon(indentLevel, arrayLength, fieldNames);

            obj.Properties[key] = value;

            // Consume optional trailing newline
            if (Peek().Type == ToonTokenType.Newline)
            {
                Advance();
            }
        }

        return obj;
    }

    /// <summary>
    ///     Parses a tabular array (array of objects with field names).
    /// </summary>
    /// <param name="indentLevel">The current indentation level.</param>
    /// <param name="expectedLength">The expected number of array elements, if specified.</param>
    /// <param name="fieldNames">The field names for tabular data, if specified.</param>
    /// <returns>A ToonArray with the parsed tabular data.</returns>
    /// <exception cref="ToonParseException">Thrown when array length mismatch occurs in strict mode.</exception>
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

            if (ExpectToken(ToonTokenType.Indent))
            {
                currentIndent = GetCurrentIndentAndAdvance();
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
                while (ExpectToken(ToonTokenType.Comma))
                {
                    Advance(); // consume comma

                    if (!IsValueToken(Peek().Type))
                    {
                        continue;
                    }

                    var token = Advance();
                    rowValues.Add(ParseValueToken(token));
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

            if (ExpectToken(ToonTokenType.Newline))
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

    /// <summary>
    ///     Parses an inline array of primitive values.
    /// </summary>
    /// <param name="expectedLength">The expected number of elements.</param>
    /// <returns>A ToonArray with the parsed primitive values.</returns>
    /// <exception cref="ToonParseException">Thrown when array length mismatch occurs in strict mode.</exception>
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

    /// <summary>
    ///     Parses an inline tabular array row (comma-separated values).
    /// </summary>
    /// <param name="fieldNames">The field names for tabular data.</param>
    /// <returns>A ToonArray with the parsed row values.</returns>
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

    /// <summary>
    ///     Parses a list (items prefixed with '-').
    /// </summary>
    /// <param name="indentLevel">The current indentation level.</param>
    /// <returns>A ToonArray containing the list items.</returns>
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
                // Demented - end of a list
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
                    array.Items.Add(ParseListItemScalar());
                }
                else if (Peek().Type == ToonTokenType.Key)
                {
                    // Inline first field: - key: value
                    // Parse as an object with the first field inline, rest indented
                    var itemObject = new ToonObject();

                    // Parse inline first field
                    var firstKey = new string(Advance().Value.Span);

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
                    ParseAdditionalObjectProperties(itemObject, indentLevel);

                    array.Items.Add(itemObject);
                }
                else if (Peek().Type == ToonTokenType.Newline)
                {
                    // Object list item: - \n properties
                    Advance(); // consume newline

                    // Parse nested object properties at higher indentation
                    var itemObject = new ToonObject();
                    ParseAdditionalObjectProperties(itemObject, indentLevel);

                    array.Items.Add(itemObject);
                }
            }
            else
            {
                // Not a list item - end of a list
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
    ///     Parses a value token (either quoted string or primitive).
    /// </summary>
    /// <param name="token">The token to parse.</param>
    /// <returns>A ToonValue representing the token.</returns>
    private static ToonValue ParseValueToken(ToonToken token)
    {
        return token.Type == ToonTokenType.QuotedString ? new ToonString(new string(token.Value.Span)) : ParsePrimitiveValue(token.Value);
    }

    /// <summary>
    ///     Parses a primitive value from a token's memory.
    /// </summary>
    /// <param name="valueMemory">The memory containing the value.</param>
    /// <returns>A ToonValue representing the primitive (null, boolean, number, or string).</returns>
    private static ToonValue ParsePrimitiveValue(ReadOnlyMemory<char> valueMemory)
    {
        var span = valueMemory.Span.Trim();

        // Check for null
        if (span.SequenceEqual("null"))
        {
            return ToonNull.Instance;
        }

        // Check for boolean
        if (span.SequenceEqual("true"))
        {
            return new ToonBoolean(true);
        }

        if (span.SequenceEqual("false"))
        {
            return new ToonBoolean(false);
        }

        // Try parse as number
        if (double.TryParse(span, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
        {
            return new ToonNumber(number);
        }

        // Fallback to string
        return new ToonString(new string(span));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the current indentation level.
    /// </summary>
    /// <returns>The number of spaces of indentation, or 0 if not at an indent token.</returns>
    /// <remarks>
    ///     Optimized to avoid repeated Peek() calls by accessing tokens directly.
    /// </remarks>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private int GetCurrentIndent()
    {
        if (_position >= _tokens.Count)
        {
            return 0;
        }

        var token = _tokens[_position]; // Direct access, no Peek()
        return token.Type == ToonTokenType.Indent ? token.Value.Length : 0;
    }

    /// <summary>
    ///     Skips all consecutive newline tokens.
    /// </summary>
    private void SkipNewlines()
    {
        // Optimized: Check category once instead of multiple equality checks
        while (!IsAtEnd() && Peek().Type == ToonTokenType.Newline)
        {
            Advance();
        }
    }

    /// <summary>
    ///     Skips all consecutive whitespace (indent and newline) tokens.
    /// </summary>
    private void SkipWhitespace()
    {
        while (!IsAtEnd() && Peek().Type == ToonTokenType.Indent)
        {
            Advance();
        }
    }

    /// <summary>
    ///     Checks if a token type represents a value (either Value or QuotedString).
    /// </summary>
    /// <param name="type">The token type to check.</param>
    /// <returns>True if the token is a value token; otherwise, false.</returns>
    private static bool IsValueToken(ToonTokenType type)
    {
        return type is ToonTokenType.Value or ToonTokenType.QuotedString;
    }

    /// <summary>
    ///     Peeks at the current token without advancing.
    /// </summary>
    /// <returns>The current token, or a cached EndOfInput token if at end.</returns>
    /// <remarks>
    ///     Optimized with token cache to avoid repeated access at the same position.
    ///     Uses AggressiveInlining for maximum performance.
    /// </remarks>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private ToonToken Peek()
    {
        // Check if the cached token is still valid for the current position
        if (_currentTokenPosition == _position)
        {
            return _currentToken;
        }

        // Update cache
        _currentTokenPosition = _position;
        _currentToken = _position < _tokens.Count ? _tokens[_position] : EndOfInputToken;
        return _currentToken;
    }

    /// <summary>
    ///     Advances to the next token and returns the current token.
    /// </summary>
    /// <returns>The current token before advancing.</returns>
    /// <remarks>
    ///     Invalidates the token cache since position changes.
    /// </remarks>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private ToonToken Advance()
    {
        var token = Peek(); // Get the current token (may use cache)

        if (_position < _tokens.Count)
        {
            _position++;
            // Cache is now invalid since position changed
            // Next Peek() will refresh it
        }

        return token;
    }

    /// <summary>
    ///     Checks if the parser has reached the end of tokens.
    /// </summary>
    /// <returns>True if at the end of tokens; otherwise, false.</returns>
    /// <remarks>
    ///     Optimized to check position and EndOfInput token type.
    /// </remarks>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private bool IsAtEnd()
    {
        return _position >= _tokens.Count || (_position < _tokens.Count && _tokens[_position].Type == ToonTokenType.EndOfInput);
    }

    /// <summary>
    ///     Checks if the current token is of the expected type.
    /// </summary>
    /// <param name="expectedType">The expected token type.</param>
    /// <returns>True if the current token matches the expected type; otherwise, false.</returns>
    /// <remarks>
    ///     Helper method to simplify repeated token type checks throughout the parser.
    ///     Uses a cached Peek() result for optimal performance.
    /// </remarks>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private bool ExpectToken(ToonTokenType expectedType)
    {
        return Peek().Type == expectedType;
    }

    /// <summary>
    ///     Gets the current token's indent level and advances the parser position.
    /// </summary>
    /// <returns>The indent level of the current token before advancing.</returns>
    /// <remarks>
    ///     Helper method that combines a common pattern of reading indent level and consuming the token.
    ///     Optimized for frequent indent processing in nested structures.
    /// </remarks>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private int GetCurrentIndentAndAdvance()
    {
        var token = Peek();
        var indentLevel = token.Type == ToonTokenType.Indent ? token.Value.Length : 0;
        Advance();
        return indentLevel;
    }

    /// <summary>
    ///     Checks if the token stream is followed by a list item pattern (Indent + ListItem).
    /// </summary>
    /// <param name="startPosition">The position to start looking from.</param>
    /// <returns>True if followed by Indent, then ListItem; otherwise, false.</returns>
    /// <remarks>
    ///     Helper method to detect list arrays in lookahead scenarios.
    ///     Skips newlines to find the next meaningful token.
    /// </remarks>
    private bool IsFollowedByListItem(int startPosition)
    {
        var pos = startPosition;

        // Skip consecutive newlines
        while (pos < _tokens.Count && _tokens[pos].Type == ToonTokenType.Newline)
        {
            pos++;
        }

        // Check for Indent + ListItem pattern
        if (pos >= _tokens.Count || _tokens[pos].Type != ToonTokenType.Indent)
        {
            return false;
        }

        var nextPos = pos + 1;
        return nextPos < _tokens.Count && _tokens[nextPos].Type == ToonTokenType.ListItem;

    }

    /// <summary>
    ///     Parses array notation (length and/or field names) if present.
    /// </summary>
    /// <returns>Tuple of optional array length and field names.</returns>
    /// <remarks>
    ///     Handles [n] for array length and [field1, field2] for field names.
    /// </remarks>
    private (int? arrayLength, string[]? fieldNames) ParseArrayNotation()
    {
        int? arrayLength = null;
        string[]? fieldNames = null;

        // Check for array length [n]
        if (Peek().Type == ToonTokenType.ArrayLength)
        {
            var lengthToken = Advance();
            var span = lengthToken.Value.Span;
            // Trim '[' and ']' manually
            if (span.Length >= 2 && span[0] == '[' && span[^1] == ']')
            {
                span = span[1..^1];
            }
            if (int.TryParse(span, out var len))
            {
                arrayLength = len;
            }
        }

        // Check for field names {field1, field2}
        if (Peek().Type != ToonTokenType.ArrayFields)
        {
            return (arrayLength, fieldNames);
        }

        var fieldsToken = Advance();
        var fieldsSpan = fieldsToken.Value.Span;
        // Trim '{' and '}' manually
        if (fieldsSpan.Length >= 2 && fieldsSpan[0] == '{' && fieldsSpan[^1] == '}')
        {
            fieldsSpan = fieldsSpan[1..^1];
        }
        // Need to convert to string for Split() - no way around this for now
        fieldNames = new string(fieldsSpan).Split(',').Select(f => f.Trim()).ToArray();

        return (arrayLength, fieldNames);
    }

    /// <summary>
    ///     Parses the value that comes after a colon in a key-value pair.
    /// </summary>
    /// <param name="indentLevel">The current indentation level.</param>
    /// <param name="arrayLength">Optional array length from notation.</param>
    /// <param name="fieldNames">Optional field names from notation.</param>
    /// <returns>The parsed value.</returns>
    private ToonValue ParseValueAfterColon(int indentLevel, int? arrayLength, string[]? fieldNames)
    {
        SkipWhitespace();

        // If newline after colon, it's a nested object or array
        if (Peek().Type == ToonTokenType.Newline || IsAtEnd())
        {
            if (Peek().Type == ToonTokenType.Newline)
            {
                Advance(); // consume newline
            }

            // Check if this is actually a list by peeking ahead for list items
            var isListArray = false;

            if (arrayLength.HasValue || fieldNames != null)
            {
                // Check if the next content is a list (Indent followed by ListItem)
                isListArray = IsFollowedByListItem(_position);
            }

            if ((arrayLength.HasValue || fieldNames != null) && !isListArray)
            {
                // Tabular array
                return ParseTabularArray(indentLevel + _options.IndentSize, arrayLength, fieldNames);
            }

            // Nested object or list array
            return ParseValue(indentLevel + _options.IndentSize);
        }

        if (IsValueToken(Peek().Type))
        {
            // Inline values or primitive array
            if (arrayLength.HasValue)
            {
                // Primitive array: tags[3]: a,b,c
                return ParseInlinePrimitiveArray(arrayLength.Value);
            }

            // Simple value
            var valueToken = Advance();
            return ParseValueToken(valueToken);
        }

        if (!IsAtEnd())
        {
            throw new ToonParseException("Expected value after ':'", Peek().Line, Peek().Column);
        }

        // End of input after colon - empty value (array or object)
        return arrayLength.HasValue ? new ToonArray() : new ToonObject();
    }

    /// <summary>
    ///     Parses a scalar list item (- value).
    /// </summary>
    /// <returns>The parsed scalar value.</returns>
    private ToonValue ParseListItemScalar()
    {
        if (!IsValueToken(Peek().Type))
        {
            throw new ToonParseException("Expected scalar value after '-'", Peek().Line, Peek().Column);
        }

        var valueToken = Advance();
        return ParseValueToken(valueToken);
    }

    /// <summary>
    ///     Parses additional object properties at a higher indentation level.
    ///     Used for list items with properties across multiple lines.
    /// </summary>
    /// <param name="targetObject">The object to add properties to.</param>
    /// <param name="listIndentLevel">The indentation level of the parent list.</param>
    private void ParseAdditionalObjectProperties(ToonObject targetObject, int listIndentLevel)
    {
        while (!IsAtEnd())
        {
            SkipNewlines();

            if (IsAtEnd())
            {
                break;
            }

            // Check indentation
            if (Peek().Type != ToonTokenType.Indent)
            {
                // No indent token - we're back at list level or less
                break;
            }

            var propIndent = Peek().Value.Length;

            // If demented to the list level or below, stop parsing this object
            if (propIndent <= listIndentLevel)
            {
                break;
            }

            Advance(); // consume indent

            // Parse key-value pair
            if (Peek().Type != ToonTokenType.Key)
            {
                break;
            }

            var key = new string(Advance().Value.Span);

            // Parse array notation (if present)
            var (arrayLength, fieldNames) = ParseArrayNotation();

            // Expect colon
            if (Peek().Type != ToonTokenType.Colon)
            {
                throw new ToonParseException($"Expected ':' after key '{key}'", Peek().Line, Peek().Column);
            }

            Advance(); // consume colon

            // Parse value after colon
            var value = ParseValueAfterColon(propIndent, arrayLength, fieldNames);

            targetObject.Properties[key] = value;

            // Consume optional trailing newline
            if (Peek().Type == ToonTokenType.Newline)
            {
                Advance();
            }
        }
    }

    #endregion
}