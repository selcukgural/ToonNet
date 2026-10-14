using ToonNet.Core.Models;

namespace ToonNet.Core.Paths;

// The syntax tree of a compiled path (RFC 9535). It is immutable once parsed and shared by every evaluation.

internal enum SelectorKind : byte
{
    Name,
    Wildcard,
    Index,
    Slice,
    Filter
}

/// <summary>A selector of a segment: <c>'name'</c>, <c>*</c>, <c>3</c>, <c>1:5:2</c> or <c>?expr</c>.</summary>
internal sealed class Selector
{
    public required SelectorKind Kind { get; init; }

    /// <summary>The member name of a name selector.</summary>
    public string? Name { get; init; }

    /// <summary>The index of an index selector (negative counts from the end).</summary>
    public long Index { get; init; }

    public long? Start { get; init; }

    public long? End { get; init; }

    public long Step { get; init; } = 1;

    /// <summary>The logical expression of a filter selector.</summary>
    public PathExpression? Filter { get; init; }
}

/// <summary>A child segment (<c>.name</c>, <c>[...]</c>) or a descendant segment (<c>..name</c>, <c>..[...]</c>).</summary>
internal sealed class Segment(bool descendant, Selector[] selectors)
{
    public bool Descendant { get; } = descendant;

    public Selector[] Selectors { get; } = selectors;

    /// <summary>Whether the segment selects at most one node: a child segment with one name or index selector.</summary>
    public bool IsSingular => !Descendant && Selectors is [{ Kind: SelectorKind.Name or SelectorKind.Index }];
}

/// <summary>A query: from the root (<c>$</c>) or, in a filter, from the current node (<c>@</c>).</summary>
internal sealed class Query(bool absolute, Segment[] segments)
{
    public bool Absolute { get; } = absolute;

    public Segment[] Segments { get; } = segments;

    /// <summary>Whether the query selects at most one node (RFC 9535 §2.3.5.1: a singular query).</summary>
    public bool IsSingular { get; } = Array.TrueForAll(segments, static s => s.IsSingular);
}

internal enum ComparisonOperator : byte
{
    Equal,
    NotEqual,
    Less,
    LessOrEqual,
    Greater,
    GreaterOrEqual
}

internal enum FunctionKind : byte
{
    Length,
    Count,
    Match,
    Search,
    Value,

    /// <summary>A ToonNet extension or a registered function (<see cref="FunctionExpression.Custom"/>).</summary>
    Custom
}

/// <summary>The declared types of function parameters and results (RFC 9535 §2.4.1).</summary>
internal enum ExpressionType : byte
{
    Value,
    Logical,
    Nodes
}

/// <summary>An expression of a filter selector.</summary>
internal abstract class PathExpression;

internal sealed class LiteralExpression(ToonValue value) : PathExpression
{
    public ToonValue Value { get; } = value;
}

internal sealed class QueryExpression(Query query) : PathExpression
{
    public Query Query { get; } = query;
}

internal sealed class FunctionExpression(FunctionKind function, PathExpression[] arguments) : PathExpression
{
    public FunctionKind Function { get; } = function;

    public PathExpression[] Arguments { get; } = arguments;

    public ExpressionType ResultType => Custom?.Result ??
                                        (Function is FunctionKind.Match or FunctionKind.Search ? ExpressionType.Logical : ExpressionType.Value);

    /// <summary>The function of <see cref="FunctionKind.Custom"/>.</summary>
    public PathFunction? Custom { get; init; }

    /// <summary>The compiled regular expression of <c>match</c> or <c>search</c> when the pattern is a literal (null when invalid).</summary>
    public System.Text.RegularExpressions.Regex? LiteralRegex { get; init; }

    public bool HasLiteralRegex { get; init; }
}

internal sealed class ComparisonExpression(PathExpression left, ComparisonOperator op, PathExpression right) : PathExpression
{
    public PathExpression Left { get; } = left;

    public ComparisonOperator Operator { get; } = op;

    public PathExpression Right { get; } = right;
}

internal sealed class AndExpression(PathExpression[] operands) : PathExpression
{
    public PathExpression[] Operands { get; } = operands;
}

internal sealed class OrExpression(PathExpression[] operands) : PathExpression
{
    public PathExpression[] Operands { get; } = operands;
}

internal sealed class NotExpression(PathExpression operand) : PathExpression
{
    public PathExpression Operand { get; } = operand;
}
