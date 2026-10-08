using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.ObjectPool;
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
    // StringBuilder pool for reducing allocations
    private static readonly ObjectPool<StringBuilder> StringBuilderPool = new DefaultObjectPoolProvider().CreateStringBuilderPool();

    private readonly ToonOptions _options = options ?? ToonOptions.Default;
    private StringBuilder? _sb;
    private bool _hasLine;
    private int _depth;

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
    ///     Thrown when a document is null or document.Root is null.
    /// </exception>
    /// <exception cref="ToonEncodingException">
    ///     Thrown when encoding exceeds the maximum depth specified in the options.
    /// </exception>
    public string Encode(ToonDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Root == null)
        {
            throw new ArgumentNullException(nameof(document), "Document root cannot be null");
        }

        _sb = StringBuilderPool.Get();

        try
        {
            _hasLine = false;
            _depth = 0;
            EncodeRoot(document.Root);
            return _sb.ToString();
        }
        finally
        {
            StringBuilderPool.Return(_sb);
            _sb = null;
        }
    }

    #region Structure

    /// <summary>
    ///     Encodes the root value: an object as top-level fields, an array with a key-less header, or a single primitive (spec §5).
    /// </summary>
    private void EncodeRoot(ToonValue root)
    {
        switch (root)
        {
            case ToonObject obj:
                // An empty root object yields an empty document (§8)
                EncodeFields(obj, 0);
                break;
            case ToonArray array:
                StartLine(0);
                EncodeArray(string.Empty, array, 1, ArrayPosition.Root);
                break;
            default:
                StartLine(0);
                _sb!.Append(FormatPrimitive(root));
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
        var encodedKey = EncodeKey(key);

        switch (value)
        {
            case ToonObject obj:
                _sb!.Append(encodedKey).Append(':');
                EnterNesting();
                EncodeFields(obj, childDepth);
                _depth--;
                break;
            case ToonArray array:
                EncodeArray(encodedKey, array, childDepth, ArrayPosition.Field);
                break;
            default:
                _sb!.Append(encodedKey).Append(": ").Append(FormatPrimitive(value));
                break;
        }
    }

    /// <summary>
    ///     Writes an array header (prefixed by an already encoded key, possibly empty) and its items (spec §9).
    /// </summary>
    private void EncodeArray(string encodedKey, ToonArray array, int childDepth, ArrayPosition position)
    {
        var sb = _sb!;

        if (array.Count == 0)
        {
            switch (position)
            {
                case ArrayPosition.Field:
                    sb.Append(encodedKey).Append(": []");
                    break;
                case ArrayPosition.Root:
                    sb.Append("[]");
                    break;
                default:
                    // Inner arrays of list items keep the header form (§9.2)
                    AppendBracket(encodedKey, 0);
                    sb.Append(':');
                    break;
            }

            return;
        }

        EnterNesting();

        AppendBracket(encodedKey, array.Count);

        if (IsPrimitiveArray(array))
        {
            sb.Append(": ");

            for (var i = 0; i < array.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(_options.Delimiter);
                }

                sb.Append(FormatPrimitive(array[i]));
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
        var sb = _sb!;
        sb.Append('{');

        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(_options.Delimiter);
            }

            sb.Append(EncodeKey(fields[i]));
        }

        sb.Append("}:");

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

                sb.Append(FormatPrimitive(row.Properties[fields[i]]));
            }
        }
    }

    /// <summary>
    ///     Writes a list item whose line has been started at <paramref name="depth"/> (spec §9.4, §10).
    /// </summary>
    private void EncodeListItem(ToonValue item, int depth)
    {
        var sb = _sb!;

        switch (item)
        {
            case ToonObject { Properties.Count: 0 }:
                // Empty object list item is a bare hyphen (§10)
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
                EncodeArray(string.Empty, array, depth + 1, ArrayPosition.ListItem);
                break;
            default:
                sb.Append("- ").Append(FormatPrimitive(item));
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

        return [.. first.Properties.Keys];
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
    private void AppendBracket(string encodedKey, int count)
    {
        var sb = _sb!;
        sb.Append(encodedKey).Append('[').Append(count.ToString(CultureInfo.InvariantCulture));

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
        var sb = _sb!;

        if (_hasLine)
        {
            sb.Append('\n');
        }

        _hasLine = true;
        sb.Append(' ', depth * _options.IndentSize);
    }

    /// <summary>
    ///     Tracks structural nesting, enforcing <see cref="ToonOptions.MaxDepth"/> and the available stack space.
    /// </summary>
    /// <exception cref="ToonEncodingException">Thrown when the document is nested too deeply.</exception>
    private void EnterNesting()
    {
        if (++_depth > _options.MaxDepth)
        {
            throw new ToonEncodingException($"Maximum depth of {_options.MaxDepth} exceeded");
        }

        if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            throw new ToonEncodingException("Document is nested too deeply to encode with the available stack space");
        }
    }

    #endregion

    #region Primitives, keys and quoting

    /// <summary>
    ///     Formats a primitive value. NaN and infinities are written as null (spec §3).
    /// </summary>
    /// <exception cref="ToonEncodingException">Thrown for unknown <see cref="ToonValue"/> subclasses.</exception>
    private string FormatPrimitive(ToonValue? value)
    {
        return value switch
        {
            null or ToonNull => "null",
            ToonBoolean b    => b.Value ? "true" : "false",
            ToonNumber n     => ToonNumberFormatter.Format(n) ?? "null",
            ToonString s     => QuoteIfNeeded(s.Value, _options.Delimiter),
            _                => throw new ToonEncodingException($"Unsupported TOON value type: {value.GetType().Name}")
        };
    }

    /// <summary>
    ///     Quotes a string value when spec §7.2 requires it.
    /// </summary>
    private static string QuoteIfNeeded(string value, char delimiter)
    {
        return NeedsQuoting(value, delimiter) ? "\"" + Escape(value) + "\"" : value;
    }

    /// <summary>
    ///     Encodes a key or tabular field name, quoting it unless it matches <c>^[A-Za-z_][A-Za-z0-9_.]*$</c> (spec §7.3).
    /// </summary>
    private static string EncodeKey(string key)
    {
        return IsSafeUnquotedKey(key) ? key : "\"" + Escape(key) + "\"";
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
    ///     Escapes a string per spec §7.1: backslash, quote, LF, CR and tab use short escapes,
    ///     other control characters U+0000–U+001F use lowercase <c>\uXXXX</c>.
    /// </summary>
    private static string Escape(string value)
    {
        var needsEscape = false;

        foreach (var ch in value)
        {
            if (ch is '\\' or '"' || ch < '\u0020')
            {
                needsEscape = true;
                break;
            }
        }

        if (!needsEscape)
        {
            return value;
        }

        var sb = new StringBuilder(value.Length + 8);

        foreach (var ch in value)
        {
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
                case < '\u0020':
                    sb.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }

        return sb.ToString();
    }

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
        await File.WriteAllTextAsync(filePath, encodedString, System.Text.Encoding.UTF8, cancellationToken).ConfigureAwait(false);
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