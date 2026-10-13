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
    ///     The number of array header spans (spec §12) being read: from an array's first item or row to the end of its
    ///     content. A blank line inside a span is an error in strict mode.
    /// </summary>
    private int _headerSpans;

    /// <summary>The tokens of the last <see cref="SplitDelimited"/> call, reused to avoid a list per row.</summary>
    private readonly List<Token> _tokens = [];

    /// <summary>
    ///     Unquoted keys seen in this document, by a hash of their text, so the keys repeated in every item of a list
    ///     share one string instead of allocating one per occurrence. Not used for short documents, where it would
    ///     cost more than it saves.
    /// </summary>
    private string?[]? _keys;

    private bool _useKeyCache;

    private const int KeyCacheSize = 256;

    /// <summary>Documents with at least this many lines use <see cref="_keys"/>.</summary>
    private const int KeyCacheMinLines = 64;


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
    ///     A delimited token: where it starts in the split text (after leading spaces) and its length (without
    ///     trailing spaces).
    /// </summary>
    private readonly record struct Token(int Start, int Length);

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
    /// <exception cref="InvalidOperationException">Thrown when MaxDepth exceeds 200 without AllowExtendedLimits.</exception>
    public ToonDocument Parse(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        _ = _options.ValidatedMaxDepth;

        _lines = SplitLines(input);

        _useKeyCache = _lines.Length >= KeyCacheMinLines;

        if (_useKeyCache)
        {
            _keys ??= new string?[KeyCacheSize];
            Array.Clear(_keys);
        }

        _position = 0;
        _depth = 0;
        _headerSpans = 0;

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

    /// <summary>
    ///     Splits the input into lines; each non-blank line allocates only its content string.
    /// </summary>
    private Line[] SplitLines(string input)
    {
        var lines = new Line[input.AsSpan().Count('\n') + 1];
        var start = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            var newline = input.IndexOf('\n', start);
            var end = newline < 0 ? input.Length : newline;
            var length = end > start && input[end - 1] == '\r' ? end - start - 1 : end - start;

            lines[i] = CreateLine(input, start, length, i + 1);
            start = end + 1;
        }

        return lines;
    }

    /// <summary>
    ///     Creates the line that starts at <paramref name="start"/> in <paramref name="input"/> (without its line break).
    /// </summary>
    private Line CreateLine(string input, int start, int length, int number)
    {
        var raw = input.AsSpan(start, length);
        var columns = 0;
        var index = 0;

        while (index < raw.Length && raw[index] is ' ' or '\t')
        {
            if (raw[index] == '\t')
            {
                if (index == raw.Length - 1 || raw[index..].Trim(" \t").IsEmpty)
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

        var content = raw[index..].TrimEnd(" \t");

        if (content.IsEmpty)
        {
            return new Line(number, 0, columns, string.Empty, IsBlank: true);
        }

        if (_options.StrictMode && columns % _options.IndentSize != 0)
        {
            throw new ToonParseException(
                $"Indentation of {columns} spaces is not a multiple of the indent size {_options.IndentSize}", number, 1);
        }

        var text = content.Length == input.Length ? input : input.Substring(start + index, content.Length);
        return new Line(number, columns / _options.IndentSize, columns, text, IsBlank: false);
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


        if (TryParseHeader(firstLine, firstLine.Content, requireKey: false) is { Key: null } rootHeader)
        {
            NextLine();
            _depth = -1; // the root array is at depth 0, like the root object
            var array = ParseArrayBody(rootHeader, firstLine, firstLine.Depth + 1);
            EnsureFullyConsumed();
            return array;
        }

        if (HasSingleNonBlankLine())
        {
            if (firstLine.Content == "[]")
            {
                return new ToonArray();
            }

            if (!IsKeyValueLine(firstLine.Content))
            {
                NextLine();
                return ParsePrimitive(firstLine.Content, firstLine, ColumnOf(firstLine, firstLine.Content));
            }
        }

        var root = new ToonObject();
        ParseFields(root, firstLine.Depth);
        EnsureFullyConsumed();
        return root;
    }

    private bool HasSingleNonBlankLine()
    {
        var found = false;

        foreach (var line in _lines)
        {
            if (!line.IsBlank)
            {
                if (found)
                {
                    return false;
                }

                found = true;
            }
        }

        return found;
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
        while (PeekLine(out var skippedBlank) is { } line)
        {
            if (line.Depth < depth)
            {
                return;
            }

            // The object continues after the blank line, so the blank is inside the enclosing array's header span
            if (skippedBlank && _headerSpans > 0 && _options.StrictMode)
            {
                throw new ToonParseException("Blank lines are not allowed inside an array", line.Number - 1, 1);
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
            var restStart = colon + 1;

            while (restStart < content.Length && content[restStart] == ' ')
            {
                restStart++;
            }

            // Line content has no trailing spaces, so the value runs to the end
            var rest = content.AsSpan(restStart);

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
            else if (rest.SequenceEqual("[]"))
            {
                CheckEmptyContainerNesting(line);
                value = new ToonArray();
            }
            else
            {
                value = ParsePrimitive(rest, line, ColumnOf(line, content) + restStart);
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

            array = ParseTabularRows(header, childDepth, ExpectedItemCapacity(header));
        }
        else if (header.InlineValues.Length > 0)
        {
            var valuesColumn = ColumnOf(headerLine, header.InlineValues);
            var tokens = SplitDelimited(header.InlineValues, header.Delimiter);
            var items = new List<ToonValue>(tokens.Count);

            foreach (var (start, length) in tokens)
            {
                items.Add(ParsePrimitive(header.InlineValues.AsSpan(start, length), headerLine, valuesColumn + start));
            }

            array = new ToonArray(items);
        }
        else
        {
            array = ParseListItems(childDepth, ExpectedItemCapacity(header));
        }

        if (_options.StrictMode && array.Count != header.Length)
        {
            throw new ToonParseException($"Array length mismatch: expected {header.Length}, got {array.Count}",
                                         headerLine.Number, headerLine.Indent + 1);
        }

        _depth--;
        return array;
    }

    /// <summary>
    ///     The declared length of an array, bounded by the lines left, which is the most a list or tabular body can hold;
    ///     a header such as <c>[2000000000]:</c> does not reserve memory for items that are not there.
    /// </summary>
    private int ExpectedItemCapacity(Header header) => Math.Min(header.Length, _lines.Length - _position);

    private ToonArray ParseTabularRows(Header header, int rowDepth, int capacity)
    {
        var fields = header.Fields!;
        var array = new ToonArray(new List<ToonValue>(capacity)) { FieldNames = fields };

        while (PeekLine(out var skippedBlank) is { } line && line.Depth == rowDepth && IsTabularRow(line.Content, header.Delimiter))
        {
            if (skippedBlank && (array.Count > 0 || _headerSpans > 0) && _options.StrictMode)
            {
                throw new ToonParseException("Blank lines are not allowed inside a tabular array", line.Number - 1, 1);
            }

            NextLine();
            var content = line.Content;
            var values = SplitDelimited(content, header.Delimiter);

            if (values.Count != fields.Length && _options.StrictMode)
            {
                throw new ToonParseException($"Row has {values.Count} values but expected {fields.Length}", line.Number, line.Indent + 1);
            }

            // Rows are objects one level below the array
            if (array.Count == 0)
            {
                EnterNesting(line);
            }

            var row = new ToonObject(fields.Length);

            for (var i = 0; i < fields.Length; i++)
            {
                row.Properties[fields[i]] = i < values.Count
                    ? ParsePrimitive(content.AsSpan(values[i].Start, values[i].Length), line, line.Indent + 1 + values[i].Start)
                    : ToonNull.Instance;
            }

            array.Items.Add(row);
        }

        if (array.Count > 0)
        {
            _depth--;
        }

        if (PeekLine() is { } next && next.Depth > rowDepth)
        {
            throw new ToonParseException("Unexpected indentation in tabular array", next.Number, next.Indent + 1);
        }

        return array;
    }

    private ToonArray ParseListItems(int itemDepth, int capacity = 0)
    {
        var array = new ToonArray(new List<ToonValue>(capacity));

        while (PeekLine(out var skippedBlank) is { } line && line.Depth == itemDepth && IsListItem(line.Content))
        {
            // Inside this array's span after the first item, or inside an enclosing array's span
            if (skippedBlank && (array.Count > 0 || _headerSpans > 0) && _options.StrictMode)
            {
                throw new ToonParseException("Blank lines are not allowed inside an array", line.Number - 1, 1);
            }

            NextLine();
            _headerSpans++;
            array.Items.Add(ParseListItem(line, itemDepth));
            _headerSpans--;
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
            CheckEmptyContainerNesting(line);
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
            return ParsePrimitive(rest, line, ColumnOf(line, rest));
        }

        // Object with its first field on the hyphen line; that field's nested content is two levels deeper
        var item = new ToonObject();
        EnterNesting(line);
        ParseField(item, line, rest, itemDepth + 2);
        ParseFields(item, itemDepth + 1);
        _depth--;
        return item;
    }

    /// <summary>
    ///     Checks the depth of an empty object or array, which has no content that would enter its level.
    /// </summary>
    private void CheckEmptyContainerNesting(Line line)
    {
        EnterNesting(line);
        _depth--;
    }

    /// <summary>
    ///     Enters the level of a nested object or array. The root value is at depth 0, each object or array inside
    ///     another one level deeper; primitive values are not counted (<see cref="ToonOptions.MaxDepth"/>).
    /// </summary>
    private void EnterNesting(Line line)
    {
        if (++_depth > _options.ValidatedMaxDepth)
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
        // A row when there is no colon, or a delimiter comes before it
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
            else if (ch == delimiter)
            {
                return true;
            }
            else if (ch == ':')
            {
                return false;
            }
        }

        return true;
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
    private static int IndexOfUnquoted(ReadOnlySpan<char> content, char target)
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
    private static int FindClosingQuote(ReadOnlySpan<char> content, int start)
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
    private int FindKeyValueColon(string content, Line line, out string key)
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

            key = Unescape(content.AsSpan(1, end - 1), line.Number, ColumnOf(line, content) + 1);
            return end + 1;
        }

        var colon = content.IndexOf(':');

        if (colon <= 0)
        {
            throw new ToonParseException(colon == 0 ? "Missing key before colon" : "Missing colon after key", line.Number, line.Indent + 1);
        }

        key = GetKey(content.AsSpan(0, colon).TrimEnd(' '));
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

            key = Unescape(content.AsSpan(1, end - 1), line.Number, ColumnOf(line, content) + 1);
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
                key = GetKey(content.AsSpan(0, bracketStart));
            }
        }

        if ((key != null) != requireKey)
        {
            return null;
        }

        var header = TryParseHeaderSegments(line, content, bracketStart, key, out var malformed, out var mixedDelimiters);

        if (header == null && malformed && _options.StrictMode && (requireKey || content.IndexOf(':') > 0))
        {
            throw new ToonParseException("Malformed array header", line.Number, line.Indent + bracketStart + 1);
        }

        if (header != null && mixedDelimiters && _options.StrictMode)
        {
            throw new ToonParseException("Header delimiter mismatch between bracket and fields segments", line.Number, line.Indent + 1);
        }

        return header;
    }

    /// <summary>
    ///     Parses <c>[N&lt;delim?&gt;]{fields?}:</c> starting at <paramref name="bracketStart"/>.
    /// </summary>
    /// <param name="line">The header line, for error positions.</param>
    /// <param name="content">The header text, a suffix of the line's content.</param>
    /// <param name="bracketStart">The index of the opening bracket.</param>
    /// <param name="key">The already parsed key, or null for key-less headers.</param>
    /// <param name="malformed">Set when the text looks like a header but is not a valid one.</param>
    /// <param name="mixedDelimiters">
    ///     Set when an unquoted field name contains another delimiter character, i.e. the fields segment uses a different
    ///     delimiter than the bracket declares.
    /// </param>
    private Header? TryParseHeaderSegments(Line line, string content, int bracketStart, string? key, out bool malformed, out bool mixedDelimiters)
    {
        malformed = true;
        mixedDelimiters = false;
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
            var fieldsEnd = IndexOfUnquoted(content.AsSpan(position), '}');

            if (fieldsEnd < 0)
            {
                return null;
            }

            var fieldsText = content.AsSpan(position + 1, fieldsEnd - 1);
            var rawFields = SplitDelimited(fieldsText, delimiter);
            var fieldsColumn = ColumnOf(line, content) + position + 1;
            fields = new string[rawFields.Count];

            for (var i = 0; i < fields.Length; i++)
            {
                var (fieldStart, fieldLength) = rawFields[i];
                var field = fieldsText.Slice(fieldStart, fieldLength);

                // The bracket's delimiter was split on, so any delimiter character left in an unquoted token is another one
                mixedDelimiters |= (field.IsEmpty || field[0] != '"') && field.IndexOfAny(",|\t") >= 0;
                fields[i] = ParseKeyToken(field, line.Number, fieldsColumn + fieldStart);
            }

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
    /// <returns>The tokens, in a list that the next call reuses.</returns>
    private List<Token> SplitDelimited(ReadOnlySpan<char> text, char delimiter)
    {
        var tokens = _tokens;
        tokens.Clear();
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
                tokens.Add(CreateToken(text, start, i));
                start = i + 1;
            }
        }

        tokens.Add(CreateToken(text, start, text.Length));
        return tokens;

        static Token CreateToken(ReadOnlySpan<char> text, int start, int end)
        {
            while (start < end && text[start] == ' ')
            {
                start++;
            }

            while (end > start && text[end - 1] == ' ')
            {
                end--;
            }

            return new Token(start, end - start);
        }
    }

    /// <summary>
    ///     The 1-based column of the first character of <paramref name="suffix"/>, a suffix of the line's content.
    /// </summary>
    private static int ColumnOf(Line line, string suffix) => line.Indent + line.Content.Length - suffix.Length + 1;

    /// <summary>
    ///     Returns the string for an unquoted key, shared with earlier occurrences of the same key in the document.
    /// </summary>
    private string GetKey(ReadOnlySpan<char> text)
    {
        if (!_useKeyCache || _keys == null)
        {
            return text.ToString();
        }

        var slot = (int)((uint)string.GetHashCode(text) % KeyCacheSize);

        if (_keys[slot] is { } cached && text.SequenceEqual(cached))
        {
            return cached;
        }

        return _keys[slot] = text.ToString();
    }

    private string ParseKeyToken(ReadOnlySpan<char> token, int lineNumber, int column)
    {
        if (token.Length >= 2 && token[0] == '"' && FindClosingQuote(token, 0) == token.Length - 1)
        {
            return Unescape(token[1..^1], lineNumber, column + 1);
        }

        return GetKey(token);
    }

    /// <summary>
    ///     Parses a primitive token: quoted string, true/false/null, number, or unquoted string (spec §4, B.4).
    /// </summary>
    /// <param name="token">The token text.</param>
    /// <param name="line">The line the token is on.</param>
    /// <param name="column">The 1-based column of the token's first character, for error positions.</param>
    private static ToonValue ParsePrimitive(ReadOnlySpan<char> token, Line line, int column)
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
                throw new ToonParseException("Unterminated string", line.Number, column);
            }

            if (end != token.Length - 1)
            {
                throw new ToonParseException("Unexpected characters after closing quote", line.Number, column + end + 1);
            }

            return new ToonString(Unescape(token[1..end], line.Number, column + 1));
        }

        if (token.SequenceEqual("true"))
        {
            return ToonBoolean.True;
        }

        if (token.SequenceEqual("false"))
        {
            return ToonBoolean.False;
        }

        if (token.SequenceEqual("null"))
        {
            return ToonNull.Instance;
        }

        return (ToonValue?)TryParseNumber(token) ?? new ToonString(token.ToString());
    }

    /// <summary>
    ///     Parses a token matching the JSON number grammar without forbidden leading zeros.
    /// </summary>
    /// <returns>The number, or null when the token is not a number or not finite as a <see cref="double"/>.</returns>
    internal static ToonNumber? TryParseNumber(ReadOnlySpan<char> token)
    {
        if (!IsJsonNumber(token, out var isInteger))
        {
            return null;
        }

        // Integers of up to 18 digits are exact in a long, a decimal and (as before) the decimal's double value.
        // "-0" keeps the general path, which preserves the sign of the decimal zero.
        if (isInteger && token.Length <= (token[0] == '-' ? 19 : 18) && !token.SequenceEqual("-0") &&
            long.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
        {
            return new ToonNumber((decimal)integer);
        }

        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
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

        return new ToonNumber(value) { ParsedText = token.ToString() };
    }

    /// <summary>
    ///     Matches <c>-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?</c>.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <param name="isInteger">Set when the number has neither a fraction nor an exponent.</param>
    private static bool IsJsonNumber(ReadOnlySpan<char> token, out bool isInteger)
    {
        var i = 0;
        isInteger = false;

        if (token.Length == 0)
        {
            return false;
        }

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

        isInteger = i == token.Length && token.IndexOfAny(".eE") < 0;
        return i == token.Length;
    }

    /// <summary>
    ///     Unescapes the content of a quoted string per spec §7.1.
    /// </summary>
    /// <exception cref="ToonParseException">Thrown for unknown escapes, truncated or surrogate <c>\uXXXX</c> escapes.</exception>
    /// <param name="text">The content between the quotes.</param>
    /// <param name="lineNumber">The line of the string, for error positions.</param>
    /// <param name="column">The 1-based column of the first character of <paramref name="text"/>.</param>
    private static string Unescape(ReadOnlySpan<char> text, int lineNumber, int column)
    {
        if (text.IndexOf('\\') < 0)
        {
            return text.ToString();
        }

        var sb = new StringBuilder(text.Length);

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            if (ch != '\\')
            {
                sb.Append(ch);
                continue;
            }

            // Error columns point at the backslash
            if (++i >= text.Length)
            {
                throw new ToonParseException("Unterminated escape sequence", lineNumber, column + i - 1);
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
                        !int.TryParse(text.Slice(i + 1, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var codePoint))
                    {
                        throw new ToonParseException("Invalid \\u escape: expected four hex digits", lineNumber, column + i - 1);
                    }

                    if (codePoint is >= 0xD800 and <= 0xDFFF)
                    {
                        throw new ToonParseException("Invalid \\u escape: surrogate code points are not allowed", lineNumber, column + i - 1);
                    }

                    sb.Append((char)codePoint);
                    i += 4;
                    break;
                default:
                    throw new ToonParseException($"Invalid escape sequence '\\{text[i]}'", lineNumber, column + i - 1);
            }
        }

        return sb.ToString();
    }

    #endregion
}
