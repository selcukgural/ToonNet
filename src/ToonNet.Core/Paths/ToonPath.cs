using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using ToonNet.Core.Models;

namespace ToonNet.Core.Paths;

/// <summary>
///     A compiled path that selects values in a document, with the syntax and semantics of JSONPath (RFC 9535):
///     <c>$.orders[?@.total &gt; 100].id</c>. TOON has the data model of JSON, so every JSONPath query works on TOON.
/// </summary>
/// <remarks>
///     <para>
///         The grammar is complete: member names (<c>.name</c>, <c>['name']</c>), indexes (<c>[0]</c>, <c>[-1]</c>),
///         wildcards, slices (<c>[1:5:2]</c>), several selectors in one bracket (<c>[0,'a']</c>), descendants
///         (<c>..name</c>) and filters (<c>[?@.price &lt; 10 &amp;&amp; @.tags]</c>) with the functions
///         <c>length()</c>, <c>count()</c>, <c>match()</c>, <c>search()</c> and <c>value()</c>.
///     </para>
///     <para>
///         A path is immutable and thread-safe. <see cref="Parse(string)"/> caches the paths it compiles (up to 512 characters),
///         so the methods that take path text (such as <see cref="ToonValue.Select(string)"/>) compile each path once.
///         Regular expressions run in linear time and spend for the size of their automaton, and <see cref="ToonPathOptions"/> bound the length and nesting of a path
///         (4,096 characters and 64 levels by default) and the work of an evaluation (a million steps plus 16 per value of
///         the document, beyond which it throws <see cref="ToonPathLimitException"/>), so a path from untrusted input
///         cannot make a query slow through a pattern, exhaust the stack, or multiply the work with nested descendant
///         segments. Give stricter options to <see cref="Parse(string, ToonPathOptions)"/> for paths from clients.
///     </para>
/// </remarks>
public sealed class ToonPath
{
    private const int MaxCacheSize = 1024;

    /// <summary>Longer paths are compiled each time, so the cache does not keep large texts.</summary>
    private const int MaxCachedLength = 512;

    private static readonly ConcurrentDictionary<string, ToonPath> Cache = new(StringComparer.Ordinal);

    private ToonPath(string text, Query query, ToonPathOptions options)
    {
        Text = text;
        Query = query;
        Options = options;
    }

    /// <summary>The options the path was compiled with (functions, limits), which also bound its evaluations.</summary>
    public ToonPathOptions Options { get; }

    /// <summary>The text of the path.</summary>
    public string Text { get; }

    internal Query Query { get; }

    /// <summary>The evaluator over text, created on first use.</summary>
    internal ToonPathTextEvaluator? TextEvaluator;

    /// <summary>
    ///     Whether the path selects at most one value: it has only member names and indexes (<c>$.a.b[0]</c>), no wildcard,
    ///     slice, filter, descendant segment or second selector in a bracket.
    /// </summary>
    public bool IsSingular => Query.IsSingular;

    /// <summary>Compiles a path with the default options, or returns the compiled path from the cache.</summary>
    /// <param name="path">The path, starting with <c>$</c>.</param>
    /// <returns>The compiled path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid, with the position of the error.</exception>
    public static ToonPath Parse(string path) => Parse(path, null);

