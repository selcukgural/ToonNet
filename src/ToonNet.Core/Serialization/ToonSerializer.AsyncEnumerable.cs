using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using ToonNet.Core.Encoding;
using ToonNet.Core.Parsing;

namespace ToonNet.Core.Serialization;

// Reading the items of a root array from a stream, a batch at a time, without holding the document in memory.
public static partial class ToonSerializer
{
    /// <summary>
    ///     Reads the items of a document whose root is an array (inline, tabular or expanded) from a UTF-8 stream as they
    ///     arrive, without holding the whole document in memory.
    /// </summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <param name="utf8Stream">The stream; it is read to the end and left open.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The items, in order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Stream"/> is null.</exception>
    /// <exception cref="ToonParseException">
    ///     Thrown while enumerating when the text is not valid UTF-8 or not valid TOON (with the line in the whole document).
    /// </exception>
    /// <exception cref="ToonSerializationException">
    ///     Thrown while enumerating when the root is not an array or an item cannot be converted.
    /// </exception>
    /// <remarks>
    ///     Rows of a tabular array and items of an expanded list are read in batches of a few dozen kilobytes of text, so
    ///     memory stays flat however long the array is; an inline array is one line and is read at once. Items are checked
    ///     like the rest of the document; in strict mode the declared length is checked when the array ends, after the
    ///     items before it were returned. For a stream of separate documents, use
    ///     <see cref="DeserializeDocumentsAsync{T}(StreamReader, ToonSerializerOptions, CancellationToken)"/>.
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static IAsyncEnumerable<T?> DeserializeAsyncEnumerable<T>(Stream utf8Stream, ToonSerializerOptions? options = null,
                                                                    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(utf8Stream);
        options ??= ToonSerializerOptions.Default;

        return ReadRootItems(utf8Stream, options, text => (List<T?>?)DeserializeText(text, typeof(List<T?>), options), DefaultBatchCharacters,
                             cancellationToken);
    }

    /// <summary>A batch is read once its items reach this many characters (at an item boundary).</summary>
    private const int DefaultBatchCharacters = 32 * 1024;

