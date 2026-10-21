using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using ToonNet.Core.Models;

namespace ToonNet.Core.Paths;

/// <summary>
///     The work one evaluation may do (<see cref="ToonPathOptions.MaxSteps"/>, plus
///     <see cref="ToonPathOptions.MaxStepsPerValue"/> per value of the document), so a path from untrusted input cannot make a
///     query run away: each descendant segment (<c>..</c>) multiplies the values it visits by the depth of the document,
///     so <c>$..*..*..*..*</c> on a small, deep document would otherwise visit billions of values.
/// </summary>
internal sealed class EvaluationBudget
{
    /// <summary>The steps between two looks at the clock.</summary>
    private const int StepsPerClockCheck = 1024;

    private long _remaining;
    private ToonValue? _root;
    private readonly long _deadline;
    private int _untilClockCheck = StepsPerClockCheck;

    /// <summary>A budget for a tree: its values are counted when <see cref="ToonPathOptions.MaxSteps"/> is used up.</summary>
    public EvaluationBudget(ToonValue root, ToonPathOptions options) : this(options)
    {
        _remaining = options.MaxSteps;
        _root = options.MaxStepsPerValue > 0 ? root : null;
        _document = root;
    }

    /// <summary>
    ///     A budget for a document evaluated as text: its values are counted when <see cref="ToonPathOptions.MaxSteps"/> is
    ///     used up, as for a tree: a value per line, and per item of inline arrays and cells of tables (not a step per
    ///     character, which a long string would turn into steps for the rest of the query).
    /// </summary>
    public EvaluationBudget(Parsing.ToonText text, ToonPathOptions options) : this(options)
    {
        _remaining = options.MaxSteps;
        _text = options.MaxStepsPerValue > 0 ? text : (Parsing.ToonText?)null;
        _documentText = text;
    }

    private Parsing.ToonText? _text;

    /// <summary>The document, whose values are counted for <see cref="CheckNodes"/> once a list gets long.</summary>
    private ToonValue? _document;

    private Parsing.ToonText? _documentText;

    /// <summary>The values a list of selected nodes may hold before the document's values are counted.</summary>
    private const long MinNodes = 1_000_000;

    /// <summary>The values a list may hold per value of the document, on top of <see cref="MinNodes"/>.</summary>
    private const int NodesPerValue = 2;

    private long _maxNodes = MinNodes;

    /// <summary>
    ///     Checks the length of a list of selected nodes. Steps bound the work, not the memory of the lists a segment
    ///     builds: selectors that repeat (<c>[*,*,*,...]</c>, which keep their duplicates, RFC 9535 §2.5) would keep
    ///     sixteen nodes per value of the document. A list may hold a million nodes, and two more per value.
    /// </summary>
    /// <exception cref="ToonPathLimitException">Thrown when the list is longer.</exception>
    public void CheckNodes(int count)
    {
        if (count <= _maxNodes)
        {
            return;
        }

        if (_document != null || _documentText != null)
        {
            var values = _document != null ? CountValues(_document).Values : CountTextValues(_documentText!.Value.Span);
            _maxNodes += NodesPerValue * values;
            _document = null;
            _documentText = null;

            if (count <= _maxNodes)
            {
                return;
            }
        }

        throw new ToonPathLimitException(
            $"The path selects more than {_maxNodes} values at once (a million, and two per value of the document): selectors that repeat keep their duplicates.",
            isTimeout: false);
    }

    private EvaluationBudget(ToonPathOptions options)
    {
        Options = options;
        _deadline = options.Timeout == Timeout.InfiniteTimeSpan
            ? long.MaxValue
            : Environment.TickCount64 + (long)Math.Ceiling(options.Timeout.TotalMilliseconds);
    }

    public ToonPathOptions Options { get; }

    public void Spend(int steps = 1)
    {
        if ((_remaining -= steps) < 0)
        {
            Extend();
        }

        if ((_untilClockCheck -= steps) <= 0)
        {
            _untilClockCheck = StepsPerClockCheck;

            if (Environment.TickCount64 > _deadline)
            {
                throw new ToonPathLimitException($"The path took longer than its timeout of {Options.Timeout} (see ToonPathOptions).", isTimeout: true);
            }
        }
    }

