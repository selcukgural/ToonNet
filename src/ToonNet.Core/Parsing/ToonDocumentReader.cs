using System.Runtime.InteropServices;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;

namespace ToonNet.Core.Parsing;

/// <summary>The kind of token a <see cref="ToonDocumentReader"/> is positioned on.</summary>
public enum ToonTokenType : byte
{
    /// <summary>Before the first <see cref="ToonDocumentReader.Read"/>, or after the end of the document.</summary>
    None,

    /// <summary>The start of an object (also a tabular row, a keyed table and its entries).</summary>
    StartObject,

    /// <summary>The end of an object.</summary>
    EndObject,

    /// <summary>The start of an array (inline, tabular or expanded); <see cref="ToonDocumentReader.ArrayLength"/> has its length.</summary>
    StartArray,

    /// <summary>The end of an array.</summary>
    EndArray,

    /// <summary>The name of a property; the next token is its value.</summary>
    PropertyName,

    /// <summary>A string, quoted or not.</summary>
    String,

    /// <summary>A number.</summary>
    Number,

    /// <summary><c>true</c>.</summary>
    True,

    /// <summary><c>false</c>.</summary>
    False,

    /// <summary><c>null</c>.</summary>
    Null
}

/// <summary>
///     Reads a TOON document forward-only, token by token, without building a <see cref="ToonDocument"/>. The tokens
///     follow the data model, not the layout: tabular rows, keyed tables and list items are objects and arrays like any
///     other, with property names and values.
/// </summary>
/// <remarks>
///     <para>
///         The reader checks the document as it goes, with the rules and errors of <see cref="ToonDocument.Parse(string,ToonOptions)"/>
///         (<see cref="ToonParseException"/> with line and column); the end of the document is checked when
///         <see cref="Read"/> returns false. UTF-8 input is decoded into a pooled buffer, so a byte document is read
///         without creating a string. Dispose the reader to return its buffers. An instance is not thread-safe.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     using var reader = new ToonDocumentReader("users[2]{id,name}:\n  1,Ada\n  2,Bob");
///     while (reader.Read())
///     {
///         if (reader.TokenType == ToonTokenType.PropertyName &amp;&amp; reader.GetPropertyName() == "name")
///         {
///             reader.Read();
///             Console.WriteLine(reader.GetString());
///         }
///     }
///     </code>
/// </example>
public sealed class ToonDocumentReader : IDisposable
{
    /// <summary>An open object or array.</summary>
    private struct Frame
    {
        public bool IsArray;
        public ToonParser.ObjectCursor Object;
        public ToonParser.ArrayCursor Array;
    }

    private readonly ToonParser _parser;
    private readonly List<Frame> _frames = [];
    private ToonPooledText _pooledText;
    private bool _started;
    private bool _done;
    private bool _disposed;

    /// <summary>
    ///     The current value: the value token (for an object or array, not yet entered unless <see cref="_entered"/>), or
    ///     the value of the current property name.
    /// </summary>
    private ToonParser.ValueRef _value;

    /// <summary>Whether the object or array of the current start token was entered or skipped (the next read moves past it).</summary>
    private bool _entered;

    private string? _propertyName;
    private string? _string;
    private double _number;
    private decimal _exactNumber;
    private bool _hasExactNumber;

    /// <summary>Creates a reader for TOON text.</summary>
    /// <param name="toon">The TOON text.</param>
    /// <param name="options">Parsing options (indentation, strict mode, limits); defaults when null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toon"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown for errors in a line's indentation.</exception>
    public ToonDocumentReader(string toon, ToonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(toon);
        _parser = new ToonParser(options);
        _parser.BeginDocument(toon);
    }

    /// <summary>Creates a reader for UTF-8 encoded TOON text, decoded into a pooled buffer.</summary>
    /// <param name="utf8Toon">The UTF-8 encoded TOON text; a byte order mark at the start is ignored.</param>
    /// <param name="options">Parsing options (indentation, strict mode, limits); defaults when null.</param>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8, or for errors in a line's indentation.</exception>
    public ToonDocumentReader(ReadOnlySpan<byte> utf8Toon, ToonOptions? options = null) : this(ToonTextEncoding.Decode(utf8Toon), options)
    {
    }

