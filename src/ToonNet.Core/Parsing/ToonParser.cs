using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Core.Parsing;

/// <summary>Whether a value read by <see cref="ToonParser"/> is an object, an array or a primitive.</summary>
internal enum ToonNodeKind : byte
{
    Object,
    Array,
    Primitive
}

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

    /// <summary>The text being parsed; lines and tokens are offsets into it.</summary>
    private string _input = string.Empty;
    /// <summary>The lines of the document being parsed, in an array rented from <see cref="ArrayPool{T}.Shared"/>.</summary>
    private Line[] _lines = [];

    /// <summary>The number of lines in <see cref="_lines"/> (the rented array may be longer).</summary>
    private int _lineCount;
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
    /// <param name="Start">Where the text after the indentation starts in the input.</param>
    /// <param name="Length">The length of that text, without trailing spaces.</param>
    /// <param name="IsBlank">Whether the line contains only spaces and tabs.</param>
    internal readonly record struct Line(int Number, int Depth, int Indent, int Start, int Length, bool IsBlank);

    /// <summary>
    ///     A delimited token: where it starts in the split text (after leading spaces) and its length (without
    ///     trailing spaces).
    /// </summary>
    internal readonly record struct Token(int Start, int Length);

    /// <summary>
    ///     A parsed array header: <c>key?[N&lt;delim?&gt;]{fields?}:</c> (spec §6).
    /// </summary>
    /// <param name="Key">The key, or null for a key-less header.</param>
    /// <param name="Length">The declared length.</param>
    /// <param name="Delimiter">The delimiter declared in the brackets (comma by default).</param>
    /// <param name="Fields">The tabular field names, or null.</param>
    /// <param name="InlineStart">Where the inline values after the colon start in the input.</param>
    /// <param name="InlineLength">The length of the inline values (0 when there are none).</param>
    internal readonly record struct Header(string? Key, int Length, char Delimiter, string[]? Fields, int InlineStart, int InlineLength);

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
        BeginDocument(input);

        try
        {
            var root = ReadRoot();
            var value = BuildValue(root);
            EndDocument();
            return new ToonDocument(value);
        }
        finally
        {
            ReleaseDocument();
        }
    }

    /// <summary>
    ///     Checks a document without building it: the same checks and errors as <see cref="Parse"/>.
    /// </summary>
    /// <exception cref="ToonParseException">Thrown when the input is invalid.</exception>
    internal void Validate(string input)
    {
        BeginDocument(input);

        try
        {
            Skip(ReadRoot());
            EndDocument();
        }
        finally
        {
            ReleaseDocument();
        }
    }

    /// <summary>
    ///     Prepares to read <paramref name="input"/> with the reader methods (<see cref="ReadRoot"/>, cursors);
    ///     <see cref="ReleaseDocument"/> must follow.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
    /// <exception cref="ToonParseException">Thrown for errors in a line's indentation.</exception>
    internal void BeginDocument(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        _ = _options.ValidatedMaxDepth;

        _input = input;
        _lines = ArrayPool<Line>.Shared.Rent(input.AsSpan().Count('\n') + 1);
        _position = 0;
        _depth = 0;
        _headerSpans = 0;
        _keyCount = 0;
        _headers.Clear();

        SplitLines(input);

        _useKeyCache = _lineCount >= KeyCacheMinLines;

        if (_useKeyCache)
        {
            _keys ??= new string?[KeyCacheSize];
            Array.Clear(_keys);
        }
    }

    /// <summary>Checks that nothing follows the root value.</summary>
    /// <exception cref="ToonParseException">Thrown when content follows the root value.</exception>
    internal void EndDocument() => EnsureFullyConsumed();

    /// <summary>Returns the line table to the pool and drops the reference to the input.</summary>
    internal void ReleaseDocument()
    {
        ArrayPool<Line>.Shared.Return(_lines);
        _lines = [];
        _lineCount = 0;
        _input = string.Empty;
        Array.Clear(_keyStack);
        _keyCount = 0;
        _headers.Clear();
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
    ///     Splits the input into <see cref="_lines"/> (offsets into the input, no strings), preparing the lines as spec
    ///     §12 orders: a byte-order mark at the start is removed, a CR before each LF and trailing spaces are excluded,
    ///     and comment lines (§5.1) are dropped.
    /// </summary>
    private void SplitLines(string input)
    {
        var lines = _lines;
        var count = 0;
        var start = input.StartsWith('\uFEFF') ? 1 : 0;

        for (var number = 1; start <= input.Length; number++)
        {
            var newline = input.AsSpan(start).IndexOf('\n');
            var end = newline < 0 ? input.Length : start + newline;
            var length = end > start && input[end - 1] == '\r' ? end - start - 1 : end - start;
            var text = input.AsSpan(start, length).TrimEnd(' ');

            // A comment line: only spaces before '#'
            var first = text.IndexOfAnyExcept(' ');

            if (first < 0 || text[first] != '#')
            {
                lines[count++] = CreateLine(input, start, text.Length, number);
            }

            start = end + 1;
        }

        _lineCount = count;
    }

    /// <summary>
    ///     Creates the line that starts at <paramref name="start"/> in <paramref name="input"/> (without its line break
    ///     and trailing spaces).
    /// </summary>
    private Line CreateLine(string input, int start, int length, int number)
    {
        var raw = input.AsSpan(start, length);
        var index = raw.IndexOfAnyExcept(' ', '\t');

        if (index < 0)
        {
            index = raw.Length;
        }

        var indentation = raw[..index];
        var tabs = indentation.Count('\t');
        var columns = index;

        if (tabs > 0)
        {
            // A tab is not whitespace that leaves a line blank (§12); non-strict mode counts it as one level (§14.4)
            if (_options.StrictMode)
            {
                throw new ToonParseException("Tabs are not allowed in indentation", number, indentation.IndexOf('\t') + 1);
            }

            columns = (index - tabs) + tabs * _options.IndentSize;
        }

        var content = raw[index..];

        if (content.IsEmpty)
        {
            return new Line(number, 0, columns, start, 0, IsBlank: true);
        }

        if (_options.StrictMode && columns % _options.IndentSize != 0)
        {
            throw new ToonParseException(
                $"Indentation of {columns} spaces is not a multiple of the indent size {_options.IndentSize}", number, 1);
        }

        return new Line(number, columns / _options.IndentSize, columns, start + index, content.Length, IsBlank: false);
    }

    /// <summary>
    ///     Skips blank lines and returns the next non-blank line without consuming it.
    /// </summary>
    /// <param name="skippedBlank">Whether blank lines were skipped.</param>
    private Line? PeekLine(out bool skippedBlank)
    {
        var position = _position;
        skippedBlank = false;

        while (position < _lineCount && _lines[position].IsBlank)
        {
            position++;
            skippedBlank = true;
        }

        return position < _lineCount ? _lines[position] : null;
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

    #region Reader

    // The parser reads a document through value references and cursors, so the same code builds the ToonValue tree
    // (Parse) and lets the serializer read values straight into objects. Every check of the spec happens while the
    // cursors advance, in the same order as a recursive descent over the document: callers read each value fully
    // before advancing the cursor that contains it.

    /// <summary>Where a value starts; decided when the reader reaches it.</summary>
    internal enum RefKind : byte
    {
        /// <summary>A primitive token at <see cref="ValueRef.Start"/>.</summary>
        Primitive,

        /// <summary>A field missing from a short tabular row (non-strict mode): null.</summary>
        MissingValue,

        /// <summary>The fields of the root object (no nesting level is entered).</summary>
        RootObject,

        /// <summary>An empty document, which is an empty object.</summary>
        EmptyRootObject,

        /// <summary><c>key:</c> followed by fields at <see cref="ValueRef.Depth"/>.</summary>
        NestedObject,

        /// <summary>A bare <c>-</c> list item.</summary>
        EmptyObject,

        /// <summary>
        ///     <c>- key: ...</c>: the first field at <see cref="ValueRef.Start"/>, the others one level below the item
        ///     depth <see cref="ValueRef.Depth"/>.
        /// </summary>
        ListItemObject,

        /// <summary>A tabular row: the fields of the header with the tokens of the line.</summary>
        Row,

        /// <summary>An array after its header; list items or rows at <see cref="ValueRef.Depth"/>.</summary>
        HeaderArray,

        /// <summary>Non-strict mode: list items at <see cref="ValueRef.Depth"/> under a bare <c>key:</c>.</summary>
        BareList,

        /// <summary><c>key: []</c>.</summary>
        EmptyArray,

        /// <summary>A document that is only <c>[]</c>.</summary>
        RootEmptyArray
    }

    /// <summary>A value the reader has reached.</summary>
    /// <remarks>
    ///     It refers to its line and header by index (<see cref="ToonParser.LineOf"/>, <see cref="ToonParser.HeaderOf"/>),
    ///     which keeps it small: it is copied for every field and item.
    /// </remarks>
    internal readonly struct ValueRef(RefKind kind, int lineIndex, int start = 0, int length = 0, int column = 0, int depth = 0, int headerIndex = -1)
    {
        public readonly RefKind Kind = kind;

        /// <summary>The index of the line the value starts on, for nesting checks and error positions.</summary>
        public readonly int LineIndex = lineIndex;

        /// <summary>The token (or list item text) offset in the input.</summary>
        public readonly int Start = start;

        public readonly int Length = length;

        /// <summary>The 1-based column of a token, for error positions.</summary>
        public readonly int Column = column;

        public readonly int Depth = depth;

        /// <summary>The index of the array header (header arrays and tabular rows), or -1.</summary>
        public readonly int HeaderIndex = headerIndex;

        public ToonNodeKind NodeKind => Kind switch
        {
            RefKind.Primitive or RefKind.MissingValue => ToonNodeKind.Primitive,
            RefKind.HeaderArray or RefKind.BareList or RefKind.EmptyArray or RefKind.RootEmptyArray => ToonNodeKind.Array,
            _ => ToonNodeKind.Object
        };
    }

    internal enum ObjectMode : byte
    {
        Empty,
        Block,
        ListItemFirst,
        Row
    }

    /// <summary>Reads the fields of an object; advanced with <see cref="NextProperty"/>.</summary>
    internal struct ObjectCursor
    {
        internal ObjectMode Mode;
        internal int Depth;
        internal int LineIndex;
        internal int FirstStart;
        internal int FirstLength;
        internal string[]? Fields;
        internal int FieldIndex;
        internal bool ExitNesting;
        internal bool Done;

        // Strict mode: the keys read so far are on the parser's key stack from KeysStart (or, for large objects, in
        // LargeKeys); the last key is checked once its value has been read, as the tree-based parser did
        internal int KeysStart;
        internal string? PendingKey;
        internal int PendingLineIndex;
        internal HashSet<string>? LargeKeys;
    }

    internal enum ArrayForm : byte
    {
        Empty,
        Inline,
        Tabular,
        List
    }

    /// <summary>Reads the items of an array; advanced with <see cref="NextItem"/>.</summary>
    internal struct ArrayCursor
    {
        internal ArrayForm Form;
        internal int HeaderIndex;
        internal bool HasHeader;
        internal int DeclaredLength;
        internal char Delimiter;
        internal string[]? TabularFields;
        internal int InlineStart;
        internal int LineIndex;
        internal int Depth;
        internal int Count;
        internal int TokenIndex;
        internal int TokenCount;
        internal int InlineColumn;
        internal bool RowsEntered;
        internal bool ItemOpen;
        internal bool ExitNesting;
        internal bool Done;

        /// <summary>The number of items to expect (the declared length, bounded by the lines left), for capacities.</summary>
        internal int CapacityHint;

        /// <summary>The tabular field names, or null.</summary>
        public readonly string[]? Fields => Form == ArrayForm.Tabular ? TabularFields : null;
    }

    /// <summary>
    ///     The keys of the objects being read (strict mode), for duplicate checks: each object's keys from its
    ///     <see cref="ObjectCursor.KeysStart"/> up to <see cref="_keyCount"/>.
    /// </summary>
    private string[] _keyStack = new string[32];

    private int _keyCount;

    /// <summary>
    ///     The headers of the arrays being read, referred to by index; an array's header is removed when the array ends.
    /// </summary>
    private readonly List<Header> _headers = [];

    /// <summary>The line at <paramref name="index"/>.</summary>
    internal ref readonly Line LineOf(int index) => ref _lines[index];

    /// <summary>The header at <paramref name="index"/>.</summary>
    internal ref readonly Header HeaderOf(int index) => ref System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_headers)[index];

    private int AddHeader(in Header header)
    {
        _headers.Add(header);
        return _headers.Count - 1;
    }

    /// <summary>
    ///     Skips blank lines and returns the index of the next non-blank line without consuming it, or -1.
    /// </summary>
    private int PeekIndex(out bool skippedBlank)
    {
        var position = _position;
        skippedBlank = false;

        while (position < _lineCount && _lines[position].IsBlank)
        {
            position++;
            skippedBlank = true;
        }

        return position < _lineCount ? position : -1;
    }

    /// <summary>Objects with more keys than this check duplicates with a hash set.</summary>
    private const int LinearKeyCheckLimit = 16;

    /// <summary>
    ///     Determines the root form (spec §5): an array with a key-less header, a single primitive or <c>[]</c>, or an
    ///     object.
    /// </summary>
    internal ValueRef ReadRoot()
    {
        var firstIndex = PeekIndex(out _);

        if (firstIndex < 0)
        {
            return new ValueRef(RefKind.EmptyRootObject, -1);
        }

        var firstLine = _lines[firstIndex];

        if (firstLine.Depth != 0 && _options.StrictMode)
        {
            throw new ToonParseException("The first line of a document must not be indented", firstLine.Number, firstLine.Indent + 1);
        }

        var content = ContentOf(firstLine);

        if (TryParseHeader(firstLine, content, requireKey: false) is { Key: null } rootHeader)
        {
            NextLine();
            _depth = -1; // the root array is at depth 0, like the root object
            return new ValueRef(RefKind.HeaderArray, firstIndex, depth: firstLine.Depth + 1, headerIndex: AddHeader(rootHeader));
        }

        if (HasSingleNonBlankLine())
        {
            if (content.SequenceEqual("[]"))
            {
                NextLine();
                return new ValueRef(RefKind.RootEmptyArray, firstIndex);
            }

            if (!IsKeyValueLine(content))
            {
                NextLine();
                return new ValueRef(RefKind.Primitive, firstIndex, firstLine.Start, firstLine.Length, ColumnOf(firstLine, content));
            }
        }

        return new ValueRef(RefKind.RootObject, firstIndex, depth: firstLine.Depth);
    }

    private bool HasSingleNonBlankLine()
    {
        var found = false;

        foreach (var line in _lines.AsSpan(0, _lineCount))
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

    /// <summary>Starts reading the fields of an object value.</summary>
    internal ObjectCursor BeginObject(in ValueRef value)
    {
        var cursor = new ObjectCursor { KeysStart = _keyCount };

        switch (value.Kind)
        {
            case RefKind.RootObject:
                cursor.Mode = ObjectMode.Block;
                cursor.Depth = value.Depth;
                break;
            case RefKind.NestedObject:
                EnterNesting(_lines[value.LineIndex]);
                cursor.Mode = ObjectMode.Block;
                cursor.Depth = value.Depth;
                cursor.ExitNesting = true;
                break;
            case RefKind.EmptyObject:
                CheckEmptyContainerNesting(_lines[value.LineIndex]);
                break;
            case RefKind.ListItemObject:
                // The first field sits on the hyphen line; its nested content is two levels deeper
                EnterNesting(_lines[value.LineIndex]);
                cursor.Mode = ObjectMode.ListItemFirst;
                cursor.LineIndex = value.LineIndex;
                cursor.FirstStart = value.Start;
                cursor.FirstLength = value.Length;
                cursor.Depth = value.Depth;
                cursor.ExitNesting = true;
                break;
            case RefKind.Row:
                cursor.Mode = ObjectMode.Row;
                cursor.LineIndex = value.LineIndex;
                cursor.Fields = HeaderOf(value.HeaderIndex).Fields;
                break;
            case RefKind.EmptyRootObject:
                break;
            default:
                throw new InvalidOperationException("The value is not an object.");
        }

        return cursor;
    }

    /// <summary>
    ///     Reads the next field of an object, or ends the object (returning false) when there is none.
    /// </summary>
    /// <exception cref="ToonParseException">Thrown for errors in the document.</exception>
    internal bool NextProperty(ref ObjectCursor cursor, out string key, out ValueRef value)
    {
        CheckPendingKey(ref cursor);

        switch (cursor.Mode)
        {
            case ObjectMode.ListItemFirst:
            {
                var itemDepth = cursor.Depth;
                cursor.Mode = ObjectMode.Block;
                cursor.Depth = itemDepth + 1;
                key = ReadField(cursor.LineIndex, _input.AsSpan(cursor.FirstStart, cursor.FirstLength), itemDepth + 2, out value);
                SetPendingKey(ref cursor, key, cursor.LineIndex);
                return true;
            }
            case ObjectMode.Block:
                var index = PeekIndex(out var skippedBlank);

                if (index >= 0 && _lines[index].Depth >= cursor.Depth)
                {
                    ref readonly var line = ref _lines[index];

                    // The object continues after the blank line, so the blank is inside the enclosing array's header span
                    if (skippedBlank && _headerSpans > 0 && _options.StrictMode)
                    {
                        throw new ToonParseException("Blank lines are not allowed inside an array", line.Number - 1, 1);
                    }

                    if (line.Depth > cursor.Depth)
                    {
                        throw new ToonParseException("Unexpected indentation", line.Number, line.Indent + 1);
                    }

                    _position = index + 1;
                    key = ReadField(index, ContentOf(line), cursor.Depth + 1, out value);
                    SetPendingKey(ref cursor, key, index);
                    return true;
                }

                break;
            case ObjectMode.Row:
                if (cursor.FieldIndex < cursor.Fields!.Length)
                {
                    var field = cursor.FieldIndex++;
                    key = cursor.Fields[field];

                    if (field < _tokens.Count)
                    {
                        var token = _tokens[field];
                        ref readonly var rowLine = ref _lines[cursor.LineIndex];
                        value = new ValueRef(RefKind.Primitive, cursor.LineIndex, rowLine.Start + token.Start, token.Length,
                                             rowLine.Indent + 1 + token.Start);
                    }
                    else
                    {
                        value = new ValueRef(RefKind.MissingValue, cursor.LineIndex);
                    }

                    return true;
                }

                break;
        }

        EndObject(ref cursor);
        key = null!;
        value = default;
        return false;
    }

    private void SetPendingKey(ref ObjectCursor cursor, string key, int lineIndex)
    {
        if (_options.StrictMode)
        {
            cursor.PendingKey = key;
            cursor.PendingLineIndex = lineIndex;
        }
    }

    /// <summary>Checks the last key read for a duplicate (strict mode), after its value has been read.</summary>
    private void CheckPendingKey(ref ObjectCursor cursor)
    {
        if (cursor.PendingKey is not { } key)
        {
            return;
        }

        cursor.PendingKey = null;
        bool duplicate;

        if (cursor.LargeKeys != null)
        {
            duplicate = !cursor.LargeKeys.Add(key);
        }
        else
        {
            var keys = _keyStack.AsSpan(cursor.KeysStart, _keyCount - cursor.KeysStart);
            duplicate = false;

            foreach (var seen in keys)
            {
                if (string.Equals(seen, key))
                {
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate)
            {
                if (keys.Length < LinearKeyCheckLimit)
                {
                    if (_keyCount == _keyStack.Length)
                    {
                        Array.Resize(ref _keyStack, _keyStack.Length * 2);
                    }

                    _keyStack[_keyCount++] = key;
                }
                else
                {
                    cursor.LargeKeys = new HashSet<string>(keys.ToArray(), StringComparer.Ordinal) { key };
                    _keyCount = cursor.KeysStart;
                }
            }
        }

        if (duplicate)
        {
            ref readonly var line = ref _lines[cursor.PendingLineIndex];
            throw new ToonParseException($"Duplicate key '{key}'", line.Number, line.Indent + 1);
        }
    }

    private void EndObject(ref ObjectCursor cursor)
    {
        if (cursor.Done)
        {
            return;
        }

        cursor.Done = true;

        // The keys stay in the array until they are overwritten or the document is released
        _keyCount = cursor.KeysStart;

        if (cursor.ExitNesting)
        {
            _depth--;
        }
    }

    /// <summary>
    ///     Reads a key-value or array-header line (<paramref name="content"/> without indentation or list marker);
    ///     nested content is read at <paramref name="childDepth"/>.
    /// </summary>
    /// <returns>The key.</returns>
    private string ReadField(int lineIndex, ReadOnlySpan<char> content, int childDepth, out ValueRef value)
    {
        var line = _lines[lineIndex];

        if (TryParseHeader(line, content, requireKey: true) is { } header)
        {
            value = new ValueRef(RefKind.HeaderArray, lineIndex, depth: childDepth, headerIndex: AddHeader(header));
            return header.Key!;
        }

        var colon = FindKeyValueColon(content, line, out var key);
        var restStart = colon + 1;

        while (restStart < content.Length && content[restStart] == ' ')
        {
            restStart++;
        }

        // Line content has no trailing spaces, so the value runs to the end
        var rest = content[restStart..];

        if (rest.Length == 0 && PeekLine() is { } next && next.Depth == childDepth && IsListItem(ContentOf(next)))
        {
            // `key:` always opens an object (§8); list items need a header. Non-strict mode reads them as an array.
            if (_options.StrictMode)
            {
                throw new ToonParseException($"List items under '{key}' require an array header such as '{key}[N]:'",
                                             next.Number, next.Indent + 1);
            }

            value = new ValueRef(RefKind.BareList, lineIndex, depth: childDepth);
        }
        else if (rest.Length == 0)
        {
            value = new ValueRef(RefKind.NestedObject, lineIndex, depth: childDepth);
        }
        else if (rest.SequenceEqual("[]"))
        {
            value = new ValueRef(RefKind.EmptyArray, lineIndex);
        }
        else
        {
            value = new ValueRef(RefKind.Primitive, lineIndex, OffsetOf(rest), rest.Length, ColumnOf(line, content) + restStart);
        }

        return key;
    }

    /// <summary>Starts reading the items of an array value: inline values, tabular rows or list items (spec §9).</summary>
    internal ArrayCursor BeginArray(in ValueRef value)
    {
        var cursor = new ArrayCursor { LineIndex = value.LineIndex };

        switch (value.Kind)
        {
            case RefKind.HeaderArray:
            {
                ref readonly var header = ref HeaderOf(value.HeaderIndex);
                var line = _lines[value.LineIndex];
                EnterNesting(line);
                cursor.HasHeader = true;
                cursor.HeaderIndex = value.HeaderIndex;
                cursor.DeclaredLength = header.Length;
                cursor.Delimiter = header.Delimiter;
                cursor.ExitNesting = true;
                cursor.Depth = value.Depth;

                if (header.Fields != null)
                {
                    if (header.InlineLength > 0)
                    {
                        throw new ToonParseException("Unexpected values after a tabular array header", line.Number, line.Indent + 1);
                    }

                    cursor.Form = ArrayForm.Tabular;
                    cursor.TabularFields = header.Fields;
                    cursor.CapacityHint = ExpectedItemCapacity(header);
                }
                else if (header.InlineLength > 0)
                {
                    var inline = _input.AsSpan(header.InlineStart, header.InlineLength);
                    cursor.Form = ArrayForm.Inline;
                    cursor.InlineStart = header.InlineStart;
                    cursor.InlineColumn = ColumnOf(line, inline);
                    cursor.TokenCount = SplitDelimited(inline, header.Delimiter).Count;
                    cursor.CapacityHint = cursor.TokenCount;
                }
                else
                {
                    cursor.Form = ArrayForm.List;
                    cursor.CapacityHint = ExpectedItemCapacity(header);
                }

                break;
            }
            case RefKind.BareList:
                EnterNesting(_lines[value.LineIndex]);
                cursor.Form = ArrayForm.List;
                cursor.Depth = value.Depth;
                cursor.ExitNesting = true;
                break;
            case RefKind.EmptyArray:
                CheckEmptyContainerNesting(_lines[value.LineIndex]);
                break;
            case RefKind.RootEmptyArray:
                break;
            default:
                throw new InvalidOperationException("The value is not an array.");
        }

        return cursor;
    }

    /// <summary>
    ///     Reads the next item of an array, or ends the array (returning false) when there is none.
    /// </summary>
    /// <exception cref="ToonParseException">Thrown for errors in the document.</exception>
    internal bool NextItem(ref ArrayCursor cursor, out ValueRef item)
    {
        switch (cursor.Form)
        {
            case ArrayForm.Inline:
                if (cursor.TokenIndex < cursor.TokenCount)
                {
                    var token = _tokens[cursor.TokenIndex++];
                    item = new ValueRef(RefKind.Primitive, cursor.LineIndex, cursor.InlineStart + token.Start, token.Length,
                                        cursor.InlineColumn + token.Start);
                    cursor.Count++;
                    return true;
                }

                break;
            case ArrayForm.Tabular:
            {
                var index = PeekIndex(out var skippedRowBlank);

                if (index >= 0 && _lines[index].Depth == cursor.Depth && IsTabularRow(ContentOf(_lines[index]), cursor.Delimiter))
                {
                    ref readonly var row = ref _lines[index];

                    if (skippedRowBlank && (cursor.Count > 0 || _headerSpans > 0) && _options.StrictMode)
                    {
                        throw new ToonParseException("Blank lines are not allowed inside a tabular array", row.Number - 1, 1);
                    }

                    _position = index + 1;
                    var values = SplitDelimited(ContentOf(row), cursor.Delimiter);
                    var fields = cursor.TabularFields!;

                    if (values.Count != fields.Length && _options.StrictMode)
                    {
                        throw new ToonParseException($"Row has {values.Count} values but expected {fields.Length}", row.Number, row.Indent + 1);
                    }

                    // Rows are objects one level below the array
                    if (cursor.Count == 0)
                    {
                        EnterNesting(row);
                        cursor.RowsEntered = true;
                    }

                    cursor.Count++;
                    item = new ValueRef(RefKind.Row, index, headerIndex: cursor.HeaderIndex);
                    return true;
                }

                break;
            }
            case ArrayForm.List:
            {
                if (cursor.ItemOpen)
                {
                    _headerSpans--;
                    cursor.ItemOpen = false;
                }

                var index = PeekIndex(out var skippedBlank);

                if (index >= 0 && _lines[index].Depth == cursor.Depth && IsListItem(ContentOf(_lines[index])))
                {
                    ref readonly var line = ref _lines[index];

                    // Inside this array's span after the first item, or inside an enclosing array's span
                    if (skippedBlank && (cursor.Count > 0 || _headerSpans > 0) && _options.StrictMode)
                    {
                        throw new ToonParseException("Blank lines are not allowed inside an array", line.Number - 1, 1);
                    }

                    _position = index + 1;
                    _headerSpans++;
                    cursor.ItemOpen = true;
                    cursor.Count++;
                    item = ReadListItem(index, cursor.Depth);
                    return true;
                }

                break;
            }
        }

        EndArray(ref cursor);
        item = default;
        return false;
    }

    private void EndArray(ref ArrayCursor cursor)
    {
        if (cursor.Done)
        {
            return;
        }

        cursor.Done = true;

        switch (cursor.Form)
        {
            case ArrayForm.Tabular:
                if (cursor.RowsEntered)
                {
                    _depth--;
                }

                if (PeekLine() is { } nextRow && nextRow.Depth > cursor.Depth)
                {
                    throw new ToonParseException("Unexpected indentation in tabular array", nextRow.Number, nextRow.Indent + 1);
                }

                break;
            case ArrayForm.List:
                if (cursor.ItemOpen)
                {
                    _headerSpans--;
                    cursor.ItemOpen = false;
                }

                if (PeekLine() is { } next && next.Depth > cursor.Depth)
                {
                    throw new ToonParseException("Unexpected indentation in list array", next.Number, next.Indent + 1);
                }

                break;
        }

        if (cursor.HasHeader && _options.StrictMode && cursor.Count != cursor.DeclaredLength)
        {
            ref readonly var line = ref _lines[cursor.LineIndex];
            throw new ToonParseException($"Array length mismatch: expected {cursor.DeclaredLength}, got {cursor.Count}", line.Number, line.Indent + 1);
        }

        if (cursor.ExitNesting)
        {
            _depth--;
        }

        // Arrays nest, so their headers form a stack: the header and anything read after it are no longer needed
        if (cursor.HasHeader && cursor.HeaderIndex < _headers.Count)
        {
            _headers.RemoveRange(cursor.HeaderIndex, _headers.Count - cursor.HeaderIndex);
        }
    }

    /// <summary>
    ///     The declared length of an array, bounded by the lines left, which is the most a list or tabular body can hold;
    ///     a header such as <c>[2000000000]:</c> does not reserve memory for items that are not there.
    /// </summary>
    private int ExpectedItemCapacity(in Header header) => Math.Min(header.Length, _lineCount - _position);

    /// <summary>
    ///     Decides what a list item line at <paramref name="itemDepth"/> holds (spec §9.4, §10).
    /// </summary>
    private ValueRef ReadListItem(int lineIndex, int itemDepth)
    {
        var line = _lines[lineIndex];
        var content = ContentOf(line);

        if (content.SequenceEqual("-"))
        {
            return new ValueRef(RefKind.EmptyObject, lineIndex); // empty object list item
        }

        var rest = content[2..].TrimStart(' ');

        // Inner array: - [M]: ...
        if (TryParseHeader(line, rest, requireKey: false) is { Key: null } innerHeader)
        {
            return new ValueRef(RefKind.HeaderArray, lineIndex, depth: itemDepth + 1, headerIndex: AddHeader(innerHeader));
        }

        if (!IsKeyValueLine(rest))
        {
            return new ValueRef(RefKind.Primitive, lineIndex, OffsetOf(rest), rest.Length, ColumnOf(line, rest));
        }

        // Object with its first field on the hyphen line
        return new ValueRef(RefKind.ListItemObject, lineIndex, OffsetOf(rest), rest.Length, depth: itemDepth);
    }

    /// <summary>The text of a primitive token (empty for a missing tabular value).</summary>
    internal ReadOnlySpan<char> TokenOf(in ValueRef value) => _input.AsSpan(value.Start, value.Length);

    /// <summary>Whether a primitive value is <c>null</c> (the unquoted token, or a missing tabular value).</summary>
    internal bool IsNull(in ValueRef value) =>
        value.Kind == RefKind.MissingValue || (value.Kind == RefKind.Primitive && TokenOf(value).SequenceEqual("null"));

    /// <summary>
    ///     Reads a quoted string token (its first character is a quote) with the checks of <see cref="ParsePrimitive"/>.
    /// </summary>
    /// <exception cref="ToonParseException">Thrown for an unterminated string, text after the closing quote or invalid escapes.</exception>
    internal string ReadQuotedString(in ValueRef value)
    {
        var token = TokenOf(value);
        var line = _lines[value.LineIndex];
        var end = FindClosingQuote(token, 0);

        if (end < 0)
        {
            throw new ToonParseException("Unterminated string", line.Number, value.Column);
        }

        if (end != token.Length - 1)
        {
            throw new ToonParseException("Unexpected characters after closing quote", line.Number, value.Column + end + 1);
        }

        return Unescape(token[1..end], line.Number, value.Column + 1);
    }

    /// <summary>
    ///     Reads past a value without building it, making the same checks as reading it (a quoted token is unescaped and
    ///     dropped).
    /// </summary>
    internal void Skip(in ValueRef value)
    {
        switch (value.NodeKind)
        {
            case ToonNodeKind.Primitive:
                if (value.Kind == RefKind.Primitive && value.Length > 0 && _input[value.Start] == '"')
                {
                    ReadQuotedString(value);
                }

                break;
            case ToonNodeKind.Array:
            {
                var cursor = BeginArray(value);

                while (NextItem(ref cursor, out var item))
                {
                    Skip(item);
                }

                break;
            }
            default:
            {
                var cursor = BeginObject(value);

                while (NextProperty(ref cursor, out _, out var fieldValue))
                {
                    Skip(fieldValue);
                }

                break;
            }
        }
    }

    /// <summary>Converts a primitive value to a <see cref="ToonValue"/>.</summary>
    internal ToonValue ReadPrimitive(in ValueRef value)
    {
        return value.Kind == RefKind.MissingValue ? ToonNull.Instance : ParsePrimitive(TokenOf(value), _lines[value.LineIndex], value.Column);
    }

    /// <summary>
    ///     Reads a value into a <see cref="ToonValue"/> tree.
    /// </summary>
    internal ToonValue BuildValue(in ValueRef value)
    {
        switch (value.NodeKind)
        {
            case ToonNodeKind.Primitive:
                return ReadPrimitive(value);
            case ToonNodeKind.Array:
            {
                var cursor = BeginArray(value);
                var items = new List<ToonValue>(cursor.CapacityHint);

                while (NextItem(ref cursor, out var item))
                {
                    items.Add(BuildValue(item));
                }

                return new ToonArray(items) { FieldNames = cursor.Fields };
            }
            default:
            {
                if (value.Kind == RefKind.Row)
                {
                    return BuildRow(value);
                }

                var cursor = BeginObject(value);
                var obj = new ToonObject();

                while (NextProperty(ref cursor, out var key, out var fieldValue))
                {
                    obj.Properties[key] = BuildValue(fieldValue);
                }

                return obj;
            }
        }
    }

    /// <summary>
    ///     Builds a tabular row object from the tokens of its line, like reading it with a cursor (rows have no checks
    ///     of their own); missing values are null.
    /// </summary>
    private ToonObject BuildRow(in ValueRef value)
    {
        var fields = HeaderOf(value.HeaderIndex).Fields!;
        var line = _lines[value.LineIndex];
        var content = ContentOf(line);
        var row = new ToonObject(fields.Length);
        var tokens = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_tokens);

        for (var i = 0; i < fields.Length; i++)
        {
            row.Properties[fields[i]] = i < tokens.Length
                ? ParsePrimitive(content.Slice(tokens[i].Start, tokens[i].Length), line, line.Indent + 1 + tokens[i].Start)
                : ToonNull.Instance;
        }

        return row;
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

    private static bool IsListItem(ReadOnlySpan<char> content)
    {
        return content.SequenceEqual("-") || content.StartsWith("- ", StringComparison.Ordinal);
    }

    /// <summary>
    ///     Checks whether a line inside a tabular array is a row rather than a key-value line (spec §9.3).
    /// </summary>
    private static bool IsTabularRow(ReadOnlySpan<char> content, char delimiter)
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
    private static bool IsKeyValueLine(ReadOnlySpan<char> content)
    {
        if (content.Length > 0 && content[0] == '"')
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
    private int FindKeyValueColon(ReadOnlySpan<char> content, Line line, out string key)
    {
        if (content.Length > 0 && content[0] == '"')
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

            key = Unescape(content.Slice(1, end - 1), line.Number, ColumnOf(line, content) + 1);
            return end + 1;
        }

        var colon = content.IndexOf(':');

        if (colon <= 0)
        {
            throw new ToonParseException(colon == 0 ? "Missing key before colon" : "Missing colon after key", line.Number, line.Indent + 1);
        }

        key = GetKey(content[..colon].TrimEnd(' '));
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
    private Header? TryParseHeader(Line line, ReadOnlySpan<char> content, bool requireKey)
    {
        string? key = null;
        int bracketStart;

        if (content.Length > 0 && content[0] == '"')
        {
            var end = FindClosingQuote(content, 0);

            if (end < 0 || end + 1 >= content.Length || content[end + 1] != '[')
            {
                return null;
            }

            key = Unescape(content.Slice(1, end - 1), line.Number, ColumnOf(line, content) + 1);
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
                key = GetKey(content[..bracketStart]);
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
    private Header? TryParseHeaderSegments(Line line, ReadOnlySpan<char> content, int bracketStart, string? key, out bool malformed,
                                           out bool mixedDelimiters)
    {
        malformed = true;
        mixedDelimiters = false;
        var bracketEnd = content[bracketStart..].IndexOf(']');

        if (bracketEnd < 0)
        {
            return null;
        }

        bracketEnd += bracketStart;
        var inner = content.Slice(bracketStart + 1, bracketEnd - bracketStart - 1);
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

            var fieldsText = content.Slice(position + 1, fieldsEnd - 1);
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
        return new Header(key, length, delimiter, fields, inlineValues.IsEmpty ? 0 : OffsetOf(inlineValues), inlineValues.Length);
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
    private static int ColumnOf(Line line, ReadOnlySpan<char> suffix) => line.Indent + line.Length - suffix.Length + 1;

    /// <summary>The text of a line after its indentation.</summary>
    private ReadOnlySpan<char> ContentOf(Line line) => _input.AsSpan(line.Start, line.Length);

    /// <summary>Where <paramref name="part"/>, a slice of the input, starts in the input.</summary>
    private int OffsetOf(ReadOnlySpan<char> part) =>
        (int)(Unsafe.ByteOffset(ref MemoryMarshal.GetReference(_input.AsSpan()), ref MemoryMarshal.GetReference(part)) / sizeof(char));

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
        if (!TryParseNumberParts(token, out var value, out var exact, out var hasExact))
        {
            return null;
        }

        return hasExact ? new ToonNumber(exact, value) : new ToonNumber(value) { ParsedText = token.ToString() };
    }

    /// <summary>
    ///     Parses a number token like <see cref="TryParseNumber"/> without creating a <see cref="ToonNumber"/>: the
    ///     number's <see cref="ToonNumber.Value"/> is <paramref name="value"/> (the double closest to the text) and its
    ///     <see cref="ToonNumber.DecimalValue"/> is <paramref name="exact"/> when <paramref name="hasExact"/>.
    /// </summary>
    internal static bool TryParseNumberParts(ReadOnlySpan<char> token, out double value, out decimal exact, out bool hasExact)
    {
        value = 0;
        exact = 0;
        hasExact = false;

        if (!IsJsonNumber(token, out var isInteger))
        {
            return false;
        }

        // Integers of up to 18 digits are exact in a long, a decimal and (as before) the decimal's double value.
        // "-0" keeps the general path, which preserves the sign of the decimal zero.
        if (isInteger && token.Length <= (token[0] == '-' ? 19 : 18) && !token.SequenceEqual("-0") &&
            long.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
        {
            exact = integer;
            value = integer; // the conversion from long rounds to the nearest double, as parsing the text does
            hasExact = true;
            return true;
        }

        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || !double.IsFinite(value))
        {
            return false;
        }

        // Keep the exact value when it fits in a decimal. Values below decimal precision (e.g. 1e-30) round to a
        // different value there, so they only keep the double.
        if (decimal.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out exact) &&
            Math.Abs((double)exact - value) <= Math.Abs(value) * 1e-12)
        {
            hasExact = true;
        }

        return true;
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

        // Unescaped text is never longer than the escaped text
        char[]? rented = null;
        var buffer = text.Length <= 256 ? stackalloc char[256] : rented = ArrayPool<char>.Shared.Rent(text.Length);

        try
        {
            var length = 0;

            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];

                if (ch != '\\')
                {
                    buffer[length++] = ch;
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
                        buffer[length++] = '\\';
                        break;
                    case '"':
                        buffer[length++] = '"';
                        break;
                    case 'n':
                        buffer[length++] = '\n';
                        break;
                    case 'r':
                        buffer[length++] = '\r';
                        break;
                    case 't':
                        buffer[length++] = '\t';
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

                        buffer[length++] = (char)codePoint;
                        i += 4;
                        break;
                    default:
                        throw new ToonParseException($"Invalid escape sequence '\\{text[i]}'", lineNumber, column + i - 1);
                }
            }

            return new string(buffer[..length]);
        }
        finally
        {
            if (rented != null)
            {
                ArrayPool<char>.Shared.Return(rented, clearArray: true);
            }
        }
    }

    #endregion
}