    /// <summary>
    ///     Spends in proportion to the work an operation on a value does (comparing, copying or searching it): a step per
    ///     value inside it and per 16 characters of its strings, before the operation, so a large value cannot make one
    ///     step cost as much as the whole document.
    /// </summary>
    public void SpendValue(ToonValue? value)
    {
        switch (value)
        {
            case ToonString text:
                SpendText(text.Value.Length);
                break;
            case ToonObject or ToonArray:
            {
                var pending = new Stack<ToonValue>();
                pending.Push(value);

                while (pending.TryPop(out var current))
                {
                    Spend();

                    switch (current)
                    {
                        case ToonString inner:
                            SpendText(inner.Value.Length);
                            break;
                        case ToonObject obj:
                            foreach (var (key, member) in obj.Properties)
                            {
                                SpendText(key.Length);
                                pending.Push(member);
                            }

                            break;
                        case ToonArray array:
                            foreach (var item in array.Items)
                            {
                                pending.Push(item);
                            }

                            break;
                    }
                }

                break;
            }
        }
    }

    /// <summary>A pattern from the document (<c>match(@.v, @.pattern)</c>), compiled once per evaluation.</summary>
    public PathPattern? Pattern(string pattern, bool full)
    {
        // Checking a pattern reads it, and compiles it when it is not cached (other documents can fill the cache)
        SpendText(pattern.Length);
        var result = ToonPathRegex.Get(pattern, full, Options.MaxPatternLength, out var compiled);

        if (compiled)
        {
            Spend(ToonPathRegex.CompileSteps);
        }

        return result;
    }

    /// <summary>
    ///     The regular expression of a pattern, after spending for a match on a text of <paramref name="length"/>
    ///     characters: a step per 64 characters and states of the automaton. A heavy pattern is compiled for this
    ///     evaluation, so the states it builds are released with the evaluation; a light one may come from the small
    ///     shared cache. Compiling spends steps, whichever cache it goes to.
    /// </summary>
    public System.Text.RegularExpressions.Regex RegexFor(PathPattern pattern, int length)
    {
        SpendMany((long)length * pattern.Weight / 64 + 1);

        // The evaluation's own first: patterns of other evaluations can push its patterns out of the shared cache
        if (_regexes != null && _regexes.TryGetValue(pattern, out var regex))
        {
            return regex;
        }

        if (!ToonPathRegex.TryGetShared(pattern, out regex))
        {
            SpendMany(pattern.Weight + (long)ToonPathRegex.CompileSteps);
            regex = ToonPathRegex.Compile(pattern);
        }

        // The pattern literals of the path and a few patterns from the document are kept for the evaluation; more (each
        // value with its own pattern) are compiled for each use
        if ((_regexes ??= new Dictionary<PathPattern, System.Text.RegularExpressions.Regex>(ReferenceEqualityComparer.Instance)).Count <
            ToonPathParser.MaxPatterns + 16)
        {
            _regexes[pattern] = regex;
        }

        return regex;
    }

    private Dictionary<PathPattern, System.Text.RegularExpressions.Regex>? _regexes;

    /// <summary>Spends more steps than an <see cref="int"/> holds.</summary>
    public void SpendMany(long steps)
    {
        while (steps > int.MaxValue)
        {
            Spend(int.MaxValue);
            steps -= int.MaxValue;
        }

        Spend((int)steps);
    }

    /// <summary>Spends a step per 16 characters of text an operation reads or writes.</summary>
    public void SpendText(int length)
    {
        if (length >= 16)
        {
            Spend(length / 16);
        }
    }

    private void Extend()
    {
        if (_root != null)
        {
            var (values, characters) = CountValues(_root);
            _remaining += Options.MaxStepsPerValue * values + TextAllowance(characters);
            _root = null;

            if (_remaining >= 0)
            {
                return;
            }
        }

        if (_text is { } text)
        {
            // As for a tree: the values, and the characters to read
            _remaining += Options.MaxStepsPerValue * CountTextValues(text.Span) + TextAllowance(text.Length);
            _text = null;

            if (_remaining >= 0)
            {
                return;
            }
        }

        throw new ToonPathLimitException(
            "The path does too much work for the size of the document (see ToonPathOptions.MaxSteps and MaxStepsPerValue): nested descendant segments (..), patterns and functions over long text, and paths of many values all spend steps.",
            isTimeout: false);
    }

