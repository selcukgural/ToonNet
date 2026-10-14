using System.Buffers;
using ToonNet.Core.Models;

namespace ToonNet.Core.Encoding;

/// <summary>
///     Writes a TOON document forward-only, value by value, without building a <see cref="ToonDocument"/>: objects,
///     inline primitive arrays, expanded lists, tabular arrays and keyed tables, in the canonical layout of
///     <see cref="ToonEncoder"/> (the same text for the same structure).
/// </summary>
/// <remarks>
///     <para>
///         TOON headers carry the length of an array (<c>items[3]:</c>), so arrays are started with the number of items
///         (rows, entries) they will have, and the writer checks it at the end. Each value must fit its place: a value
///         inside an object needs a property name, inline arrays and table rows take primitives only, and a document has
///         one root value (an object, an array or a primitive). A misuse throws <see cref="InvalidOperationException"/>.
///     </para>
///     <para>
///         The text is collected in a pooled buffer and written to the destination (as UTF-8 without a byte order mark
///         for byte destinations) by <see cref="Flush"/>, <see cref="FlushAsync"/> and <see cref="Dispose"/>; flush
///         now and then to keep memory flat while writing a large document. An instance is not thread-safe.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     using var writer = new ToonDocumentWriter(stream);
///     writer.WriteStartObject();
///     writer.WriteString("name", "Spring");
///     writer.WriteStartTable("products", 2, ["sku", "price"]);
///     writer.WriteStringValue("A1");
///     writer.WriteNumberValue(9.99m);
///     writer.WriteStringValue("B2");
///     writer.WriteNumberValue(19.5m);
///     writer.WriteEndArray();
///     writer.WriteEndObject();
///     // name: Spring
///     // products[2]{sku,price}:
///     //   A1,9.99
///     //   B2,19.5
///     </code>
/// </example>
public sealed class ToonDocumentWriter : IDisposable, IAsyncDisposable
{
    private enum Kind : byte
    {
        Object,

        /// <summary>An object item of a list: its first field goes on the hyphen line, which is written with it.</summary>
        ListItemObject,
        InlineArray,
        List,
        Table,
        KeyedTable
    }

    private enum Position : byte
    {
        Root,
        Field,
        ListItem
    }

    /// <summary>An open object or array.</summary>
    private struct Frame
    {
        public Kind Kind;

        /// <summary>Objects: the depth of the field lines (list item objects: of the hyphen line); arrays: of the item lines.</summary>
        public int Depth;

        /// <summary>The declared number of items, rows or entries.</summary>
        public int Length;

        /// <summary>Fields, items, rows or entries written.</summary>
        public int Count;

        /// <summary>Tables: the number of fields, and the cells of the current row written.</summary>
        public int FieldCount;

        public int Cell;

        /// <summary>Keyed tables: whether the current row's key is written.</summary>
        public bool RowStarted;

        /// <summary>The nesting levels to leave at the end.</summary>
        public int Levels;
    }

    private readonly ToonTextBuffer _buffer = new();
    private readonly ToonWriter _writer;
    private readonly ToonDomWriter _tree;
    private readonly List<Frame> _frames = [];
    private readonly IBufferWriter<byte>? _bufferWriter;
    private readonly Stream? _stream;
    private readonly TextWriter? _textWriter;
    private bool _rootStarted;
    private bool _disposed;

    /// <summary>Set when a write failed after part of it was written; the text is then incomplete.</summary>
    private bool _faulted;

    /// <summary>Creates a writer that writes UTF-8 to a buffer writer, such as a <c>PipeWriter</c>.</summary>
    /// <param name="utf8Destination">The destination; the writer does not flush or complete it.</param>
    /// <param name="options">Indentation, delimiter and depth limit; defaults when null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Destination"/> is null.</exception>
    public ToonDocumentWriter(IBufferWriter<byte> utf8Destination, ToonOptions? options = null) : this(options)
    {
        _bufferWriter = utf8Destination ?? throw new ArgumentNullException(nameof(utf8Destination));
    }

