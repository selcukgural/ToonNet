using System.Globalization;
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
///     This class is designed to be used for serializing <see cref="ToonDocument"/> instances into 
///     their string representation in the TOON format. It supports various TOON value types such as 
///     objects, arrays, strings, numbers, booleans, and nulls. The encoder ensures proper formatting 
///     and handles indentation, quoting, and escaping as per the TOON specification.
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
    // Cache for indent strings to avoid allocations in hot paths
    // Expanded to support MaxDepth=100 (51 levels = 0-100 spaces)
    private static readonly string[] IndentCache = Enumerable.Range(0, 51).Select(i => new string(' ', i * 2)).ToArray();

    // StringBuilder pool for reducing allocations
    private static readonly ObjectPool<StringBuilder> StringBuilderPool = new DefaultObjectPoolProvider().CreateStringBuilderPool();

    private readonly ToonOptions _options = options ?? ToonOptions.Default;
    private StringBuilder? _sb;
    private int _depth;

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
    /// <remarks>
    ///     This method gets a StringBuilder from the pool, encodes the document,
    ///     and returns the StringBuilder to the pool after use.
    /// </remarks>
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
            _depth = 0;
            EncodeValue(document.Root, 0);
            return _sb.ToString();
        }
        finally
        {
            StringBuilderPool.Return(_sb);
            _sb = null;
        }
    }

    /// <summary>
    ///     Encodes a <see cref="ToonValue"/> into the internal string builder.
    /// </summary>
    /// <param name="value">
    ///     The value to encode. This parameter must not be null.
    /// </param>
    /// <param name="indentLevel">
    ///     The current indentation level, used to format the output string.
    /// </param>
    /// <exception cref="ToonEncodingException">
    ///     Thrown when encoding exceeds the maximum depth specified in the options.
    /// </exception>
    /// <remarks>
    ///     This method uses a switch statement to determine the type of the provided value
    ///     and delegates the encoding to the appropriate helper method. Supported value types
    ///     include null, boolean, number, string, object, and array. The depth counter is
    ///     incremented and decremented to ensure proper tracking of nested structures.
    /// </remarks>
    private void EncodeValue(ToonValue value, int indentLevel)
    {
        if (_depth > _options.MaxDepth)
        {
            throw new ToonEncodingException($"Maximum depth of {_options.MaxDepth} exceeded");
        }

        _depth++;

        switch (value)
        {
            case ToonNull:
                _sb!.Append("null");
                break;
            case ToonBoolean b:
                _sb!.Append(b.Value ? "true" : "false");
                break;
            case ToonNumber n:
                _sb!.Append(FormatNumber(n.Value));
                break;
            case ToonString s:
                _sb!.Append(QuoteIfNeeded(s.Value));
                break;
            case ToonObject o:
                EncodeObject(o, indentLevel);
                break;
            case ToonArray a:
                EncodeArray(a, indentLevel);
                break;
        }

        _depth--;
    }

    /// <summary>
    ///     Encodes an object to TOON format.
    /// </summary>
    /// <param name="obj">
    ///     The object to encode. This parameter must not be null.
    /// </param>
    /// <param name="indentLevel">
    ///     The current indentation level, used to format the output string.
    /// </param>
    /// <remarks>
    ///     This method iterates through the properties of the provided <see cref="ToonObject"/> and encodes
    ///     each key-value pair. Keys are quoted if they contain special characters. Values are encoded
    ///     recursively based on their type. The method ensures proper indentation and formatting for nested
    ///     structures.
    /// </remarks>
    private void EncodeObject(ToonObject obj, int indentLevel)
    {
        var isFirst = true;

        foreach (var (key, value) in obj.Properties)
        {
            if (!isFirst)
            {
                _sb!.AppendLine();
            }

            WriteIndent(indentLevel);

            // Quote the key if it contains special characters
            _sb!.Append(QuoteKeyIfNeeded(key));

            if (value is ToonArray array)
            {
                EncodeArrayHeader(array);
            }

            _sb!.Append(':');

            switch (value)
            {
                case ToonObject:
                    _sb!.AppendLine();
                    EncodeValue(value, indentLevel + _options.IndentSize);
                    break;
                case ToonArray arr:
                    EncodeValue(arr, indentLevel + _options.IndentSize);
                    break;
                default:
                    _sb!.Append(' ');
                    EncodeValue(value, indentLevel);
                    break;
            }

            isFirst = false;
        }
    }

    /// <summary>
    ///     Encodes an object inline (first property on the same line, rest indented).
    /// </summary>
    /// <param name="obj">
    ///     The object to encode. This parameter must not be null.
    /// </param>
    /// <param name="indentLevel">
    ///     The base indentation level for subsequent properties.
    /// </param>
    /// <remarks>
    ///     This method is used when encoding objects as array items. The first property
    ///     appears on the same line as the array marker ('-'), while subsequent properties
    ///     are indented at the appropriate level.
    /// </remarks>
    private void EncodeObjectInline(ToonObject obj, int indentLevel)
    {
        var isFirst = true;

        foreach (var (key, value) in obj.Properties)
        {
            if (!isFirst)
            {
                _sb!.AppendLine();
                WriteIndent(indentLevel + _options.IndentSize);
            }

            // Quote the key if it contains special characters
            _sb!.Append(QuoteKeyIfNeeded(key));

            if (value is ToonArray array)
            {
                EncodeArrayHeader(array);
            }

            _sb!.Append(':');

            switch (value)
            {
                case ToonObject:
                    _sb!.AppendLine();
                    EncodeValue(value, indentLevel + _options.IndentSize * 2);
                    break;
                case ToonArray arr:
                    EncodeValue(arr, indentLevel + _options.IndentSize * 2);
                    break;
                default:
                    _sb!.Append(' ');
                    EncodeValue(value, indentLevel + _options.IndentSize);
                    break;
            }

            isFirst = false;
        }
    }

    /// <summary>
    ///     Encodes the array header with length and optional field names.
    /// </summary>
    /// <param name="array">
    ///     The array to encode the header for. This parameter must not be null.
    /// </param>
    /// <remarks>
    ///     This method writes the array length in square brackets. If the array is tabular and contains
    /// field names, those field names are written in curly braces after the length.
    /// </remarks>
    private void EncodeArrayHeader(ToonArray array)
    {
        _sb!.Append($"[{array.Count}]");

        if (!array.IsTabular || array.FieldNames == null)
        {
            return;
        }

        _sb!.Append('{');
        
        // Optimized: avoid string.Join() allocation
        for (int i = 0; i < array.FieldNames.Length; i++)
        {
            if (i > 0)
            {
                _sb.Append(',');
            }
            _sb.Append(array.FieldNames[i]);
        }
        
        _sb!.Append('}');
    }

    /// <summary>
    ///     Encodes an array to TOON format.
    /// </summary>
    /// <param name="array">
    ///     The array to encode. This parameter must not be null.
    /// </param>
    /// <param name="indentLevel">
    ///     The current indentation level, used to format the output string.
    /// </param>
    /// <remarks>
    ///     This method determines the type of the array and encodes it accordingly:
    ///     - Tabular arrays are encoded with field names and rows.
    ///     - Primitive arrays are encoded inline as a comma-separated list.
    ///     - Mixed arrays are encoded as a list with each item on a new line.
    ///     Empty arrays are skipped.
    /// </remarks>
    private void EncodeArray(ToonArray array, int indentLevel)
    {
        if (array.Count == 0)
        {
            return;
        }

        // Check if it's a tabular array (array of objects with same fields)
        if (array is { IsTabular: true, FieldNames: not null })
        {
            _sb!.AppendLine();
            EncodeTabularArray(array, indentLevel);
        }
        // Check if it's a primitive array that can be inline
        else if (IsPrimitiveArray(array))
        {
            _sb!.Append(' ');
            EncodePrimitiveArrayInline(array);
        }
        else
        {
            // Mixed array - use list notation
            _sb!.AppendLine();
            EncodeListArray(array, indentLevel);
        }
    }

    /// <summary>
    ///     Encodes a tabular array (array of objects with field names) into the TOON format.
    /// </summary>
    /// <param name="array">
    ///     The tabular array to encode. This parameter must not be null and should contain
    ///     objects with consistent field names.
    /// </param>
    /// <param name="indentLevel">
    ///     The current indentation level, used to format the output string.
    /// </param>
    /// <remarks>
    ///     This method iterates through the items in the array and encodes each row as a
    ///     comma-separated list of field values. If the array contains objects with field names,
    ///     the values are extracted and formatted accordingly. Non-object items are formatted
    ///     directly. Proper indentation is applied for each row.
    /// </remarks>
    private void EncodeTabularArray(ToonArray array, int indentLevel)
    {
        foreach (var item in array.Items)
        {
            WriteIndent(indentLevel);

            if (item is ToonObject rowObj && array.FieldNames != null)
            {
                // Optimized: direct append instead of List + Join
                for (int j = 0; j < array.FieldNames.Length; j++)
                {
                    if (j > 0)
                    {
                        _sb!.Append(',');
                    }
                    
                    var fieldValue = rowObj[array.FieldNames[j]];
                    _sb!.Append(FormatValue(fieldValue));
                }
            }
            else
            {
                _sb!.Append(FormatValue(item));
            }

            _sb!.AppendLine();
        }
    }

    /// <summary>
    ///     Encodes a primitive array as an inline comma-separated list.
    /// </summary>
    /// <param name="array">
    ///     The array to encode. This parameter must not be null and should contain only
    ///     primitive values (e.g., null, boolean, number, or string).
    /// </param>
    /// <remarks>
    ///     This method formats the array as a single line of comma-separated values.
    ///     It is optimized for arrays containing only primitive types.
    /// </remarks>
    private void EncodePrimitiveArrayInline(ToonArray array)
    {
        // Optimized: direct append instead of Select + Join
        for (int i = 0; i < array.Items.Count; i++)
        {
            if (i > 0)
            {
                _sb!.Append(',');
            }
            _sb!.Append(FormatValue(array.Items[i]));
        }
    }

    /// <summary>
    ///     Encodes an array as a list with each item prefixed by a '-' character.
    /// </summary>
    /// <param name="array">
    ///     The array to encode. This parameter must not be null.
    /// </param>
    /// <param name="indentLevel">
    ///     The current indentation level, used to format the output string.
    /// </param>
    /// <remarks>
    ///     This method encodes each item in the array as a separate line. If an item is
    ///     an object or another array, it is encoded recursively with increased indentation.
    ///     Primitive items are encoded inline. Proper indentation is applied for each item.
    /// </remarks>
    private void EncodeListArray(ToonArray array, int indentLevel)
    {
        foreach (var item in array.Items)
        {
            WriteIndent(indentLevel);
            _sb!.Append("- ");

            if (item is ToonObject obj)
            {
                EncodeObjectInline(obj, indentLevel);
                _sb!.AppendLine();
            }
            else if (item is ToonArray arr)
            {
                EncodeValue(arr, indentLevel + _options.IndentSize);
                _sb!.AppendLine();
            }
            else
            {
                EncodeValue(item, indentLevel);
                _sb!.AppendLine();
            }
        }
    }

    /// <summary>
    ///     Checks if an array contains only primitive values.
    /// </summary>
    /// <param name="array">
    ///     The array to check. This parameter must not be null.
    /// </param>
    /// <returns>
    ///     True if all items in the array are primitive values (null, boolean, number, or string);
    ///     otherwise, false.
    /// </returns>
    /// <remarks>
    ///     This method is used to determine if an array can be encoded as a single line
    ///     of comma-separated values. Non-primitive items (e.g., objects or arrays) will
    ///     cause the method to return false.
    /// </remarks>
    private static bool IsPrimitiveArray(ToonArray array)
    {
        return array.Items.All(item => item is ToonNull or ToonBoolean or ToonNumber or ToonString);
    }

    /// <summary>
    /// Formats a TOON value as a string.
    /// </summary>
    /// <param name="value">
    /// The value to format. This can be null or any type derived from <see cref="ToonValue"/>.
    /// </param>
    /// <returns>
    /// A string representation of the TOON value. Returns "null" for null values or <see cref="ToonNull"/>,
    /// "true"/"false" for <see cref="ToonBoolean"/>, a formatted number for <see cref="ToonNumber"/>,
    /// a quoted string for <see cref="ToonString"/>, or the result of <see cref="object.ToString"/> for other types.
    /// </returns>
    private static string FormatValue(ToonValue? value)
    {
        return value switch
        {
            null          => "null",
            ToonNull      => "null",
            ToonBoolean b => b.Value ? "true" : "false",
            ToonNumber n  => FormatNumber(n.Value),
            ToonString s  => QuoteIfNeeded(s.Value),
            _             => value.ToString() ?? "null"
        };
    }

    /// <summary>
    /// Formats a numeric value as a TOON-compatible string.
    /// </summary>
    /// <param name="value">
    /// The number to format. Must not be NaN or Infinity.
    /// </param>
    /// <returns>
    /// A string representation of the number, formatted according to the TOON specification.
    /// Scientific notation is used for very large or very small numbers.
    /// </returns>
    /// <exception cref="ToonEncodingException">
    /// Thrown when the value is NaN or Infinity, as these are not allowed in the TOON format.
    /// </exception>
    private static string FormatNumber(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ToonEncodingException("NaN and Infinity are not allowed in TOON");
        }

        // Normalize -0 to 0
        if (value == 0)
        {
            return "0";
        }

        // Check if we need to use scientific notation based on the actual exponent
        var needsScientific = CheckNeedsScientific(value);

        // Use the appropriate format based on whether we need scientific notation
        var str = needsScientific ? value.ToString("E17", CultureInfo.InvariantCulture) : value.ToString("G17", CultureInfo.InvariantCulture);

        // If G17 already produced scientific notation, but we don't need it, convert to decimal
        if (!needsScientific && (str.IndexOf('e') != -1 || str.IndexOf('E') != -1))
        {
            str = value.ToString("F17", CultureInfo.InvariantCulture).TrimEnd('0');

            if (str.EndsWith('.'))
            {
                str += '0';
            }

            return str;
        }

        // If we're using scientific notation, normalize it
        var eIndex = str.IndexOf('E');
        if (eIndex == -1)
        {
            eIndex = str.IndexOf('e');
        }
        
        if (eIndex != -1)
        {
            // Build normalized scientific notation without allocating intermediate strings
            Span<char> buffer = stackalloc char[str.Length + 8]; // Extra space for reformatted exponent
            
            // Copy mantissa
            var mantissa = str.AsSpan(0, eIndex);
            mantissa.CopyTo(buffer);
            buffer[eIndex] = 'e'; // Use lowercase 'e'
            
            // Parse and reformat exponent
            var exponentPart = str.AsSpan(eIndex + 1);
            if (int.TryParse(exponentPart, out var expValue))
            {
                expValue.TryFormat(buffer[(eIndex + 1)..], out var written);
                return new string(buffer[..(eIndex + 1 + written)]);
            }
            
            // Fallback if parsing fails
            str = str.Replace('E', 'e');
        }
        else if (str.IndexOf('.') != -1)
        {
            // Remove trailing zeros after decimal point
            str = str.TrimEnd('0');

            // If we end up with just a decimal point, add a zero
            if (str.EndsWith('.'))
            {
                str += '0';
            }
        }

        return str;
    }

    /// <summary>
    /// Checks if a number requires scientific notation based on the TOON specification.
    /// </summary>
    /// <param name="value">
    /// The number to check. Must not be zero.
    /// </param>
    /// <returns>
    /// True if the number requires scientific notation (e.g., exponent &gt;= 21 or &lt;= -21); otherwise, false.
    /// </returns>
    private static bool CheckNeedsScientific(double value)
    {
        if (value == 0)
        {
            return false;
        }

        var absValue = Math.Abs(value);

        // For very large numbers (exponent >= 21)
        if (absValue >= 1e21)
        {
            return true;
        }

        // For very small numbers (exponent <= -21)
        return absValue is > 0 and < 1e-20;
    }

    /// <summary>
    /// Quotes a string value if it needs quoting.
    /// </summary>
    /// <param name="value">
    /// The string vaLue to potentially quote. Can be null or empty.
    /// </param>
    /// <returns>
    /// The quoted string if quoting is necessary; otherwise, the original string.
    /// </returns>
    private static string QuoteIfNeeded(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        return NeedsQuoting(value) ? $"\"{EscapeString(value)}\"" : value;
    }

    /// <summary>
    /// Quotes a key if it needs quoting.
    /// </summary>
    /// <param name="key">
    /// The key to potentially quote. Can be null or empty.
    /// </param>
    /// <returns>
    /// The quoted key if quoting is necessary; otherwise, the original key.
    /// </returns>
    private static string QuoteKeyIfNeeded(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return "\"\"";
        }

        return NeedsQuoting(key) ? $"\"{EscapeString(key)}\"" : key;
    }

    /// <summary>
    ///     Checks if a string needs to be quoted based on the TOON format specification.
    /// </summary>
    /// <param name="value">
    ///     The string to check. This parameter must not be null.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the string needs quoting; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     A string requires quoting if it is empty, contains leading or trailing whitespace,
    ///     includes spaces, matches reserved keywords ("true", "false", "null"), resembles a number,
    ///     or contains special characters such as ':', ',', '[', ']', '{', '}', newline, carriage return,
    ///     double quotes, or backslashes.
    /// </remarks>
    private static bool NeedsQuoting(string value)
    {
        // Empty strings
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        // Leading or trailing whitespace
        if (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1]))
        {
            return true;
        }

        // Contains whitespace in the middle
        if (value.Contains(' '))
        {
            return true;
        }

        // Keywords
        if (value is "true" or "false" or "null")
        {
            return true;
        }

        // Looks like a number
        return double.TryParse(value, out _) || value.Any(ch => ch is ':' or ',' or '[' or ']' or '{' or '}' or '\n' or '\r' or '"' or '\\');
    }

    /// <summary>
    ///     Escapes special characters in a string to ensure compatibility with the TOON format.
    /// </summary>
    /// <param name="value">
    ///     The string to escape. This parameter must not be null.
    /// </param>
    /// <returns>
    ///     The escaped string, where special characters such as backslashes, double quotes, newlines,
    ///     carriage returns, and tabs are replaced with their escaped equivalents.
    /// </returns>
    /// <remarks>
    ///     This method replaces the following characters with their escaped representations:
    ///     - Backslash ('\\') becomes '\\\\'
    ///     - Double quote ('"') becomes '\\\"'
    ///     - Newline ('\n') becomes '\\n'
    ///     - Carriage return ('\r') becomes '\\r'
    ///     - Tab ('\t') becomes '\\t'
    ///     Performance: Single-pass implementation to minimize allocations (5 Replace calls → 1 StringBuilder).
    /// </remarks>
    private static string EscapeString(string value)
    {
        // Quick check: if no special chars, return original
        if (value.IndexOfAny(['\\', '"', '\n', '\r', '\t']) == -1)
        {
            return value;
        }

        var sb = new StringBuilder(value.Length + 16); // +16 for potential escape sequences
        
        foreach (char c in value)
        {
            switch (c)
            {
                case '\\':
                    sb.Append(@"\\");
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
                    sb.Append(c);
                    break;
            }
        }
        
        return sb.ToString();
    }

    /// <summary>
    ///     Writes indentation to the output string builder based on the specified indentation level.
    /// </summary>
    /// <param name="indentLevel">
    ///     The indentation level, in spaces. If the level is less than or equal to zero, no indentation is added.
    /// </param>
    /// <remarks>
    ///     This method uses a cached array of precomputed indentation strings for common levels to minimize
    ///     allocations. For uncommon levels, it generates the indentation dynamically. If the level is odd,
    ///     an additional space is appended to the cached string.
    /// </remarks>
    private void WriteIndent(int indentLevel)
    {
        if (indentLevel <= 0)
        {
            return;
        }

        if (indentLevel < IndentCache.Length * 2)
        {
            // For commonly-used indent levels, use cache when possible
            var cacheIndex = indentLevel / 2;

            if (cacheIndex > 0 && cacheIndex < IndentCache.Length)
            {
                _sb!.Append(IndentCache[cacheIndex]);

                if (indentLevel % 2 == 1)
                {
                    _sb!.Append(' ');
                }

                return;
            }
        }

        _sb!.Append(new string(' ', indentLevel));
    }

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
        
        var encodedString = await EncodeAsync(document, cancellationToken);
        await File.WriteAllTextAsync(filePath, encodedString, System.Text.Encoding.UTF8, cancellationToken);
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
    public async Task EncodeToStreamAsync(ToonDocument document, Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        
        var encodedString = await EncodeAsync(document, cancellationToken);
        var bytes = System.Text.Encoding.UTF8.GetBytes(encodedString);
        await stream.WriteAsync(bytes, cancellationToken);
    }
}