    /// <summary>
    ///     About the number of values of a document, without parsing it: a value per line, and per delimiter (outside
    ///     quotes) of an inline array's line (<c>a[3]: 1,2,3</c>) and of the rows of a table (<c>a[2]{x,y}:</c>).
    /// </summary>
    internal static long CountTextValues(ReadOnlySpan<char> text)
    {
        long count = 0;
        var tableIndent = -1; // the indentation of the open table's header, or -1
        var tableDelimiter = ',';

        while (!text.IsEmpty)
        {
            var end = text.IndexOf('\n');
            var line = end < 0 ? text : text[..end];
            text = end < 0 ? default : text[(end + 1)..];
            count++;

            var indent = line.IndexOfAnyExcept(' ');

            if (indent < 0)
            {
                continue;
            }

            if (tableIndent >= 0 && indent > tableIndent)
            {
                count += CountDelimiters(line[indent..], tableDelimiter);
                continue;
            }

            tableIndent = -1;

            // A header `key[N<delimiter>]` followed by `:` and values (inline) or `{fields}:` (a table)
            if (FindHeader(line, out var close, out var delimiter))
            {
                var rest = line[(close + 1)..];

                if (rest.Length > 1 && rest[0] == ':')
                {
                    count += CountDelimiters(rest[1..], delimiter);
                }
                else if (rest.Length > 0 && rest[0] == '{')
                {
                    tableIndent = indent;
                    tableDelimiter = delimiter;
                }
            }
        }

        return count;

        // The ']' of the first `[digits]` outside quotes, and the delimiter it declares
        static bool FindHeader(ReadOnlySpan<char> line, out int close, out char delimiter)
        {
            close = -1;
            delimiter = ',';
            var quoted = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (quoted)
                {
                    if (c == '\\')
                    {
                        i++;
                    }
                    else if (c == '"')
                    {
                        quoted = false;
                    }
                }
                else if (c == '"')
                {
                    quoted = true;
                }
                else if (c == ':')
                {
                    return false; // the key ended without a header
                }
                else if (c == '[')
                {
                    var j = i + 1;

                    while (j < line.Length && char.IsAsciiDigit(line[j]))
                    {
                        j++;
                    }

                    if (j < line.Length && line[j] is '\t' or '|')
                    {
                        delimiter = line[j++];
                    }

                    if (j > i + 1 && j < line.Length && line[j] == ']')
                    {
                        close = j;
                        return true;
                    }
                }
            }

            return false;
        }

        static long CountDelimiters(ReadOnlySpan<char> values, char delimiter)
        {
            long delimiters = 0;
            var quoted = false;

            for (var i = 0; i < values.Length; i++)
            {
                var c = values[i];

                if (quoted)
                {
                    if (c == '\\')
                    {
                        i++;
                    }
                    else if (c == '"')
                    {
                        quoted = false;
                    }
                }
                else if (c == '"')
                {
                    quoted = true;
                }
                else if (c == delimiter)
                {
                    delimiters++;
                }
            }

            return delimiters;
        }
    }

    /// <summary>
    ///     The steps that let a query read the characters of the document twice (a step per 16 characters it reads), so a
    ///     filter over a long string is not over the budget. Not a step per character: nested descendant segments would
    ///     spend that on the values.
    /// </summary>
    private long TextAllowance(long characters) => Options.MaxStepsPerValue == 0 ? 0 : 2 * (characters / 16);

    /// <summary>The values of a tree, and the characters of its strings and keys.</summary>
    private static (long Values, long Characters) CountValues(ToonValue root)
    {
        var count = 0L;
        var characters = 0L;
        var pending = new Stack<ToonValue>();
        pending.Push(root);

        while (pending.TryPop(out var value))
        {
            count++;

            switch (value)
            {
                case ToonString text:
                    characters += text.Value.Length;
                    break;
                case ToonObject obj:
                    foreach (var (key, member) in obj.Properties)
                    {
                        characters += key.Length;
                        pending.Push(member);
                    }

                    break;
                case ToonArray array:
                    foreach (var item in array.Items)
                    {
                        pending.Push(item);
                    }

                    break;
            }
        }

        return (count, characters);
    }
}

