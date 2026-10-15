using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using ToonNet.Core.Encoding;
using ToonNet.Core.Parsing;
using ToonNet.Core.Paths;

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
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The items, in order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Stream"/> or <paramref name="itemTypeInfo"/> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the options contain converters and the generated code is from an older generator.</exception>
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

    /// <summary>
    ///     Reads the items of the array at a path of a document from a UTF-8 stream as they arrive, such as the rows of a
    ///     response envelope (<c>$.data.orders</c>), without holding the whole document in memory.
    /// </summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <param name="utf8Stream">The stream; it is read up to the end of the array and left open.</param>
    /// <param name="path">
    ///     A path of member names (<c>$.data.orders</c>), or <c>$</c> for a root array. Other singular paths
    ///     (<c>$.pages[0].rows</c>) are supported by reading the whole document first.
    /// </param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The items, in order; none when the document has no value at the path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Stream"/> or <paramref name="path"/> is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="ArgumentException">Thrown when the path is not singular.</exception>
    /// <exception cref="ToonParseException">Thrown while enumerating when the array is not valid TOON (with the line in the whole document).</exception>
    /// <exception cref="ToonSerializationException">Thrown while enumerating when the value is not an array or an item cannot be converted.</exception>
    /// <remarks>
    ///     The lines before the array are only followed by their indentation and keys to find it (strict mode reports
    ///     the indentation and member lines it does not allow), and the stream is read no further than the end of the
    ///     array, so the rest of the document is not checked. The array's items are read
    ///     and checked in batches as with <see cref="DeserializeAsyncEnumerable{T}(Stream, ToonSerializerOptions, CancellationToken)"/>.
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public static IAsyncEnumerable<T?> DeserializeAsyncEnumerableAt<T>(Stream utf8Stream, string path, ToonSerializerOptions? options = null,
                                                                      CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(utf8Stream);
        var compiled = SingularPath(path, options);
        options ??= ToonSerializerOptions.Default;

        // Non-strict mode lets a repeated key replace an earlier one and indentation vary, which only the parser follows
        return MemberNames(compiled) is { } names && (names.Length == 0 || options.ToonOptions.StrictMode)
            ? ReadRootItems(utf8Stream, options, text => (List<T?>?)DeserializeText(text, typeof(List<T?>), options), DefaultBatchCharacters,
                            cancellationToken, names.Length == 0 ? null : names)
            : ReadWholeAt(utf8Stream, compiled, options, cancellationToken,
                          static (text, path, options) => (List<T?>?)DeserializeAtText(text, path, typeof(List<T?>), options));
    }

    /// <summary>
    ///     Reads the items of the array at a path of a document from a UTF-8 stream as they arrive, with the type info of
    ///     the items, without reflection; see <see cref="DeserializeAsyncEnumerableAt{T}(Stream, string, ToonSerializerOptions, CancellationToken)"/>.
    /// </summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <param name="utf8Stream">The stream; it is read up to the end of the array and left open.</param>
    /// <param name="path">A path of member names (<c>$.data.orders</c>), or <c>$</c> for a root array.</param>
    /// <param name="itemTypeInfo">The type info of the items, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The items, in order; none when the document has no value at the path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="utf8Stream"/>, <paramref name="path"/> or <paramref name="itemTypeInfo"/> is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    /// <exception cref="ArgumentException">Thrown when the path is not singular.</exception>
    /// <exception cref="NotSupportedException">Thrown when the options contain converters and the generated code is from an older generator.</exception>
    /// <exception cref="ToonParseException">Thrown while enumerating when the array is not valid TOON.</exception>
    /// <exception cref="ToonSerializationException">Thrown while enumerating when the value is not an array or an item cannot be converted.</exception>
    public static IAsyncEnumerable<T?> DeserializeAsyncEnumerableAt<T>(Stream utf8Stream, string path, ToonTypeInfo<T> itemTypeInfo,
                                                                      ToonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(utf8Stream);
        var compiled = SingularPath(path, options);
        options = CheckTypeInfo(itemTypeInfo, options);
        var listInfo = ToonTypeInfo.CreateList(itemTypeInfo);

        return MemberNames(compiled) is { } names && (names.Length == 0 || options.ToonOptions.StrictMode)
            ? ReadRootItems(utf8Stream, options, text => DeserializeText(text, listInfo, options), DefaultBatchCharacters, cancellationToken,
                            names.Length == 0 ? null : names)
            : ReadWholeAt(utf8Stream, compiled, options, cancellationToken, (text, path, options) => DeserializeAtText(text, path, listInfo, options));
    }

    /// <summary>The reflection-based reader of the array at a path with a given batch size, so tests can make batches of a few items.</summary>
    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    internal static IAsyncEnumerable<T?> DeserializeAsyncEnumerableAt<T>(Stream utf8Stream, string path, ToonSerializerOptions options, int batchCharacters,
                                                                        CancellationToken cancellationToken = default) =>
        ReadRootItems(utf8Stream, options, text => (List<T?>?)DeserializeText(text, typeof(List<T?>), options), batchCharacters, cancellationToken,
                      MemberNames(SingularPath(path, options)) is { Length: > 0 } names ? names : null);

    /// <summary>The member names of a path made only of them (none for <c>$</c>), or null for other paths.</summary>
    private static string[]? MemberNames(ToonPath path)
    {
        var segments = path.Query.Segments;
        var names = new string[segments.Length];

        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i].Selectors[0] is not { Kind: SelectorKind.Name, Name: { } name })
            {
                return null;
            }

            names[i] = name;
        }

        return names;
    }

    /// <summary>Reads the whole document and the items of the array at the path (a path that is not only member names).</summary>
    private static async IAsyncEnumerable<T?> ReadWholeAt<T>(Stream stream, ToonPath path, ToonSerializerOptions options,
                                                            [EnumeratorCancellation] CancellationToken cancellationToken,
                                                            Func<ToonText, ToonPath, ToonSerializerOptions, IEnumerable<T?>?> read)
    {
        IEnumerable<T?>? items;

        using (var text = await ToonTextEncoding.ReadAsync(stream, cancellationToken).ConfigureAwait(false))
        {
            items = read(text.Text, path, options);
        }

        foreach (var item in items ?? [])
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    private static async IAsyncEnumerable<T?> ReadRootItems<T>(Stream stream, ToonSerializerOptions options, Func<ToonText, IEnumerable<T?>?> readBatch,
                                                              int batchCharacters, [EnumeratorCancellation] CancellationToken cancellationToken,
                                                              string[]? target = null)
    {
        using var batches = new RootArrayBatches(stream, options.ToonOptions, batchCharacters, target);

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
            catch (ToonSerializationException ex)
            {
                // The batch is a root array of some of the items: name the item of the whole array
                ex.RebaseItemPath(batches.ItemsBefore, batches.ArrayPath);
                throw;
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
    /// <remarks>
    ///     With a <c>target</c> (the member names of a path such as <c>$.data.orders</c>), the lines before the array are
    ///     only followed by their indentation and keys to find it, and the array's lines lose the indentation of its
    ///     header, so batches are root arrays as well; reading stops where the array ends.
    /// </remarks>
    private sealed class RootArrayBatches(Stream stream, ToonOptions options, int batchCharacters, string[]? target = null) : IDisposable
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

        /// <summary>With a target: the number of its member names found so far (the depth of the object being searched).</summary>
        private int _level;

        /// <summary>With a target: the indentation of its header, which the array's lines lose.</summary>
        private int _dedent;

        /// <summary>With a target: where the header's <c>[</c> is in its line, for error columns.</summary>
        private int _headerColumn;

        /// <summary>With a target: the array ended (or is not there), so the stream is not read further.</summary>
        private bool _ended;

        /// <summary>Whether a line read makes the document an error (in strict mode), so no more lines are read.</summary>
        private bool _errorFound;

        /// <summary>With a target: the document has no value at the path.</summary>
        private bool _missing;

        /// <summary>With a target: a member line was read, so the document is an object.</summary>
        private bool _memberSeen;

        public ToonText Text => _text.Text;

        /// <summary>The items of the array in the batches before the current one.</summary>
        public long ItemsBefore => _totalItems - _lastBatchItems;

        /// <summary>The path of the array, as errors give it.</summary>
        public string ArrayPath => target == null ? "$" : PathText();

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

                if (_endOfStream || _ended)
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

            // An item line lost the indentation of the header: the count in the message is that of the whole document
            const string indentation = "Indentation of ";

            if (error.Line > 1 && _dedent > 0 && message.StartsWith(indentation, StringComparison.Ordinal) &&
                message.IndexOf(' ', indentation.Length) is var end and > 0 &&
                int.TryParse(message.AsSpan(indentation.Length, end - indentation.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var spaces))
            {
                message = indentation + (spaces + _dedent).ToString(CultureInfo.InvariantCulture) + message[end..];
            }

            // In the batch, the header has no key or indentation and the items lost the header's indentation. On the
            // header line, a count mismatch is reported at the start of the line and other errors at their character;
            // column 1 of an item line is a blank line
            var column = error.Line == 1
                ? message.StartsWith(mismatch, StringComparison.Ordinal) ? _dedent + 1 : error.Column + _headerColumn
                : error.Column == 1 ? 1 : error.Column + _dedent;

            if (error.Line == 1 && _headerRest.Length > 0 && message.StartsWith(mismatch, StringComparison.Ordinal) &&
                int.TryParse(message.AsSpan(message.LastIndexOf(' ') + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var got))
            {
                message = $"{mismatch}{_declaredLength}, got {_totalItems - _lastBatchItems + got}";
            }

            return new ToonParseException(message, line, column);
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
            while (!_ended)
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

            return false;
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
                        if (target == null)
                        {
                            ReadHeader(line);
                        }
                        else
                        {
                            Locate(spaces, content.TrimEnd(' '));
                        }
                    }

                    return true;
                case Form.Whole or Form.Tail when _errorFound:
                    // The document is an error at a line already read: the rest is not needed (or kept) to report it
                    _ended = true;
                    return true;
                case Form.Whole or Form.Tail:
                    _lineNumber++;
                    Append(line);
                    return true;
            }

            // Tab-indented lines are left to the parser (an error in strict mode)
            if (!isBlank && !isComment && content[0] != '\t')
            {
                if (spaces <= _dedent && target != null)
                {
                    // A line outside the target array: it ended
                    _ended = true;
                    return true;
                }

                if (spaces == 0)
                {
                    // Content after the array: the parser reports it with the last batch, at this line in strict mode
                    _form = Form.Tail;
                    _errorFound = options.StrictMode;
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
            Append(_dedent == 0 ? line : spaces >= _dedent ? line[_dedent..] : content);
            return true;
        }

        /// <summary>
        ///     Follows the lines before the target array: a member at the depth being searched that has the next name of
        ///     the path is entered, or, for the last name, its header is read as the root header.
        /// </summary>
        private void Locate(int spaces, ReadOnlySpan<char> content)
        {
            if (content[0] == '\t' || spaces % options.IndentSize != 0)
            {
                // Strict mode: the error the parser reports (the target is only read in strict mode, or for tests)
                if (options.StrictMode)
                {
                    throw content[0] == '\t'
                        ? new ToonParseException("Tabs are not allowed in indentation", _lineNumber, spaces + 1)
                        : new ToonParseException($"Indentation of {spaces} spaces is not a multiple of the indent size {options.IndentSize}", _lineNumber, 1);
                }

                return; // not a line of a valid document at the depth searched (the target, if any, is still found)
            }

            var depth = spaces / options.IndentSize;

            if (depth > _level)
            {
                return; // inside the value of another member
            }

            if (depth < _level)
            {
                _missing = _ended = true; // the object being searched ended
                return;
            }

            if (!TryReadKey(content, out var key, out var rest))
            {
                // The first line of a document that is not an object (a root array or primitive) has no members;
                // elsewhere a line of an object that is not a member is an error
                if (options.StrictMode && (_level > 0 || _memberSeen))
                {
                    throw new ToonParseException(
                        content[0] == '-' && (content.Length == 1 || content[1] == ' ') ? "List items require an array header such as 'key[N]:'" : "Missing colon after key",
                        _lineNumber, spaces + 1);
                }

                _missing = _ended = true;
                return;
            }

            _memberSeen = true;

            if (key != target![_level])
            {
                return;
            }

            if (_level < target.Length - 1)
            {
                if (rest is ":")
                {
                    _level++;
                }
                else
                {
                    _missing = _ended = true; // the member is not an object
                }

                return;
            }

            if (rest.Length == 0 || rest[0] != '[')
            {
                if (rest is ": null")
                {
                    _missing = _ended = true; // null has no items, like a document that is only null
                    return;
                }

                if (rest is ": []")
                {
                    // An empty array
                    _ended = true;
                    _form = Form.Whole;
                    _bodyFirstLine = _lineNumber;
                    _headerLineNumber = _lineNumber;
                    Append("[]");
                    return;
                }

                throw new ToonSerializationException($"The value at {PathText()} is not an array") { Path = PathText() };
            }

            // The header, without its key, is read as a root header; the array's lines are one level below it
            _dedent = spaces;
            _headerColumn = spaces + content.Length - rest.Length;
            ReadHeader(rest);

            if (_form == Form.Whole)
            {
                // `[N:]{fields}:` opens a keyed table, an object
                var digits = rest[1..].IndexOfAnyExceptInRange('0', '9');

                if (digits > 0 && rest[1 + digits] == ':')
                {
                    throw new ToonSerializationException($"The value at {PathText()} is a keyed table (an object), not an array") { Path = PathText() };
                }

                _ended = true; // an inline array is one line
            }
        }

        private string PathText() => target!.Aggregate(new StringBuilder("$"), ToonErrorText.AppendMember).ToString();

        /// <summary>The key of a member line (<c>name: ...</c>, <c>"quoted name"[2]: ...</c>) and what follows it.</summary>
        private static bool TryReadKey(ReadOnlySpan<char> content, out string key, out ReadOnlySpan<char> rest)
        {
            key = "";
            rest = default;

            if (content[0] == '"')
            {
                var builder = new System.Text.StringBuilder();

                for (var i = 1; i < content.Length; i++)
                {
                    var c = content[i];

                    if (c == '"')
                    {
                        key = builder.ToString();
                        rest = content[(i + 1)..];
                        return rest.Length > 0 && rest[0] is ':' or '[';
                    }

                    if (c == '\\')
                    {
                        if (++i >= content.Length)
                        {
                            return false;
                        }

                        builder.Append(content[i] switch
                        {
                            'n' => '\n',
                            'r' => '\r',
                            't' => '\t',
                            '"' => '"',
                            '\\' => '\\',
                            _ => '\0'
                        });

                        if (builder[^1] == '\0')
                        {
                            return false;
                        }

                        continue;
                    }

                    builder.Append(c);
                }

                return false;
            }

            if (content[0] == '-' && (content.Length == 1 || content[1] == ' '))
            {
                return false; // a list item
            }

            var end = content.IndexOfAny(':', '[');

            if (end <= 0)
            {
                return false;
            }

            key = content[..end].ToString();
            rest = content[end..];
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
                // `[]` and anything that is not a root array header (the parser or the check below reports it, after
                // reading all of it: an error further on comes first, as for the whole document)
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

            if (_missing || (target != null && _form == Form.None))
            {
                return false; // no value at the path
            }

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
                throw new ToonParseException($"Array length mismatch: expected {_declaredLength}, got {_totalItems}", _headerLineNumber, _dedent + 1);
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
