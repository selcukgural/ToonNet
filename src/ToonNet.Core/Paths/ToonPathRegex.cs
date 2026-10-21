using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace ToonNet.Core.Paths;

/// <summary>
///     Compiles the I-Regexp patterns (RFC 9485) of <c>match()</c> and <c>search()</c> to .NET regular expressions that
///     run in linear time (<see cref="RegexOptions.NonBacktracking"/>). A pattern outside I-Regexp is invalid, and the
///     function is then false.
/// </summary>
/// <remarks>
///     Linear time is still the length of the text times the size of the pattern's automaton (<c>[a-z]{1000}</c> has a
///     thousand states), so each pattern has a <see cref="PathPattern.Weight"/> that evaluations spend steps for before a
///     match. The automaton also keeps the states it builds while matching (megabytes for a light pattern on a long
///     text): a few light patterns share a regular expression, in a small cache; others get one per evaluation, released
///     with it. Compiled patterns and paths keep only the pattern's text.
/// </remarks>
internal static class ToonPathRegex
{
    private const int MaxCacheSize = 256;

    /// <summary>Patterns up to this weight may share a compiled regular expression.</summary>
    internal const int MaxSharedWeight = 64;

    /// <summary>The shared regular expressions; few, because each keeps the states it has built.</summary>
    private const int MaxSharedCount = 16;

    private static readonly ConcurrentDictionary<PathPattern, Regex> Shared = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///     The steps an evaluation spends to compile a regular expression, on top of its weight: building one takes about
    ///     as long as a few thousand steps, whatever its size.
    /// </summary>
    internal const int CompileSteps = 4096;

    /// <summary>Heavier patterns are invalid (.NET rejects automata much smaller than this).</summary>
    private const long MaxWeight = 1_000_000;

    /// <summary>I-Regexp's '.': any character but CR and LF, a surrogate pair being one character.</summary>
    private const string Dot = @"(?:[^\n\r\uD800-\uDFFF]|[\uD800-\uDBFF][\uDC00-\uDFFF])";

    private static readonly ConcurrentDictionary<(string Pattern, bool Full), PathPattern?> Cache = new();

    /// <summary>The compiled pattern, or null when it is not a valid I-Regexp.</summary>
    /// <param name="pattern">The I-Regexp pattern.</param>
    /// <param name="full">Whether the whole string must match (<c>match()</c>) rather than a substring (<c>search()</c>).</param>
    /// <param name="maxLength">Longer patterns are not valid, which bounds the cost of compiling one.</param>
    public static PathPattern? Get(string pattern, bool full, int maxLength) => Get(pattern, full, maxLength, out _);

    /// <summary>The compiled pattern, or null when it is not a valid I-Regexp; see <see cref="Get(string, bool, int)"/>.</summary>
    /// <param name="pattern">The I-Regexp pattern.</param>
    /// <param name="full">Whether the whole string must match rather than a substring.</param>
    /// <param name="maxLength">Longer patterns are not valid.</param>
    /// <param name="compiled">Whether the pattern was not in the cache, so it was compiled now.</param>
    public static PathPattern? Get(string pattern, bool full, int maxLength, out bool compiled)
    {
        compiled = false;

        if (pattern.Length > maxLength)
        {
            return null;
        }

        if (Cache.TryGetValue((pattern, full), out var result))
        {
            return result;
        }

        compiled = true;
        result = Compile(pattern, full);

        if (Cache.Count >= MaxCacheSize)
        {
            Cache.Clear();
        }

        Cache.TryAdd((pattern, full), result);
        return result;
    }

    private static PathPattern? Compile(string pattern, bool full)
    {
        if (Translate(pattern, out var weight) is not { } translated)
        {
            return null;
        }

        var text = full ? @"\A(?:" + translated + @")\z" : translated;

        try
        {
            // Only to see that .NET accepts it (not every I-Regexp fits its limits); evaluations compile their own
            PathPattern.Create(text);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return null;
        }

        return new PathPattern(text, (int)weight);
    }

    /// <summary>The shared regular expression of a pattern, if it has been compiled and is still in the cache.</summary>
    public static bool TryGetShared(PathPattern pattern, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Regex? regex) =>
        Shared.TryGetValue(pattern, out regex);

    /// <summary>
    ///     Compiles a pattern; a light one is also shared with other evaluations. The caller pays for compiling
    ///     (<see cref="CompileSteps"/>): patterns that keep replacing each other in the small cache are compiled again on
    ///     each use.
    /// </summary>
    public static Regex Compile(PathPattern pattern)
    {
        if (pattern.Weight > MaxSharedWeight)
        {
            return pattern.Compile();
        }

        if (Shared.Count >= MaxSharedCount)
        {
            Shared.Clear();
        }

        return Shared.GetOrAdd(pattern, static p => p.Compile());
    }