/// <summary>Evaluates a compiled path over a <see cref="ToonValue"/> tree, with the semantics of RFC 9535.</summary>
internal static class ToonPathEvaluator
{
    /// <summary>A selected value and, when locations are tracked, where it is.</summary>
    internal readonly struct Node(ToonValue value, Location? location)
    {
        public readonly ToonValue Value = value ?? ToonNull.Instance; // a null that code put into ToonArray.Items
        public readonly Location? Location = location;
    }

    /// <summary>Where a node is: the container it is in and its key or index there; the root has none.</summary>
    internal sealed class Location(Location? parent, ToonValue container, string? name, int index)
    {
        public Location? Parent { get; } = parent;

        public ToonValue Container { get; } = container;

        /// <summary>The member name, or null for an array item.</summary>
        public string? Name { get; } = name;

        public int Index { get; } = index;

        /// <summary>The normalized path (RFC 9535 §2.7): <c>$['users'][0]</c>.</summary>
        /// <param name="location">The location.</param>
        /// <param name="work">
        ///     The budget of the evaluation, which pays for the path first: a step per segment and per 16 characters of its
        ///     names (paths of deep values with long keys are long, and each selected value has one).
        /// </param>
        public static string Format(Location? location, EvaluationBudget work)
        {
            if (location == null)
            {
                return "$";
            }

            var chain = new List<Location>();
            var names = 0L;

            for (var l = location; l != null; l = l.Parent)
            {
                chain.Add(l);
                work.Spend();
                names += l.Name?.Length ?? 0;
            }

            work.SpendMany(names / 16);

            var text = new StringBuilder("$");

            for (var i = chain.Count - 1; i >= 0; i--)
            {
                var l = chain[i];

                if (l.Name == null)
                {
                    text.Append('[').Append(l.Index.ToString(CultureInfo.InvariantCulture)).Append(']');
                }
                else
                {
                    text.Append("['");
                    AppendEscaped(text, l.Name);
                    text.Append("']");
                }
            }

            return text.ToString();
        }

        private static void AppendEscaped(StringBuilder text, string name)
        {
            foreach (var c in name)
            {
                switch (c)
                {
                    case '\b': text.Append(@"\b"); break;
                    case '\f': text.Append(@"\f"); break;
                    case '\n': text.Append(@"\n"); break;
                    case '\r': text.Append(@"\r"); break;
                    case '\t': text.Append(@"\t"); break;
                    case '\'': text.Append(@"\'"); break;
                    case '\\': text.Append(@"\\"); break;
                    case < ' ': text.Append(@"\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture)); break;
                    default: text.Append(c); break;
                }
            }
        }
    }

    /// <summary>Selects the nodes of <paramref name="query"/>, starting at <paramref name="start"/> (the root or the current node).</summary>
    public static List<Node> Select(Query query, ToonValue root, ToonValue start, bool track, EvaluationBudget work)
    {
        var nodes = new List<Node>(1) { new(start, null) };

        foreach (var segment in query.Segments)
        {
            var next = new List<Node>(nodes.Count);

            foreach (var node in nodes)
            {
                if (segment.Descendant)
                {
                    SelectDescendants(segment.Selectors, node, root, track, next, work);
                }
                else
                {
                    foreach (var selector in segment.Selectors)
                    {
                        SelectChildren(selector, node, root, track, next, work);
                    }
                }
            }

            nodes = next;

            if (nodes.Count == 0)
            {
                break;
            }
        }

        return nodes;
    }

    /// <summary>Applies the selectors to a node and all its descendants, in document order (§2.5.2.2).</summary>
    private static void SelectDescendants(Selector[] selectors, Node node, ToonValue root, bool track, List<Node> output, EvaluationBudget work)
    {
        work.Spend();

        foreach (var selector in selectors)
        {
            SelectChildren(selector, node, root, track, output, work);
        }

        switch (node.Value)
        {
            case ToonObject obj:
                RuntimeHelpers.EnsureSufficientExecutionStack();

                foreach (var (key, value) in obj.Properties)
                {
                    SelectDescendants(selectors, new Node(value, track ? new Location(node.Location, obj, key, -1) : null), root, track, output, work);
                }

                break;
            case ToonArray array:
                RuntimeHelpers.EnsureSufficientExecutionStack();

                for (var i = 0; i < array.Items.Count; i++)
                {
                    SelectDescendants(selectors, new Node(array.Items[i], track ? new Location(node.Location, array, null, i) : null), root, track, output, work);
                }

                break;
        }
    }

