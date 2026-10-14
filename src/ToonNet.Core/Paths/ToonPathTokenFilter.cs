using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.Core.Paths;

/// <summary>
///     A filter tested on the tokens of a child's members, without building any value: for filters made of comparisons
///     between a member and a literal (<c>@.total &gt; 100</c>, <c>'eu' == @.region</c>), member tests (<c>@.paid</c>) and
///     <c>&amp;&amp;</c>, <c>||</c>, <c>!</c>. It gives what <see cref="ToonPathEvaluator.Test"/> gives on the child's tree.
/// </summary>
internal sealed class ToonPathTokenFilter
{
    private abstract class Node;

    private sealed class And(Node[] operands) : Node
    {
        public readonly Node[] Operands = operands;
    }

    private sealed class Or(Node[] operands) : Node
    {
        public readonly Node[] Operands = operands;
    }

    private sealed class Not(Node operand) : Node
    {
        public readonly Node Operand = operand;
    }

    private sealed class Exists(int member) : Node
    {
        public readonly int Member = member;
    }

    /// <summary><c>@.member op literal</c> (a literal on the left is turned around).</summary>
    private sealed class Compare(int member, ComparisonOperator op, ToonValue literal) : Node
    {
        public readonly int Member = member;
        public readonly ComparisonOperator Operator = op;
        public readonly ToonValue Literal = literal;
    }

    private readonly Node _root;

    private ToonPathTokenFilter(string[] members, Node root)
    {
        Members = members;
        _root = root;
    }

    /// <summary>The names of the members the filter reads.</summary>
    public string[] Members { get; }

    /// <summary>The filter for an expression, or null when it reads more than members compared with literals.</summary>
    public static ToonPathTokenFilter? TryCreate(PathExpression expression)
    {
        var members = new List<string>();
        var root = Convert(expression, members);
        return root == null ? null : new ToonPathTokenFilter(members.ToArray(), root);
    }

    private static Node? Convert(PathExpression expression, List<string> members)
    {
        switch (expression)
        {
            case AndExpression and:
                return ConvertAll(and.Operands, members) is { } andOperands ? new And(andOperands) : null;
            case OrExpression or:
                return ConvertAll(or.Operands, members) is { } orOperands ? new Or(orOperands) : null;
            case NotExpression not:
                return Convert(not.Operand, members) is { } operand ? new Not(operand) : null;
            case QueryExpression query when MemberOf(query, members) is var member and >= 0:
                return new Exists(member);
            case ComparisonExpression { Left: QueryExpression left, Right: LiteralExpression literal } comparison
                when MemberOf(left, members) is var leftMember and >= 0:
                return new Compare(leftMember, comparison.Operator, literal.Value);
            case ComparisonExpression { Left: LiteralExpression literal, Right: QueryExpression right } comparison
                when MemberOf(right, members) is var rightMember and >= 0:
                return new Compare(rightMember, Mirror(comparison.Operator), literal.Value);
            default:
                return null;
        }
    }

    private static Node[]? ConvertAll(PathExpression[] expressions, List<string> members)
    {
        var nodes = new Node[expressions.Length];

        for (var i = 0; i < nodes.Length; i++)
        {
            if (Convert(expressions[i], members) is not { } node)
            {
                return null;
            }

            nodes[i] = node;
        }

        return nodes;
    }

    /// <summary>The index of the member of <c>@.name</c> in <paramref name="members"/> (added if new), or -1 for other queries.</summary>
    private static int MemberOf(QueryExpression expression, List<string> members)
    {
        if (expression.Query is not { Absolute: false, Segments: [{ Descendant: false, Selectors: [{ Kind: SelectorKind.Name, Name: { } name }] }] })
        {
            return -1;
        }

        var index = members.IndexOf(name);

        if (index < 0)
        {
            members.Add(name);
            index = members.Count - 1;
        }

        return index;
    }

    /// <summary>The operator with its sides swapped: <c>1 &lt; @.a</c> is <c>@.a &gt; 1</c>.</summary>
    private static ComparisonOperator Mirror(ComparisonOperator op) => op switch
    {
        ComparisonOperator.Less => ComparisonOperator.Greater,
        ComparisonOperator.LessOrEqual => ComparisonOperator.GreaterOrEqual,
        ComparisonOperator.Greater => ComparisonOperator.Less,
        ComparisonOperator.GreaterOrEqual => ComparisonOperator.LessOrEqual,
        _ => op
    };

    /// <summary>
    ///     Tests the members of a child: <paramref name="found"/> tells which members it has, <paramref name="values"/>
    ///     where their values are (primitives as tokens; objects and arrays were skipped).
    /// </summary>
    public bool Test(ToonParser parser, ReadOnlySpan<ToonParser.ValueRef> values, ReadOnlySpan<bool> found) => Test(_root, parser, values, found);

