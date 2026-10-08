using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Core.Parsing;

/// <summary>
/// Parses TOON text into a document structure following TOON spec v3.3.2.
/// </summary>
/// <remarks>
/// <para>
/// This is an internal implementation detail. Users should use <see cref="ToonSerializer"/> instead.
/// </para>
/// <para>
/// The parser is line based: each line's depth is derived from its leading spaces and
/// <see cref="ToonOptions.IndentSize"/>. With <see cref="ToonOptions.StrictMode"/> enabled, every strict-mode error of
/// spec §14 is reported (count and width mismatches, malformed headers, duplicate keys, indentation that is not a
/// multiple of the indent size, tabs in indentation, blank lines inside arrays). In non-strict mode depth is
/// floor(spaces / indent size), a tab in indentation counts as <see cref="ToonOptions.IndentSize"/> spaces,
/// malformed headers are read as literal keys, duplicate keys use the last value, array lengths are not checked,
/// and list items under a bare <c>key:</c> (without an <c>[N]</c> header) are read as an array.
/// </para>
/// <para>
/// Numbers follow the JSON number grammar; tokens with leading zeros (<c>05</c>) or other text decode as strings.
/// Numbers that fit in a <see cref="decimal"/> keep their exact value in <see cref="ToonNumber.DecimalValue"/>;
/// numbers outside the <see cref="double"/> range decode as strings.
/// </para>
/// </remarks>
internal sealed class ToonParser(ToonOptions? options = null)
{
    private readonly ToonOptions _options = options ?? ToonOptions.Default;
    private Line[] _lines = [];
    private int _position;
    private int _depth;

    /// <summary>
    ///     A physical line of input.
    /// </summary>
    /// <param name="Number">The 1-based line number.</param>
    /// <param name="Depth">The indentation depth; meaningless for blank lines.</param>
    /// <param name="Indent">The number of indentation columns.</param>
    /// <param name="Content">The text after the indentation, without trailing spaces.</param>
    /// <param name="IsBlank">Whether the line contains only spaces and tabs.</param>
    private readonly record struct Line(int Number, int Depth, int Indent, string Content, bool IsBlank);

    /// <summary>
    ///     A parsed array header: <c>key?[N&lt;delim?&gt;]{fields?}:</c> (spec §6).
    /// </summary>
    private sealed record Header(string? Key, int Length, char Delimiter, string[]? Fields, string InlineValues);

    #region Public API