    /// <summary>Creates a writer that writes UTF-8 to a stream.</summary>
    /// <param name="utf8Stream">The destination; it is left open.</param>
    /// <param name="options">Indentation, delimiter and depth limit; defaults when null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Stream"/> is null.</exception>
    public ToonDocumentWriter(Stream utf8Stream, ToonOptions? options = null) : this(options)
    {
        _stream = utf8Stream ?? throw new ArgumentNullException(nameof(utf8Stream));
    }

    /// <summary>Creates a writer that writes text to a text writer, such as a <see cref="StringWriter"/>.</summary>
    /// <param name="destination">The destination; it is left open.</param>
    /// <param name="options">Indentation, delimiter and depth limit; defaults when null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="destination"/> is null.</exception>
    public ToonDocumentWriter(TextWriter destination, ToonOptions? options = null) : this(options)
    {
        _textWriter = destination ?? throw new ArgumentNullException(nameof(destination));
    }

    private ToonDocumentWriter(ToonOptions? options)
    {
        Options = options ?? ToonOptions.Default;
        _ = Options.ValidatedMaxDepth;
        _writer = new ToonWriter(_buffer, Options);
        _tree = new ToonDomWriter(_writer);
    }

    /// <summary>The options the writer formats with.</summary>
    public ToonOptions Options { get; }

    /// <summary>The number of objects and arrays started and not yet ended.</summary>
    public int CurrentDepth => _frames.Count;

    /// <summary>Whether the root value has been written completely.</summary>
    public bool IsComplete => _rootStarted && _frames.Count == 0;

    #region Objects

    /// <summary>
    ///     Starts an object: the root object, or an item of a list. Its fields are written with the methods that take a
    ///     property name.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when an object cannot be written here.</exception>
    public void WriteStartObject()
    {
        CheckNotDisposed();

        if (_frames.Count == 0)
        {
            StartRoot();
            _frames.Add(new Frame { Kind = Kind.Object, Depth = 0 });
            return;
        }

        ref var list = ref Top;

        if (list.Kind != Kind.List)
        {
            throw Misplaced("An object", list.Kind);
        }

        StartListItem(ref list);

        // The hyphen is written with the first field, or alone for an empty object
        _frames.Add(new Frame { Kind = Kind.ListItemObject, Depth = list.Depth });
    }

    /// <summary>Starts an object as the value of a property of the current object.</summary>
    /// <param name="propertyName">The property name.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current value is not an object.</exception>
    public void WriteStartObject(string propertyName)
    {
        var childDepth = StartField(propertyName);
        _writer.WriteKey(propertyName);
        _writer.Write(':');
        EnterNesting();
        _frames.Add(new Frame { Kind = Kind.Object, Depth = childDepth, Levels = 1 });
    }

    /// <summary>Ends the current object.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the current value is not an object.</exception>
    public void WriteEndObject()
    {
        CheckNotDisposed();
        ref var frame = ref Top;

        switch (frame.Kind)
        {
            case Kind.Object:
                _writer.Nesting -= frame.Levels;
                break;
            case Kind.ListItemObject when frame.Count == 0:
                // An empty object item is a bare hyphen (§10)
                Guard(static writer => writer.CheckEmptyNesting(), _writer);
                _writer.Write('-');
                break;
            case Kind.ListItemObject:
                _writer.ExitNesting();
                break;
            default:
                throw new InvalidOperationException("WriteEndObject does not end an array; use WriteEndArray.");
        }

        _frames.RemoveAt(_frames.Count - 1);
    }

    #endregion

    #region Arrays

    /// <summary>
    ///     Starts an inline array of primitives, <c>[N]: a,b,c</c>: the root array, or an item of a list.
    /// </summary>
    /// <param name="length">The number of values that follow.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an array cannot be written here.</exception>
    public void WriteStartArray(int length) => StartUnnamedArray(Kind.InlineArray, length, default);