    /// <summary>Creates a reader for TOON text in a span of characters, copied into a pooled buffer.</summary>
    /// <param name="toon">The TOON text.</param>
    /// <param name="options">Parsing options (indentation, strict mode, limits); defaults when null.</param>
    /// <exception cref="ToonParseException">Thrown for errors in a line's indentation.</exception>
    public ToonDocumentReader(ReadOnlySpan<char> toon, ToonOptions? options = null) : this(ToonPooledText.Copy(toon), options)
    {
    }

    private ToonDocumentReader(ToonPooledText text, ToonOptions? options)
    {
        _pooledText = text;

        try
        {
            _parser = new ToonParser(options);
            _parser.BeginDocument(text.Text);
        }
        catch
        {
            _pooledText.Dispose();
            throw;
        }
    }

    /// <summary>Creates a reader for UTF-8 encoded TOON text read from a stream to its end.</summary>
    /// <param name="utf8Toon">The stream; it is read to the end and left open.</param>
    /// <param name="options">Parsing options (indentation, strict mode, limits); defaults when null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Toon"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8, or for errors in a line's indentation.</exception>
    public static ToonDocumentReader Create(Stream utf8Toon, ToonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(utf8Toon);
        return new ToonDocumentReader(ToonTextEncoding.Read(utf8Toon), options);
    }

    /// <summary>Asynchronously creates a reader for UTF-8 encoded TOON text read from a stream to its end.</summary>
    /// <param name="utf8Toon">The stream; it is read to the end and left open.</param>
    /// <param name="options">Parsing options (indentation, strict mode, limits); defaults when null.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Toon"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8, or for errors in a line's indentation.</exception>
    public static async ValueTask<ToonDocumentReader> CreateAsync(Stream utf8Toon, ToonOptions? options = null,
                                                                  CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(utf8Toon);
        var text = await ToonTextEncoding.ReadAsync(utf8Toon, cancellationToken).ConfigureAwait(false);
        return new ToonDocumentReader(text, options);
    }

    /// <summary>The token the reader is positioned on.</summary>
    public ToonTokenType TokenType { get; private set; }

    /// <summary>The number of objects and arrays entered and not yet ended.</summary>
    public int CurrentDepth => _frames.Count;

    /// <summary>
    ///     The declared length of the array of the current <see cref="ToonTokenType.StartArray"/> token (its items or
    ///     rows). Strict mode requires the array to have exactly that many; non-strict mode accepts another number (§14.4).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the reader is not on the start of an array.</exception>
    public int ArrayLength
    {
        get
        {
            if (TokenType != ToonTokenType.StartArray || _entered)
            {
                throw new InvalidOperationException("ArrayLength is available on the StartArray token.");
            }

            return _value.Kind == ToonParser.RefKind.HeaderArray ? _parser.HeaderOf(_value.HeaderIndex).Length : 0;
        }
    }

    /// <summary>The 1-based line of the current property name or value.</summary>
    public int LineNumber => _started && !_done ? _parser.LineOf(_value.LineIndex).Number : 0;

    /// <summary>
    ///     The text of the current primitive token as it is in the document (a quoted string with its quotes and escapes).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the reader is not on a primitive.</exception>
    public ReadOnlySpan<char> ValueSpan
    {
        get
        {
            CheckPrimitive();
            return _parser.TokenOf(_value);
        }
    }

