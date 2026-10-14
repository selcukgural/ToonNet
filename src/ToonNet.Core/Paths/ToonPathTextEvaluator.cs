using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Core.Paths;

/// <summary>
///     Evaluates a path over TOON text with the parser's cursors, without building the document: what no segment selects
///     is skipped, and only the selected values are handed to a sink (built as trees, or read into objects).
/// </summary>
/// <remarks>
///     <para>
///         The parser reads forward, so a segment is evaluated over the text when its single selector visits children in
///         document order: a name, an index, a wildcard, a slice with a positive step, or a filter. Other segments
///         (several selectors, which RFC 9535 orders by selector, negative steps, descendants) build the tree of the
///         value they apply to and evaluate the rest of the path on it.
///     </para>
///     <para>
///         A filter whose queries are relative member names (<c>@.total</c>, <c>@.ship.city</c>) is tested on a partial
///         tree of only those members; the parser then returns to the child (a checkpoint) to read it if it matched. Other
///         filters are tested on the child's tree. Filters with absolute queries (<c>$</c>) need the whole document and
///         are not evaluated over text.
///     </para>
/// </remarks>
internal sealed class ToonPathTextEvaluator
{
    /// <summary>Receives the selected values: in the text (with the parser on them), or as trees.</summary>
    internal interface ISink
    {
        /// <summary>Whether the sink reports where values are (for errors); trees are then evaluated with locations.</summary>
        bool WantsLocations { get; }

        void Add(ToonParser parser, in ToonParser.ValueRef value, LocationStack location);

        /// <summary>A value selected in a tree, at <paramref name="relative"/> below <paramref name="location"/> (when locations are wanted).</summary>
        void Add(ToonValue tree, LocationStack location, ToonPathEvaluator.Location? relative);
    }

    /// <summary>Where the walk is in the document: the member names and indexes from the root.</summary>
    internal sealed class LocationStack
    {
        private readonly List<(string? Name, int Index)> _steps = [];

        public void Push(string name) => _steps.Add((name, -1));

        public void Push(int index) => _steps.Add((null, index));

        public void Pop() => _steps.RemoveAt(_steps.Count - 1);

        /// <summary>The path as errors give it (<c>$.data.orders[2]</c>), followed by a location in a tree below it.</summary>
        public string Format(ToonPathEvaluator.Location? relative = null)
        {
            var text = new System.Text.StringBuilder("$");

            foreach (var (name, index) in _steps)
            {
                Append(text, name, index);
            }

            var chain = new List<ToonPathEvaluator.Location>();

            for (var l = relative; l != null; l = l.Parent)
            {
                chain.Add(l);
            }

            for (var i = chain.Count - 1; i >= 0; i--)
            {
                Append(text, chain[i].Name, chain[i].Index);
            }

            return text.ToString();

            static void Append(System.Text.StringBuilder text, string? name, int index)
            {
                if (name != null)
                {
                    text.Append('.').Append(name);
                }
                else
                {
                    text.Append('[').Append(index.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(']');
                }
            }
        }
    }

    /// <summary>The members a filter reads from the current node: for each name, the whole value, or members below it.</summary>
    private sealed class MemberTree
    {
        public bool Whole;

        /// <summary>The names of the members read (a few, so they are searched in order), and what is read below each.</summary>
        public string[] Names = [];

        public MemberTree[] Below = [];

        public MemberTree? Find(string name)
        {
            for (var i = 0; i < Names.Length; i++)
            {
                if (string.Equals(Names[i], name))
                {
                    return Below[i];
                }
            }

            return null;
        }

        public MemberTree GetOrAdd(string name)
        {
            if (Find(name) is { } existing)
            {
                return existing;
            }

            var added = new MemberTree();
            Names = [.. Names, name];
            Below = [.. Below, added];
            return added;
        }
    }

    private readonly Segment[] _segments;

    /// <summary>For each segment index, the rest of the path as a relative query (evaluated on a tree).</summary>
    private readonly Query[] _rests;

    /// <summary>For each filter selector, the members its expression reads, or null when it needs the whole child.</summary>
    private readonly Dictionary<Selector, MemberTree?> _filterMembers = new(ReferenceEqualityComparer.Instance);