    /// <summary>Starts an inline array of primitives, <c>name[N]: a,b,c</c>, as the value of a property.</summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="length">The number of values that follow.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current value is not an object.</exception>
    public void WriteStartArray(string propertyName, int length) => StartNamedArray(propertyName, Kind.InlineArray, length, default);

    /// <summary>
    ///     Starts an expanded list, <c>[N]:</c> with one <c>- item</c> line per item (primitives, objects or arrays): the
    ///     root array, or an item of a list.
    /// </summary>
    /// <param name="length">The number of items that follow.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an array cannot be written here.</exception>
    public void WriteStartList(int length) => StartUnnamedArray(Kind.List, length, default);

    /// <summary>Starts an expanded list, <c>name[N]:</c> with one <c>- item</c> line per item, as the value of a property.</summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="length">The number of items that follow.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current value is not an object.</exception>
    public void WriteStartList(string propertyName, int length) => StartNamedArray(propertyName, Kind.List, length, default);

    /// <summary>
    ///     Starts the root tabular array, <c>[N]{a,b}:</c>, whose rows are written as primitive values, one per field and
    ///     row (a row ends after its last field).
    /// </summary>
    /// <param name="length">The number of rows that follow.</param>
    /// <param name="fields">The field names, at least one, distinct.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fields"/> is empty, has a null or a repeated name.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when a table cannot be written here (also as a list item, where <see cref="ToonEncoder"/> writes a list).
    /// </exception>
    public void WriteStartTable(int length, ReadOnlySpan<string> fields) => StartUnnamedArray(Kind.Table, length, fields);

    /// <summary>
    ///     Starts a tabular array, <c>name[N]{a,b}:</c>, as the value of a property; its rows are written as primitive
    ///     values, one per field and row.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="length">The number of rows that follow.</param>
    /// <param name="fields">The field names, at least one, distinct.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fields"/> is empty, has a null or a repeated name.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current value is not an object.</exception>
    public void WriteStartTable(string propertyName, int length, ReadOnlySpan<string> fields) =>
        StartNamedArray(propertyName, Kind.Table, length, fields);

    /// <summary>
    ///     Starts the root keyed table, <c>[N:]{a,b}:</c> (spec §9.5): an object whose entries are objects with the same
    ///     fields, one <c>key: a,b</c> row per entry. Each row starts with <see cref="WriteRowKey"/>, followed by one
    ///     primitive value per field.
    /// </summary>
    /// <param name="length">The number of entries that follow, at least one.</param>
    /// <param name="fields">The field names, at least one, distinct.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fields"/> is empty, has a null or a repeated name.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is less than one.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a keyed table cannot be written here.</exception>
    public void WriteStartKeyedTable(int length, ReadOnlySpan<string> fields) => StartUnnamedArray(Kind.KeyedTable, length, fields);

    /// <summary>
    ///     Starts a keyed table, <c>name[N:]{a,b}:</c>, as the value of a property: an object whose entries are objects
    ///     with the same fields. Each row starts with <see cref="WriteRowKey"/>, followed by one primitive value per field.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="length">The number of entries that follow, at least one.</param>
    /// <param name="fields">The field names, at least one, distinct.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fields"/> is empty, has a null or a repeated name.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is less than one.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current value is not an object.</exception>
    public void WriteStartKeyedTable(string propertyName, int length, ReadOnlySpan<string> fields) =>
        StartNamedArray(propertyName, Kind.KeyedTable, length, fields);

    /// <summary>Starts the next row of the current keyed table with the entry's key.</summary>
    /// <param name="key">The entry key.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the current value is not a keyed table, the previous row is incomplete, or all rows are written.
    /// </exception>
    public void WriteRowKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        CheckNotDisposed();
        CheckText(key);
        ref var frame = ref Top;

        if (frame.Kind != Kind.KeyedTable)
        {
            throw new InvalidOperationException("WriteRowKey starts a row of a keyed table only.");
        }

