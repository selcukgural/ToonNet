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
/// Parses TOON text into a document structure following TOON spec v4.4.
/// </summary>
/// <remarks>
/// <para>
/// This is an internal implementation detail. Users should use <see cref="ToonSerializer"/> instead.
/// </para>
/// <para>
/// The parser is line based: lines are prepared as spec §12 orders (byte-order mark, CR, trailing spaces, comment
/// lines), and each line's depth is derived from its indentation and <see cref="ToonOptions.IndentSize"/>. With
/// <see cref="ToonOptions.StrictMode"/> enabled, every error of spec §14 is reported. Non-strict mode applies exactly
/// the five recoveries of §14.4: declared counts are not checked, duplicate keys and field names use the last value,
/// a line's depth is its tabs plus floor(spaces / indent size), blank lines inside arrays and keyed objects are
/// ignored, and a scope's deeper first line sets the depth of the scope.
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
    private ToonText _input;
    /// <summary>The lines of the document being parsed, in an array rented from <see cref="ArrayPool{T}.Shared"/>.</summary>
    private Line[] _lines = [];

    /// <summary>Whether <see cref="_lines"/> was rented from the pool (a table of over <see cref="MaxPooledLines"/> lines was not).</summary>
    private bool _linesPooled;

    /// <summary>
    ///     The most lines the first line table holds, so the first allocation does not follow the line breaks of the input
    ///     (which may be only line breaks); larger documents grow the table as their lines are read.
    /// </summary>
    private const int MaxInitialLines = 64 * 1024;

    /// <summary>
    ///     The most lines a table grown from the pool holds (24 MB): a larger table, for a document of over a million
    ///     lines, is allocated outside the pool, so the pool does not keep it.
    /// </summary>
    private const int MaxPooledLines = 1024 * 1024;

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
    ///     A delimited token: where it starts in the input (after leading spaces) and its length (without trailing
    ///     spaces).
    /// </summary>
    internal readonly record struct Token(int Start, int Length);

    /// <summary>
    ///     A parsed header (spec §6): an array header <c>key?[N&lt;delim?&gt;]{fields?}:</c>, or a keyed header
    ///     <c>key?[N:&lt;delim?&gt;]{fields}:</c>.
    /// </summary>
    /// <param name="Key">The key, or null for a key-less header.</param>
    /// <param name="Length">
    ///     The declared length or entry count; <see cref="int.MaxValue"/> for a larger one, which no content matches.
    /// </param>
    /// <param name="Delimiter">The delimiter declared in the brackets (comma by default).</param>
    /// <param name="Fields">The field list, or null.</param>
    /// <param name="LeafCount">The number of cells of each row or entry row.</param>
    /// <param name="Keyed">Whether the header opens a keyed tabular object (§9.5).</param>
    /// <param name="InlineStart">Where the inline values after the colon start in the input.</param>
    /// <param name="InlineLength">The length of the inline values (0 when there are none).</param>
    internal readonly record struct Header(string? Key, int Length, char Delimiter, FieldEntry[]? Fields, int LeafCount, bool Keyed,
                                           int InlineStart, int InlineLength);

    /// <summary>
    ///     An entry of a header's field list. The list is stored in pre-order: an entry with a nested field group (§9.3)
    ///     is followed by the group's entries, and <see cref="End"/> is the index after them.
    /// </summary>
    /// <param name="Name">The field name.</param>
    /// <param name="Leaf">The index of the field's cell in a row; for a group, of its first cell.</param>
    /// <param name="End">The index after the entry and its group.</param>
    internal readonly record struct FieldEntry(string Name, int Leaf, int End);

    #region Public API

    /// <summary>
    ///  Parses a TOON format string into a document.
    /// </summary>
    /// <param name="input">The TOON format string to parse.</param>
    /// <returns>A ToonDocument representing the parsed input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the input is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when MaxDepth exceeds 200 without AllowExtendedLimits.</exception>
    public ToonDocument Parse(ToonText input)
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
    internal void Validate(ToonText input)
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
    internal void BeginDocument(ToonText input)
    {
        _ = _options.ValidatedMaxDepth;

        _input = input;
        _lines = ArrayPool<Line>.Shared.Rent(Math.Min(input.Span.Count('\n') + 1, MaxInitialLines));
        _linesPooled = true;
        _position = 0;
        _depth = 0;
        _reservedListItems = 0;
        _headerSpans = 0;
        _keyCount = 0;
        _headers.Clear();
        _lastFields = null;

        SplitLines(input.Span);

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
        if (_linesPooled)
        {
            ArrayPool<Line>.Shared.Return(_lines);
            _linesPooled = false;
        }

        _lines = [];
        _lineCount = 0;
        _input = default;
        Array.Clear(_keyStack);
        _keyCount = 0;
        _headers.Clear();
        _lastFields = null;
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
        
        using var input = await Encoding.ToonTextEncoding.ReadFileAsync(filePath, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return Parse(input.Text);
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
        
        using var input = await Encoding.ToonTextEncoding.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return Parse(input.Text);
    }

    #endregion

    #region Lines

    /// <summary>
    ///     Splits the input into <see cref="_lines"/> (offsets into the input, no strings), preparing the lines as spec
    ///     §12 orders: a byte-order mark at the start is removed, a CR before each LF and trailing spaces are excluded,
    ///     and comment lines (§5.1) are dropped.
    /// </summary>
    private void SplitLines(ReadOnlySpan<char> input)
    {
        var lines = _lines;
        var count = 0;
        var start = !input.IsEmpty && input[0] == '\uFEFF' ? 1 : 0;

        for (var number = 1; start <= input.Length; number++)
        {
            var newline = input[start..].IndexOf('\n');
            var end = newline < 0 ? input.Length : start + newline;
            var length = end > start && input[end - 1] == '\r' ? end - start - 1 : end - start;
            var text = input.Slice(start, length).TrimEnd(' ');

            // A comment line: only spaces before '#'
            var first = text.IndexOfAnyExcept(' ');

            if (first < 0 || text[first] != '#')
            {
                var line = CreateLine(input, start, text.Length, number);

                if (line.IsBlank && count > 0 && lines[count - 1].IsBlank)
                {
                    // Consecutive blank lines are one entry (errors name the last, the one before the next content)
                    lines[count - 1] = line;
                }
                else
                {
                    if (count == lines.Length)
                    {
                        lines = GrowLines(count);
                    }

                    lines[count++] = line;
                }
            }

            start = end + 1;
        }

        _lineCount = count;
    }

    /// <summary>Doubles the line table (from the pool up to <see cref="MaxPooledLines"/>) and returns the new array.</summary>
    private Line[] GrowLines(int count)
    {
        var size = (int)Math.Min((long)count * 2, Array.MaxLength);
        var pooled = size <= MaxPooledLines;
        var larger = pooled ? ArrayPool<Line>.Shared.Rent(size) : new Line[size];
        _lines.AsSpan(0, count).CopyTo(larger);

        if (_linesPooled)
        {
            ArrayPool<Line>.Shared.Return(_lines);
        }

        _linesPooled = pooled;
        return _lines = larger;
    }

    /// <summary>
    ///     Creates the line that starts at <paramref name="start"/> in <paramref name="input"/> (without its line break
    ///     and trailing spaces).
    /// </summary>
    private Line CreateLine(ReadOnlySpan<char> input, int start, int length, int number)
    {
        var raw = input.Slice(start, length);
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

        /// <summary>The fields of the root object (no nesting level is entered).</summary>
        RootObject,

        /// <summary>An empty document, which is an empty object.</summary>
        EmptyRootObject,

        /// <summary><c>key:</c> followed by fields at <see cref="ValueRef.Depth"/>.</summary>
        NestedObject,

        /// <summary>A bare <c>-</c> list item.</summary>
        EmptyObject,

        /// <summary>
        ///     <c>- key: ...</c>: the first field on the hyphen line, the others one level below the item depth
        ///     <see cref="ValueRef.Depth"/>.
        /// </summary>
        ListItemObject,

        /// <summary>A tabular row or keyed entry row: the fields of the header with the cells of the line.</summary>
        Row,

        /// <summary>A nested field group of a row (§9.3): the group's field entry at <see cref="ValueRef.Start"/>.</summary>
        RowGroup,

        /// <summary>A keyed tabular object after its header (§9.5); entry rows at <see cref="ValueRef.Depth"/>.</summary>
        KeyedObject,

        /// <summary>An array after its header; list items or rows at <see cref="ValueRef.Depth"/>.</summary>
        HeaderArray,

        /// <summary><c>key: []</c> or <c>- []</c>.</summary>
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

        /// <summary>The index of the header (header arrays, keyed objects, rows and row groups), or -1.</summary>
        public readonly int HeaderIndex = headerIndex;

        public ToonNodeKind NodeKind => Kind switch
        {
            RefKind.Primitive => ToonNodeKind.Primitive,
            RefKind.HeaderArray or RefKind.EmptyArray or RefKind.RootEmptyArray => ToonNodeKind.Array,
            _ => ToonNodeKind.Object
        };
    }

    internal enum ObjectMode : byte
    {
        Empty,
        Block,
        ListItemFirst,
        Row,
        Keyed
    }

    /// <summary>Reads the fields of an object; advanced with <see cref="NextProperty"/>.</summary>
    internal struct ObjectCursor
    {
        internal ObjectMode Mode;
        internal int Depth;
        internal int LineIndex;

        // Rows: the field list, the entry read next and the end of the entries to read
        internal FieldEntry[]? Fields;
        internal int FieldIndex;
        internal int FieldEnd;

        // Rows and keyed objects: the header; keyed objects: the entry rows read
        internal int HeaderIndex;
        internal int Count;
        internal bool ExitNesting;
        internal bool Done;

        /// <summary>Non-strict mode: whether a deeper first line may set <see cref="Depth"/> (§14.4).</summary>
        internal bool MayJump;

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
        internal FieldEntry[]? TabularFields;
        internal int LeafCount;
        internal int LineIndex;
        internal int Depth;
        internal int Count;
        internal int TokenIndex;
        internal int TokenCount;
        internal bool RowsEntered;
        internal bool ItemOpen;
        internal bool ExitNesting;
        internal bool Done;

        /// <summary>Non-strict mode: whether a deeper first item or row may set <see cref="Depth"/> (§14.4).</summary>
        internal bool MayJump;

        /// <summary>The number of items to expect (the declared length, bounded by the lines left), for capacities.</summary>
        internal int CapacityHint;
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
    ///     Determines the root form (spec §5): an array or keyed tabular object with a key-less header, <c>[]</c>, a
    ///     single primitive, or an object.
    /// </summary>
    internal ValueRef ReadRoot()
    {
        var firstIndex = PeekIndex(out _);

        if (firstIndex < 0)
        {
            return new ValueRef(RefKind.EmptyRootObject, -1);
        }

        var firstLine = _lines[firstIndex];

        // No scope encloses the first line, so it cannot be indented (§8), in either mode
        if (firstLine.Depth != 0)
        {
            throw new ToonParseException("The first line of a document must not be indented", firstLine.Number, firstLine.Indent + 1);
        }

        var content = ContentOf(firstLine);
        var colon = FindStructuralColon(content, out var bracket);

        if (colon >= 0 && bracket == 0)
        {
            var header = ParseHeader(firstLine, content, bracket);
            NextLine();
            _depth = -1; // the root value is at depth 0, like the root object
            return new ValueRef(header.Keyed ? RefKind.KeyedObject : RefKind.HeaderArray, firstIndex, depth: 1, headerIndex: AddHeader(header));
        }

        if (content.SequenceEqual("[]"))
        {
            NextLine();
            return new ValueRef(RefKind.RootEmptyArray, firstIndex);
        }

        if (colon < 0 && HasSingleNonBlankLine())
        {
            NextLine();
            return new ValueRef(RefKind.Primitive, firstIndex, firstLine.Start, firstLine.Length, ColumnOf(firstLine, content));
        }

        return new ValueRef(RefKind.RootObject, firstIndex, depth: 0);
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
                cursor.MayJump = !_options.StrictMode;
                break;
            case RefKind.EmptyObject:
                CheckEmptyContainerNesting(_lines[value.LineIndex]);
                break;
            case RefKind.ListItemObject:
                // The first field sits on the hyphen line; its nested content is two levels deeper
                EnterNesting(_lines[value.LineIndex]);
                cursor.Mode = ObjectMode.ListItemFirst;
                cursor.LineIndex = value.LineIndex;
                cursor.Depth = value.Depth;
                cursor.ExitNesting = true;
                break;
            case RefKind.Row:
                cursor.Mode = ObjectMode.Row;
                cursor.LineIndex = value.LineIndex;
                cursor.HeaderIndex = value.HeaderIndex;
                cursor.Fields = HeaderOf(value.HeaderIndex).Fields;
                cursor.FieldEnd = cursor.Fields!.Length;
                break;
            case RefKind.RowGroup:
            {
                // A nested field group is an object one level below its row
                EnterNesting(_lines[value.LineIndex]);
                var fields = HeaderOf(value.HeaderIndex).Fields!;
                cursor.Mode = ObjectMode.Row;
                cursor.LineIndex = value.LineIndex;
                cursor.HeaderIndex = value.HeaderIndex;
                cursor.Fields = fields;
                cursor.FieldIndex = value.Start + 1;
                cursor.FieldEnd = fields[value.Start].End;
                cursor.ExitNesting = true;
                break;
            }
            case RefKind.KeyedObject:
                EnterNesting(_lines[value.LineIndex]);
                cursor.Mode = ObjectMode.Keyed;
                cursor.LineIndex = value.LineIndex;
                cursor.HeaderIndex = value.HeaderIndex;
                cursor.Depth = value.Depth;
                cursor.ExitNesting = true;
                cursor.MayJump = !_options.StrictMode;
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
                key = ReadField(cursor.LineIndex, ContentOf(_lines[cursor.LineIndex])[2..].TrimStart(' '), itemDepth + 2, out value);
                SetPendingKey(ref cursor, key, cursor.LineIndex);
                return true;
            }
            case ObjectMode.Block:
            {
                var index = PeekIndex(out var skippedBlank);

                if (index >= 0 && _lines[index].Depth >= cursor.Depth)
                {
                    ref readonly var line = ref _lines[index];

                    // The object continues after the blank line, so the blank is inside the enclosing array's header span
                    if (skippedBlank && _headerSpans > 0 && _options.StrictMode)
                    {
                        throw new ToonParseException("Blank lines are not allowed inside an array", _lines[index - 1].Number, 1);
                    }

                    if (line.Depth > cursor.Depth)
                    {
                        if (!cursor.MayJump)
                        {
                            throw new ToonParseException("Unexpected indentation", line.Number, line.Indent + 1);
                        }

                        cursor.Depth = line.Depth;
                    }

                    cursor.MayJump = false;
                    _position = index + 1;
                    key = ReadField(index, ContentOf(line), cursor.Depth + 1, out value);
                    SetPendingKey(ref cursor, key, index);
                    return true;
                }

                break;
            }
            case ObjectMode.Row:
                if (cursor.FieldIndex < cursor.FieldEnd)
                {
                    var index = cursor.FieldIndex;
                    ref readonly var field = ref cursor.Fields![index];
                    cursor.FieldIndex = field.End;
                    key = field.Name;

                    if (field.End == index + 1)
                    {
                        var token = _tokens[field.Leaf];
                        value = new ValueRef(RefKind.Primitive, cursor.LineIndex, token.Start, token.Length,
                                             ColumnAt(_lines[cursor.LineIndex], token.Start));
                    }
                    else
                    {
                        value = new ValueRef(RefKind.RowGroup, cursor.LineIndex, index, headerIndex: cursor.HeaderIndex);
                    }

                    return true;
                }

                break;
            case ObjectMode.Keyed:
                if (NextEntry(ref cursor, out key, out value))
                {
                    return true;
                }

                break;
        }

        EndObject(ref cursor);
        key = null!;
        value = default;
        return false;
    }

    /// <summary>Reads the next entry row of a keyed tabular object, or returns false at its end.</summary>
    private bool NextEntry(ref ObjectCursor cursor, out string key, out ValueRef value)
    {
        var index = PeekIndex(out var skippedBlank);

        if (index < 0 || _lines[index].Depth < cursor.Depth)
        {
            key = null!;
            value = default;
            return false;
        }

        ref readonly var line = ref _lines[index];

        if (line.Depth > cursor.Depth)
        {
            if (!cursor.MayJump)
            {
                throw new ToonParseException("Unexpected indentation in keyed tabular object", line.Number, line.Indent + 1);
            }

            cursor.Depth = line.Depth;
        }

        cursor.MayJump = false;

        // Inside this object's span after the first entry row, or inside an enclosing array's span
        if (skippedBlank && (cursor.Count > 0 || _headerSpans > 0) && _options.StrictMode)
        {
            throw new ToonParseException("Blank lines are not allowed inside a keyed tabular object", _lines[index - 1].Number, 1);
        }

        _position = index + 1;
        key = ReadEntryRow(index, cursor.HeaderIndex);

        // Entry values are objects one level below the keyed object
        if (cursor.Count++ == 0)
        {
            EnterNesting(line);
        }

        value = new ValueRef(RefKind.Row, index, headerIndex: cursor.HeaderIndex);
        SetPendingKey(ref cursor, key, index);
        return true;
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

        if (cursor.Mode == ObjectMode.Keyed)
        {
            if (cursor.Count > 0)
            {
                _depth--;
            }

            ref readonly var header = ref HeaderOf(cursor.HeaderIndex);

            if (_options.StrictMode && cursor.Count != header.Length)
            {
                ref readonly var line = ref _lines[cursor.LineIndex];
                throw new ToonParseException($"Entry count mismatch: expected {header.Length}, got {cursor.Count}", line.Number, line.Indent + 1);
            }

            _headers.RemoveRange(cursor.HeaderIndex, _headers.Count - cursor.HeaderIndex);
        }

        if (cursor.ExitNesting)
        {
            _depth--;
        }
    }

    /// <summary>
    ///     Reads an entry row of a keyed tabular object (spec §9.5): the entry key before the first unquoted colon, then
    ///     the cells, split as in a tabular row.
    /// </summary>
    /// <returns>The entry key.</returns>
    private string ReadEntryRow(int lineIndex, int headerIndex)
    {
        ref readonly var line = ref _lines[lineIndex];
        var content = ContentOf(line);
        var colon = IndexOfUnquoted(content, ':');

        if (colon < 0)
        {
            throw new ToonParseException("Missing colon after the entry key of a keyed tabular object", line.Number, line.Indent + 1);
        }

        ref readonly var header = ref HeaderOf(headerIndex);
        var key = ParseKeyToken(content[..colon].TrimEnd(' '), line.Number, line.Indent + 1);
        var cells = content[(colon + 1)..];

        // Nothing after the colon is no cells, not one empty cell
        var count = cells.TrimStart(' ').IsEmpty ? 0 : SplitDelimited(cells, header.Delimiter).Count;
        CheckRowWidth(count, header.LeafCount, line);
        return key;
    }

    private static void CheckRowWidth(int cells, int leafCount, in Line line)
    {
        if (cells != leafCount)
        {
            throw new ToonParseException($"Row has {cells} values but expected {leafCount}", line.Number, line.Indent + 1);
        }
    }

    /// <summary>
    ///     Reads a key-value or array-header line (<paramref name="content"/> without indentation or list marker);
    ///     nested content is read at <paramref name="childDepth"/>.
    /// </summary>
    /// <returns>The key.</returns>
    private string ReadField(int lineIndex, ReadOnlySpan<char> content, int childDepth, out ValueRef value)
    {
        ref readonly var line = ref _lines[lineIndex];
        var colon = FindStructuralColon(content, out var bracket);

        if (colon < 0)
        {
            throw new ToonParseException(IsListItem(content) ? "List items require an array header such as 'key[N]:'" : "Missing colon after key",
                                         line.Number, ColumnOf(line, content));
        }

        if (bracket >= 0)
        {
            // A header without a key is only allowed at the root or as a list item (§6)
            if (bracket == 0)
            {
                throw new ToonParseException("An array header needs a key here", line.Number, ColumnOf(line, content));
            }

            var header = ParseHeader(line, content, bracket);
            value = new ValueRef(header.Keyed ? RefKind.KeyedObject : RefKind.HeaderArray, lineIndex, depth: childDepth,
                                 headerIndex: AddHeader(header));
            return header.Key!;
        }

        var key = ParseKeyToken(content[..colon].TrimEnd(' '), line.Number, ColumnOf(line, content));

        // Line content has no trailing spaces, so the value runs to the end
        var rest = content[(colon + 1)..].TrimStart(' ');

        if (rest.Length == 0)
        {
            value = new ValueRef(RefKind.NestedObject, lineIndex, depth: childDepth);
        }
        else if (rest.SequenceEqual("[]"))
        {
            value = new ValueRef(RefKind.EmptyArray, lineIndex);
        }
        else
        {
            value = new ValueRef(RefKind.Primitive, lineIndex, OffsetOf(rest), rest.Length, ColumnOf(line, rest));
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
                    cursor.Form = ArrayForm.Tabular;
                    cursor.TabularFields = header.Fields;
                    cursor.LeafCount = header.LeafCount;
                    cursor.CapacityHint = CountRows(header.Length, cursor.Depth);
                    cursor.MayJump = !_options.StrictMode;
                }
                else if (header.InlineLength > 0)
                {
                    cursor.Form = ArrayForm.Inline;
                    cursor.TokenCount = SplitDelimited(_input.Span.Slice(header.InlineStart, header.InlineLength), header.Delimiter).Count;
                    cursor.CapacityHint = cursor.TokenCount;
                }
                else
                {
                    cursor.Form = ArrayForm.List;
                    cursor.CapacityHint = ReserveListItems(header.Length);
                    cursor.MayJump = !_options.StrictMode;
                }

                break;
            }
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
                    item = new ValueRef(RefKind.Primitive, cursor.LineIndex, token.Start, token.Length, ColumnAt(_lines[cursor.LineIndex], token.Start));
                    cursor.Count++;
                    return true;
                }

                break;
            case ArrayForm.Tabular:
            {
                var index = PeekIndex(out var skippedRowBlank);
                AdoptJumpedDepth(ref cursor, index);

                if (index >= 0 && _lines[index].Depth == cursor.Depth && IsTabularRow(ContentOf(_lines[index]), cursor.Delimiter))
                {
                    ref readonly var row = ref _lines[index];

                    if (skippedRowBlank && (cursor.Count > 0 || _headerSpans > 0) && _options.StrictMode)
                    {
                        throw new ToonParseException("Blank lines are not allowed inside a tabular array", _lines[index - 1].Number, 1);
                    }

                    _position = index + 1;
                    CheckRowWidth(SplitDelimited(ContentOf(row), cursor.Delimiter).Count, cursor.LeafCount, row);

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
                AdoptJumpedDepth(ref cursor, index);

                if (index >= 0 && _lines[index].Depth == cursor.Depth && IsListItem(ContentOf(_lines[index])))
                {
                    ref readonly var line = ref _lines[index];

                    // Inside this array's span after the first item, or inside an enclosing array's span
                    if (skippedBlank && (cursor.Count > 0 || _headerSpans > 0) && _options.StrictMode)
                    {
                        throw new ToonParseException("Blank lines are not allowed inside an array", _lines[index - 1].Number, 1);
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

    /// <summary>
    ///     Non-strict mode: a first item or row deeper than the array's content depth sets it (§14.4); the lines up to the
    ///     end of the array are read at that depth, and a shallower line among them is an error of the enclosing scope.
    /// </summary>
    private void AdoptJumpedDepth(ref ArrayCursor cursor, int index)
    {
        if (cursor.MayJump)
        {
            cursor.MayJump = false;

            if (index >= 0 && _lines[index].Depth > cursor.Depth)
            {
                cursor.Depth = _lines[index].Depth;
            }
        }
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
    ///     The capacity reserved for the items of a list once the document's budget (see <see cref="ReserveListItems"/>)
    ///     is used up.
    /// </summary>
    private const int ListCapacityHint = 16;

    /// <summary>The list item slots reserved in this document so far.</summary>
    private int _reservedListItems;

    /// <summary>
    ///     The capacity to reserve for the items of a list with a declared length. Items can span several lines, so their
    ///     number is not known without reading them; but each starts on a line of its own, so the declared lengths of
    ///     an honest document add up to at most its number of lines. Declared lengths are reserved while they fit in
    ///     that budget (a list of 10,000 items gets one array, not a series of growing ones on the large object heap);
    ///     beyond it, lists start at <see cref="ListCapacityHint"/> and grow, so lengths the document overstates
    ///     reserve memory only in proportion to its size.
    /// </summary>
    private int ReserveListItems(int declaredLength)
    {
        var available = Math.Max(_lineCount - _reservedListItems, ListCapacityHint);
        var reserved = Math.Min(declaredLength, available);
        _reservedListItems += reserved;
        return reserved;
    }

    /// <summary>
    ///     The number of rows of a tabular array: the lines that follow at the row depth (or deeper, non-strict mode),
    ///     at most the declared length. Rows are single lines, so this is exact and reserves nothing for rows that are
    ///     not there, however large the declared length or deep the nesting.
    /// </summary>
    private int CountRows(int declaredLength, int rowDepth)
    {
        var count = 0;

        for (var i = _position; i < _lineCount && count < declaredLength; i++)
        {
            ref readonly var line = ref _lines[i];

            if (line.IsBlank)
            {
                continue;
            }

            if (line.Depth < rowDepth)
            {
                break;
            }

            count++;
        }

        return count;
    }

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

        if (rest.SequenceEqual("[]"))
        {
            return new ValueRef(RefKind.EmptyArray, lineIndex); // empty inner array (§9.2)
        }

        var colon = FindStructuralColon(rest, out var bracket);

        if (colon < 0)
        {
            return new ValueRef(RefKind.Primitive, lineIndex, OffsetOf(rest), rest.Length, ColumnOf(line, rest));
        }

        // Inner array: - [M]: ... (a key-less header with a field list is only allowed at the root, §6)
        if (bracket == 0)
        {
            var header = ParseHeader(line, rest, bracket);

            if (header.Fields != null)
            {
                throw new ToonParseException("A header with a field list needs a key here", line.Number, ColumnOf(line, rest));
            }

            return new ValueRef(RefKind.HeaderArray, lineIndex, depth: itemDepth + 1, headerIndex: AddHeader(header));
        }

        // Object with its first field on the hyphen line
        return new ValueRef(RefKind.ListItemObject, lineIndex, depth: itemDepth);
    }

    /// <summary>The text of a primitive token.</summary>
    internal ReadOnlySpan<char> TokenOf(in ValueRef value) => _input.Span.Slice(value.Start, value.Length);

    /// <summary>Whether a value is the primitive <c>null</c>.</summary>
    internal bool IsNull(in ValueRef value) => value.Kind == RefKind.Primitive && TokenOf(value).SequenceEqual("null");

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

    /// <summary>The state of the reader methods before a value is read, to read the value again (<see cref="Restore"/>).</summary>
    internal readonly struct Checkpoint(int position, int depth, int headerSpans, int keyCount, int reservedListItems, int headersFrom,
                                        int headerCount, Header header, Header[]? headers)
    {
        public readonly int Position = position;
        public readonly int Depth = depth;
        public readonly int HeaderSpans = headerSpans;
        public readonly int KeyCount = keyCount;
        public readonly int ReservedListItems = reservedListItems;

        /// <summary>The headers from this index on, which reading the value may remove; those before it stay.</summary>
        public readonly int HeadersFrom = headersFrom;

        /// <summary>The number of headers from <see cref="HeadersFrom"/> on: one is kept in <see cref="Header"/>, more in <see cref="Headers"/>.</summary>
        public readonly int HeaderCount = headerCount;

        public readonly Header Header = header;
        public readonly Header[]? Headers = headers;
    }

    /// <summary>
    ///     Saves the state before <paramref name="value"/> is read, so the value can be read again after
    ///     <see cref="Restore"/> (when reading it straight into an object gave up, it is built as a tree instead). The
    ///     cursors that contain the value are not changed by reading it.
    /// </summary>
    internal Checkpoint Save(in ValueRef value)
    {
        // Reading an array or keyed object removes its own header when it ends (and the headers added after it, which
        // Restore drops); a row or field group only refers to the header of the array it is in
        var from = value.Kind is RefKind.HeaderArray or RefKind.KeyedObject ? Math.Min(value.HeaderIndex, _headers.Count) : _headers.Count;
        var count = _headers.Count - from;

        return new Checkpoint(_position, _depth, _headerSpans, _keyCount, _reservedListItems, from, count,
                              count == 1 ? _headers[from] : default, count > 1 ? _headers.GetRange(from, count).ToArray() : null);
    }

    /// <summary>Returns to the state <see cref="Save"/> recorded.</summary>
    internal void Restore(in Checkpoint checkpoint)
    {
        _position = checkpoint.Position;
        _depth = checkpoint.Depth;
        _headerSpans = checkpoint.HeaderSpans;
        _keyCount = checkpoint.KeyCount;
        _reservedListItems = checkpoint.ReservedListItems;

        if (_headers.Count > checkpoint.HeadersFrom)
        {
            _headers.RemoveRange(checkpoint.HeadersFrom, _headers.Count - checkpoint.HeadersFrom);
        }

        if (checkpoint.HeaderCount == 1)
        {
            _headers.Add(checkpoint.Header);
        }
        else if (checkpoint.Headers != null)
        {
            _headers.AddRange(checkpoint.Headers);
        }
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
                if (value.Length > 0 && _input.Span[value.Start] == '"')
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
        return ParsePrimitive(TokenOf(value), _lines[value.LineIndex], value.Column);
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
                var fieldNames = cursor.Form == ArrayForm.Tabular ? TopLevelNames(cursor.TabularFields!) : null;

                while (NextItem(ref cursor, out var item))
                {
                    items.Add(BuildValue(item));
                }

                return new ToonArray(items) { FieldNames = fieldNames };
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
    ///     Builds a row object from the cells of its line, like reading it with a cursor (rows have no checks of their
    ///     own). A field repeated in the field list (non-strict mode) takes its last cell.
    /// </summary>
    private ToonObject BuildRow(in ValueRef value)
    {
        ref readonly var header = ref HeaderOf(value.HeaderIndex);
        var fields = header.Fields!;
        ref readonly var line = ref _lines[value.LineIndex];

        if (header.LeafCount != fields.Length)
        {
            return BuildRowFields(fields, 0, fields.Length, line);
        }

        // Without nested field groups, each field takes the cell at its index
        var row = new ToonObject(fields.Length);
        var tokens = CollectionsMarshal.AsSpan(_tokens);

        for (var i = 0; i < fields.Length; i++)
        {
            var token = tokens[i];
            row.Properties[fields[i].Name] = ParsePrimitive(_input.Span.Slice(token.Start, token.Length), line, ColumnAt(line, token.Start));
        }

        return row;
    }

    /// <summary>Builds the object of the field entries from <paramref name="start"/> to <paramref name="end"/>.</summary>
    private ToonObject BuildRowFields(FieldEntry[] fields, int start, int end, in Line line)
    {
        var obj = new ToonObject(end - start);
        var tokens = CollectionsMarshal.AsSpan(_tokens);

        for (var i = start; i < end; i = fields[i].End)
        {
            ref readonly var field = ref fields[i];
            ToonValue value;

            if (field.End == i + 1)
            {
                var token = tokens[field.Leaf];
                value = ParsePrimitive(_input.Span.Slice(token.Start, token.Length), line, ColumnAt(line, token.Start));
            }
            else
            {
                // A nested field group is an object one level below
                EnterNesting(line);
                value = BuildRowFields(fields, i + 1, field.End, line);
                _depth--;
            }

            obj.Properties[field.Name] = value;
        }

        return obj;
    }

    /// <summary>The names of the top-level entries of a field list.</summary>
    private static string[] TopLevelNames(FieldEntry[] fields)
    {
        var count = 0;

        for (var i = 0; i < fields.Length; i = fields[i].End)
        {
            count++;
        }

        var names = new string[count];

        for (int i = 0, n = 0; i < fields.Length; i = fields[i].End)
        {
            names[n++] = fields[i].Name;
        }

        return names;
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

    private static readonly SearchValues<char> ColonBracketQuote = SearchValues.Create(":[\"");

    /// <summary>
    ///     Finds the first unquoted colon of a line's content and the first unquoted <c>[</c> when it comes before the
    ///     colon, which makes the line an array header (spec §5.2). A quote opens a span that the next unescaped quote
    ///     closes, or the end of the line.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="bracket">The index of the <c>[</c>, or -1.</param>
    /// <returns>The index of the colon, or -1.</returns>
    private static int FindStructuralColon(ReadOnlySpan<char> content, out int bracket)
    {
        bracket = -1;
        var i = 0;

        while (true)
        {
            var next = content[i..].IndexOfAny(ColonBracketQuote);

            if (next < 0)
            {
                return -1;
            }

            i += next;

            switch (content[i])
            {
                case ':':
                    return i;
                case '[':
                    if (bracket < 0)
                    {
                        bracket = i;
                    }

                    i++;
                    break;
                default:
                    i = FindClosingQuote(content, i);

                    if (i < 0)
                    {
                        return -1;
                    }

                    i++;
                    break;
            }
        }
    }

    /// <summary>
    ///     Checks whether a line inside a tabular array is a row rather than a key-value line (spec §9.3): it has no
    ///     unquoted colon, or an unquoted delimiter comes first.
    /// </summary>
    private static bool IsTabularRow(ReadOnlySpan<char> content, char delimiter)
    {
        var i = 0;

        while (true)
        {
            var next = content[i..].IndexOfAny(delimiter, ':', '"');

            if (next < 0)
            {
                return true;
            }

            i += next;

            if (content[i] != '"')
            {
                return content[i] == delimiter;
            }

            i = FindClosingQuote(content, i);

            if (i < 0)
            {
                return true;
            }

            i++;
        }
    }

    /// <summary>
    ///     Returns the index of the first occurrence of <paramref name="target"/> outside quoted spans, or -1.
    /// </summary>
    private static int IndexOfUnquoted(ReadOnlySpan<char> content, char target)
    {
        var i = 0;

        while (true)
        {
            var next = content[i..].IndexOfAny(target, '"');

            if (next < 0)
            {
                return -1;
            }

            i += next;

            if (content[i] == target)
            {
                return i;
            }

            i = FindClosingQuote(content, i);

            if (i < 0)
            {
                return -1;
            }

            i++;
        }
    }

    /// <summary>
    ///     Returns the index of the quote closing the string that starts at <paramref name="start"/>, or -1.
    /// </summary>
    private static int FindClosingQuote(ReadOnlySpan<char> content, int start)
    {
        var i = start + 1;

        while (i < content.Length)
        {
            var next = content[i..].IndexOfAny('\\', '"');

            if (next < 0)
            {
                return -1;
            }

            i += next;

            if (content[i] == '"')
            {
                return i;
            }

            i += 2; // the escaped character
        }

        return -1;
    }

    /// <summary>
    ///     Parses an array or keyed header (spec §6) whose bracket segment starts at <paramref name="bracket"/>. A line
    ///     whose first unquoted <c>[</c> precedes its first unquoted colon must be one, in either mode.
    /// </summary>
    /// <param name="line">The header line, for error positions.</param>
    /// <param name="content">The header text (without indentation or list marker), a suffix of the line's content.</param>
    /// <param name="bracket">The index of the <c>[</c>; 0 for a key-less header.</param>
    /// <exception cref="ToonParseException">Thrown when the header does not match the grammar.</exception>
    private Header ParseHeader(in Line line, ReadOnlySpan<char> content, int bracket)
    {
        var column = ColumnOf(line, content);
        string? key = null;

        if (bracket > 0)
        {
            if (content[bracket - 1] is ' ' or '\t')
            {
                throw new ToonParseException("Whitespace is not allowed between a key and its bracket segment", line.Number, column + bracket - 1);
            }

            key = ParseKeyToken(content[..bracket], line.Number, column);
        }

        // [N], [N<delim>], [N:] or [N:<delim>]; N without leading zeros
        var i = bracket + 1;
        var digits = content[i..];
        var digitCount = digits.IndexOfAnyExceptInRange('0', '9');
        digits = digits[..(digitCount < 0 ? digits.Length : digitCount)];
        i += digits.Length;

        if (digits.IsEmpty || (digits.Length > 1 && digits[0] == '0'))
        {
            throw new ToonParseException("Malformed array header: invalid length", line.Number, column + bracket);
        }

        // A length beyond int still forms a header; no content matches it (§6)
        var length = int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ? parsed : int.MaxValue;
        var keyed = i < content.Length && content[i] == ':';
        var delimiter = ',';

        if (keyed)
        {
            i++;
        }

        if (i < content.Length && content[i] is '\t' or '|')
        {
            delimiter = content[i++];
        }

        if (i >= content.Length || content[i] != ']')
        {
            throw new ToonParseException("Malformed array header: invalid bracket segment", line.Number, column + bracket);
        }

        i++;
        FieldEntry[]? fields = null;
        var leafCount = 0;

        if (i < content.Length && content[i] == '{')
        {
            fields = ParseFieldList(line, content, ref i, delimiter, out leafCount);
        }
        else if (keyed)
        {
            throw new ToonParseException("A keyed header needs a field list", line.Number, column + i);
        }

        if (i >= content.Length || content[i] != ':')
        {
            throw new ToonParseException("Malformed array header: expected ':'", line.Number, column + i);
        }

        var inline = content[(i + 1)..].TrimStart(' ');

        if (fields != null && !inline.IsEmpty)
        {
            throw new ToonParseException("Unexpected values after a tabular array header", line.Number, ColumnOf(line, inline));
        }

        return new Header(key, length, delimiter, fields, leafCount, keyed, inline.IsEmpty ? 0 : OffsetOf(inline), inline.Length);
    }

    /// <summary>The entries of the field list being parsed, reused.</summary>
    private readonly List<FieldEntry> _fieldEntries = [];

    // The last field list parsed: its text (an offset into the input, braces included), delimiter and result. Objects
    // in a list usually repeat the same header, which then shares one field list.
    private int _lastFieldsStart;
    private int _lastFieldsLength;
    private char _lastFieldsDelimiter;
    private FieldEntry[]? _lastFields;
    private int _lastLeafCount;

    /// <summary>
    ///     Parses the field list at the <c>{</c> at <paramref name="i"/> (spec §6), leaving <paramref name="i"/> after
    ///     its <c>}</c>. The same text as the last field list gives the same (immutable) result.
    /// </summary>
    private FieldEntry[] ParseFieldList(in Line line, ReadOnlySpan<char> content, ref int i, char delimiter, out int leafCount)
    {
        var rest = content[i..];

        // Parsing reads up to the matching brace only, so a text that starts with the last list parses to it
        if (_lastFields != null && delimiter == _lastFieldsDelimiter && rest.StartsWith(_input.Span.Slice(_lastFieldsStart, _lastFieldsLength)))
        {
            i += _lastFieldsLength;
            leafCount = _lastLeafCount;
            return _lastFields;
        }

        var start = i;
        var entries = _fieldEntries;
        entries.Clear();
        leafCount = 0;
        ParseFieldGroup(line, content, ref i, delimiter, entries, ref leafCount, 1);

        _lastFieldsStart = OffsetOf(rest);
        _lastFieldsLength = i - start;
        _lastFieldsDelimiter = delimiter;
        _lastLeafCount = leafCount;
        return _lastFields = entries.ToArray();
    }

    /// <summary>
    ///     Parses the field list or nested field group at the <c>{</c> at <paramref name="i"/> into
    ///     <paramref name="entries"/> (spec §6), leaving <paramref name="i"/> after its <c>}</c>.
    /// </summary>
    private void ParseFieldGroup(in Line line, ReadOnlySpan<char> content, ref int i, char delimiter, List<FieldEntry> entries,
                                 ref int leafCount, int level)
    {
        var column = ColumnOf(line, content);

        if (level > _options.ValidatedMaxDepth || !RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            throw new ToonParseException($"Maximum nesting depth of {_options.MaxDepth} exceeded", line.Number, column + i);
        }

        var groupStart = entries.Count;
        i++;

        while (true)
        {
            while (i < content.Length && content[i] == ' ')
            {
                i++;
            }

            // A name runs to the next unquoted delimiter or brace
            var nameStart = i;

            while (i < content.Length)
            {
                var ch = content[i];

                if (ch == '"')
                {
                    i = FindClosingQuote(content, i);

                    if (i < 0)
                    {
                        throw new ToonParseException("Unterminated string", line.Number, column + nameStart);
                    }
                }
                else if (ch == delimiter || ch is '{' or '}')
                {
                    break;
                }
                else if (ch is ',' or '|' or '\t')
                {
                    throw new ToonParseException("Header delimiter mismatch between bracket and fields segments", line.Number, column + i);
                }

                i++;
            }

            if (i >= content.Length)
            {
                throw new ToonParseException("Unclosed field list", line.Number, column + nameStart);
            }

            var name = content[nameStart..i].TrimEnd(' ');

            if (name.IsEmpty)
            {
                throw new ToonParseException("Empty field name in field list", line.Number, column + nameStart);
            }

            var key = ParseKeyToken(name, line.Number, column + nameStart);
            var index = entries.Count;

            if (content[i] == '{')
            {
                if (content[i - 1] == ' ')
                {
                    throw new ToonParseException("Whitespace is not allowed between a field name and its nested field group", line.Number, column + i - 1);
                }

                var firstLeaf = leafCount;
                entries.Add(default);
                ParseFieldGroup(line, content, ref i, delimiter, entries, ref leafCount, level + 1);
                entries[index] = new FieldEntry(key, firstLeaf, entries.Count);

                while (i < content.Length && content[i] == ' ')
                {
                    i++;
                }

                if (i >= content.Length || (content[i] != delimiter && content[i] != '}'))
                {
                    throw new ToonParseException("Unexpected characters after a nested field group", line.Number, column + i);
                }
            }
            else
            {
                entries.Add(new FieldEntry(key, leafCount++, index + 1));
            }

            if (content[i++] == '}')
            {
                break;
            }
        }

        if (_options.StrictMode)
        {
            CheckDuplicateFields(entries, groupStart, line);
        }
    }

    /// <summary>Checks a field list level for repeated names (spec §9.3).</summary>
    private static void CheckDuplicateFields(List<FieldEntry> entries, int start, in Line line)
    {
        var span = CollectionsMarshal.AsSpan(entries);
        var count = 0;

        for (var a = start; a < span.Length; a = span[a].End)
        {
            count++;
        }

        if (count <= LinearKeyCheckLimit)
        {
            for (var a = start; a < span.Length; a = span[a].End)
            {
                for (var b = span[a].End; b < span.Length; b = span[b].End)
                {
                    if (string.Equals(span[a].Name, span[b].Name))
                    {
                        throw DuplicateField(span[a].Name, line);
                    }
                }
            }

            return;
        }

        // Long field lists: count the names, then report the first one that is repeated, as the pairwise check does
        var occurrences = new Dictionary<string, int>(count, StringComparer.Ordinal);
        var repeated = false;

        for (var a = start; a < span.Length; a = span[a].End)
        {
            ref var seen = ref CollectionsMarshal.GetValueRefOrAddDefault(occurrences, span[a].Name, out var exists);
            seen++;
            repeated |= exists;
        }

        if (!repeated)
        {
            return;
        }

        for (var a = start; a < span.Length; a = span[a].End)
        {
            if (occurrences[span[a].Name] > 1)
            {
                throw DuplicateField(span[a].Name, line);
            }
        }

        static ToonParseException DuplicateField(string name, in Line line) =>
            new($"Duplicate field '{name}' in field list", line.Number, line.Indent + 1);
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
        var offset = OffsetOf(text);
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
                tokens.Add(CreateToken(text, start, i, offset));
                start = i + 1;
            }
        }

        tokens.Add(CreateToken(text, start, text.Length, offset));
        return tokens;

        static Token CreateToken(ReadOnlySpan<char> text, int start, int end, int offset)
        {
            while (start < end && text[start] == ' ')
            {
                start++;
            }

            while (end > start && text[end - 1] == ' ')
            {
                end--;
            }

            return new Token(offset + start, end - start);
        }
    }

    /// <summary>
    ///     The 1-based column of the first character of <paramref name="suffix"/>, a suffix of the line's content.
    /// </summary>
    private static int ColumnOf(in Line line, ReadOnlySpan<char> suffix) => line.Indent + line.Length - suffix.Length + 1;

    /// <summary>The 1-based column of the character at <paramref name="offset"/> in the input, on the line.</summary>
    private static int ColumnAt(in Line line, int offset) => line.Indent + 1 + offset - line.Start;

    /// <summary>The text of a line after its indentation.</summary>
    private ReadOnlySpan<char> ContentOf(Line line) => _input.Span.Slice(line.Start, line.Length);

    /// <summary>Where <paramref name="part"/>, a slice of the input, starts in the input.</summary>
    private int OffsetOf(ReadOnlySpan<char> part) =>
        (int)(Unsafe.ByteOffset(ref MemoryMarshal.GetReference(_input.Span), ref MemoryMarshal.GetReference(part)) / sizeof(char));

    /// <summary>
    ///     Returns the string for an unquoted key, shared with earlier occurrences of the same key in the document.
    /// </summary>
    private string GetKey(ReadOnlySpan<char> text)
    {
        if (!_useKeyCache || _keys == null)
        {
            return text.ToString();
        }

        // A hash that is the same in every process (string hashes are randomized per process, which made two keys of a
        // document share a slot in some runs and not in others), and two slots per key, so two keys whose first slot
        // is the same do not push each other out on every row
        var hash = KeyHash(text);
        var first = (int)(hash % KeyCacheSize);

        if (_keys[first] is { } cached && text.SequenceEqual(cached))
        {
            return cached;
        }

        var second = (int)((hash >> 8) % KeyCacheSize);

        if (_keys[second] is { } other && text.SequenceEqual(other))
        {
            return other;
        }

        var key = text.ToString();
        _keys[_keys[first] == null || _keys[second] != null ? first : second] = key;
        return key;
    }

    /// <summary>FNV-1a over the characters of a key.</summary>
    private static uint KeyHash(ReadOnlySpan<char> text)
    {
        var hash = 2166136261u;

        foreach (var c in text)
        {
            hash = (hash ^ c) * 16777619u;
        }

        return hash;
    }

    /// <summary>
    ///     Reads a key token (spec §7.4): a quoted key must end at its closing quote; any other token, even an empty one,
    ///     is a literal key.
    /// </summary>
    /// <exception cref="ToonParseException">Thrown for an unterminated quoted key, text after its closing quote or invalid escapes.</exception>
    private string ParseKeyToken(ReadOnlySpan<char> token, int lineNumber, int column)
    {
        if (token.Length > 0 && token[0] == '"')
        {
            var end = FindClosingQuote(token, 0);

            if (end < 0)
            {
                throw new ToonParseException("Unterminated string", lineNumber, column);
            }

            if (end != token.Length - 1)
            {
                throw new ToonParseException("Unexpected characters after closing quote", lineNumber, column + end + 1);
            }

            return Unescape(token[1..end], lineNumber, column + 1);
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
