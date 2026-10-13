using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using ToonNet.Core.Models;

namespace ToonNet.Core.Encoding;

/// <summary>
///     Provides functionality to encode a <see cref="ToonDocument"/> into a TOON format string.
/// </summary>
/// <param name="options">
///     Optional encoding options to customize the behavior of the encoder. If not provided, 
///     the default options (<see cref="ToonOptions.Default"/>) will be used.
/// </param>
/// <remarks>
///     The output follows the canonical encoding of TOON spec v3.3.2: arrays of uniform primitive-only objects use the
///     tabular form, primitive arrays are written inline, other arrays as expanded lists, empty arrays as <c>[]</c>,
///     numbers in canonical decimal form, and strings and keys are quoted only when the spec requires it.
///     The document has LF line endings, no trailing spaces and no trailing newline.
///     An instance is not thread-safe; use one encoder per thread.
/// </remarks>
/// <example>
///     <code>
///     var document = new ToonDocument(new ToonObject
///     {
///         { "key", new ToonString("value") }
///     });
///     var encoder = new ToonEncoder();
///     var encodedString = encoder.Encode(document);
///     Console.WriteLine(encodedString);
///     </code>
/// </example>
public sealed class ToonEncoder(ToonOptions? options = null)
{
    private readonly ToonOptions _options = options ?? ToonOptions.Default;

    /// <summary>
    ///     Identifies where an array is written, which decides how an empty array is represented (spec §9.1, §9.2).
    /// </summary>
    private enum ArrayPosition
    {
        Root,
        Field,
        ListItem
    }

    /// <summary>
    ///     Encodes a TOON document into its string representation.
    /// </summary>
    /// <param name="document">
    ///     The document to encode. This parameter must not be null.
    /// </param>
    /// <returns>
    ///     The encoded TOON format string.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when a document is null.
    /// </exception>
    /// <exception cref="ToonEncodingException">
    ///     Thrown when encoding exceeds the maximum depth specified in the options.
    /// </exception>
    public string Encode(ToonDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var buffer = new ToonTextBuffer();
        Write(document, buffer);
        return buffer.ToString();
    }

    /// <summary>
    ///     Encodes a document into <paramref name="buffer"/>, so stream output does not need an intermediate string.
    /// </summary>
    internal void Write(ToonDocument document, ToonTextBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(document);

        _ = _options.ValidatedMaxDepth;

        new Writer(_options, buffer).WriteRoot(document.Root);
    }

    #region Structure

    /// <summary>
    ///     Holds the state of a single <see cref="Encode"/> call, so one encoder instance can be shared across threads.
    /// </summary>
    private sealed class Writer(ToonOptions options, ToonTextBuffer sb)
    {
        private readonly ToonOptions _options = options;
        private readonly ToonTextBuffer _sb = sb;
        private bool _hasLine;
        private int _depth;

        /// <summary>
        ///     Encodes the root value: an object as top-level fields, an array with a key-less header, or a single primitive (spec §5).
        /// </summary>
        public void WriteRoot(ToonValue root)
        {
            switch (root)
            {
                case ToonObject obj:
                    // An empty root object yields an empty document (§8)
                    EncodeFields(obj, 0);
                    break;
                case ToonArray array:
                    StartLine(0);
                    _depth = -1; // the root array is at depth 0, like the root object
                    EncodeArray(null, array, 1, ArrayPosition.Root);
                    break;
                default:
                    StartLine(0);
                    AppendPrimitive(root);
                    break;
            }
        }

        /// <summary>
        ///     Encodes the fields of an object, one line per field at the given depth.
        /// </summary>
        private void EncodeFields(ToonObject obj, int depth)
        {
            foreach (var (key, value) in obj.Properties)
            {
                StartLine(depth);
                EncodeField(key, value, depth + 1);
            }
        }

        /// <summary>
        ///     Writes a field on the current line (after indentation or a list marker) and its nested content at <paramref name="childDepth"/>.
        /// </summary>
        private void EncodeField(string key, ToonValue value, int childDepth)
        {
            switch (value)
            {
                case ToonObject obj:
                    AppendKey(_sb, key);
                    _sb.Append(':');
                    EnterNesting();
                    EncodeFields(obj, childDepth);
                    _depth--;
                    break;
                case ToonArray array:
                    EncodeArray(key, array, childDepth, ArrayPosition.Field);
                    break;
                default:
                    AppendKey(_sb, key);
                    _sb.Append(": ");
                    AppendPrimitive(value);
                    break;
            }
        }