        if (frame.RowStarted)
        {
            throw new InvalidOperationException($"The row has {frame.Cell} of {frame.FieldCount} values; write the rest before the next row.");
        }

        CheckRoom(frame, "row");
        _writer.StartLine(frame.Depth);
        _writer.WriteKey(key);
        _writer.Write(": ");
        frame.RowStarted = true;
    }

    /// <summary>Ends the current array, table or keyed table, checking that its declared length was written.</summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the current value is not an array, or fewer items, rows or values were written than declared.
    /// </exception>
    public void WriteEndArray()
    {
        CheckNotDisposed();
        ref var frame = ref Top;

        if (frame.Kind is Kind.Object or Kind.ListItemObject)
        {
            throw new InvalidOperationException("WriteEndArray does not end an object; use WriteEndObject.");
        }

        if (frame.Count != frame.Length || frame.Cell != 0 || frame.RowStarted)
        {
            var unit = frame.Kind switch { Kind.Table => "rows", Kind.KeyedTable => "entries", _ => "items" };
            var row = frame.Cell != 0 || frame.RowStarted ? $" and a row with {frame.Cell} of {frame.FieldCount} values" : "";
            throw new InvalidOperationException($"The array declares {frame.Length} {unit} but has {frame.Count}{row}.");
        }

        _writer.Nesting -= frame.Levels;
        _frames.RemoveAt(_frames.Count - 1);
    }

    #endregion

    #region Primitive values

    /// <summary>Writes a string value (null writes <c>null</c>): the root value, an array item or a table cell.</summary>
    /// <exception cref="InvalidOperationException">Thrown when a value cannot be written here.</exception>
    /// <exception cref="ToonEncodingException">Thrown when the string has an unpaired surrogate.</exception>
    public void WriteStringValue(string? value)
    {
        if (value is null)
        {
            WriteNullValue();
            return;
        }

        CheckText(value);
        StartValue();
        _writer.WriteString(value);
    }

    /// <summary>Writes a string value: the root value, an array item or a table cell.</summary>
    /// <exception cref="InvalidOperationException">Thrown when a value cannot be written here.</exception>
    /// <exception cref="ToonEncodingException">Thrown when the string has an unpaired surrogate.</exception>
    public void WriteStringValue(ReadOnlySpan<char> value)
    {
        CheckText(value);
        StartValue();
        _writer.WriteString(value);
    }

    /// <summary>Writes a number: the root value, an array item or a table cell.</summary>
    /// <exception cref="InvalidOperationException">Thrown when a value cannot be written here.</exception>
    public void WriteNumberValue(long value)
    {
        StartValue();
        _writer.WriteInteger(value);
    }

    /// <summary>Writes a number: the root value, an array item or a table cell.</summary>
    /// <exception cref="InvalidOperationException">Thrown when a value cannot be written here.</exception>
    public void WriteNumberValue(ulong value)
    {
        StartValue();
        _writer.WriteDecimal(value);
    }

    /// <summary>Writes a number in canonical decimal form: the root value, an array item or a table cell.</summary>
    /// <exception cref="InvalidOperationException">Thrown when a value cannot be written here.</exception>
    public void WriteNumberValue(decimal value)
    {
        StartValue();
        _writer.WriteDecimal(value);
    }

    /// <summary>
    ///     Writes a number in its shortest round-trip form (NaN and infinities as <c>null</c>, spec §3): the root value, an
    ///     array item or a table cell.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a value cannot be written here.</exception>
    public void WriteNumberValue(double value)
    {
        StartValue();
        _writer.WriteDouble(value);
    }

    /// <summary>
    ///     Writes a number in its shortest round-trip form (NaN and infinities as <c>null</c>): the root value, an array
    ///     item or a table cell.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a value cannot be written here.</exception>
    public void WriteNumberValue(float value)
    {
        StartValue();
        _writer.WriteSingle(value);
    }

    /// <summary>Writes <c>true</c> or <c>false</c>: the root value, an array item or a table cell.</summary>
    /// <exception cref="InvalidOperationException">Thrown when a value cannot be written here.</exception>
    public void WriteBooleanValue(bool value)
    {
        StartValue();
        _writer.WriteBoolean(value);
    }

    /// <summary>Writes <c>null</c>: the root value, an array item or a table cell.</summary>
    /// <exception cref="InvalidOperationException">Thrown when a value cannot be written here.</exception>
    public void WriteNullValue()
    {
        StartValue();
        _writer.WriteNull();
    }

    #endregion

    #region Properties

    /// <summary>Writes a property with a string value (null writes <c>null</c>).</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current value is not an object.</exception>
    /// <exception cref="ToonEncodingException">Thrown when the string has an unpaired surrogate.</exception>
    public void WriteString(string propertyName, string? value)
    {
        CheckText(value);
        StartPrimitiveField(propertyName);

        if (value is null)
        {
            _writer.WriteNull();
        }
        else
        {
            _writer.WriteString(value);
        }
    }

    /// <summary>Writes a property with a string value.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current value is not an object.</exception>
    /// <exception cref="ToonEncodingException">Thrown when the string has an unpaired surrogate.</exception>
    public void WriteString(string propertyName, ReadOnlySpan<char> value)
    {
        CheckText(value);
        StartPrimitiveField(propertyName);
        _writer.WriteString(value);
    }

    /// <summary>Writes a property with a number value.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current value is not an object.</exception>
    public void WriteNumber(string propertyName, long value)
    {
        StartPrimitiveField(propertyName);
        _writer.WriteInteger(value);
    }

    /// <summary>Writes a property with a number value.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current value is not an object.</exception>
    public void WriteNumber(string propertyName, ulong value)
    {
        StartPrimitiveField(propertyName);
        _writer.WriteDecimal(value);
    }

    /// <summary>Writes a property with a number value in canonical decimal form.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current value is not an object.</exception>
    public void WriteNumber(string propertyName, decimal value)
    {
        StartPrimitiveField(propertyName);
        _writer.WriteDecimal(value);
    }

    /// <summary>Writes a property with a number value (NaN and infinities as <c>null</c>).</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current value is not an object.</exception>
    public void WriteNumber(string propertyName, double value)
    {
        StartPrimitiveField(propertyName);
        _writer.WriteDouble(value);
    }

    /// <summary>Writes a property with a number value (NaN and infinities as <c>null</c>).</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current value is not an object.</exception>
    public void WriteNumber(string propertyName, float value)
    {
        StartPrimitiveField(propertyName);
        _writer.WriteSingle(value);
    }

    /// <summary>Writes a property with a <c>true</c> or <c>false</c> value.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current value is not an object.</exception>
    public void WriteBoolean(string propertyName, bool value)
    {
        StartPrimitiveField(propertyName);
        _writer.WriteBoolean(value);
    }

    /// <summary>Writes a property with a <c>null</c> value.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current value is not an object.</exception>
    public void WriteNull(string propertyName)
    {
        StartPrimitiveField(propertyName);
        _writer.WriteNull();
    }

    #endregion

    #region Trees

    /// <summary>
    ///     Writes a <see cref="ToonValue"/> tree where a value goes (the root, an array item or a table cell), in the form
    ///     <see cref="ToonEncoder"/> chooses for it; table cells and inline array items take primitives only.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the value cannot be written here.</exception>
    /// <exception cref="ToonEncodingException">Thrown when the tree is nested too deeply or has an invalid string.</exception>
    public void WriteValue(ToonValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        CheckNotDisposed();

        if (_frames.Count == 0)
        {
            StartRoot();
            Guard(static state => state.Tree.WriteRoot(state.Value), (Tree: _tree, Value: value));
            return;
        }

        ref var frame = ref Top;

        if (frame.Kind == Kind.List)
        {
            StartListItem(ref frame);
            Guard(static state => state.Tree.WriteListItem(state.Value, state.Depth), (Tree: _tree, Value: value, frame.Depth));
            return;
        }

        if (value is ToonObject or ToonArray)
        {
            throw Misplaced("An object or array", frame.Kind);
        }

        StartValue();
        _writer.WritePrimitive(value);
    }

    /// <summary>
    ///     Writes a property whose value is a <see cref="ToonValue"/> tree, in the form <see cref="ToonEncoder"/> chooses
    ///     for it.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> or <paramref name="value"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current value is not an object.</exception>
    /// <exception cref="ToonEncodingException">Thrown when the tree is nested too deeply or has an invalid string.</exception>
    public void WriteValue(string propertyName, ToonValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var childDepth = StartField(propertyName);
        Guard(static state => state.Tree.WriteField(state.Name, state.Value, state.ChildDepth), (Tree: _tree, Name: propertyName, Value: value, ChildDepth: childDepth));
    }

    #endregion

    #region Output

    /// <summary>Writes the text collected so far to the destination and clears the buffer.</summary>
    /// <exception cref="IOException">Thrown when writing to the destination fails.</exception>
    public void Flush()
    {
        // Also after a failed write: the text written so far goes to the destination
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_buffer.Length == 0)
        {
            return;
        }

        if (_bufferWriter != null)
        {
            _buffer.WriteUtf8(_bufferWriter);
        }
        else if (_stream != null)
        {
            _buffer.WriteUtf8(_stream);
            _stream.Flush();
        }
        else
        {
            _buffer.WriteTo(_textWriter!);
            _textWriter!.Flush();
        }

        _buffer.Clear();
    }

    /// <summary>Asynchronously writes the text collected so far to the destination and clears the buffer.</summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <exception cref="IOException">Thrown when writing to the destination fails.</exception>
    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_buffer.Length == 0)
        {
            return;
        }

        if (_stream != null)
        {
            await _buffer.WriteUtf8Async(_stream, cancellationToken).ConfigureAwait(false);
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (_textWriter != null)
        {
            await _buffer.WriteToAsync(_textWriter, cancellationToken).ConfigureAwait(false);
            await _textWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _buffer.WriteUtf8(_bufferWriter!);
        }

        _buffer.Clear();
    }

    /// <summary>Flushes the text collected so far and releases the buffer; the document is not checked for completeness.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            Flush();
        }
        finally
        {
            _disposed = true;
            _buffer.Dispose();
        }
    }

    /// <summary>Asynchronously flushes the text collected so far and releases the buffer.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            await FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            _disposed = true;
            _buffer.Dispose();
        }
    }

    #endregion

    #region State

    private ref Frame Top
    {
        get
        {
            if (_frames.Count == 0)
            {
                throw new InvalidOperationException(_rootStarted ? "The document is complete." : "No object or array is open.");
            }

            return ref System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_frames)[^1];
        }
    }

    private void CheckNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_faulted)
        {
            throw new InvalidOperationException("A previous write failed part way, so the text is incomplete; write the document again with a new writer.");
        }
    }

    /// <summary>
    ///     Rejects a string with an unpaired surrogate before anything of the value (or its key) is written, so a failed
    ///     write leaves the text as it was.
    /// </summary>
    private static void CheckText(ReadOnlySpan<char> text)
    {
        var index = text.IndexOfAnyInRange('\uD800', '\uDFFF');

        if (index < 0)
        {
            return;
        }

        for (var i = index; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                i++;
            }
            else if (char.IsSurrogate(text[i]))
            {
                throw new ToonEncodingException($"A string contains an unpaired surrogate (U+{(int)text[i]:X4}), which TOON cannot represent");
            }
        }
    }

    /// <summary>Enters a nesting level; a failure (the depth limit) leaves the writer faulted, as the header is written.</summary>
    private void EnterNesting() => Guard(static writer => writer.EnterNesting(), _writer);

    /// <summary>Runs a step that may fail after writing part of its text; a failure leaves the writer faulted.</summary>
    private void Guard<TState>(Action<TState> step, TState state)
    {
        try
        {
            step(state);
        }
        catch
        {
            _faulted = true;
            throw;
        }
    }

    private void StartRoot()
    {
        if (_rootStarted)
        {
            throw new InvalidOperationException("The document is complete: it has one root value.");
        }

        _rootStarted = true;
    }

    /// <summary>Starts a field of the current object on its line; returns the depth of the field's nested content.</summary>
    private int StartField(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        CheckNotDisposed();
        CheckText(propertyName);

        if (_frames.Count == 0)
        {
            throw new InvalidOperationException(_rootStarted
                ? "The document is complete."
                : "A property needs an object; start the root object with WriteStartObject() first.");
        }

        ref var frame = ref Top;
        int childDepth;

        switch (frame.Kind)
        {
            case Kind.Object:
                _writer.StartLine(frame.Depth);
                childDepth = frame.Depth + 1;
                break;
            case Kind.ListItemObject:
                // The first field goes on the hyphen line, the others one level below it; nested content two levels below (§10)
                if (frame.Count == 0)
                {
                    _writer.Write("- ");
                    EnterNesting();
                }
                else
                {
                    _writer.StartLine(frame.Depth + 1);
                }

                childDepth = frame.Depth + 2;
                break;
            default:
                throw new InvalidOperationException($"A property name is only valid inside an object, not in {Describe(frame.Kind)}.");
        }

        frame.Count++;
        return childDepth;
    }

    private void StartPrimitiveField(string propertyName)
    {
        StartField(propertyName);
        _writer.WriteKey(propertyName);
        _writer.Write(": ");
    }

    /// <summary>Prepares for a primitive value: the root value, the next array item or table cell.</summary>
    private void StartValue()
    {
        CheckNotDisposed();

        if (_frames.Count == 0)
        {
            StartRoot();
            _writer.StartLine(0);
            _writer.BeginRootPrimitive();
            return;
        }

        ref var frame = ref Top;

        switch (frame.Kind)
        {
            case Kind.InlineArray:
                CheckRoom(frame, "item");

                if (frame.Count > 0)
                {
                    _writer.WriteDelimiter();
                }

                frame.Count++;
                break;
            case Kind.List:
                StartListItem(ref frame);
                _writer.Write("- ");
                break;
            case Kind.Table:
                if (frame.Cell == 0)
                {
                    CheckRoom(frame, "row");
                    _writer.StartLine(frame.Depth);
                }
                else
                {
                    _writer.WriteDelimiter();
                }

                EndCell(ref frame);
                break;
            case Kind.KeyedTable:
                if (!frame.RowStarted)
                {
                    throw new InvalidOperationException("Start each row of a keyed table with WriteRowKey.");
                }

                if (frame.Cell > 0)
                {
                    _writer.WriteDelimiter();
                }

                EndCell(ref frame);
                break;
            default:
                throw new InvalidOperationException("A value inside an object needs a property name; use the methods that take one.");
        }
    }

    private static void EndCell(ref Frame frame)
    {
        if (++frame.Cell == frame.FieldCount)
        {
            frame.Cell = 0;
            frame.Count++;
            frame.RowStarted = false;
        }
    }

    /// <summary>Starts the line of the next item of a list.</summary>
    private void StartListItem(ref Frame list)
    {
        CheckRoom(list, "item");
        _writer.StartLine(list.Depth);
        list.Count++;
    }

    private static void CheckRoom(in Frame frame, string unit)
    {
        if (frame.Count >= frame.Length)
        {
            throw new InvalidOperationException($"The array declares {frame.Length} {unit}{(frame.Length == 1 ? "" : "s")}; all are written.");
        }
    }

    private void StartUnnamedArray(Kind kind, int length, ReadOnlySpan<string> fields)
    {
        CheckNotDisposed();
        CheckArguments(kind, length, fields);

        if (_frames.Count == 0)
        {
            StartRoot();
            _writer.StartLine(0);
            _writer.Nesting = -1; // the root array is at depth 0, like the root object
            StartArray(kind, null, length, fields, Position.Root, 1);
            return;
        }

        ref var list = ref Top;

        if (list.Kind != Kind.List)
        {
            throw Misplaced("An array", list.Kind);
        }

        if (kind is Kind.Table or Kind.KeyedTable)
        {
            throw new InvalidOperationException("A list item cannot be a table; write it as a list of objects.");
        }

        StartListItem(ref list);
        _writer.Write("- ");
        StartArray(kind, null, length, fields, Position.ListItem, list.Depth + 1);
    }

    private void StartNamedArray(string propertyName, Kind kind, int length, ReadOnlySpan<string> fields)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        CheckArguments(kind, length, fields);

        var childDepth = StartField(propertyName);
        StartArray(kind, propertyName, length, fields, Position.Field, childDepth);
    }

    private static void CheckArguments(Kind kind, int length, ReadOnlySpan<string> fields)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        if (kind is not (Kind.Table or Kind.KeyedTable))
        {
            return;
        }

        if (kind == Kind.KeyedTable)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);
        }

        if (fields.IsEmpty)
        {
            throw new ArgumentException("A table needs at least one field.", nameof(fields));
        }

        for (var i = 0; i < fields.Length; i++)
        {
            if (fields[i] is null)
            {
                throw new ArgumentException("A field name is null.", nameof(fields));
            }

            // Before anything is written: the header with a field TOON cannot represent would be cut off
            CheckText(fields[i]);

            for (var j = 0; j < i; j++)
            {
                if (fields[j] == fields[i])
                {
                    throw new ArgumentException($"The field '{fields[i]}' is repeated.", nameof(fields));
                }
            }
        }
    }

    /// <summary>Writes an array header (or an empty array) like <see cref="ToonDomWriter"/> and opens its frame.</summary>
    private void StartArray(Kind kind, string? key, int length, ReadOnlySpan<string> fields, Position position, int childDepth)
    {
        var writer = _writer;

        if (length == 0)
        {
            var arrayPosition = position switch
            {
                Position.Root => ToonArrayPosition.Root,
                Position.Field => ToonArrayPosition.Field,
                _ => ToonArrayPosition.ListItem
            };

            Guard(static state => ToonDomWriter.WriteEmptyArray(state.Writer, state.Key, state.Position), (Writer: writer, Key: key, Position: arrayPosition));

            _frames.Add(new Frame { Kind = kind, FieldCount = fields.Length });
            return;
        }

        EnterNesting();

        switch (kind)
        {
            case Kind.InlineArray:
                writer.WriteArrayHeader(key, length);
                writer.Write(": ");
                _frames.Add(new Frame { Kind = kind, Length = length, Levels = 1 });
                break;
            case Kind.List:
                writer.WriteArrayHeader(key, length);
                writer.Write(':');
                _frames.Add(new Frame { Kind = kind, Depth = childDepth, Length = length, Levels = 1 });
                break;
            default:
                if (kind == Kind.Table)
                {
                    writer.WriteArrayHeader(key, length);
                }
                else
                {
                    writer.WriteKeyedHeader(key, length);
                }

                writer.WriteFieldList(fields); // the fields were checked before the header was started

                // Rows are objects one level below the array
                EnterNesting();
                _frames.Add(new Frame { Kind = kind, Depth = childDepth, Length = length, FieldCount = fields.Length, Levels = 2 });
                break;
        }
    }

    private static InvalidOperationException Misplaced(string what, Kind current) =>
        new($"{what} cannot be written in {Describe(current)}{current switch
        {
            Kind.Object or Kind.ListItemObject => "; use the overload that takes a property name",
            Kind.InlineArray => "; an inline array holds primitives, use WriteStartList for other items",
            _ => ""
        }}.");

    private static string Describe(Kind kind) => kind switch
    {
        Kind.Object or Kind.ListItemObject => "an object",
        Kind.InlineArray => "an inline array",
        Kind.List => "a list",
        Kind.Table => "a table",
        _ => "a keyed table"
    };

    #endregion
}
