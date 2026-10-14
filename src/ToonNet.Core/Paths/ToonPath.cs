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
///         A path is immutable and thread-safe. <see cref="Parse"/> caches the paths it compiles, so the methods that take
///         path text (such as <see cref="ToonValue.Select(string)"/>) compile each path once. Regular expressions run in
///         linear time, and nesting is limited, so a path from untrusted input cannot make evaluation slow through a
///         pattern or exhaust the stack; the work of a query still grows with the document and with nested descendant
///         filters.
///     </para>
/// </remarks>
public sealed class ToonPath
{
    private const int MaxCacheSize = 1024;

    private static readonly ConcurrentDictionary<string, ToonPath> Cache = new(StringComparer.Ordinal);

    private ToonPath(string text, Query query)
    {
        Text = text;
        Query = query;
    }

    /// <summary>The text of the path.</summary>
    public string Text { get; }

    internal Query Query { get; }

    /// <summary>
    ///     Whether the path selects at most one value: it has only member names and indexes (<c>$.a.b[0]</c>), no wildcard,
    ///     slice, filter, descendant segment or second selector in a bracket.
    /// </summary>
    public bool IsSingular => Query.IsSingular;

    /// <summary>Compiles a path, or returns the compiled path from the cache.</summary>
    /// <param name="path">The path, starting with <c>$</c>.</param>
    /// <returns>The compiled path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid, with the position of the error.</exception>
    public static ToonPath Parse(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Cache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var compiled = new ToonPath(path, ToonPathParser.Parse(path));

        if (Cache.Count >= MaxCacheSize)
        {
            Cache.Clear();
        }

        Cache.TryAdd(path, compiled);
        return compiled;
    }

    /// <summary>Compiles a path, without throwing when it is not valid.</summary>
    /// <param name="path">The path, starting with <c>$</c>.</param>
    /// <param name="result">The compiled path, or null.</param>
    /// <returns>Whether the path is valid.</returns>
    public static bool TryParse([NotNullWhen(true)] string? path, [NotNullWhen(true)] out ToonPath? result)
    {
        if (path == null)
        {
            result = null;
            return false;
        }

        try
        {
            result = Parse(path);
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
        var nodes = ToonPathEvaluator.Select(Query, root, root, track: false);
        var values = new ToonValue[nodes.Count];

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = nodes[i].Value;
        }

        return values;
    }

    /// <summary>Selects the values the path leads to with their locations, as normalized paths (<c>$['orders'][2]</c>).</summary>
    /// <param name="root">The value <c>$</c> stands for.</param>
    /// <returns>The selected values and their normalized paths.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="root"/> is null.</exception>
    public IReadOnlyList<ToonPathMatch> SelectMatches(ToonValue root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var nodes = ToonPathEvaluator.Select(Query, root, root, track: true);
        var matches = new ToonPathMatch[nodes.Count];

        for (var i = 0; i < matches.Length; i++)
        {
            matches[i] = new ToonPathMatch(ToonPathEvaluator.Location.Format(nodes[i].Location), nodes[i].Value);
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
        var nodes = ToonPathEvaluator.Select(Query, root, root, track: false);

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
                obj.Properties[selector.Name!] = next;
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
                    if (last)
                    {
                        array.Items[(int)index] = next!;
                    }
                    else
                    {
                        container = array.Items[(int)index];
                    }

                    continue;
                }

                next ??= CreateFor(segments[i + 1]);
                array.Items.Add(next);
                container = next;
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
        var nodes = ToonPathEvaluator.Select(Query, root, root, track: true);

        if (nodes.Count == 0)
        {
            return 0;
        }

        // Members by key; items by index, from the last, so earlier indexes stay valid
        var items = new Dictionary<ToonArray, SortedSet<int>>(ReferenceEqualityComparer.Instance);
        var removed = 0;

        foreach (var node in nodes)
        {
            switch (node.Location)
            {
                case null:
                    throw new InvalidOperationException("The root cannot be removed.");
                case { Container: ToonObject obj, Name: { } name }:
                    if (obj.Properties.Remove(name))
                    {
                        removed++;
                    }

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

        foreach (var (array, indexes) in items)
        {
            foreach (var index in indexes.Reverse())
            {
                array.Items.RemoveAt(index);
                removed++;
            }
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
