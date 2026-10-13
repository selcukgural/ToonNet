using System.Globalization;
using System.Text;

namespace ToonNet.Shared;

/// <summary>
///     The property naming algorithms, compiled into ToonNet.Core and ToonNet.SourceGenerators so that the serializer
///     and generated code produce the same keys. They follow System.Text.Json's <c>JsonNamingPolicy.CamelCase</c> and
///     <c>JsonNamingPolicy.SnakeCaseLower</c>, so acronyms stay together (<c>URLValue</c> → <c>urlValue</c>,
///     <c>HTTPServer</c> → <c>http_server</c>).
/// </summary>
internal static class ToonNaming
{
    /// <summary>
    ///     Lowercases the leading uppercase run, keeping the last letter of an acronym that starts the next word:
    ///     <c>FirstName</c> → <c>firstName</c>, <c>URL</c> → <c>url</c>, <c>IOStream</c> → <c>ioStream</c>.
    /// </summary>
    public static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
        {
            return name;
        }

        var chars = name.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            if (i == 1 && !char.IsUpper(chars[i]))
            {
                break;
            }

            var hasNext = i + 1 < chars.Length;

            // Stop at the last uppercase letter before a lowercase one: it starts the next word
            if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
            {
                if (chars[i + 1] == ' ')
                {
                    chars[i] = char.ToLowerInvariant(chars[i]);
                }

                break;
            }

            chars[i] = char.ToLowerInvariant(chars[i]);
        }

        return new string(chars);
    }

    /// <summary>
    ///     Lowercase words separated by underscores: <c>FirstName</c> → <c>first_name</c>, <c>HTTPServer</c> →
    ///     <c>http_server</c>, <c>Address2Line</c> → <c>address2_line</c>.
    /// </summary>
    public static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var result = new StringBuilder(name.Length + 4);
        var state = State.NotStarted;

        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];

            switch (char.GetUnicodeCategory(current))
            {
                case UnicodeCategory.UppercaseLetter:
                    if (state is State.LowercaseOrDigit or State.Space ||
                        (state == State.Uppercase && i + 1 < name.Length && char.IsLower(name[i + 1])))
                    {
                        result.Append('_');
                    }

                    result.Append(char.ToLowerInvariant(current));
                    state = State.Uppercase;
                    break;
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.DecimalDigitNumber:
                    if (state == State.Space)
                    {
                        result.Append('_');
                    }

                    result.Append(current);
                    state = State.LowercaseOrDigit;
                    break;
                case UnicodeCategory.SpaceSeparator:
                    if (state != State.NotStarted)
                    {
                        state = State.Space;
                    }

                    break;
                default:
                    result.Append(current);
                    state = State.NotStarted;
                    break;
            }
        }

        return result.ToString();
    }

    private enum State
    {
        NotStarted,
        Uppercase,
        LowercaseOrDigit,
        Space
    }
}