        /// <summary>
        ///     Writes an array header (prefixed by the key, if any) and its items (spec §9).
        /// </summary>
        private void EncodeArray(string? key, ToonArray array, int childDepth, ArrayPosition position)
        {
            var sb = _sb;

            if (array.Count == 0)
            {
                EnterNesting();
                _depth--;

                switch (position)
                {
                    case ArrayPosition.Field:
                        AppendKey(sb, key!);
                        sb.Append(": []");
                        break;
                    case ArrayPosition.Root:
                        sb.Append("[]");
                        break;
                    default:
                        // Inner arrays of list items keep the header form (§9.2)
                        AppendBracket(key, 0);
                        sb.Append(':');
                        break;
                }

                return;
            }

            EnterNesting();

            AppendBracket(key, array.Count);

            if (IsPrimitiveArray(array))
            {
                sb.Append(": ");

                for (var i = 0; i < array.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(_options.Delimiter);
                    }

                    AppendPrimitive(array[i]);
                }
            }
            else if (position != ArrayPosition.ListItem && TryGetTabularFields(array) is { } fields)
            {
                EncodeTabularArray(array, fields, childDepth);
            }
            else
            {
                sb.Append(':');

                foreach (var item in array.Items)
                {
                    StartLine(childDepth);
                    EncodeListItem(item, childDepth);
                }
            }

            _depth--;
        }

        /// <summary>
        ///     Writes the field list of a tabular array followed by one row per object (spec §9.3).
        /// </summary>
        private void EncodeTabularArray(ToonArray array, string[] fields, int rowDepth)
        {
            var sb = _sb;
            sb.Append('{');

            for (var i = 0; i < fields.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(_options.Delimiter);
                }

                AppendKey(sb, fields[i]);
            }

            sb.Append("}:");

            // Rows are objects one level below the array
            EnterNesting();

