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

    public string Encode(ToonDocument document)
    {
        _sb.Clear();
        _depth = 0;

        EncodeValue(document.Root, 0);

        return _sb.ToString();
    }

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
            
            _sb.Append(key);

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
        // Handle integers without a decimal point
        if (Math.Abs(value - Math.Floor(value)) < 0.1 && !double.IsInfinity(value))
        {
            return ((long)value).ToString();
        }

        return value.ToString("G");
    }

    private string QuoteIfNeeded(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }
        
        return NeedsQuoting(value) ? $"\"{EscapeString(value)}\"" : value;
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
        if (indentLevel > 0)
        {
            _sb.Append(new string(' ', indentLevel));
        }
    }
}