    /// <summary>Reads the next token.</summary>
    /// <returns>True when a token was read; false at the end of the document, after checking that nothing follows the root value.</returns>
    /// <exception cref="ToonParseException">Thrown when the document is invalid.</exception>
    public bool Read()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_done)
        {
            return false;
        }

        if (!_started)
        {
            _started = true;
            Emit(_parser.ReadRoot());
            return true;
        }

        switch (TokenType)
        {
            case ToonTokenType.PropertyName:
                Emit(_value);
                return true;
            case ToonTokenType.StartObject when !_entered:
                _frames.Add(new Frame { Object = _parser.BeginObject(_value) });
                break;
            case ToonTokenType.StartArray when !_entered:
                _frames.Add(new Frame { IsArray = true, Array = _parser.BeginArray(_value) });
                break;
        }

        return Advance();
    }

    /// <summary>
    ///     Skips the current value: on a property name its value, on the start of an object or array everything up to its
    ///     end. The reader is then on the value's last token, and the next <see cref="Read"/> moves past it.
    /// </summary>
    /// <exception cref="ToonParseException">Thrown when the skipped content is invalid.</exception>
    public void Skip()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (TokenType == ToonTokenType.PropertyName)
        {
            Emit(_value);
        }

        if (TokenType is ToonTokenType.StartObject or ToonTokenType.StartArray && !_entered)
        {
            _parser.Skip(_value);
            TokenType = TokenType == ToonTokenType.StartObject ? ToonTokenType.EndObject : ToonTokenType.EndArray;
            _entered = true;
        }
    }

    /// <summary>
    ///     Reads the current value into a <see cref="ToonValue"/> tree: on a property name its value, on the start of an
    ///     object or array the whole object or array, on a primitive the primitive. The reader is then on the value's last
    ///     token.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the reader is not on a property name or a value.</exception>
    /// <exception cref="ToonParseException">Thrown when the value is invalid.</exception>
    public ToonValue ReadValue()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (TokenType == ToonTokenType.PropertyName)
        {
            Emit(_value);
        }

        switch (TokenType)
        {
            case ToonTokenType.StartObject or ToonTokenType.StartArray when !_entered:
            {
                var value = _parser.BuildValue(_value);
                TokenType = TokenType == ToonTokenType.StartObject ? ToonTokenType.EndObject : ToonTokenType.EndArray;
                _entered = true;
                return value;
            }
            case ToonTokenType.String or ToonTokenType.Number or ToonTokenType.True or ToonTokenType.False or ToonTokenType.Null:
                return _parser.ReadPrimitive(_value);
            default:
                throw new InvalidOperationException($"ReadValue needs a property name or the start of a value, not {TokenType}.");
        }
    }

    /// <summary>The name of the current property.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the reader is not on a property name.</exception>
    public string GetPropertyName()
    {
        return TokenType == ToonTokenType.PropertyName
            ? _propertyName!
            : throw new InvalidOperationException($"GetPropertyName needs a PropertyName token, not {TokenType}.");
    }

    /// <summary>The current string, unescaped.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the reader is not on a string.</exception>
    public string GetString()
    {
        if (TokenType != ToonTokenType.String)
        {
            throw new InvalidOperationException($"GetString needs a String token, not {TokenType}.");
        }

        return _string ??= _parser.TokenOf(_value).ToString();
    }

    /// <summary>The current boolean.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the reader is not on <c>true</c> or <c>false</c>.</exception>
    public bool GetBoolean()
    {
        return TokenType switch
        {
            ToonTokenType.True => true,
            ToonTokenType.False => false,
            _ => throw new InvalidOperationException($"GetBoolean needs a True or False token, not {TokenType}.")
        };
    }

    /// <summary>The current number as the closest <see cref="double"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the reader is not on a number.</exception>
    public double GetDouble()
    {
        CheckNumber();
        return _number;
    }

    /// <summary>The current number as a <see cref="decimal"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the reader is not on a number.</exception>
    /// <exception cref="FormatException">Thrown when the number does not fit a decimal.</exception>
    public decimal GetDecimal() => TryGetDecimal(out var value) ? value : throw NumberFormat(nameof(Decimal));

    /// <summary>The current number as a <see cref="long"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the reader is not on a number.</exception>
    /// <exception cref="FormatException">Thrown when the number is not an integer in the range of <see cref="long"/>.</exception>
    public long GetInt64() => TryGetInt64(out var value) ? value : throw NumberFormat(nameof(Int64));

    /// <summary>The current number as an <see cref="int"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the reader is not on a number.</exception>
    /// <exception cref="FormatException">Thrown when the number is not an integer in the range of <see cref="int"/>.</exception>
    public int GetInt32() => TryGetInt32(out var value) ? value : throw NumberFormat(nameof(Int32));

    /// <summary>Gets the current number as a <see cref="decimal"/>, when it fits one exactly.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the reader is not on a number.</exception>
    public bool TryGetDecimal(out decimal value)
    {
        CheckNumber();
        value = _exactNumber;
        return _hasExactNumber;
    }

    /// <summary>Gets the current number as a <see cref="long"/>, when it is an integer in its range.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the reader is not on a number.</exception>
    public bool TryGetInt64(out long value)
    {
        CheckNumber();

        if (_hasExactNumber && _exactNumber == decimal.Truncate(_exactNumber) && _exactNumber is >= long.MinValue and <= long.MaxValue)
        {
            value = (long)_exactNumber;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>Gets the current number as an <see cref="int"/>, when it is an integer in its range.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the reader is not on a number.</exception>
    public bool TryGetInt32(out int value)
    {
        if (TryGetInt64(out var number) && number is >= int.MinValue and <= int.MaxValue)
        {
            value = (int)number;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>Returns the reader's buffers to the pool.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _parser.ReleaseDocument();
        _pooledText.Dispose();
        _frames.Clear();
    }

    /// <summary>Moves to the next property, item or end in the innermost open object or array.</summary>
    private bool Advance()
    {
        if (_frames.Count == 0)
        {
            // The root value is complete
            _parser.EndDocument();
            _done = true;
            TokenType = ToonTokenType.None;
            return false;
        }

        ref var frame = ref CollectionsMarshal.AsSpan(_frames)[^1];

        if (frame.IsArray)
        {
            if (_parser.NextItem(ref frame.Array, out var item))
            {
                Emit(item);
                return true;
            }

            _frames.RemoveAt(_frames.Count - 1);
            SetEnd(ToonTokenType.EndArray);
            return true;
        }

        if (_parser.NextProperty(ref frame.Object, out var key, out var value))
        {
            TokenType = ToonTokenType.PropertyName;
            _propertyName = key;
            _value = value;
            _entered = false;
            return true;
        }

        _frames.RemoveAt(_frames.Count - 1);
        SetEnd(ToonTokenType.EndObject);
        return true;
    }

    private void SetEnd(ToonTokenType token)
    {
        TokenType = token;
        _entered = true;
    }

    /// <summary>Positions the reader on a value: the start of an object or array, or a classified primitive.</summary>
    private void Emit(in ToonParser.ValueRef value)
    {
        _value = value;
        _entered = false;
        _string = null;

        switch (value.NodeKind)
        {
            case ToonNodeKind.Object:
                TokenType = ToonTokenType.StartObject;
                return;
            case ToonNodeKind.Array:
                TokenType = ToonTokenType.StartArray;
                return;
        }

        var token = _parser.TokenOf(value);

        if (token.Length > 0 && token[0] == '"')
        {
            // Unescaped now, so an invalid string is an error of the document even when it is not read
            _string = _parser.ReadQuotedString(value);
            TokenType = ToonTokenType.String;
        }
        else if (token.SequenceEqual("true"))
        {
            TokenType = ToonTokenType.True;
        }
        else if (token.SequenceEqual("false"))
        {
            TokenType = ToonTokenType.False;
        }
        else if (token.SequenceEqual("null"))
        {
            TokenType = ToonTokenType.Null;
        }
        else if (ToonParser.TryParseNumberParts(token, out _number, out _exactNumber, out _hasExactNumber))
        {
            TokenType = ToonTokenType.Number;
        }
        else
        {
            TokenType = ToonTokenType.String;
        }
    }

    private void CheckPrimitive()
    {
        if (TokenType is not (ToonTokenType.String or ToonTokenType.Number or ToonTokenType.True or ToonTokenType.False or ToonTokenType.Null))
        {
            throw new InvalidOperationException($"ValueSpan needs a primitive token, not {TokenType}.");
        }
    }

    private void CheckNumber()
    {
        if (TokenType != ToonTokenType.Number)
        {
            throw new InvalidOperationException($"Reading a number needs a Number token, not {TokenType}.");
        }
    }

    private FormatException NumberFormat(string type) => new($"The number on line {LineNumber} does not fit {type}.");
}