    private static void SelectChildren(Selector selector, Node node, ToonValue root, bool track, List<Node> output, EvaluationBudget work)
    {
        var before = output.Count;
        AddChildren(selector, node, root, track, output, work);
        work.Spend(output.Count - before);
        work.CheckNodes(output.Count);
    }

    private static void AddChildren(Selector selector, Node node, ToonValue root, bool track, List<Node> output, EvaluationBudget work)
    {
        switch (selector.Kind)
        {
            case SelectorKind.Name:
                if (node.Value is ToonObject obj && obj.Properties.TryGetValue(selector.Name!, out var member))
                {
                    output.Add(new Node(member, track ? new Location(node.Location, obj, selector.Name, -1) : null));
                }

                return;
            case SelectorKind.Index:
                if (node.Value is ToonArray indexed && ResolveIndex(selector.Index, indexed.Items.Count) is var index and >= 0)
                {
                    output.Add(new Node(indexed.Items[index], track ? new Location(node.Location, indexed, null, index) : null));
                }

                return;
            case SelectorKind.Wildcard:
                switch (node.Value)
                {
                    case ToonObject all:
                        foreach (var (key, value) in all.Properties)
                        {
                            output.Add(new Node(value, track ? new Location(node.Location, all, key, -1) : null));
                        }

                        break;
                    case ToonArray items:
                        for (var i = 0; i < items.Items.Count; i++)
                        {
                            output.Add(new Node(items.Items[i], track ? new Location(node.Location, items, null, i) : null));
                        }

                        break;
                }

                return;
            case SelectorKind.Slice:
                if (node.Value is ToonArray sliced)
                {
                    foreach (var i in SliceIndexes(selector, sliced.Items.Count))
                    {
                        output.Add(new Node(sliced.Items[i], track ? new Location(node.Location, sliced, null, i) : null));
                    }
                }

                return;
            case SelectorKind.Filter:
                switch (node.Value)
                {
                    case ToonObject filtered:
                        foreach (var (key, value) in filtered.Properties)
                        {
                            if (Test(selector.Filter!, root, value, work))
                            {
                                output.Add(new Node(value, track ? new Location(node.Location, filtered, key, -1) : null));
                            }
                        }

                        break;
                    case ToonArray items:
                        for (var i = 0; i < items.Items.Count; i++)
                        {
                            if (Test(selector.Filter!, root, items.Items[i], work))
                            {
                                output.Add(new Node(items.Items[i], track ? new Location(node.Location, items, null, i) : null));
                            }
                        }

                        break;
                }

                return;
        }
    }

    /// <summary>The position of an index in an array of <paramref name="count"/> items, or -1 when it is outside.</summary>
    internal static int ResolveIndex(long index, int count)
    {
        var resolved = index < 0 ? count + index : index;
        return resolved >= 0 && resolved < count ? (int)resolved : -1;
    }

    /// <summary>The indexes a slice selects, in order (§2.3.4.2.2).</summary>
    internal static IEnumerable<int> SliceIndexes(Selector slice, int count)
    {
        var step = slice.Step;

        if (step == 0)
        {
            yield break;
        }

        long length = count;

        if (step > 0)
        {
            var lower = Math.Min(Math.Max(Normalize(slice.Start ?? 0, length), 0), length);
            var upper = Math.Min(Math.Max(Normalize(slice.End ?? length, length), 0), length);

            for (var i = lower; i < upper; i += step)
            {
                yield return (int)i;
            }
        }
        else
        {
            var upper = Math.Min(Math.Max(Normalize(slice.Start ?? length - 1, length), -1), length - 1);
            var lower = Math.Min(Math.Max(Normalize(slice.End ?? -length - 1, length), -1), length - 1);

            for (var i = upper; lower < i; i += step)
            {
                yield return (int)i;
            }
        }

        static long Normalize(long index, long length) => index >= 0 ? index : length + index;
    }

    #region Filter expressions