    /// <summary>For each filter selector that only compares members with literals, the filter tested on tokens.</summary>
    private readonly Dictionary<Selector, ToonPathTokenFilter> _tokenFilters = new(ReferenceEqualityComparer.Instance);

    private ToonPathTextEvaluator(ToonPath path)
    {
        _segments = path.Query.Segments;
        _rests = new Query[_segments.Length + 1];

        for (var i = 0; i <= _segments.Length; i++)
        {
            _rests[i] = new Query(absolute: false, _segments[i..]);
        }

        foreach (var segment in _segments)
        {
            foreach (var selector in segment.Selectors)
            {
                if (selector.Kind == SelectorKind.Filter)
                {
                    _filterMembers[selector] = MembersOf(selector.Filter!);
                    NeedsTree |= HasAbsoluteQuery(selector.Filter!);

                    if (ToonPathTokenFilter.TryCreate(selector.Filter!) is { } tokenFilter)
                    {
                        _tokenFilters[selector] = tokenFilter;
                    }
                }
            }
        }
    }

    /// <summary>Whether a filter has an absolute query, which needs the whole document: the path is then evaluated on its tree.</summary>
    public bool NeedsTree { get; }

    /// <summary>The evaluator of a path, created once per path.</summary>
    internal static ToonPathTextEvaluator For(ToonPath path) => path.TextEvaluator ??= new ToonPathTextEvaluator(path);

    /// <summary>The state of one evaluation (the evaluator itself is shared).</summary>
    private sealed class Walker(ToonParser parser, ISink sink, int segments)
    {
        public readonly ToonParser Parser = parser;
        public readonly ISink Sink = sink;
        public readonly LocationStack Location = new();

        /// <summary>
        ///     Per segment, the object the members a filter reads are put in, reused for every child (the filter only reads
        ///     it; a child that matches is read again).
        /// </summary>
        public readonly ToonObject?[] Partials = new ToonObject?[segments];

        /// <summary>For token filters: where the members it reads are, and which the child has (reused).</summary>
        public ToonParser.ValueRef[] MemberValues = [];

        public bool[] MembersFound = [];
    }

    /// <summary>Evaluates the path over the document the parser has begun, and checks that nothing follows the root.</summary>
    public void Evaluate(ToonParser parser, ISink sink)
    {
        var walker = new Walker(parser, sink, _segments.Length);
        Walk(walker, parser.ReadRoot(), 0);
        parser.EndDocument();
    }

    private void Walk(Walker walker, in ToonParser.ValueRef value, int index)
    {
        var parser = walker.Parser;

        if (index == _segments.Length)
        {
            walker.Sink.Add(parser, value, walker.Location);
            return;
        }

        var segment = _segments[index];

        if (segment.Descendant || segment.Selectors.Length != 1 || segment.Selectors[0] is { Kind: SelectorKind.Slice, Step: <= 0 })
        {
            WalkTree(walker, parser.BuildValue(value), index);
            return;
        }

        var selector = segment.Selectors[0];

        switch (value.NodeKind)
        {
            case ToonNodeKind.Object:
            {
                if (selector.Kind is SelectorKind.Index or SelectorKind.Slice)
                {
                    parser.Skip(value);
                    return;
                }

                var cursor = parser.BeginObject(value);

                while (parser.NextProperty(ref cursor, out var key, out var member))
                {
                    if (selector.Kind == SelectorKind.Name && key != selector.Name)
                    {
                        parser.Skip(member);
                        continue;
                    }

                    walker.Location.Push(key);

                    if (selector.Kind == SelectorKind.Filter)
                    {
                        Filter(walker, member, selector, index);
                    }
                    else
                    {
                        Walk(walker, member, index + 1);
                    }

                    walker.Location.Pop();
                }

                return;
            }
            case ToonNodeKind.Array:
            {
                if (selector.Kind == SelectorKind.Name)
                {
                    parser.Skip(value);
                    return;
                }

                // Negative indexes and slice bounds count from the declared length, which strict mode holds the array to
                var length = value.Kind == ToonParser.RefKind.HeaderArray ? parser.HeaderOf(value.HeaderIndex).Length : 0;
                var position = selector.Kind == SelectorKind.Index ? ToonPathEvaluator.ResolveIndex(selector.Index, length) : -1;
                var (start, end) = selector.Kind == SelectorKind.Slice ? SliceBounds(selector, length) : (0L, 0L);
                var cursor = parser.BeginArray(value);

                for (var i = 0; parser.NextItem(ref cursor, out var item); i++)
                {
                    var selected = selector.Kind switch
                    {
                        SelectorKind.Index => i == position,
                        SelectorKind.Slice => i >= start && i < end && (i - start) % selector.Step == 0,
                        _ => true
                    };

                    if (!selected)
                    {
                        parser.Skip(item);
                        continue;
                    }

                    walker.Location.Push(i);

                    if (selector.Kind == SelectorKind.Filter)
                    {
                        Filter(walker, item, selector, index);
                    }
                    else
                    {
                        Walk(walker, item, index + 1);
                    }

                    walker.Location.Pop();
                }

                return;
            }
            default:
                // A primitive has no children
                parser.Skip(value);
                return;
        }
    }