    /// <summary>The reflection-based reader with a given batch size, so tests can make batches of a few items.</summary>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    internal static IAsyncEnumerable<T?> DeserializeAsyncEnumerable<T>(Stream utf8Stream, ToonSerializerOptions options, int batchCharacters,
                                                                      CancellationToken cancellationToken = default) =>
        ReadRootItems(utf8Stream, options, text => (List<T?>?)DeserializeText(text, typeof(List<T?>), options), batchCharacters, cancellationToken);

    /// <summary>
    ///     Reads the items of a document whose root is an array from a UTF-8 stream as they arrive, with the type info of
    ///     the items, without reflection and without holding the whole document in memory.
    /// </summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <param name="utf8Stream">The stream; it is read to the end and left open.</param>
    /// <param name="itemTypeInfo">The type info of the items, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options; converters are not supported.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The items, in order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Stream"/> or <paramref name="itemTypeInfo"/> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the options contain converters.</exception>
    /// <exception cref="ToonParseException">Thrown while enumerating when the text is not valid UTF-8 or not valid TOON.</exception>
    /// <exception cref="ToonSerializationException">Thrown while enumerating when the root is not an array or an item cannot be converted.</exception>
    public static IAsyncEnumerable<T?> DeserializeAsyncEnumerable<T>(Stream utf8Stream, ToonTypeInfo<T> itemTypeInfo, ToonSerializerOptions? options = null,
                                                                    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(utf8Stream);
        options = CheckTypeInfo(itemTypeInfo, options);
        var listInfo = ToonTypeInfo.CreateList(itemTypeInfo);

        return ReadRootItems(utf8Stream, options, text => DeserializeText(text, listInfo, options), DefaultBatchCharacters, cancellationToken);
    }

    private static async IAsyncEnumerable<T?> ReadRootItems<T>(Stream stream, ToonSerializerOptions options, Func<ToonText, IEnumerable<T?>?> readBatch,
                                                              int batchCharacters, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var batches = new RootArrayBatches(stream, options.ToonOptions, batchCharacters);

        while (await batches.NextAsync(cancellationToken).ConfigureAwait(false))
        {
            IEnumerable<T?>? items;

            try
            {
                items = readBatch(batches.Text);
            }
            catch (ToonParseException ex)
            {
                throw batches.Relocate(ex);
            }

            // A document that is only `null` has no items
            foreach (var item in items ?? [])
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }
        }
    }

    /// <summary>
    ///     Splits the root array of a streamed document into batches that are documents themselves: the root header with
    ///     the batch's item count, followed by complete items (rows, or list items with their nested lines) as they are in
    ///     the stream. The parser reads each batch; its line numbers map back to the stream's.
    /// </summary>
    private sealed class RootArrayBatches(Stream stream, ToonOptions options, int batchCharacters) : IDisposable
    {
        private const int ByteBufferSize = 16 * 1024;

        private enum Form : byte
        {
            /// <summary>Before the header.</summary>
            None,

            /// <summary>A tabular array or an expanded list: items are batched.</summary>
            Block,

            /// <summary>An inline array or <c>[]</c>: the whole document is one batch.</summary>
            Whole,

            /// <summary>The array ended; the rest of the document goes into the last batch (where it is an error).</summary>
            Tail
        }

        private readonly Decoder _decoder = ToonTextEncoding.Utf8Strict.GetDecoder();
        private byte[]? _bytes = ArrayPool<byte>.Shared.Rent(ByteBufferSize);
        private char[]? _chars = ArrayPool<char>.Shared.Rent(ByteBufferSize + 2);
        private int _charStart;
        private int _charEnd;
        private bool _endOfStream;

        // The items read but not yet batched, and the batch handed to the parser
        private ToonPooledText _body = new(Math.Min(batchCharacters, DefaultBatchCharacters) + 1024);
        private ToonPooledText _text = new(Math.Min(batchCharacters, DefaultBatchCharacters) + 1024);

        private Form _form;

        /// <summary>How far the buffered characters after <see cref="_charStart"/> were searched for a line feed.</summary>
        private int _scanned;

        /// <summary>Strict mode: an item followed a blank line, so the batch is read with that item (the parser reports it).</summary>
        private bool _batchAfterLine;

        /// <summary>The items of the last batch, to report counts of the whole array.</summary>
        private int _lastBatchItems;
        private int _lineNumber;
        private int _headerLineNumber;
        private string _headerRest = "";
        private int _declaredLength;
        private int _itemIndent = -1;
        private int _bodyItems;
        private int _bodyFirstLine;
        private bool _blankSinceItem;
        private long _totalItems;
        private bool _finished;
        private bool _lengthChecked;
        private bool _firstLine = true;

        /// <summary>The line of the stream the current batch's second line comes from (its first is the header).</summary>
        private int _textFirstLine;

        public ToonText Text => _text.Text;

        /// <summary>Reads lines until a batch is ready; false after the last batch.</summary>
        public async ValueTask<bool> NextAsync(CancellationToken cancellationToken)
        {
            if (_finished)
            {
                // After the last batch was read
                CheckDeclaredLength();
                return false;
            }

            while (true)
            {
                if (TakeBatchFromBuffer())
                {
                    return true;
                }

                if (_endOfStream)
                {
                    return Finish();
                }

                cancellationToken.ThrowIfCancellationRequested();
                var read = await stream.ReadAsync(_bytes.AsMemory(), cancellationToken).ConfigureAwait(false);
                Decode(read);
            }
        }

        /// <summary>A parse error of the batch with the line of the stream.</summary>
        public ToonParseException Relocate(ToonParseException error)
        {
            if (error.Line <= 0)
            {
                return error;
            }

            var line = error.Line == 1 ? _headerLineNumber : _textFirstLine + error.Line - 2;
            var message = error.Reason;

            // The batch declares its own count; the error names the counts of the whole array
            const string mismatch = "Array length mismatch: expected ";

            if (error.Line == 1 && _headerRest.Length > 0 && message.StartsWith(mismatch, StringComparison.Ordinal) &&
                int.TryParse(message.AsSpan(message.LastIndexOf(' ') + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var got))
            {
                message = $"{mismatch}{_declaredLength}, got {_totalItems - _lastBatchItems + got}";
            }

            return new ToonParseException(message, line, error.Column);
        }

        private void Decode(int read)
        {
            try
            {
                // Keep the characters not yet consumed (part of a line) at the start of the buffer
                if (_charStart > 0)
                {
                    var remaining = _charEnd - _charStart;
                    _chars.AsSpan(_charStart, remaining).CopyTo(_chars);

                    // Clear what the move left behind, so no document text stays past the characters in use
                    _chars.AsSpan(remaining, _charEnd - remaining).Clear();
                    _charEnd = remaining;
                    _charStart = 0;
                }

                if (_chars!.Length - _charEnd < read + 2)
                {
                    var larger = ArrayPool<char>.Shared.Rent(_charEnd + read + 2);
                    _chars.AsSpan(0, _charEnd).CopyTo(larger);
                    ReturnChars(_chars, _chars.Length);
                    _chars = larger;
                }

                _charEnd += _decoder.GetChars(_bytes.AsSpan(0, read), _chars.AsSpan(_charEnd), flush: read == 0);
                _bytes.AsSpan(0, read).Clear();
            }
            catch (DecoderFallbackException)
            {
                throw ToonTextEncoding.InvalidUtf8();
            }

            if (read == 0)
            {
                // The last line may have no line feed; give it one, so every line ends the same way
                if (_charEnd > _charStart)
                {
                    _chars[_charEnd++] = '\n';
                }

                _endOfStream = true;
            }
        }

        /// <summary>Feeds the buffered complete lines; returns true when a batch was made (the line that ended it stays buffered).</summary>
        private bool TakeBatchFromBuffer()
        {
            while (true)
            {
                var buffered = _chars.AsSpan(_charStart, _charEnd - _charStart);

                // A long line arrives in several reads: search only what was not searched yet
                var newline = buffered[_scanned..].IndexOf('\n');

                if (newline < 0)
                {
                    _scanned = buffered.Length;
                    return false;
                }

                newline += _scanned;

                if (!Feed(buffered[..newline]))
                {
                    return true; // the line is fed again after the batch
                }

                _charStart += newline + 1;
                _scanned = 0;

                if (_batchAfterLine)
                {
                    _batchAfterLine = false;
                    MakeBatch();
                    return true;
                }
            }
        }

        /// <summary>
        ///     Takes one line of the document (without its line feed). Returns false, without taking it, when the line
        ///     starts an item and the batch so far must be read first.
        /// </summary>
        private bool Feed(ReadOnlySpan<char> line)
        {
            if (line.Length > 0 && line[^1] == '\r')
            {
                line = line[..^1];
            }

            if (_firstLine)
            {
                _firstLine = false;

                if (line.Length > 0 && line[0] == '﻿')
                {
                    line = line[1..];
                }
            }

            var spaces = line.IndexOfAnyExcept(' ');
            var content = spaces < 0 ? ReadOnlySpan<char>.Empty : line[spaces..];
            var isBlank = content.TrimEnd(' ').IsEmpty;
            var isComment = !isBlank && content[0] == '#';

            switch (_form)
            {
                case Form.None:
                    _lineNumber++;

                    if (!isBlank && !isComment)
                    {
                        ReadHeader(line);
                    }

                    return true;
                case Form.Whole or Form.Tail:
                    _lineNumber++;
                    Append(line);
                    return true;
            }

            // Tab-indented lines are left to the parser (an error in strict mode)
            if (!isBlank && !isComment && content[0] != '\t')
            {
                if (spaces == 0)
                {
                    // Content after the array: the parser reports it with the last batch
                    _form = Form.Tail;
                }
                else
                {
                    if (_itemIndent < 0)
                    {
                        _itemIndent = spaces;
                    }

                    if (spaces == _itemIndent)
                    {
                        if (_blankSinceItem && options.StrictMode && _bodyItems + _totalItems > 0)
                        {
                            // A blank line between items is an error in strict mode: read the batch with this item now, so
                            // the parser reports it without waiting for the rest of the stream
                            _batchAfterLine = true;
                        }
                        else if (_bodyItems > 0 && _body.Length >= batchCharacters)
                        {
                            // A new item (row or list item) and the batch is full: read it first
                            MakeBatch();
                            return false;
                        }

                        _bodyItems++;
                        _blankSinceItem = false;
                    }
                }
            }
            else if (isBlank)
            {
                _blankSinceItem = true;
            }

            _lineNumber++;
            Append(line);
            return true;
        }

        private void Append(ReadOnlySpan<char> line)
        {
            if (_body.Length == 0)
            {
                _bodyFirstLine = _lineNumber;
            }

            _body.EnsureCapacity(line.Length + 1);
            line.CopyTo(_body.Chars.AsSpan(_body.Length));
            _body.Length += line.Length;
            _body.Chars[_body.Length++] = '\n';
        }

        /// <summary>Reads the root header: <c>[N]:</c>, <c>[N]{fields}:</c> (items follow), or an inline array.</summary>
        private void ReadHeader(ReadOnlySpan<char> line)
        {
            _headerLineNumber = _lineNumber;
            var text = line.TrimEnd(' ');

            if (text.SequenceEqual("[]") || text.Length == 0 || text[0] != '[')
            {
                // `[]` and anything that is not a root array header (the parser or the check below reports it)
                _form = Form.Whole;
                _bodyFirstLine = _lineNumber;
                Append(line);
                return;
            }

            var digits = text[1..].IndexOfAnyExceptInRange('0', '9');
            var close = digits < 0 ? -1 : 1 + digits;

            if (digits > 1 && text[1] == '0')
            {
                digits = -1; // a length with a leading zero is malformed (§6): the parser reports it
            }

            // `[N` then an optional delimiter, then `]`; a keyed header (`[N:]`) is an object, not an array
            var bracketEnd = close >= 0 && close < text.Length && text[close] is '\t' or '|' ? close + 1 : close;

            if (digits <= 0 || bracketEnd >= text.Length || text[bracketEnd] != ']' ||
                !int.TryParse(text[1..close], NumberStyles.None, CultureInfo.InvariantCulture, out _declaredLength))
            {
                _form = Form.Whole;
                _bodyFirstLine = _lineNumber;
                Append(line);
                return;
            }

            var afterBracket = text[(bracketEnd + 1)..];

            // A block header ends with the colon (after the field list, if any); an inline array has values after it
            if (text[^1] == ':' && (afterBracket is [':'] || (afterBracket.Length > 0 && afterBracket[0] == '{')))
            {
                _form = Form.Block;
                _headerRest = text[close..].ToString();
                return;
            }

            _form = Form.Whole;
            _bodyFirstLine = _lineNumber;
            Append(line);
        }

        /// <summary>Makes the parser's batch: the header with the batch's count, then the batched lines.</summary>
        private void MakeBatch()
        {
            // Clear the previous batch, which may be longer than this one
            _text.Chars.AsSpan(0, _text.Length).Clear();
            _text.Length = 0;

            if (_form is Form.Block or Form.Tail && _headerRest.Length > 0)
            {
                var header = $"[{_bodyItems.ToString(CultureInfo.InvariantCulture)}{_headerRest}\n";
                _text.EnsureCapacity(header.Length);
                header.CopyTo(_text.Chars.AsSpan());
                _text.Length = header.Length;
                _textFirstLine = _bodyFirstLine;
            }
            else
            {
                // The whole document as it is: its first line is the header line
                _textFirstLine = _bodyFirstLine + 1;
            }

            _text.EnsureCapacity(_body.Length);
            _body.Chars.AsSpan(0, _body.Length).CopyTo(_text.Chars.AsSpan(_text.Length));
            _text.Length += _body.Length;

            _totalItems += _bodyItems;
            _lastBatchItems = _bodyItems;
            _body.Chars.AsSpan(0, _body.Length).Clear();
            _body.Length = 0;
            _bodyItems = 0;
            _blankSinceItem = false;
        }

        /// <summary>At the end of the stream: the last batch, if any; then the checks of the whole array.</summary>
        private bool Finish()
        {
            _finished = true;

            switch (_form)
            {
                case Form.None:
                    // An empty document (an empty object): read it, so the error is the same as for any document
                    _form = Form.Whole;
                    MakeBatch();
                    return true;
                case Form.Whole:
                    MakeBatch();
                    return true;
            }

            if (_body.Length > 0 || _totalItems == 0)
            {
                MakeBatch();
                return true;
            }

            CheckDeclaredLength();
            return false;
        }

        /// <summary>Strict mode: the array must have the declared number of items (checked once, after all were read).</summary>
        private void CheckDeclaredLength()
        {
            if (_lengthChecked)
            {
                return;
            }

            _lengthChecked = true;

            if (_form is Form.Block or Form.Tail && options.StrictMode && _totalItems != _declaredLength)
            {
                throw new ToonParseException($"Array length mismatch: expected {_declaredLength}, got {_totalItems}", _headerLineNumber, 1);
            }
        }

        public void Dispose()
        {
            if (_bytes != null)
            {
                ArrayPool<byte>.Shared.Return(_bytes, clearArray: true);
                _bytes = null;
            }

            if (_chars != null)
            {
                ReturnChars(_chars, _chars.Length);
                _chars = null;
            }

            _body.Dispose();
            _text.Dispose();
        }

        private static void ReturnChars(char[] chars, int length)
        {
            chars.AsSpan(0, length).Clear();
            ArrayPool<char>.Shared.Return(chars);
        }
    }
}