    /// <summary>Evaluates a logical expression with <paramref name="current"/> as <c>@</c>.</summary>
    internal static bool Test(PathExpression expression, ToonValue root, ToonValue current, EvaluationBudget work)
    {
        work.Spend();

        // Expressions nest as deep as the path does (up to ToonPathOptions.MaxNesting): fail safely on a small stack
        RuntimeHelpers.EnsureSufficientExecutionStack();

        switch (expression)
        {
            case OrExpression or:
                foreach (var operand in or.Operands)
                {
                    if (Test(operand, root, current, work))
                    {
                        return true;
                    }
                }

                return false;
            case AndExpression and:
                foreach (var operand in and.Operands)
                {
                    if (!Test(operand, root, current, work))
                    {
                        return false;
                    }
                }

                return true;
            case NotExpression not:
                return !Test(not.Operand, root, current, work);
            case QueryExpression { Query: var query }:
                return query.IsSingular
                    ? SelectSingular(query, root, current) != null
                    : Select(query, root, query.Absolute ? root : current, track: false, work).Count != 0;
            case ComparisonExpression comparison:
            {
                var left = Evaluate(comparison.Left, root, current, work);
                var right = Evaluate(comparison.Right, root, current, work);

                // Comparing strings, objects and arrays reads them: two strings as far as the shorter one goes
                if (left is ToonString leftText && right is ToonString rightText)
                {
                    work.SpendText(Math.Min(leftText.Value.Length, rightText.Value.Length));
                }
                else if (left is not (null or ToonNumber or ToonBoolean or ToonNull) && right is not (null or ToonNumber or ToonBoolean or ToonNull))
                {
                    work.SpendValue(left.ValueType <= right.ValueType ? left : right);
                }

                return Compare(left, comparison.Operator, right);
            }
            case FunctionExpression function:
                return TestFunction(function, root, current, work);
            default:
                throw new InvalidOperationException("Not a logical expression.");
        }
    }

    /// <summary>Evaluates an expression of ValueType; null is Nothing (§2.4.1).</summary>
    private static ToonValue? Evaluate(PathExpression expression, ToonValue root, ToonValue current, EvaluationBudget work)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();

        switch (expression)
        {
            case LiteralExpression literal:
                return literal.Value;
            case QueryExpression { Query: var query }:
                return SelectSingular(query, root, current);
            case FunctionExpression function:
                return EvaluateFunction(function, root, current, work);
            default:
                throw new InvalidOperationException("Not a value expression.");
        }
    }

    /// <summary>The node of a singular query, or null.</summary>
    private static ToonValue? SelectSingular(Query query, ToonValue root, ToonValue current)
    {
        var value = query.Absolute ? root : current;

        foreach (var segment in query.Segments)
        {
            var selector = segment.Selectors[0];

            if (selector.Kind == SelectorKind.Name)
            {
                if (value is not ToonObject obj || !obj.Properties.TryGetValue(selector.Name!, out value))
                {
                    return null;
                }
            }
            else
            {
                if (value is not ToonArray array || ResolveIndex(selector.Index, array.Items.Count) is not (var index and >= 0))
                {
                    return null;
                }

                value = array.Items[index] ?? ToonNull.Instance;
            }
        }

        return value;
    }

    private static List<Node> SelectNodes(PathExpression argument, ToonValue root, ToonValue current, EvaluationBudget work)
    {
        var query = ((QueryExpression)argument).Query;
        return Select(query, root, query.Absolute ? root : current, track: false, work);
    }

    private static ToonValue? EvaluateFunction(FunctionExpression function, ToonValue root, ToonValue current, EvaluationBudget work)
    {
        switch (function.Function)
        {
            case FunctionKind.Length:
                var measured = Evaluate(function.Arguments[0], root, current, work);

                if (measured is ToonString counted)
                {
                    work.SpendText(counted.Value.Length);
                }

                return measured switch
                {
                    ToonString text => new ToonNumber((long)CountScalars(text.Value)),
                    ToonArray array => new ToonNumber((long)array.Items.Count),
                    ToonObject obj => new ToonNumber((long)obj.Properties.Count),
                    _ => null
                };
            case FunctionKind.Count:
                return new ToonNumber((long)SelectNodes(function.Arguments[0], root, current, work).Count);
            case FunctionKind.Value:
                return SelectNodes(function.Arguments[0], root, current, work) is [var single] ? single.Value : null;
            case FunctionKind.Custom:
                return (ToonValue?)InvokeCustom(function, root, current, work);
            default:
                throw new InvalidOperationException("The function does not return a value.");
        }
    }