    /// <summary>
    ///     Compiles a path with options on its length and nesting, which also bound its evaluations (see
    ///     <see cref="ToonPathOptions"/>); use stricter options for paths from untrusted input.
    /// </summary>
    /// <param name="path">The path, starting with <c>$</c>.</param>
    /// <param name="options">The options; <see cref="ToonPathOptions.Default"/> when null.</param>
    /// <returns>The compiled path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid or exceeds the options, with the position of the error.</exception>
    public static ToonPath Parse(string path, ToonPathOptions? options)
    {
        ArgumentNullException.ThrowIfNull(path);
        options ??= ToonPathOptions.Default;

        // Only paths with the default options are cached
        var cacheable = ReferenceEquals(options, ToonPathOptions.Default) && path.Length <= MaxCachedLength;

        if (cacheable && Cache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var compiled = new ToonPath(path, ToonPathParser.Parse(path, options), options);

        if (!cacheable)
        {
            return compiled;
        }

        if (Cache.Count >= MaxCacheSize)
        {
            Cache.Clear();
        }

        Cache.TryAdd(path, compiled);
        return compiled;
    }

    /// <summary>Compiles a path with the default options, without throwing when it is not valid.</summary>
    /// <param name="path">The path, starting with <c>$</c>.</param>
    /// <param name="result">The compiled path, or null.</param>
    /// <returns>Whether the path is valid.</returns>
    public static bool TryParse([NotNullWhen(true)] string? path, [NotNullWhen(true)] out ToonPath? result) => TryParse(path, null, out result);

    /// <summary>Compiles a path with options, without throwing when it is not valid or exceeds them.</summary>
    /// <param name="path">The path, starting with <c>$</c>.</param>
    /// <param name="options">The options; <see cref="ToonPathOptions.Default"/> when null.</param>
    /// <param name="result">The compiled path, or null.</param>
    /// <returns>Whether the path is valid within the options.</returns>
    public static bool TryParse([NotNullWhen(true)] string? path, ToonPathOptions? options, [NotNullWhen(true)] out ToonPath? result)
    {
        if (path == null)
        {
            result = null;
            return false;
        }

        try
        {
            result = Parse(path, options);
            return true;
        }
        catch (ToonPathException)
        {
            result = null;
            return false;
        }
    }

    /// <summary>Selects the values the path leads to, in the order RFC 9535 defines (document order for wildcards and descendants).</summary>
    /// <param name="root">The value <c>$</c> stands for, such as <see cref="ToonDocument.Root"/>.</param>
    /// <returns>The selected values; empty when there are none.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="root"/> is null.</exception>
    public IReadOnlyList<ToonValue> Select(ToonValue root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var nodes = ToonPathEvaluator.Select(Query, root, root, track: false, new EvaluationBudget(root, Options));
        var values = new ToonValue[nodes.Count];

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = nodes[i].Value;
        }

        return values;
    }

    /// <summary>
    ///     Selects the values the path leads to straight from TOON text, building only the selected values: what the path
    ///     does not select is skipped, not built.
    /// </summary>
    /// <param name="toon">The TOON text.</param>
    /// <param name="options">Parsing options; defaults when null.</param>
    /// <returns>The selected values, as <see cref="Select"/> gives them for the parsed document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toon"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid TOON.</exception>
    /// <remarks>
    ///     The whole text is read and checked. Segments with one selector are evaluated over the text; a segment with
    ///     several selectors, a negative slice step or a descendant segment builds the value it applies to. A filter that
    ///     only compares members (<c>[?@.total &gt; 100]</c>) reads just those members of each child. In non-strict mode,
    ///     and for filters with absolute queries (<c>$</c>), the document is parsed into a tree first.
    /// </remarks>
    public IReadOnlyList<ToonValue> SelectFrom(string toon, ToonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(toon);
        return SelectFromText(toon, options ?? ToonOptions.Default);
    }

    /// <summary>
    ///     Selects the values the path leads to straight from UTF-8 encoded TOON text, building only the selected values;
    ///     see <see cref="SelectFrom(string, ToonOptions)"/>.
    /// </summary>
    /// <param name="utf8Toon">The UTF-8 encoded TOON text; a byte order mark at the start is ignored.</param>
    /// <param name="options">Parsing options; defaults when null.</param>
    /// <returns>The selected values.</returns>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8 or not valid TOON.</exception>
    public IReadOnlyList<ToonValue> SelectFrom(ReadOnlySpan<byte> utf8Toon, ToonOptions? options = null)
    {
        using var text = Encoding.ToonTextEncoding.Decode(utf8Toon);
        return SelectFromText(text.Text, options ?? ToonOptions.Default);
    }

    private IReadOnlyList<ToonValue> SelectFromText(Parsing.ToonText text, ToonOptions options)
    {
        var sink = new TreeSink();
        Evaluate(text, options, sink);
        return sink.Values;
    }

