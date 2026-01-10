using System.Globalization;
using System.Text;
using ToonNet.Core.Models;

namespace ToonNet.Core.Encoding;

/// <summary>
///     Encodes ToonDocument into TOON format string.
/// </summary>
public sealed class ToonEncoder(ToonOptions? options = null)
{
    // Cache for indent strings to avoid allocations in hot paths
    private static readonly string[] IndentCache = Enumerable.Range(0, 32).Select(i => new string(' ', i * 2)).ToArray();

    private readonly ToonOptions _options = options ?? ToonOptions.Default;
    private readonly StringBuilder _sb = new();
    private int _depth;

    /// <summary>
    ///     Encodes a TOON document into its string representation.
    /// </summary>
    /// <param name="document">The document to encode.</param>
    /// <returns>The encoded TOON format string.</returns>
    /// <exception cref="ToonEncodingException">Thrown when encoding exceeds maximum depth.</exception>
    public string Encode(ToonDocument document)
    {
        _sb.Clear();
        _depth = 0;

        EncodeValue(document.Root, 0);

        return _sb.ToString();
    }

    /// <summary>
    ///     Encodes a ToonValue into the string builder.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="indentLevel">The current indentation level.</param>
    /// <exception cref="ToonEncodingException">Thrown when encoding exceeds maximum depth.</exception>
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
                _sb.Append("null");
                break;
            case ToonBoolean b:
                _sb.Append(b.Value ? "true" : "false");
                break;
            case ToonNumber n:
                _sb.Append(FormatNumber(n.Value));
                break;
            case ToonString s:
                _sb.Append(QuoteIfNeeded(s.Value));
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
    /// <param name="obj">The object to encode.</param>
    /// <param name="indentLevel">The current indentation level.</param>
    private void EncodeObject(ToonObject obj, int indentLevel)
    {
        var isFirst = true;

        foreach (var (key, value) in obj.Properties)
        {
            if (!isFirst)
            {
                _sb.AppendLine();
            }

            WriteIndent(indentLevel);

            // Quote key if it contains special characters
            _sb.Append(QuoteKeyIfNeeded(key));

            if (value is ToonArray array)
            {
                EncodeArrayHeader(array);
            }

            _sb.Append(':');

            switch (value)
            {
                case ToonObject:
                    _sb.AppendLine();
                    EncodeValue(value, indentLevel + _options.IndentSize);
                    break;
                case ToonArray arr:
                    EncodeValue(arr, indentLevel + _options.IndentSize);
                    break;
                default:
                    _sb.Append(' ');
                    EncodeValue(value, indentLevel);
                    break;
            }

            isFirst = false;
        }
    }

    /// <summary>
    ///     Encodes the array header with length and optional field names.
    /// </summary>
    /// <param name="array">The array to encode the header for.</param>
    private void EncodeArrayHeader(ToonArray array)
    {
        _sb.Append($"[{array.Count}]");

        if (!array.IsTabular || array.FieldNames == null)
        {
            return;
        }

        _sb.Append('{');
        _sb.Append(string.Join(",", array.FieldNames));
        _sb.Append('}');
    }

    /// <summary>
    ///     Encodes an array to TOON format.
    /// </summary>
    /// <param name="array">The array to encode.</param>
    /// <param name="indentLevel">The current indentation level.</param>
    private void EncodeArray(ToonArray array, int indentLevel)
    {
        if (array.Count == 0)
        {
            return;
        }

        // Check if it's a tabular array (array of objects with same fields)
        if (array is { IsTabular: true, FieldNames: not null })
        {
            _sb.AppendLine();
            EncodeTabularArray(array, indentLevel);
        }
        // Check if it's a primitive array that can be inline
        else if (IsPrimitiveArray(array))
        {
            _sb.Append(' ');
            EncodePrimitiveArrayInline(array);
        }
        else
        {
            // Mixed array - use list notation
            _sb.AppendLine();
            EncodeListArray(array, indentLevel);
        }
    }

    /// <summary>
    ///     Encodes a tabular array (array of objects with field names).
    /// </summary>
    /// <param name="array">The tabular array to encode.</param>
    /// <param name="indentLevel">The current indentation level.</param>
    private void EncodeTabularArray(ToonArray array, int indentLevel)
    {
        foreach (var item in array.Items)
        {
            WriteIndent(indentLevel);

            if (item is ToonObject rowObj && array.FieldNames != null)
            {
                var values = new List<string>();

                foreach (var fieldName in array.FieldNames)
                {
                    var fieldValue = rowObj[fieldName];
                    values.Add(FormatValue(fieldValue));
                }

                _sb.Append(string.Join(",", values));
            }
            else
            {
                _sb.Append(FormatValue(item));
            }

            _sb.AppendLine();
        }
    }