    /// <summary>
    ///     Reads a counted repetition (<c>{n}</c>, <c>{n,}</c>, <c>{n,m}</c>) at <paramref name="start"/>: the copies of its
    ///     atom the automaton has (m, or n + 1 for an open end), and the position of its '}'.
    /// </summary>
    private static bool TryReadRepetition(string pattern, int start, out long copies, out int end)
    {
        copies = 0;
        end = start + 1;
        long min = 0, max = -1;
        var digits = 0;

        for (; end < pattern.Length && char.IsAsciiDigit(pattern[end]); end++, digits++)
        {
            min = Math.Min(min * 10 + (pattern[end] - '0'), MaxWeight);
        }

        if (digits == 0 || end >= pattern.Length)
        {
            return false;
        }

        if (pattern[end] == ',')
        {
            end++;
            digits = 0;
            long upper = 0;

            for (; end < pattern.Length && char.IsAsciiDigit(pattern[end]); end++, digits++)
            {
                upper = Math.Min(upper * 10 + (pattern[end] - '0'), MaxWeight);
            }

            max = digits == 0 ? min + 1 : upper;
        }
        else
        {
            max = min;
        }

        if (end >= pattern.Length || pattern[end] != '}')
        {
            return false;
        }

        copies = Math.Max(Math.Max(min, max), 1);
        return true;
    }

    /// <summary>
    ///     Rewrites an I-Regexp for .NET: '.' outside a class matches one character but CR and LF, '$' only the end of
    ///     the string; escapes are limited to those of I-Regexp, and groups to plain ones.
    /// </summary>
    /// <param name="pattern">The I-Regexp pattern.</param>
    /// <param name="weight">
    ///     About the number of states of the pattern's automaton: an atom is one (a '.' two), a group the sum of its
    ///     branches, and a counted repetition (<c>{n}</c>, <c>{n,m}</c>, <c>{n,}</c>) copies its atom.
    /// </param>
    private static string? Translate(string pattern, out long weight)
    {
        var result = new StringBuilder(pattern.Length + 16);
        var inClass = false;

        // The weight of the alternatives before the current one, of the current one, and of its last atom (which a
        // quantifier repeats), for the current group and the groups around it
        long total = 0, branch = 0, last = 0;
        var groups = new Stack<(long Total, long Branch, long Last)>();
        weight = 0;

        void Atom(long atomWeight)
        {
            branch += atomWeight;
            last = atomWeight;
        }

        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];

            if (branch > MaxWeight || total > MaxWeight)
            {
                return null;
            }

            if (c == '\\')
            {
                if (++i >= pattern.Length)
                {
                    return null;
                }

                if (!inClass)
                {
                    Atom(1);
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
                    Atom(1);
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
                    Atom(2);
                    break;
                case '$':
                    result.Append(@"\z");
                    break;
                case '(' when i + 1 < pattern.Length && pattern[i + 1] == '?':
                    return null;
                case '(':
                    groups.Push((total, branch, last));
                    (total, branch, last) = (0, 0, 0);
                    result.Append(c);
                    break;
                case ')':
                {
                    if (!groups.TryPop(out var outer))
                    {
                        return null;
                    }

                    var group = total + branch;
                    (total, branch, last) = outer;
                    Atom(group);
                    result.Append(c);
                    break;
                }
                case '|':
                    total += branch;
                    (branch, last) = (0, 0);
                    result.Append(c);
                    break;
                case '*' or '+' or '?':
                    last = 0; // a loop does not copy its atom (and a second quantifier, as in a*?, repeats nothing)
                    result.Append(c);
                    break;
                case '{' when TryReadRepetition(pattern, i, out var copies, out var end):
                    branch += last * (copies - 1);
                    last = 0;
                    result.Append(pattern, i, end - i + 1);
                    i = end;
                    break;
                case '^':
                    result.Append(c);
                    break;
                default:
                    Atom(1);
                    result.Append(c);
                    break;
            }
        }

        weight = total + branch;
        return inClass || groups.Count > 0 || weight > MaxWeight ? null : result.ToString();
    }
}

/// <summary>A translated <c>match()</c> or <c>search()</c> pattern (its regular expressions are compiled when used).</summary>
internal sealed class PathPattern(string text, int weight)
{
    /// <summary>About the number of states of its automaton (see <see cref="ToonPathRegex"/>).</summary>
    public int Weight { get; } = weight;

    /// <summary>A new regular expression for the pattern.</summary>
    public Regex Compile() => Create(text);

    internal static Regex Create(string text) => new(text, RegexOptions.NonBacktracking | RegexOptions.CultureInvariant);
}
