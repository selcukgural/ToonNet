using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using ToonNet.Core.Models;

namespace ToonNet.Core.Paths;

/// <summary>Evaluates a compiled path over a <see cref="ToonValue"/> tree, with the semantics of RFC 9535.</summary>
internal static class ToonPathEvaluator
{
    /// <summary>A selected value and, when locations are tracked, where it is.</summary>
    internal readonly struct Node(ToonValue value, Location? location)
    {
        public readonly ToonValue Value = value;
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
        public static string Format(Location? location)
        {
            if (location == null)
            {
                return "$";
            }

            var chain = new List<Location>();

            for (var l = location; l != null; l = l.Parent)
            {
                chain.Add(l);
            }

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
    public static List<Node> Select(Query query, ToonValue root, ToonValue start, bool track)
    {
        var nodes = new List<Node>(1) { new(start, null) };

        foreach (var segment in query.Segments)
        {
            var next = new List<Node>(nodes.Count);

            foreach (var node in nodes)
            {
                if (segment.Descendant)
                {
                    SelectDescendants(segment.Selectors, node, root, track, next);
                }
                else
                {
                    foreach (var selector in segment.Selectors)
                    {
                        SelectChildren(selector, node, root, track, next);
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
    private static void SelectDescendants(Selector[] selectors, Node node, ToonValue root, bool track, List<Node> output)
    {
        foreach (var selector in selectors)
        {
            SelectChildren(selector, node, root, track, output);
        }

        switch (node.Value)
        {
            case ToonObject obj:
                RuntimeHelpers.EnsureSufficientExecutionStack();

                foreach (var (key, value) in obj.Properties)
                {
                    SelectDescendants(selectors, new Node(value, track ? new Location(node.Location, obj, key, -1) : null), root, track, output);
                }

                break;
            case ToonArray array:
                RuntimeHelpers.EnsureSufficientExecutionStack();

                for (var i = 0; i < array.Items.Count; i++)
                {
                    SelectDescendants(selectors, new Node(array.Items[i], track ? new Location(node.Location, array, null, i) : null), root, track, output);
                }

                break;
        }
    }

    private static void SelectChildren(Selector selector, Node node, ToonValue root, bool track, List<Node> output)
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
                            if (Test(selector.Filter!, root, value))
                            {
                                output.Add(new Node(value, track ? new Location(node.Location, filtered, key, -1) : null));
                            }
                        }

                        break;
                    case ToonArray items:
                        for (var i = 0; i < items.Items.Count; i++)
                        {
                            if (Test(selector.Filter!, root, items.Items[i]))
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
    internal static bool Test(PathExpression expression, ToonValue root, ToonValue current)
    {
        switch (expression)
        {
            case OrExpression or:
                foreach (var operand in or.Operands)
                {
                    if (Test(operand, root, current))
                    {
                        return true;
                    }
                }

                return false;
            case AndExpression and:
                foreach (var operand in and.Operands)
                {
                    if (!Test(operand, root, current))
                    {
                        return false;
                    }
                }

                return true;
            case NotExpression not:
                return !Test(not.Operand, root, current);
            case QueryExpression { Query: var query }:
                return query.IsSingular
                    ? SelectSingular(query, root, current) != null
                    : Select(query, root, query.Absolute ? root : current, track: false).Count != 0;
            case ComparisonExpression comparison:
                return Compare(Evaluate(comparison.Left, root, current), comparison.Operator, Evaluate(comparison.Right, root, current));
            case FunctionExpression function:
                return TestFunction(function, root, current);
            default:
                throw new InvalidOperationException("Not a logical expression.");
        }
    }

    /// <summary>Evaluates an expression of ValueType; null is Nothing (§2.4.1).</summary>
    private static ToonValue? Evaluate(PathExpression expression, ToonValue root, ToonValue current)
    {
        switch (expression)
        {
            case LiteralExpression literal:
                return literal.Value;
            case QueryExpression { Query: var query }:
                return SelectSingular(query, root, current);
            case FunctionExpression function:
                return EvaluateFunction(function, root, current);
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

                value = array.Items[index];
            }
        }

        return value;
    }

    private static List<Node> SelectNodes(PathExpression argument, ToonValue root, ToonValue current)
    {
        var query = ((QueryExpression)argument).Query;
        return Select(query, root, query.Absolute ? root : current, track: false);
    }

    private static ToonValue? EvaluateFunction(FunctionExpression function, ToonValue root, ToonValue current)
    {
        switch (function.Function)
        {
            case FunctionKind.Length:
                return Evaluate(function.Arguments[0], root, current) switch
                {
                    ToonString text => new ToonNumber((long)CountScalars(text.Value)),
                    ToonArray array => new ToonNumber((long)array.Items.Count),
                    ToonObject obj => new ToonNumber((long)obj.Properties.Count),
                    _ => null
                };
            case FunctionKind.Count:
                return new ToonNumber((long)SelectNodes(function.Arguments[0], root, current).Count);
            case FunctionKind.Value:
                return SelectNodes(function.Arguments[0], root, current) is [var single] ? single.Value : null;
            default:
                throw new InvalidOperationException("The function does not return a value.");
        }
    }

    private static bool TestFunction(FunctionExpression function, ToonValue root, ToonValue current)
    {
        if (Evaluate(function.Arguments[0], root, current) is not ToonString text)
        {
            return false;
        }

        var full = function.Function == FunctionKind.Match;
        var regex = function.HasLiteralRegex
            ? function.LiteralRegex
            : Evaluate(function.Arguments[1], root, current) is ToonString pattern ? ToonPathRegex.Get(pattern.Value, full) : null;

        return regex != null && regex.IsMatch(text.Value);
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