    /// <summary>The range of indexes a slice with a positive step covers (§2.3.4.2.2).</summary>
    private static (long Start, long End) SliceBounds(Selector slice, long length)
    {
        var start = slice.Start ?? 0;
        var end = slice.End ?? length;
        start = Math.Min(Math.Max(start >= 0 ? start : length + start, 0), length);
        end = Math.Min(Math.Max(end >= 0 ? end : length + end, 0), length);
        return (start, end);
    }

    /// <summary>Tests a child against a filter and walks the rest of the path from it when it matches.</summary>
    private void Filter(Walker walker, in ToonParser.ValueRef child, Selector selector, int index)
    {
        var parser = walker.Parser;

        if (_tokenFilters.TryGetValue(selector, out var tokenFilter))
        {
            var tokenCheckpoint = parser.Save(child);

            if (TestTokens(walker, child, tokenFilter))
            {
                parser.Restore(tokenCheckpoint);
                Walk(walker, child, index + 1);
            }

            return;
        }

        var members = _filterMembers[selector];

        if (members == null)
        {
            var tree = parser.BuildValue(child);

            if (ToonPathEvaluator.Test(selector.Filter!, tree, tree))
            {
                WalkTree(walker, tree, index + 1);
            }

            return;
        }

        // Read only what the filter reads; read the child again when it matched
        var checkpoint = parser.Save(child);
        ToonValue partial;

        if (child.NodeKind == ToonNodeKind.Object)
        {
            var obj = walker.Partials[index] ??= new ToonObject();
            obj.Properties.Clear();
            AddMembers(parser, child, members, obj);
            partial = obj;
        }
        else
        {
            partial = BuildMembers(parser, child, members);
        }

        if (ToonPathEvaluator.Test(selector.Filter!, partial, partial))
        {
            parser.Restore(checkpoint);
            Walk(walker, child, index + 1);
        }
    }

    /// <summary>Reads past a child, noting where the members a token filter reads are, and tests the filter.</summary>
    private static bool TestTokens(Walker walker, in ToonParser.ValueRef child, ToonPathTokenFilter filter)
    {
        var parser = walker.Parser;
        var names = filter.Members;

        if (walker.MemberValues.Length < names.Length)
        {
            walker.MemberValues = new ToonParser.ValueRef[names.Length];
            walker.MembersFound = new bool[names.Length];
        }

        var values = walker.MemberValues.AsSpan(0, names.Length);
        var found = walker.MembersFound.AsSpan(0, names.Length);
        found.Clear();

        if (child.NodeKind == ToonNodeKind.Object)
        {
            var cursor = parser.BeginObject(child);

            while (parser.NextProperty(ref cursor, out var key, out var member))
            {
                for (var i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], key))
                    {
                        values[i] = member;
                        found[i] = true;
                        break;
                    }
                }