    /// <summary>
    ///     Encodes a primitive array as an inline comma-separated list.
    /// </summary>
    /// <param name="array">The array to encode.</param>
    private void EncodePrimitiveArrayInline(ToonArray array)
    {
        var values = array.Items.Select(FormatValue);
        _sb.Append(string.Join(",", values));
    }

    /// <summary>
    ///     Encodes an array as a list with '-' prefixes.
    /// </summary>
    /// <param name="array">The array to encode.</param>
    /// <param name="indentLevel">The current indentation level.</param>
    private void EncodeListArray(ToonArray array, int indentLevel)
    {
        foreach (var item in array.Items)
        {
            WriteIndent(indentLevel);
            _sb.Append("- ");

            if (item is ToonObject || item is ToonArray)
            {
                _sb.AppendLine();
                EncodeValue(item, indentLevel + _options.IndentSize);
                _sb.AppendLine();
            }
            else
            {
                EncodeValue(item, indentLevel);
                _sb.AppendLine();
            }
        }
    }

    /// <summary>
    ///     Checks if an array contains only primitive values.
    /// </summary>
    /// <param name="array">The array to check.</param>
    /// <returns>True if all items are primitives; otherwise, false.</returns>
    private static bool IsPrimitiveArray(ToonArray array)
    {
        return array.Items.All(item => item is ToonNull or ToonBoolean or ToonNumber or ToonString);
    }

    /// <summary>
    ///     Formats a TOON value as a string.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted string representation.</returns>
    private string FormatValue(ToonValue? value)
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
    ///     Formats a numeric value as a TOON-compatible string.
    /// </summary>
    /// <param name="value">The number to format.</param>
    /// <returns>The formatted string representation.</returns>
    /// <exception cref="ToonEncodingException">Thrown when the value is NaN or Infinity.</exception>
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

        // Use appropriate format based on whether we need scientific notation
        var str = needsScientific ? value.ToString("E17", CultureInfo.InvariantCulture) : value.ToString("G17", CultureInfo.InvariantCulture);

        // If G17 already produced scientific notation but we don't need it, convert to decimal
        if (!needsScientific && (str.Contains('e') || str.Contains('E')))
        {
            str = value.ToString("F17", CultureInfo.InvariantCulture).TrimEnd('0');

            if (str.EndsWith('.'))
            {
                str += '0';
            }

            return str;
        }

        // If we're using scientific notation, normalize it
        if (str.Contains('e') || str.Contains('E'))
        {
            // Use lowercase 'e'
            str = str.Replace('E', 'e');

            // Remove leading zeros in exponent and remove '+' sign
            var eIndex = str.IndexOf('e');
            var mantissa = str.Substring(0, eIndex);
            var exponentPart = str.Substring(eIndex + 1);

            // Parse and reformat exponent
            if (int.TryParse(exponentPart, out var expValue))
            {
                str = $"{mantissa}e{expValue}";
            }
        }
        else if (str.Contains('.'))
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
    ///     Checks if a number requires scientific notation based on TOON spec.
    /// </summary>
    /// <param name="value">The number to check.</param>
    /// <returns>True if scientific notation is required; otherwise, false.</returns>
    private static bool CheckNeedsScientific(double value)
    {
        // Determine if scientific notation is needed based on exponent
        // TOON spec: scientific notation MUST be used when exponent >= 21

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
        if (absValue > 0 && absValue < 1e-20)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Quotes a string value if it needs quoting.
    /// </summary>
    /// <param name="value">The string value to potentially quote.</param>
    /// <returns>The quoted string if needed; otherwise, the original string.</returns>
    private string QuoteIfNeeded(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        return NeedsQuoting(value) ? $"\"{EscapeString(value)}\"" : value;
    }

    /// <summary>
    ///     Quotes a key if it needs quoting.
    /// </summary>
    /// <param name="key">The key to potentially quote.</param>
    /// <returns>The quoted key if needed; otherwise, the original key.</returns>
    private string QuoteKeyIfNeeded(string key)
    {
        // Keys need quoting if they contain any special characters
        if (string.IsNullOrEmpty(key))
        {
            return "\"\"";
        }

        return NeedsQuoting(key) ? $"\"{EscapeString(key)}\"" : key;
    }

    /// <summary>
    ///     Checks if a string needs to be quoted.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if the string needs quoting; otherwise, false.</returns>
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
    ///     Escapes special characters in a string for TOON format.
    /// </summary>
    /// <param name="value">The string to escape.</param>
    /// <returns>The escaped string.</returns>
    private static string EscapeString(string value)
    {
        return value.Replace("\\", @"\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    /// <summary>
    ///     Writes indentation to the output.
    /// </summary>
    /// <param name="indentLevel">The indentation level (in spaces).</param>
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
                _sb.Append(IndentCache[cacheIndex]);

                if (indentLevel % 2 == 1)
                {
                    _sb.Append(' ');
                }

                return;
            }
        }

        _sb.Append(new string(' ', indentLevel));
    }
}