    /// <summary>
    ///     Evaluates the path over text into a sink: with the parser's cursors in strict mode, on the parsed tree otherwise
    ///     (repeated keys keep their last value) or when a filter needs the whole document.
    /// </summary>
    internal void Evaluate(Parsing.ToonText text, ToonOptions options, ToonPathTextEvaluator.ISink sink)
    {
        var evaluator = ToonPathTextEvaluator.For(this);
        var parser = new Parsing.ToonParser(options);

        if (!options.StrictMode || evaluator.NeedsTree)
        {
            var root = parser.Parse(text).Root;
            var location = new ToonPathTextEvaluator.LocationStack();

            foreach (var node in ToonPathEvaluator.Select(Query, root, root, track: sink.WantsLocations, new EvaluationBudget(root, Options)))
            {
                sink.Add(node.Value, location, node.Location);
            }

            return;
        }

        parser.BeginDocument(text);

        try
        {
            evaluator.Evaluate(parser, sink, new EvaluationBudget(text, Options));
        }
        finally
        {
            parser.ReleaseDocument();
        }
    }

    /// <summary>Collects the selected values as trees.</summary>
    private sealed class TreeSink : ToonPathTextEvaluator.ISink
    {
        public readonly List<ToonValue> Values = [];

        public bool WantsLocations => false;

        public void Add(Parsing.ToonParser parser, in Parsing.ToonParser.ValueRef value, ToonPathTextEvaluator.LocationStack location) =>
            Values.Add(parser.BuildValue(value));

        public void Add(ToonValue tree, ToonPathTextEvaluator.LocationStack location, ToonPathEvaluator.Location? relative) => Values.Add(tree);
    }

    /// <summary>Selects the values the path leads to with their locations, as normalized paths (<c>$['orders'][2]</c>).</summary>
    /// <param name="root">The value <c>$</c> stands for.</param>
    /// <returns>The selected values and their normalized paths.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="root"/> is null.</exception>
    public IReadOnlyList<ToonPathMatch> SelectMatches(ToonValue root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var work = new EvaluationBudget(root, Options);
        var nodes = ToonPathEvaluator.Select(Query, root, root, track: true, work);
        var matches = new ToonPathMatch[nodes.Count];

        for (var i = 0; i < matches.Length; i++)
        {
            matches[i] = new ToonPathMatch(ToonPathEvaluator.Location.Format(nodes[i].Location, work), nodes[i].Value);
        }

        return matches;
    }

    /// <summary>Selects the one value the path leads to.</summary>
    /// <param name="root">The value <c>$</c> stands for.</param>
    /// <returns>The value, or null when the path selects none.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="root"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the path selects more than one value.</exception>
    public ToonValue? SelectSingle(ToonValue root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var nodes = ToonPathEvaluator.Select(Query, root, root, track: false, new EvaluationBudget(root, Options));

        return nodes.Count switch
        {
            0 => null,
            1 => nodes[0].Value,
            _ => throw new InvalidOperationException($"The path '{Text}' selects {nodes.Count} values, not one.")
        };
    }

