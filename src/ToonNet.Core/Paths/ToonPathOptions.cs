namespace ToonNet.Core.Paths;

/// <summary>
///     Options for compiling and evaluating a <see cref="ToonPath"/>: the functions its filters may call, and limits that
///     keep paths from untrusted input (such as a filter a client sends) from using unbounded time or memory. The defaults
///     suit paths written in code; give stricter limits to <see cref="ToonPath.Parse(string, ToonPathOptions)"/> for paths
///     from clients.
/// </summary>
/// <example>
///     <code>
///     var limits = new ToonPathOptions { MaxLength = 256, MaxNesting = 8, MaxSteps = 100_000, MaxStepsPerValue = 4,
///                                       Timeout = TimeSpan.FromMilliseconds(200) };
///     var path = ToonPath.Parse(filterFromClient, limits);   // ToonPathException when longer or deeper
///     var rows = path.SelectFrom(toon);                      // ToonPathLimitException: too much work, or too long
///
///     // The serializer's path methods take the limits from the options
///     var options = new ToonSerializerOptions { PathOptions = limits };
///     var orders = ToonSerializer.DeserializeAll&lt;Order&gt;(toon, filterFromClient, options);
///     </code>
/// </example>
public sealed class ToonPathOptions
{
    /// <summary>The default options, used by the methods that take path text.</summary>
    public static ToonPathOptions Default { get; } = new();

    private readonly int _maxLength = 4096;
    private readonly int _maxNesting = 64;
    private readonly int _maxPatternLength = 1024;
    private readonly long _maxSteps = 1_000_000;
    private readonly int _maxStepsPerValue = 16;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    /// <summary>The longest path, in characters (default 4,096); a longer one is a <see cref="ToonPathException"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is less than 1.</exception>
    public int MaxLength
    {
        get => _maxLength;
        init => _maxLength = Positive(value);
    }

    /// <summary>
    ///     The deepest nesting of filters, parentheses, function calls and queries in filters (default 64); deeper is a
    ///     <see cref="ToonPathException"/>. It also bounds the stack the parser and the evaluator use.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is less than 1 or more than 256.</exception>
    public int MaxNesting
    {
        get => _maxNesting;
        init => _maxNesting = value is >= 1 and <= 256 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "MaxNesting must be between 1 and 256");
    }

    /// <summary>
    ///     The longest regular expression of <c>match()</c> and <c>search()</c>, in characters (default 1,024); a longer
    ///     pattern is not valid, and the function is false. Patterns run in time linear in the length of the text and the
    ///     size of their automaton, and a match spends a step per 64 characters and states before it runs.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is less than 1.</exception>
    public int MaxPatternLength
    {
        get => _maxPatternLength;
        init => _maxPatternLength = Positive(value);
    }

    /// <summary>
    ///     The steps every evaluation may take, whatever the size of the document (default 1,000,000). A step is a value
    ///     visited or selected, a filter tested, or 16 characters of a string an operation reads (comparing, searching,
    ///     <c>lower</c>, <c>length</c>, ...); comparing or passing an object or array to a function costs a step per value
    ///     in it.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is less than 1.</exception>
    public long MaxSteps
    {
        get => _maxSteps;
        init => _maxSteps = value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "MaxSteps must be at least 1");
    }

    /// <summary>
    ///     The steps an evaluation may take per value of the document and per 16 characters of its strings and keys (per
    ///     line and per 16 characters, when the path is evaluated over TOON text), beyond <see cref="MaxSteps"/> (default
    ///     16; 0 makes
    ///     <see cref="MaxSteps"/> the whole budget). Beyond the budget, evaluation throws
    ///     <see cref="ToonPathLimitException"/>: nested descendant segments (<c>$..*..*..*</c>) multiply the values a
    ///     query visits.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative.</exception>
    public int MaxStepsPerValue
    {
        get => _maxStepsPerValue;
        init => _maxStepsPerValue = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "MaxStepsPerValue cannot be negative");
    }

    /// <summary>
    ///     The longest an evaluation may run (default 10 seconds); beyond it, evaluation throws <see cref="ToonPathLimitException"/> (<see cref="ToonPathLimitException.IsTimeout"/>).
    ///     <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> turns it off. The time is checked every thousand or so
    ///     steps, so the cost is negligible.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is not positive (and not infinite).</exception>
    public TimeSpan Timeout
    {
        get => _timeout;
        init => _timeout = value > TimeSpan.Zero || value == System.Threading.Timeout.InfiniteTimeSpan
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Timeout must be positive, or Timeout.InfiniteTimeSpan");
    }

    /// <summary>
    ///     Functions the path's filters may call besides the standard and built-in ones (see <see cref="ToonPathFunctions"/>);
    ///     null for none.
    /// </summary>
    public ToonPathFunctions? Functions { get; init; }

    /// <summary>
    ///     Whether only the five functions of RFC 9535 (<c>length</c>, <c>count</c>, <c>match</c>, <c>search</c>,
    ///     <c>value</c>) may be called, so a path that compiles also works with other JSONPath implementations. By default
    ///     (false), the ToonNet extensions (<c>starts_with</c>, <c>contains</c>, <c>lower</c>, <c>sum</c>, ...) and
    ///     <see cref="Functions"/> may be called too.
    /// </summary>
    public bool StandardFunctionsOnly { get; init; }

    private static int Positive(int value) =>
        value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "The limit must be at least 1");
}
