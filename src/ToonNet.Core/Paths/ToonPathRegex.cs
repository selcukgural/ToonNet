using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace ToonNet.Core.Paths;

/// <summary>
///     Compiles the I-Regexp patterns (RFC 9485) of <c>match()</c> and <c>search()</c> to .NET regular expressions that
///     run in linear time (<see cref="RegexOptions.NonBacktracking"/>), so a pattern from untrusted input cannot make a
///     query slow. A pattern outside I-Regexp is invalid, and the function is then false.
/// </summary>
internal static class ToonPathRegex
{
    private const int MaxCacheSize = 256;

    /// <summary>I-Regexp's '.': any character but CR and LF, a surrogate pair being one character.</summary>
    private const string Dot = @"(?:[^\n\r\uD800-\uDFFF]|[\uD800-\uDBFF][\uDC00-\uDFFF])";

    private static readonly ConcurrentDictionary<(string Pattern, bool Full), Regex?> Cache = new();

    /// <summary>The compiled pattern, or null when it is not a valid I-Regexp.</summary>
    /// <param name="pattern">The I-Regexp pattern.</param>
    /// <param name="full">Whether the whole string must match (<c>match()</c>) rather than a substring (<c>search()</c>).</param>
    /// <param name="maxLength">Longer patterns are not valid, which bounds the cost of compiling one.</param>
    public static Regex? Get(string pattern, bool full, int maxLength)
    {
        if (pattern.Length > maxLength)
        {
            return null;
        }

        if (Cache.TryGetValue((pattern, full), out var regex))
        {
            return regex;
        }

        regex = Compile(pattern, full);

        if (Cache.Count >= MaxCacheSize)
        {
            Cache.Clear();
        }

        Cache.TryAdd((pattern, full), regex);
        return regex;
    }

    private static Regex? Compile(string pattern, bool full)
    {
        if (Translate(pattern) is not { } translated)
        {
            return null;
        }

        try
        {
            return new Regex(full ? @"\A(?:" + translated + @")\z" : translated,
                             RegexOptions.NonBacktracking | RegexOptions.CultureInvariant);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>
    ///     Rewrites an I-Regexp for .NET: '.' outside a class matches one character but CR and LF, '$' only the end of
    ///     the string; escapes are limited to those of I-Regexp, and groups to plain ones.
    /// </summary>
    private static string? Translate(string pattern)
    {
        var result = new StringBuilder(pattern.Length + 16);
        var inClass = false;

        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];

            if (c == '\\')
            {
                if (++i >= pattern.Length)
                {
                    return null;
                }

                var escaped = pattern[i];

                switch (escaped)
                {
                    case '(' or ')' or '*' or '+' or '-' or '.' or '?' or '[' or '\\' or ']' or '^' or '{' or '|' or '}' or 'n' or 'r' or 't':
                        result.Append('\\').Append(escaped);
                        break;
                    case 'p' or 'P':
                    {
                        // \p{Category}
                        var close = pattern.IndexOf('}', i);

                        if (i + 1 >= pattern.Length || pattern[i + 1] != '{' || close < 0)
                        {
                            return null;
                        }

                        result.Append('\\').Append(pattern, i, close - i + 1);
                        i = close;
                        break;
                    }
                    default:
                        return null;
                }

                continue;
            }

            if (inClass)
            {
                if (c == ']')
                {
                    inClass = false;
                }
                else if (c == '[')
                {
                    return null; // no nested classes or set operations
                }

                result.Append(c);
                continue;
            }

            switch (c)
            {
                case '[':
                    inClass = true;
                    result.Append(c);

                    // A '^' right after '[' negates the class (not an anchor)
                    if (i + 1 < pattern.Length && pattern[i + 1] == '^')
                    {
                        result.Append('^');
                        i++;
                    }

                    break;
                case '.':
                    result.Append(Dot);
                    break;
                case '$':
                    result.Append(@"\z");
                    break;
                case '(' when i + 1 < pattern.Length && pattern[i + 1] == '?':
                    return null;
                default:
                    result.Append(c);
                    break;
            }
        }

        return inClass ? null : result.ToString();
    }
}
