using System.Text;
using ToonNet.Core.Models;

namespace ToonNet.Core.Encoding;

/// <summary>
/// Encodes ToonDocument into TOON format string.
/// </summary>
public sealed class ToonEncoder(ToonOptions? options = null)
{
    private readonly ToonOptions _options = options ?? ToonOptions.Default;
    private readonly StringBuilder _sb = new();
    private int _depth;
    
    // Cache for indent strings to avoid allocations in hot paths
    private static readonly string[] IndentCache = 
        Enumerable.Range(0, 32).Select(i => new string(' ', i * 2)).ToArray();

    /// <summary>
    /// Encodes a TOON document into its string representation.
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
    /// Encodes a ToonValue into the string builder.
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

    private void EncodePrimitiveArrayInline(ToonArray array)
    {
        var values = array.Items.Select(FormatValue);
        _sb.Append(string.Join(",", values));
    }

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
            }
            else
            {
                EncodeValue(item, indentLevel);
                _sb.AppendLine();
            }
        }
    }

    private static bool IsPrimitiveArray(ToonArray array)
    {
        return array.Items.All(item => 
            item is ToonNull or ToonBoolean or ToonNumber or ToonString);
    }

    private string FormatValue(ToonValue? value)
    {
        return value switch
        {
            null => "null",
            ToonNull => "null",
            ToonBoolean b => b.Value ? "true" : "false",
            ToonNumber n => FormatNumber(n.Value),
            ToonString s => QuoteIfNeeded(s.Value),
            _ => value.ToString() ?? "null"
        };
    }

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
        var str = needsScientific 
            ? value.ToString("E17", System.Globalization.CultureInfo.InvariantCulture)
            : value.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);

        // If G17 already produced scientific notation but we don't need it, convert to decimal
        if (!needsScientific && (str.Contains('e') || str.Contains('E')))
        {
            str = value.ToString("F17", System.Globalization.CultureInfo.InvariantCulture).TrimEnd('0');
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

    private static bool CheckNeedsScientific(double value)
    {
        // Determine if scientific notation is needed based on exponent
        // TOON spec: scientific notation MUST be used when exponent >= 21
        
        if (value == 0)
            return false;

        var absValue = Math.Abs(value);
        
        // For very large numbers (exponent >= 21)
        if (absValue >= 1e21)
            return true;
        
        // For very small numbers (exponent <= -21)
        if (absValue > 0 && absValue < 1e-20)
            return true;
        
        return false;
    }

    private string QuoteIfNeeded(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }
        
        return NeedsQuoting(value) ? $"\"{EscapeString(value)}\"" : value;
    }

    private string QuoteKeyIfNeeded(string key)
    {
        // Keys need quoting if they contain any special characters
        if (string.IsNullOrEmpty(key))
        {
            return "\"\"";
        }
        
        return NeedsQuoting(key) ? $"\"{EscapeString(key)}\"" : key;
    }

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
        return double.TryParse(value, out _) ||
               value.Any(ch => ch is ':' or ',' or '[' or ']' or '{' or '}' or '\n' or '\r' or '"' or '\\');
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", @"\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

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