    private static bool Test(Node node, ToonParser parser, ReadOnlySpan<ToonParser.ValueRef> values, ReadOnlySpan<bool> found)
    {
        switch (node)
        {
            case And and:
                foreach (var operand in and.Operands)
                {
                    if (!Test(operand, parser, values, found))
                    {
                        return false;
                    }
                }

                return true;
            case Or or:
                foreach (var operand in or.Operands)
                {
                    if (Test(operand, parser, values, found))
                    {
                        return true;
                    }
                }

                return false;
            case Not not:
                return !Test(not.Operand, parser, values, found);
            case Exists exists:
                return found[exists.Member];
            default:
            {
                var compare = (Compare)node;

                if (!found[compare.Member])
                {
                    // Nothing: only != holds against a literal (§2.3.5.2.2)
                    return compare.Operator == ComparisonOperator.NotEqual;
                }

                ref readonly var value = ref values[compare.Member];

                if (value.NodeKind != ToonNodeKind.Primitive)
                {
                    // An object or array never equals a primitive literal and has no order
                    return compare.Operator == ComparisonOperator.NotEqual;
                }

                return CompareToken(parser, value, compare.Operator, compare.Literal);
            }
        }
    }

    /// <summary>Compares a primitive token with a literal as <see cref="ToonPathEvaluator.Compare"/> compares their values.</summary>
    private static bool CompareToken(ToonParser parser, in ToonParser.ValueRef value, ComparisonOperator op, ToonValue literal)
    {
        var token = parser.TokenOf(value);
        int? order; // the token's order against the literal: <0, 0, >0, or null when not comparable by order
        bool equal;

        if (token.Length > 0 && token[0] == '"')
        {
            if (literal is not ToonString text)
            {
                return op == ComparisonOperator.NotEqual;
            }

            var inner = token[1..^1];
            order = inner.Contains('\\')
                ? ToonPathEvaluator.CompareScalars(parser.ReadQuotedString(value), text.Value)
                : CompareScalars(inner, text.Value);
            equal = order == 0;
        }
        else if (token.SequenceEqual("true") || token.SequenceEqual("false"))
        {
            equal = literal is ToonBoolean boolean && boolean.Value == (token[0] == 't');
            order = null;
        }
        else if (token.SequenceEqual("null"))
        {
            equal = literal is ToonNull;
            order = null;
        }
        else if (TryParseShortNumber(token, out var shortNumber) && literal is ToonNumber shortOther && shortNumber != shortOther.Value)
        {
            // Rounding to double keeps order (a <= b gives double(a) <= double(b)), so different doubles decide the
            // comparison of the exact values; equal doubles are compared exactly below
            order = shortNumber < shortOther.Value ? -1 : 1;
            equal = false;
        }
        else if (ToonParser.TryParseNumberParts(token, out var number, out var exact, out var hasExact))
        {
            if (literal is not ToonNumber other)
            {
                return op == ComparisonOperator.NotEqual;
            }

            if (hasExact && other.DecimalValue is { } otherExact)
            {
                order = exact.CompareTo(otherExact);
                equal = order == 0;
            }
            else
            {
                // Beyond decimal: compare as the tree does
                var tree = (ToonNumber)parser.ReadPrimitive(value);
                equal = ToonValue.NumbersEqual(tree, other);
                order = equal ? 0 : number.CompareTo(other.Value);
            }
        }
        else
        {
            if (literal is not ToonString text)
            {
                return op == ComparisonOperator.NotEqual;
            }

            order = CompareScalars(token, text.Value);
            equal = order == 0;
        }

        return op switch
        {
            ComparisonOperator.Equal => equal,
            ComparisonOperator.NotEqual => !equal,
            ComparisonOperator.Less => order < 0,
            ComparisonOperator.LessOrEqual => equal || order < 0,
            ComparisonOperator.Greater => order > 0,
            _ => equal || order > 0
        };
    }

    /// <summary>The powers of ten a double holds exactly.</summary>
    private static readonly double[] PowersOfTen = [1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11, 1e12, 1e13, 1e14, 1e15];

    /// <summary>
    ///     The double closest to a number token without an exponent and with at most 15 digits (<c>-12.5</c>), computed
    ///     exactly: the digits and the power of ten are exact doubles, so their quotient is correctly rounded. Other tokens
    ///     (and text that is not a number) return false.
    /// </summary>
    internal static bool TryParseShortNumber(ReadOnlySpan<char> token, out double value)
    {
        value = 0;
        var i = 0;
        var negative = token.Length > 0 && token[0] == '-';

        if (negative)
        {
            i++;
        }

        // An integer part of 0, or digits without a leading zero
        if (i >= token.Length || !char.IsAsciiDigit(token[i]) || (token[i] == '0' && i + 1 < token.Length && char.IsAsciiDigit(token[i + 1])))
        {
            return false;
        }

        long digits = 0;
        var count = 0;
        var fraction = -1;

        for (; i < token.Length; i++)
        {
            var c = token[i];

            if (char.IsAsciiDigit(c))
            {
                if (++count > 15)
                {
                    return false;
                }

                digits = digits * 10 + (c - '0');

                if (fraction >= 0)
                {
                    fraction++;
                }
            }
            else if (c == '.' && fraction < 0 && i + 1 < token.Length && char.IsAsciiDigit(token[i + 1]))
            {
                fraction = 0;
            }
            else
            {
                return false;
            }
        }

        value = fraction > 0 ? digits / PowersOfTen[fraction] : digits;
        value = negative ? -value : value;
        return true;
    }

    /// <summary><see cref="ToonPathEvaluator.CompareScalars"/> for a span.</summary>
    private static int CompareScalars(ReadOnlySpan<char> left, string right)
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

        static int Rank(char c) => c >= 0xE000 ? c - 0x800 : c >= 0xD800 ? c + 0x2000 : c;
    }
}