    /// <summary>Calls an extension or registered function with its arguments: values (null for Nothing) or lists of values.</summary>
    private static object? InvokeCustom(FunctionExpression function, ToonValue root, ToonValue current, EvaluationBudget work)
    {
        var custom = function.Custom!;
        var arguments = new object?[function.Arguments.Length];

        for (var i = 0; i < arguments.Length; i++)
        {
            if (custom.Parameters[i] == ExpressionType.Nodes)
            {
                arguments[i] = SelectNodes(function.Arguments[i], root, current, work).ConvertAll(n => n.Value);
            }
            else
            {
                // A function may read all of a value (lower, contains, ...): spend for it first
                var argument = Evaluate(function.Arguments[i], root, current, work);
                work.SpendValue(argument);
                arguments[i] = argument;
            }
        }

        // contains() compares the part at each position of the text: a long part costs more than reading the text
        if (custom.Test == PathFunction.StringTest.Contains && arguments is [ToonString text, ToonString part])
        {
            work.SpendMany(ContainsSteps(text.Value.Length, part.Value.Length));
        }

        return custom.Invoke(arguments);
    }

    /// <summary>The steps of looking for a part in a text, beyond reading the text: a step per 16 characters for each 64 of the part.</summary>
    internal static long ContainsSteps(int textLength, int partLength) => (long)(textLength / 16) * (partLength / 64);

    private static bool TestFunction(FunctionExpression function, ToonValue root, ToonValue current, EvaluationBudget work)
    {
        if (function.Function == FunctionKind.Custom)
        {
            return (bool)InvokeCustom(function, root, current, work)!;
        }

        if (Evaluate(function.Arguments[0], root, current, work) is not ToonString text)
        {
            return false;
        }

        work.SpendText(text.Value.Length);
        var full = function.Function == FunctionKind.Match;
        var pattern = function.HasLiteralPattern
            ? function.LiteralPattern
            : Evaluate(function.Arguments[1], root, current, work) is ToonString literal ? work.Pattern(literal.Value, full) : null;

        // A match takes time in proportion to the length of the text and the size of the pattern's automaton, and
        // cannot be stopped once it runs: spend for it first
        return pattern != null && work.RegexFor(pattern, text.Value.Length).IsMatch(text.Value);
    }

    /// <summary>The number of Unicode scalar values (a surrogate pair is one).</summary>
    internal static int CountScalars(string text)
    {
        var count = text.Length;

        for (var i = 0; i + 1 < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && char.IsLowSurrogate(text[i + 1]))
            {
                count--;
                i++;
            }
        }

        return count;
    }

    /// <summary>Compares two values, either of which may be Nothing (null), as §2.3.5.2.2 defines.</summary>
    internal static bool Compare(ToonValue? left, ComparisonOperator op, ToonValue? right)
    {
        return op switch
        {
            ComparisonOperator.Equal => AreEqual(left, right),
            ComparisonOperator.NotEqual => !AreEqual(left, right),
            ComparisonOperator.Less => IsLess(left, right),
            ComparisonOperator.LessOrEqual => IsLess(left, right) || AreEqual(left, right),
            ComparisonOperator.Greater => IsLess(right, left),
            _ => IsLess(right, left) || AreEqual(left, right)
        };
    }

    private static bool AreEqual(ToonValue? left, ToonValue? right) => left == null || right == null ? left == right : ToonValue.DeepEquals(left, right);

    private static bool IsLess(ToonValue? left, ToonValue? right)
    {
        return (left, right) switch
        {
            (ToonNumber a, ToonNumber b) => a.DecimalValue is { } x && b.DecimalValue is { } y ? x < y : a.Value < b.Value,
            (ToonString a, ToonString b) => CompareScalars(a.Value, b.Value) < 0,
            _ => false
        };
    }

    /// <summary>Compares strings by their Unicode scalar values (UTF-16 order differs for characters above U+E000).</summary>
    internal static int CompareScalars(string left, string right)
    {
        var length = Math.Min(left.Length, right.Length);

        for (var i = 0; i < length; i++)
        {
            if (left[i] != right[i])
            {
                return Rank(left[i]) - Rank(right[i]);
            }
        }

        return left.Length - right.Length;

        // Surrogates (pairs, above U+FFFF) sort after U+E000-U+FFFF
        static int Rank(char c) => c >= 0xE000 ? c - 0x800 : c >= 0xD800 ? c + 0x2000 : c;
    }

    #endregion
}