    /// <summary>
    ///  Parses a TOON format string into a document.
    /// </summary>
    /// <param name="input">The TOON format string to parse.</param>
    /// <returns>A ToonDocument representing the parsed input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the input is invalid.</exception>
    public ToonDocument Parse(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        _lines = SplitLines(input);
        _position = 0;
        _depth = 0;

        return new ToonDocument(ParseRoot());
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
        
        var input = await File.ReadAllTextAsync(filePath, System.Text.Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        return await ParseAsync(input, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Asynchronously parses a TOON document from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous parse operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the input is invalid.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public async Task<ToonDocument> ParseFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var input = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return await ParseAsync(input, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Lines

    private Line[] SplitLines(string input)
    {
        var rawLines = input.Split('\n');
        var lines = new Line[rawLines.Length];

        for (var i = 0; i < rawLines.Length; i++)
        {
            var raw = rawLines[i];

            if (raw.EndsWith('\r'))
            {
                raw = raw[..^1];
            }

            lines[i] = CreateLine(raw, i + 1);
        }

        return lines;
    }

    private Line CreateLine(string raw, int number)
    {
        var columns = 0;
        var index = 0;

        while (index < raw.Length && raw[index] is ' ' or '\t')
        {
            if (raw[index] == '\t')
            {
                if (index == raw.Length - 1 || raw.AsSpan(index).Trim(" \t").IsEmpty)
                {
                    break; // whitespace-only line, handled as blank below
                }

                if (_options.StrictMode)
                {
                    throw new ToonParseException("Tabs are not allowed in indentation", number, index + 1);
                }

                columns += _options.IndentSize;
            }
            else
            {
                columns++;
            }

            index++;
        }

        var content = raw[index..].TrimEnd(' ', '\t');

        if (content.Length == 0)
        {
            return new Line(number, 0, columns, string.Empty, IsBlank: true);
        }

        if (_options.StrictMode && columns % _options.IndentSize != 0)
        {
            throw new ToonParseException(
                $"Indentation of {columns} spaces is not a multiple of the indent size {_options.IndentSize}", number, 1);
        }

        return new Line(number, columns / _options.IndentSize, columns, content, IsBlank: false);
    }

    /// <summary>
    ///     Skips blank lines and returns the next non-blank line without consuming it.
    /// </summary>
    /// <param name="skippedBlank">Whether blank lines were skipped.</param>
    private Line? PeekLine(out bool skippedBlank)
    {
        var position = _position;
        skippedBlank = false;

        while (position < _lines.Length && _lines[position].IsBlank)
        {
            position++;
            skippedBlank = true;
        }

        return position < _lines.Length ? _lines[position] : null;
    }

    private Line? PeekLine()
    {
        return PeekLine(out _);
    }

    /// <summary>
    ///     Consumes the next non-blank line (which must have been peeked).
    /// </summary>
    private Line NextLine()
    {
        while (_lines[_position].IsBlank)
        {
            _position++;
        }

        return _lines[_position++];
    }

    #endregion

    #region Structure

    /// <summary>
    ///     Determines the root form (spec §5) and parses the document.
    /// </summary>
    private ToonValue ParseRoot()
    {
        var first = PeekLine();

        if (first is not { } firstLine)
        {
            return new ToonObject(); // empty document
        }

        if (firstLine.Depth != 0 && _options.StrictMode)
        {
            throw new ToonParseException("The first line of a document must not be indented", firstLine.Number, firstLine.Indent + 1);
        }

        var nonBlankCount = _lines.Count(l => !l.IsBlank);

        if (TryParseHeader(firstLine, firstLine.Content, requireKey: false) is { Key: null } rootHeader)
        {
            NextLine();
            var array = ParseArrayBody(rootHeader, firstLine, firstLine.Depth + 1);
            EnsureFullyConsumed();
            return array;
        }

        if (nonBlankCount == 1)
        {
            if (firstLine.Content == "[]")
            {
                return new ToonArray();
            }

            if (!IsKeyValueLine(firstLine.Content))
            {
                NextLine();
                return ParsePrimitive(firstLine.Content, firstLine);
            }
        }

        var root = new ToonObject();
        ParseFields(root, firstLine.Depth);
        EnsureFullyConsumed();
        return root;
    }

    private void EnsureFullyConsumed()
    {
        if (PeekLine() is { } extra)
        {
            throw new ToonParseException("Unexpected content after the end of the document structure", extra.Number, extra.Indent + 1);
        }
    }

    /// <summary>
    ///     Parses the fields of an object at <paramref name="depth"/> into <paramref name="target"/>.
    /// </summary>
    private void ParseFields(ToonObject target, int depth)
    {
        while (PeekLine() is { } line)
        {
            if (line.Depth < depth)
            {
                return;
            }

            if (line.Depth > depth)
            {
                throw new ToonParseException("Unexpected indentation", line.Number, line.Indent + 1);
            }

            NextLine();
            ParseField(target, line, line.Content, depth + 1);
        }
    }

    /// <summary>
    ///     Parses a key-value or array-header line (<paramref name="content"/> without indentation or list marker)
    ///     and adds the field to <paramref name="target"/>; nested content is read at <paramref name="childDepth"/>.
    /// </summary>
    private void ParseField(ToonObject target, Line line, string content, int childDepth)
    {
        string key;
        ToonValue value;

        if (TryParseHeader(line, content, requireKey: true) is { } header)
        {
            key = header.Key!;
            value = ParseArrayBody(header, line, childDepth);
        }
        else
        {
            var colon = FindKeyValueColon(content, line, out key);
            var rest = content[(colon + 1)..].Trim(' ');

            if (rest.Length == 0 && PeekLine() is { } next && next.Depth == childDepth && IsListItem(next.Content))
            {
                // `key:` always opens an object (§8); list items need a header. Non-strict mode reads them as an array.
                if (_options.StrictMode)
                {
                    throw new ToonParseException($"List items under '{key}' require an array header such as '{key}[N]:'",
                                                 next.Number, next.Indent + 1);
                }

                EnterNesting(line);
                value = ParseListItems(childDepth);
                _depth--;
            }
            else if (rest.Length == 0)
            {
                var nested = new ToonObject();
                EnterNesting(line);
                ParseFields(nested, childDepth);
                _depth--;
                value = nested;
            }
            else if (rest == "[]")
            {
                value = new ToonArray();
            }
            else
            {
                value = ParsePrimitive(rest, line);
            }
        }

        if (_options.StrictMode && target.Properties.ContainsKey(key))
        {
            throw new ToonParseException($"Duplicate key '{key}'", line.Number, line.Indent + 1);
        }

        target.Properties[key] = value;
    }

    /// <summary>
    ///     Parses the body of an array whose header has been read: inline values, tabular rows or list items (spec §9).
    /// </summary>
    private ToonArray ParseArrayBody(Header header, Line headerLine, int childDepth)
    {
        EnterNesting(headerLine);

        ToonArray array;

        if (header.Fields != null)
        {
            if (header.InlineValues.Length > 0)
            {
                throw new ToonParseException("Unexpected values after a tabular array header", headerLine.Number, headerLine.Indent + 1);
            }

            array = ParseTabularRows(header, childDepth);
        }
        else if (header.InlineValues.Length > 0)
        {
            var items = SplitDelimited(header.InlineValues, header.Delimiter, headerLine).Select(token => ParsePrimitive(token, headerLine));
            array = new ToonArray([.. items]);
        }
        else
        {
            array = ParseListItems(childDepth);
        }

        if (_options.StrictMode && array.Count != header.Length)
        {
            throw new ToonParseException($"Array length mismatch: expected {header.Length}, got {array.Count}",
                                         headerLine.Number, headerLine.Indent + 1);
        }

        _depth--;
        return array;
    }

    private ToonArray ParseTabularRows(Header header, int rowDepth)
    {
        var fields = header.Fields!;
        var array = new ToonArray { FieldNames = fields };

        while (PeekLine(out var skippedBlank) is { } line && line.Depth == rowDepth && IsTabularRow(line.Content, header.Delimiter))
        {
            if (skippedBlank && array.Count > 0 && _options.StrictMode)
            {
                throw new ToonParseException("Blank lines are not allowed inside a tabular array", line.Number - 1, 1);
            }

            NextLine();
            var values = SplitDelimited(line.Content, header.Delimiter, line);

            if (values.Count != fields.Length && _options.StrictMode)
            {
                throw new ToonParseException($"Row has {values.Count} values but expected {fields.Length}", line.Number, line.Indent + 1);
            }

            var row = new ToonObject();

            for (var i = 0; i < fields.Length; i++)
            {
                row.Properties[fields[i]] = i < values.Count ? ParsePrimitive(values[i], line) : ToonNull.Instance;
            }

            array.Items.Add(row);
        }

        if (PeekLine() is { } next && next.Depth > rowDepth)
        {
            throw new ToonParseException("Unexpected indentation in tabular array", next.Number, next.Indent + 1);
        }

        return array;
    }

    private ToonArray ParseListItems(int itemDepth)
    {
        var array = new ToonArray();

        while (PeekLine(out var skippedBlank) is { } line && line.Depth == itemDepth && IsListItem(line.Content))
        {
            if (skippedBlank && array.Count > 0 && _options.StrictMode)
            {
                throw new ToonParseException("Blank lines are not allowed inside an array", line.Number - 1, 1);
            }

            NextLine();
            array.Items.Add(ParseListItem(line, itemDepth));
        }

        if (PeekLine() is { } next && next.Depth > itemDepth)
        {
            throw new ToonParseException("Unexpected indentation in list array", next.Number, next.Indent + 1);
        }

        return array;
    }

    /// <summary>
    ///     Parses a list item line at <paramref name="itemDepth"/> (spec §9.4, §10).
    /// </summary>
    private ToonValue ParseListItem(Line line, int itemDepth)
    {
        if (line.Content == "-")
        {
            return new ToonObject(); // empty object list item
        }

        var rest = line.Content[2..].TrimStart(' ');

        // Inner array: - [M]: ...
        if (TryParseHeader(line, rest, requireKey: false) is { Key: null } innerHeader)
        {
            return ParseArrayBody(innerHeader, line, itemDepth + 1);
        }

        if (!IsKeyValueLine(rest))
        {
            return ParsePrimitive(rest, line);
        }

        // Object with its first field on the hyphen line; that field's nested content is two levels deeper
        var item = new ToonObject();
        EnterNesting(line);
        ParseField(item, line, rest, itemDepth + 2);
        ParseFields(item, itemDepth + 1);
        _depth--;
        return item;
    }

    private void EnterNesting(Line line)
    {
        if (++_depth > _options.MaxDepth)
        {
            _depth--;
            throw new ToonParseException($"Maximum nesting depth of {_options.MaxDepth} exceeded", line.Number, line.Indent + 1);
        }

        if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            _depth--;
            throw new ToonParseException("Document is nested too deeply to parse with the available stack space", line.Number, line.Indent + 1);
        }
    }

    #endregion

    #region Line classification

    private static bool IsListItem(string content)
    {
        return content == "-" || content.StartsWith("- ", StringComparison.Ordinal);
    }

    /// <summary>
    ///     Checks whether a line inside a tabular array is a row rather than a key-value line (spec §9.3).
    /// </summary>
    private static bool IsTabularRow(string content, char delimiter)
    {
        var delimiterIndex = IndexOfUnquoted(content, delimiter);
        var colonIndex = IndexOfUnquoted(content, ':');

        return colonIndex < 0 || (delimiterIndex >= 0 && delimiterIndex < colonIndex);
    }

    /// <summary>
    ///     Checks whether content is a key-value line (a key followed by a colon) or an array header with a key.
    /// </summary>
    private static bool IsKeyValueLine(string content)
    {
        if (content.StartsWith('"'))
        {
            var end = FindClosingQuote(content, 0);
            return end > 0 && end + 1 < content.Length && content[end + 1] is ':' or '[';
        }

        return content.IndexOf(':') > 0;
    }

    /// <summary>
    ///     Returns the index of the first occurrence of <paramref name="target"/> outside quoted strings, or -1.
    /// </summary>
    private static int IndexOfUnquoted(string content, char target)
    {
        var inQuotes = false;

        for (var i = 0; i < content.Length; i++)
        {
            var ch = content[i];

            if (inQuotes)
            {
                if (ch == '\\')
                {
                    i++;
                }
                else if (ch == '"')
                {
                    inQuotes = false;
                }
            }
            else if (ch == '"')
            {
                inQuotes = true;
            }
            else if (ch == target)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    ///     Returns the index of the quote closing the string that starts at <paramref name="start"/>, or -1.
    /// </summary>
    private static int FindClosingQuote(string content, int start)
    {
        for (var i = start + 1; i < content.Length; i++)
        {
            if (content[i] == '\\')
            {
                i++;
            }
            else if (content[i] == '"')
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    ///     Finds the colon of a key-value line and extracts the (unescaped) key.
    /// </summary>
    /// <returns>The index of the colon in <paramref name="content"/>.</returns>
    /// <exception cref="ToonParseException">Thrown when the key is not followed by a colon.</exception>
    private static int FindKeyValueColon(string content, Line line, out string key)
    {
        if (content.StartsWith('"'))
        {
            var end = FindClosingQuote(content, 0);

            if (end < 0)
            {
                throw new ToonParseException("Unterminated string", line.Number, line.Indent + 1);
            }

            if (end + 1 >= content.Length || content[end + 1] != ':')
            {
                throw new ToonParseException("Missing colon after key", line.Number, line.Indent + end + 2);
            }

            key = Unescape(content[1..end], line);
            return end + 1;
        }

        var colon = content.IndexOf(':');

        if (colon <= 0)
        {
            throw new ToonParseException(colon == 0 ? "Missing key before colon" : "Missing colon after key", line.Number, line.Indent + 1);
        }

        key = content[..colon].TrimEnd(' ');
        return colon;
    }

    /// <summary>
    ///     Tries to parse an array header at the start of <paramref name="content"/> (spec §6).
    /// </summary>
    /// <param name="line">The line, for error positions.</param>
    /// <param name="content">The header text (without indentation or list marker).</param>
    /// <param name="requireKey">Whether a key prefix is required (fields) or forbidden (root and list-item arrays).</param>
    /// <returns>The header, or null when the content is not a header.</returns>
    /// <exception cref="ToonParseException">Thrown in strict mode for malformed bracket segments or delimiter mismatches.</exception>
    private Header? TryParseHeader(Line line, string content, bool requireKey)
    {
        string? key = null;
        int bracketStart;

        if (content.StartsWith('"'))
        {
            var end = FindClosingQuote(content, 0);

            if (end < 0 || end + 1 >= content.Length || content[end + 1] != '[')
            {
                return null;
            }

            key = Unescape(content[1..end], line);
            bracketStart = end + 1;
        }
        else
        {
            bracketStart = content.IndexOf('[');
            var colon = content.IndexOf(':');

            if (bracketStart < 0 || (colon >= 0 && colon < bracketStart))
            {
                return null;
            }

            if (bracketStart > 0)
            {
                key = content[..bracketStart];
            }
        }

        if ((key != null) != requireKey)
        {
            return null;
        }

        var header = TryParseHeaderSegments(content, bracketStart, key, out var malformed);

        if (header == null && malformed && _options.StrictMode && (requireKey || content.IndexOf(':') > 0))
        {
            throw new ToonParseException("Malformed array header", line.Number, line.Indent + bracketStart + 1);
        }

        if (header?.Fields != null && _options.StrictMode)
        {
            foreach (var field in header.Fields)
            {
                if (field.IndexOfAny([',', '|', '\t']) >= 0 && !field.Contains(header.Delimiter) && IsUnquotedFieldInHeader(content, field))
                {
                    throw new ToonParseException("Header delimiter mismatch between bracket and fields segments", line.Number, line.Indent + 1);
                }
            }
        }

        return header;
    }

    private static bool IsUnquotedFieldInHeader(string content, string field)
    {
        return content.Contains(field, StringComparison.Ordinal) && !content.Contains("\"" + field + "\"", StringComparison.Ordinal);
    }

    /// <summary>
    ///     Parses <c>[N&lt;delim?&gt;]{fields?}:</c> starting at <paramref name="bracketStart"/>.
    /// </summary>
    /// <param name="content">The header text.</param>
    /// <param name="bracketStart">The index of the opening bracket.</param>
    /// <param name="key">The already parsed key, or null for key-less headers.</param>
    /// <param name="malformed">Set when the text looks like a header but is not a valid one.</param>
    private Header? TryParseHeaderSegments(string content, int bracketStart, string? key, out bool malformed)
    {
        malformed = true;
        var bracketEnd = content.IndexOf(']', bracketStart);

        if (bracketEnd < 0)
        {
            return null;
        }

        var inner = content.AsSpan(bracketStart + 1, bracketEnd - bracketStart - 1);
        var delimiter = ',';

        if (inner.Length > 0 && inner[^1] is '\t' or '|')
        {
            delimiter = inner[^1];
            inner = inner[..^1];
        }

        if (!IsCanonicalLength(inner) || !int.TryParse(inner, NumberStyles.None, CultureInfo.InvariantCulture, out var length))
        {
            return null;
        }

        var position = bracketEnd + 1;
        string[]? fields = null;

        if (position < content.Length && content[position] == '{')
        {
            var fieldsEnd = IndexOfUnquoted(content[position..], '}');

            if (fieldsEnd < 0)
            {
                return null;
            }

            var fieldsText = content.Substring(position + 1, fieldsEnd - 1);
            fields = [.. SplitDelimited(fieldsText, delimiter, null).Select(field => ParseKeyToken(field))];
            position += fieldsEnd + 1;
        }

        if (position >= content.Length || content[position] != ':')
        {
            return null;
        }

        malformed = false;
        var inlineValues = content[(position + 1)..].Trim(' ');
        return new Header(key, length, delimiter, fields, inlineValues);
    }

    private static bool IsCanonicalLength(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty || (text.Length > 1 && text[0] == '0'))
        {
            return false;
        }

        foreach (var ch in text)
        {
            if (!char.IsAsciiDigit(ch))
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    #region Tokens

    /// <summary>
    ///     Splits delimited values outside quotes, trimming surrounding spaces; empty tokens are kept (spec §11.2, B.3).
    /// </summary>
    private static List<string> SplitDelimited(string text, char delimiter, Line? line)
    {
        var tokens = new List<string>();
        var start = 0;
        var inQuotes = false;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            if (inQuotes)
            {
                if (ch == '\\')
                {
                    i++;
                }
                else if (ch == '"')
                {
                    inQuotes = false;
                }
            }
            else if (ch == '"')
            {
                inQuotes = true;
            }
            else if (ch == delimiter)
            {
                tokens.Add(text[start..i].Trim(' '));
                start = i + 1;
            }
        }

        tokens.Add(text[start..].Trim(' '));
        return tokens;
    }

    private static string ParseKeyToken(string token)
    {
        if (token.Length >= 2 && token[0] == '"' && FindClosingQuote(token, 0) == token.Length - 1)
        {
            return Unescape(token[1..^1], null);
        }

        return token;
    }

    /// <summary>
    ///     Parses a primitive token: quoted string, true/false/null, number, or unquoted string (spec §4, B.4).
    /// </summary>
    private static ToonValue ParsePrimitive(string token, Line line)
    {
        if (token.Length == 0)
        {
            return new ToonString(string.Empty);
        }

        if (token[0] == '"')
        {
            var end = FindClosingQuote(token, 0);

            if (end < 0)
            {
                throw new ToonParseException("Unterminated string", line.Number, line.Indent + 1);
            }

            if (end != token.Length - 1)
            {
                throw new ToonParseException("Unexpected characters after closing quote", line.Number, line.Indent + 1);
            }

            return new ToonString(Unescape(token[1..end], line));
        }

        return token switch
        {
            "true"  => new ToonBoolean(true),
            "false" => new ToonBoolean(false),
            "null"  => ToonNull.Instance,
            _       => (ToonValue?)TryParseNumber(token) ?? new ToonString(token)
        };
    }

    /// <summary>
    ///     Parses a token matching the JSON number grammar without forbidden leading zeros.
    /// </summary>
    /// <returns>The number, or null when the token is not a number or not finite as a <see cref="double"/>.</returns>
    internal static ToonNumber? TryParseNumber(string token)
    {
        if (!IsJsonNumber(token) ||
            !double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
            !double.IsFinite(value))
        {
            return null;
        }

        // Keep the exact value when it fits in a decimal. Values below decimal precision (e.g. 1e-30) round to a
        // different value there, so they only keep the double.
        if (decimal.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var exact) &&
            Math.Abs((double)exact - value) <= Math.Abs(value) * 1e-12)
        {
            return new ToonNumber(exact);
        }

        return new ToonNumber(value);
    }

    /// <summary>
    ///     Matches <c>-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?</c>.
    /// </summary>
    private static bool IsJsonNumber(string token)
    {
        var i = 0;

        if (token[i] == '-')
        {
            i++;
        }

        if (i >= token.Length || !char.IsAsciiDigit(token[i]))
        {
            return false;
        }

        if (token[i] == '0')
        {
            i++;
        }
        else
        {
            while (i < token.Length && char.IsAsciiDigit(token[i]))
            {
                i++;
            }
        }

        if (i < token.Length && token[i] == '.')
        {
            i++;
            var fractionStart = i;

            while (i < token.Length && char.IsAsciiDigit(token[i]))
            {
                i++;
            }

            if (i == fractionStart)
            {
                return false;
            }
        }

        if (i < token.Length && token[i] is 'e' or 'E')
        {
            i++;

            if (i < token.Length && token[i] is '+' or '-')
            {
                i++;
            }

            var exponentStart = i;

            while (i < token.Length && char.IsAsciiDigit(token[i]))
            {
                i++;
            }

            if (i == exponentStart)
            {
                return false;
            }
        }

        return i == token.Length;
    }

    /// <summary>
    ///     Unescapes the content of a quoted string per spec §7.1.
    /// </summary>
    /// <exception cref="ToonParseException">Thrown for unknown escapes, truncated or surrogate <c>\uXXXX</c> escapes.</exception>
    private static string Unescape(string text, Line? line)
    {
        if (text.IndexOf('\\') < 0)
        {
            return text;
        }

        var sb = new StringBuilder(text.Length);
        var lineNumber = line?.Number ?? 0;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            if (ch != '\\')
            {
                sb.Append(ch);
                continue;
            }

            if (++i >= text.Length)
            {
                throw new ToonParseException("Unterminated escape sequence", lineNumber, 0);
            }

            switch (text[i])
            {
                case '\\':
                    sb.Append('\\');
                    break;
                case '"':
                    sb.Append('"');
                    break;
                case 'n':
                    sb.Append('\n');
                    break;
                case 'r':
                    sb.Append('\r');
                    break;
                case 't':
                    sb.Append('\t');
                    break;
                case 'u':
                    if (text.Length - (i + 1) < 4 ||
                        !int.TryParse(text.AsSpan(i + 1, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var codePoint))
                    {
                        throw new ToonParseException("Invalid \\u escape: expected four hex digits", lineNumber, 0);
                    }

                    if (codePoint is >= 0xD800 and <= 0xDFFF)
                    {
                        throw new ToonParseException("Invalid \\u escape: surrogate code points are not allowed", lineNumber, 0);
                    }

                    sb.Append((char)codePoint);
                    i += 4;
                    break;
                default:
                    throw new ToonParseException($"Invalid escape sequence '\\{text[i]}'", lineNumber, 0);
            }
        }

        return sb.ToString();
    }

    #endregion
}