    /// <summary>
    ///     Sets the value at a singular path (member names and indexes only), creating the objects and arrays on the way
    ///     that are missing: a missing member becomes an object, or an array when an index follows it. An index equal to
    ///     the length of the array appends.
    /// </summary>
    /// <param name="root">The value <c>$</c> stands for.</param>
    /// <param name="value">The value to set; null sets <see cref="ToonNull.Instance"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="root"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the path is not singular or is only <c>$</c>, when a value on the way is not the object or array the
    ///     path needs, or when an index is beyond the end of its array.
    /// </exception>
    public void SetValue(ToonValue root, ToonValue? value)
    {
        ArgumentNullException.ThrowIfNull(root);

        if (!IsSingular)
        {
            throw new InvalidOperationException($"The path '{Text}' can select several values; setting needs member names and indexes only.");
        }

        var segments = Query.Segments;

        if (segments.Length == 0)
        {
            throw new InvalidOperationException("The root cannot be replaced; set a path below it.");
        }

        // The first pass only checks, on detached new containers, so a path that fails halfway leaves the tree unchanged.
        Walk(apply: false);
        Walk(apply: true);

        void Walk(bool apply)
        {
            var container = root;

            for (var i = 0; i < segments.Length; i++)
            {
                var selector = segments[i].Selectors[0];
                var last = i == segments.Length - 1;
                ToonValue? next = last ? value ?? ToonNull.Instance : null;

                if (selector.Kind == SelectorKind.Name)
                {
                    if (container is not ToonObject obj)
                    {
                        throw NotA(container, "an object", i);
                    }

                    if (!last && obj.Properties.TryGetValue(selector.Name!, out var existing))
                    {
                        container = existing;
                        continue;
                    }

                    next ??= CreateFor(segments[i + 1]);

                    if (apply)
                    {
                        obj.Properties[selector.Name!] = next;
                    }

                    container = next;
                }
                else
                {
                    if (container is not ToonArray array)
                    {
                        throw NotA(container, "an array", i);
                    }

                    var count = array.Items.Count;
                    var index = selector.Index < 0 ? count + selector.Index : selector.Index;

                    if (index < 0 || index > count)
                    {
                        throw new InvalidOperationException(
                            $"The index {selector.Index} of the path '{Text}' is outside the array of {count} item{(count == 1 ? "" : "s")}.");
                    }

                    if (index < count)
                    {
                        if (!last)
                        {
                            container = array.Items[(int)index];
                        }
                        else if (apply)
                        {
                            array.Items[(int)index] = next!;
                        }

                        continue;
                    }

                    next ??= CreateFor(segments[i + 1]);

                    if (apply)
                    {
                        array.Items.Add(next);
                    }

                    container = next;
                }
            }
        }

        static ToonValue CreateFor(Segment segment) => segment.Selectors[0].Kind == SelectorKind.Name ? new ToonObject() : new ToonArray();
    }

    private InvalidOperationException NotA(ToonValue value, string kind, int segment) =>
        new($"The value at segment {segment + 1} of the path '{Text}' is {ToonValue.DescribeKind(value.ValueType)}, not {kind}.");

    /// <summary>Removes every value the path selects from the object or array it is in.</summary>
    /// <param name="root">The value <c>$</c> stands for.</param>
    /// <returns>The number of values removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="root"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the path selects the root itself.</exception>
    public int RemoveAll(ToonValue root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var nodes = ToonPathEvaluator.Select(Query, root, root, track: true, new EvaluationBudget(root, Options));

        if (nodes.Count == 0)
        {
            return 0;
        }

        // Members by key and items by index, of each container at once: removing them one by one shifts the rest each time
        var members = new Dictionary<ToonObject, HashSet<string>>(ReferenceEqualityComparer.Instance);
        var items = new Dictionary<ToonArray, HashSet<int>>(ReferenceEqualityComparer.Instance);
        var removed = 0;

        foreach (var node in nodes)
        {
            switch (node.Location)
            {
                case null:
                    throw new InvalidOperationException("The root cannot be removed.");
                case { Container: ToonObject obj, Name: { } name }:
                    if (!members.TryGetValue(obj, out var keys))
                    {
                        members[obj] = keys = new HashSet<string>(StringComparer.Ordinal);
                    }

                    keys.Add(name);
                    break;
                case { Container: ToonArray array, Index: var index }:
                    if (!items.TryGetValue(array, out var indexes))
                    {
                        items[array] = indexes = [];
                    }

                    indexes.Add(index);
                    break;
            }
        }

        foreach (var (obj, keys) in members)
        {
            removed += obj.Properties.RemoveKeys(keys);
        }

        foreach (var (array, indexes) in items)
        {
            var list = array.Items;
            var write = 0;

            for (var read = 0; read < list.Count; read++)
            {
                if (!indexes.Contains(read))
                {
                    list[write++] = list[read];
                }
            }

            removed += list.Count - write;
            list.RemoveRange(write, list.Count - write);
        }

        return removed;
    }

    /// <summary>Returns the text of the path.</summary>
    public override string ToString() => Text;
}

/// <summary>A value a path selected, and where it is in the document.</summary>
/// <param name="Path">The normalized path of the value (RFC 9535 §2.7), such as <c>$['orders'][2]['id']</c>.</param>
/// <param name="Value">The value.</param>
public readonly record struct ToonPathMatch(string Path, ToonValue Value);