                // Checks the member like reading it would; a primitive's token stays in the text
                parser.Skip(member);
            }
        }
        else
        {
            // A primitive or an array has no members
            parser.Skip(child);
        }

        return filter.Test(parser, values, found);
    }

    /// <summary>A tree of the members a filter reads (the rest is skipped); a primitive or an array stands for itself.</summary>
    private static ToonValue BuildMembers(ToonParser parser, in ToonParser.ValueRef value, MemberTree members)
    {
        switch (value.NodeKind)
        {
            case ToonNodeKind.Object:
            {
                var obj = new ToonObject();
                AddMembers(parser, value, members, obj);
                return obj;
            }
            case ToonNodeKind.Array:
                // Member names select nothing in an array
                parser.Skip(value);
                return new ToonArray();
            default:
                return parser.ReadPrimitive(value);
        }
    }

    /// <summary>Reads the members of an object a filter reads into <paramref name="obj"/>, skipping the others.</summary>
    private static void AddMembers(ToonParser parser, in ToonParser.ValueRef value, MemberTree members, ToonObject obj)
    {
        var cursor = parser.BeginObject(value);

        while (parser.NextProperty(ref cursor, out var key, out var member))
        {
            if (members.Find(key) is not { } below)
            {
                parser.Skip(member);
            }
            else if (below.Whole)
            {
                obj.Properties.Add(key, parser.BuildValue(member));
            }
            else
            {
                obj.Properties.Add(key, BuildMembers(parser, member, below));
            }
        }
    }

    /// <summary>Evaluates the rest of the path, from <paramref name="index"/>, on a tree.</summary>
    private void WalkTree(Walker walker, ToonValue tree, int index)
    {
        foreach (var node in ToonPathEvaluator.Select(_rests[index], tree, tree, track: walker.Sink.WantsLocations))
        {
            walker.Sink.Add(node.Value, walker.Location, node.Location);
        }
    }

    #region Filter analysis

    private static bool HasAbsoluteQuery(PathExpression expression) => expression switch
    {
        QueryExpression { Query: var query } => query.Absolute || query.Segments.Any(s => s.Selectors.Any(x => x.Filter != null && HasAbsoluteQuery(x.Filter))),
        FunctionExpression function => function.Arguments.Any(HasAbsoluteQuery),
        ComparisonExpression comparison => HasAbsoluteQuery(comparison.Left) || HasAbsoluteQuery(comparison.Right),
        AndExpression and => and.Operands.Any(HasAbsoluteQuery),
        OrExpression or => or.Operands.Any(HasAbsoluteQuery),
        NotExpression not => HasAbsoluteQuery(not.Operand),
        _ => false
    };

    /// <summary>
    ///     The members a filter reads when all its queries are relative member names (<c>@.a.b</c>), or null when it reads
    ///     the current node in other ways (<c>@</c>, <c>@.*</c>, <c>@[0]</c>, <c>@..a</c>).
    /// </summary>
    private static MemberTree? MembersOf(PathExpression filter)
    {
        var root = new MemberTree();
        return Collect(filter, root) ? root : null;

        static bool Collect(PathExpression expression, MemberTree root)
        {
            switch (expression)
            {
                case LiteralExpression:
                    return true;
                case QueryExpression { Query: var query }:
                {
                    if (query.Absolute || query.Segments.Length == 0 ||
                        !Array.TrueForAll(query.Segments, s => s is { Descendant: false, Selectors: [{ Kind: SelectorKind.Name }] }))
                    {
                        return false;
                    }

                    var node = root;

                    foreach (var segment in query.Segments)
                    {
                        node = node.GetOrAdd(segment.Selectors[0].Name!);
                    }

                    node.Whole = true;
                    return true;
                }
                case FunctionExpression function:
                    return Array.TrueForAll(function.Arguments, a => Collect(a, root));
                case ComparisonExpression comparison:
                    return Collect(comparison.Left, root) && Collect(comparison.Right, root);
                case AndExpression and:
                    return Array.TrueForAll(and.Operands, o => Collect(o, root));
                case OrExpression or:
                    return Array.TrueForAll(or.Operands, o => Collect(o, root));
                case NotExpression not:
                    return Collect(not.Operand, root);
                default:
                    return false;
            }
        }
    }

    #endregion
}