            foreach (var item in array.Items)
            {
                var row = (ToonObject)item;
                StartLine(rowDepth);

                for (var i = 0; i < fields.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(_options.Delimiter);
                    }

                    // Rows usually hold the fields in header order, so the entry at the same position is checked first
                    ref readonly var entry = ref row.Properties.EntryAt(i);
                    AppendPrimitive(string.Equals(entry.Key, fields[i]) ? entry.Value : row.Properties[fields[i]]);
                }
            }

            _depth--;
        }

        /// <summary>
        ///     Writes a list item whose line has been started at <paramref name="depth"/> (spec §9.4, §10).
        /// </summary>
        private void EncodeListItem(ToonValue item, int depth)
        {
            var sb = _sb;

            switch (item)
            {
                case ToonObject { Properties.Count: 0 }:
                    // Empty object list item is a bare hyphen (§10)
                    EnterNesting();
                    _depth--;
                    sb.Append('-');
                    break;
                case ToonObject obj:
                    sb.Append("- ");
                    EnterNesting();

                    var isFirst = true;

                    foreach (var (key, value) in obj.Properties)
                    {
                        if (!isFirst)
                        {
                            StartLine(depth + 1);
                        }

                        // The first field sits on the hyphen line; its nested content goes two levels deeper (§10)
                        EncodeField(key, value, depth + 2);
                        isFirst = false;
                    }

                    _depth--;
                    break;
                case ToonArray array:
                    sb.Append("- ");
                    EncodeArray(null, array, depth + 1, ArrayPosition.ListItem);
                    break;
                default:
                    sb.Append("- ");
                    AppendPrimitive(item);
                    break;
            }
        }

        /// <summary>
        ///     Returns the tabular field names when the array qualifies for tabular form (spec §9.3), otherwise null.
        /// </summary>
        /// <remarks>
        ///     Every item must be a non-empty object with the same key set and only primitive values.
        ///     The field order is the first object's key order.
        /// </remarks>
        private static string[]? TryGetTabularFields(ToonArray array)
        {
            if (array[0] is not ToonObject { Properties.Count: > 0 } first)
            {
                return null;
            }

            var fieldCount = first.Properties.Count;

            foreach (var item in array.Items)
            {
                if (item is not ToonObject obj || obj.Properties.Count != fieldCount)
                {
                    return null;
                }

                foreach (var (key, value) in obj.Properties)
                {
                    if (!first.Properties.ContainsKey(key) || value is ToonObject or ToonArray)
                    {
                        return null;
                    }
                }
            }

            var fields = new string[fieldCount];

            for (var i = 0; i < fields.Length; i++)
            {
                fields[i] = first.Properties.EntryAt(i).Key;
            }

            return fields;
        }

        private static bool IsPrimitiveArray(ToonArray array)
        {
            foreach (var item in array.Items)
            {
                if (item is ToonObject or ToonArray)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Appends <c>key[N]</c> including the delimiter symbol for tab and pipe (spec §6).
        /// </summary>
        private void AppendBracket(string? key, int count)
        {
            var sb = _sb;

            if (key != null)
            {
                AppendKey(sb, key);
            }

            sb.Append('[');
            count.TryFormat(sb.GetSpan(11), out var length, default, CultureInfo.InvariantCulture);
            sb.Advance(length);

            if (_options.Delimiter != ',')
            {
                sb.Append(_options.Delimiter);
            }

            sb.Append(']');
        }

        /// <summary>
        ///     Starts a new line at the given depth. Lines are separated by LF and the document has no trailing newline (spec §12).
        /// </summary>
        private void StartLine(int depth)
        {
            var sb = _sb;

            if (_hasLine)
            {
                sb.Append('\n');
            }

            _hasLine = true;
            sb.Append(' ', depth * _options.IndentSize);
        }

        /// <summary>
        ///     Enters the level of a nested object or array, enforcing <see cref="ToonOptions.MaxDepth"/> (the root value
        ///     is at depth 0, each object or array inside another one level deeper) and the available stack space.
        /// </summary>
        /// <exception cref="ToonEncodingException">Thrown when the document is nested too deeply.</exception>
        private void EnterNesting()
        {
            if (++_depth > _options.ValidatedMaxDepth)
            {
                throw new ToonEncodingException($"Maximum depth of {_options.MaxDepth} exceeded");
            }

            if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
            {
                throw new ToonEncodingException("Document is nested too deeply to encode with the available stack space");
            }
        }

        /// <summary>
        ///     Appends a primitive value. NaN and infinities are written as null (spec §3).
        /// </summary>
        /// <exception cref="ToonEncodingException">Thrown for unknown <see cref="ToonValue"/> subclasses.</exception>
        private void AppendPrimitive(ToonValue? value)
        {
            var sb = _sb;

            switch (value)
            {
                case null or ToonNull:
                    sb.Append("null");
                    break;
                case ToonBoolean b:
                    sb.Append(b.Value ? "true" : "false");
                    break;
                case ToonNumber n:
                    if (ToonNumberFormatter.TryFormat(n, sb.GetSpan(ToonNumberFormatter.MaxLength), out var written))
                    {
                        sb.Advance(written);
                    }
                    else
                    {
                        sb.Append("null");
                    }

                    break;
                case ToonString s:
                    AppendString(sb, s.Value, _options.Delimiter);
                    break;
                default:
                    throw new ToonEncodingException($"Unsupported TOON value type: {value.GetType().Name}");
            }
        }
    }

    #endregion

    #region Primitives, keys and quoting

    /// <summary>
    ///     Appends a string value, quoted and escaped when spec §7.2 requires it.
    /// </summary>
    private static void AppendString(ToonTextBuffer sb, string value, char delimiter)
    {
        if (NeedsQuoting(value, delimiter))
        {
            AppendQuoted(sb, value);
        }
        else
        {
            sb.Append(value);
        }
    }

    /// <summary>
    ///     Appends a key or tabular field name, quoting it unless it matches <c>^[A-Za-z_][A-Za-z0-9_.]*$</c> (spec §7.3).
    /// </summary>
    private static void AppendKey(ToonTextBuffer sb, string key)
    {
        if (IsSafeUnquotedKey(key))
        {
            sb.Append(key);
        }
        else
        {
            AppendQuoted(sb, key);
        }
    }

    private static bool IsSafeUnquotedKey(string key)
    {
        if (key.Length == 0 || !(char.IsAsciiLetter(key[0]) || key[0] == '_'))
        {
            return false;
        }

        foreach (var ch in key.AsSpan(1))
        {
            if (!(char.IsAsciiLetterOrDigit(ch) || ch is '_' or '.'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Checks the quoting rules of spec §7.2 for a string value.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="delimiter">The delimiter in effect for the value.</param>
    /// <returns><c>true</c> if the string must be quoted.</returns>
    internal static bool NeedsQuoting(string value, char delimiter)
    {
        if (value.Length == 0 || char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1]))
        {
            return true;
        }

        if (value is "true" or "false" or "null" || value[0] == '-' || IsNumericLike(value))
        {
            return true;
        }

        foreach (var ch in value)
        {
            if (ch is ':' or '"' or '\\' or '[' or ']' or '{' or '}' || ch < '\u0020' || ch == delimiter)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Matches <c>/^-?\d+(?:\.\d+)?(?:e[+-]?\d+)?$/i</c> (spec §7.2).
    /// </summary>
    private static bool IsNumericLike(string value)
    {
        var i = 0;

        if (value[i] == '-')
        {
            i++;
        }

        if (!SkipDigits(value, ref i))
        {
            return false;
        }

        if (i < value.Length && value[i] == '.')
        {
            i++;

            if (!SkipDigits(value, ref i))
            {
                return false;
            }
        }

        if (i < value.Length && value[i] is 'e' or 'E')
        {
            i++;

            if (i < value.Length && value[i] is '+' or '-')
            {
                i++;
            }

            if (!SkipDigits(value, ref i))
            {
                return false;
            }
        }

        return i == value.Length;

        static bool SkipDigits(string text, ref int index)
        {
            var start = index;

            while (index < text.Length && char.IsAsciiDigit(text[index]))
            {
                index++;
            }

            return index > start;
        }
    }

    /// <summary>
    ///     Appends a quoted string, escaped per spec §7.1: backslash, quote, LF, CR and tab use short escapes,
    ///     other control characters U+0000–U+001F use lowercase <c>\uXXXX</c>. Runs of characters that need no escape
    ///     are appended in one piece.
    /// </summary>
    private static void AppendQuoted(ToonTextBuffer sb, string value)
    {
        sb.Append('"');
        var text = value.AsSpan();
        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            if (ch is not ('\\' or '"') && ch >= '\u0020')
            {
                continue;
            }

            sb.Append(text[start..i]);
            start = i + 1;

            switch (ch)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    sb.Append("\\u00").Append(HexDigits[ch >> 4]).Append(HexDigits[ch & 0xF]);
                    break;
            }
        }

        sb.Append(text[start..]).Append('"');
    }

    private const string HexDigits = "0123456789abcdef";

    #endregion

    /// <summary>
    ///     Asynchronously encodes a TOON document into its string representation.
    /// </summary>
    /// <param name="document">The document to encode.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous encoding operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a document is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public async Task<string> EncodeAsync(ToonDocument document, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Encode(document);
        }, cancellationToken);
    }

    /// <summary>
    ///     Asynchronously encodes a TOON document and writes it to a file.
    /// </summary>
    /// <param name="document">The document to encode.</param>
    /// <param name="filePath">The file path to write to.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous encoding and write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a document or filePath is null.</exception>
    /// <exception cref="IOException">Thrown when file I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public async Task EncodeToFileAsync(ToonDocument document, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        
        var encodedString = await EncodeAsync(document, cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(filePath, encodedString, ToonTextEncoding.Utf8NoBom, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Asynchronously encodes a TOON document and writes it to a stream.
    /// </summary>
    /// <param name="document">The document to encode.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous encoding and write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a document or stream is null.</exception>
    /// <exception cref="IOException">Thrown when stream I/O fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <remarks>
    /// This method uses ArrayPool&lt;byte&gt; for efficient memory usage.
    /// Suitable for high-throughput scenarios with minimal GC pressure.
    /// </remarks>
    public async Task EncodeToStreamAsync(ToonDocument document, Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        
        var encodedString = await EncodeAsync(document, cancellationToken).ConfigureAwait(false);
        
        // Use ArrayPool to avoid allocating byte arrays
        var encoding = System.Text.Encoding.UTF8;
        var maxByteCount = encoding.GetMaxByteCount(encodedString.Length);
        var buffer = ArrayPool<byte>.Shared.Rent(maxByteCount);
        
        try
        {
            var bytesWritten = encoding.GetBytes(encodedString, 0, encodedString.Length, buffer, 0);
            await stream.WriteAsync(buffer.AsMemory(0, bytesWritten), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}