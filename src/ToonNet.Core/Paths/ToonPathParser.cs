using System.Globalization;
using ToonNet.Core.Models;

namespace ToonNet.Core.Paths;

/// <summary>
///     Parses a path with the grammar of RFC 9535 (JSONPath), including its well-typedness rules for function
///     expressions; anything else is a <see cref="ToonPathException"/> with the position of the error.
/// </summary>
internal sealed class ToonPathParser
{
    /// <summary>The largest index and slice bound, and the smallest negated (I-JSON integers, §2.1).</summary>
    private const long MaxInteger = (1L << 53) - 1;

    private readonly string _text;
    private readonly ToonPathOptions _limits;
    private int _position;
    private int _nesting;

    private ToonPathParser(string text, ToonPathOptions limits)
    {
        _text = text;
        _limits = limits;
    }

    public static Query Parse(string text, ToonPathOptions limits)
    {
        var parser = new ToonPathParser(text, limits);

        if (text.Length > limits.MaxLength)
        {
            throw new ToonPathException($"The path is longer than {limits.MaxLength} characters", text, limits.MaxLength);
        }

        return parser.ParseRootQuery();
    }

    /// <summary>
    ///     Parses a filter expression on its own (<c>@.total &gt;= 0 &amp;&amp; @.status</c>), as the rules of a
    ///     <c>ToonRuleSet</c> give it: what follows <c>?</c> in a filter selector, to the end of the text.
    /// </summary>
    public static PathExpression ParseExpression(string text, ToonPathOptions options)
    {
        var parser = new ToonPathParser(text, options);

        if (text.Length > options.MaxLength)
        {
            throw new ToonPathException($"The expression is longer than {options.MaxLength} characters", text, options.MaxLength);
        }

        parser.SkipBlank();
        var position = parser._position;
        var expression = parser.RequireLogical(parser.ParseOr(), position);
        parser.SkipBlank();

        if (parser._position < text.Length)
        {
            throw parser.Error($"Unexpected '{text[parser._position]}'");
        }

        return expression;
    }

    private Query ParseRootQuery()
    {
        if (!TryConsume('$'))
        {
            throw Error("A path starts with '$'");
        }

        var segments = ParseSegments();

        if (_position < _text.Length)
        {
            throw Error(char.IsWhiteSpace(_text[_position]) ? "Unexpected whitespace" : $"Unexpected '{_text[_position]}'");
        }

        return new Query(absolute: true, segments);
    }

    /// <summary>Reads segments while one follows (whitespace may come before each).</summary>
    private Segment[] ParseSegments()
    {
        List<Segment>? segments = null;

        while (true)
        {
            var start = _position;
            SkipBlank();

            if (Peek() is not ('.' or '['))
            {
                _position = start;
                return segments?.ToArray() ?? [];
            }

            (segments ??= []).Add(ParseSegment());
        }
    }

    private Segment ParseSegment()
    {
        if (TryConsume('['))
        {
            return new Segment(descendant: false, ParseBracketed());
        }

        _position++; // '.'

        if (TryConsume('.'))
        {
            if (TryConsume('['))
            {
                return new Segment(descendant: true, ParseBracketed());
            }

            return new Segment(descendant: true, [ParseShorthand("'..'")]);
        }

        return new Segment(descendant: false, [ParseShorthand("'.'")]);
    }

    /// <summary>The wildcard or member name after <c>.</c> or <c>..</c>.</summary>
    private Selector ParseShorthand(string after)
    {
        if (TryConsume('*'))
        {
            return new Selector { Kind = SelectorKind.Wildcard };
        }

        if (_position < _text.Length && IsNameFirst(_text, _position))
        {
            var start = _position;

            while (_position < _text.Length && (IsNameFirst(_text, _position) || char.IsAsciiDigit(_text[_position])))
            {
                _position += char.IsHighSurrogate(_text[_position]) ? 2 : 1;
            }

            return new Selector { Kind = SelectorKind.Name, Name = _text[start.._position] };
        }

        throw Error($"Expected a member name or '*' after {after}");
    }

    /// <summary>name-first: ALPHA, '_', or a character beyond ASCII (a surrogate only as part of a pair).</summary>
    private static bool IsNameFirst(string text, int index)
    {
        var c = text[index];

        if (char.IsAsciiLetter(c) || c == '_')
        {
            return true;
        }

        if (c < 0x80)
        {
            return false;
        }

        if (char.IsHighSurrogate(c))
        {
            return index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]);
        }

        return !char.IsLowSurrogate(c);
    }

    /// <summary>The selectors of <c>[...]</c>, after the bracket.</summary>
    private Selector[] ParseBracketed()
    {
        var selectors = new List<Selector>(1);

        while (true)
        {
            SkipBlank();
            selectors.Add(ParseSelector());
            SkipBlank();

            if (TryConsume(','))
            {
                continue;
            }

            if (TryConsume(']'))
            {
                return selectors.ToArray();
            }

            throw Error("Expected ',' or ']'");
        }
    }

    private Selector ParseSelector()
    {
        switch (Peek())
        {
            case '\'' or '"':
                return new Selector { Kind = SelectorKind.Name, Name = ParseString() };
            case '*':
                _position++;
                return new Selector { Kind = SelectorKind.Wildcard };
            case '?':
            {
                _position++;
                Enter();
                SkipBlank();
                var filter = RequireLogical(ParseOr());
                _nesting--;
                return new Selector { Kind = SelectorKind.Filter, Filter = filter };
            }
            case '-' or ':' or (>= '0' and <= '9'):
                return ParseIndexOrSlice();
            default:
                throw Error("Expected a selector: a quoted name, '*', an index, a slice or a filter");
        }
    }

    private Selector ParseIndexOrSlice()
    {
        var start = Peek() == ':' ? (long?)null : ParseInteger();
        var afterStart = _position;
        SkipBlank();

        if (!TryConsume(':'))
        {
            _position = afterStart;
            return new Selector { Kind = SelectorKind.Index, Index = start!.Value };
        }

        SkipBlank();
        long? end = Peek() is '-' or (>= '0' and <= '9') ? ParseInteger() : null;
        SkipBlank();
        long step = 1;

        if (TryConsume(':'))
        {
            SkipBlank();

            if (Peek() is '-' or (>= '0' and <= '9'))
            {
                step = ParseInteger();
            }
        }

        return new Selector { Kind = SelectorKind.Slice, Start = start, End = end, Step = step };
    }

    /// <summary>An index or slice bound: 0, or an optional '-' and digits without a leading zero, within ±(2^53 - 1).</summary>
    private long ParseInteger()
    {
        var start = _position;
        var negative = TryConsume('-');

        if (Peek() is not (>= '0' and <= '9'))
        {
            throw Error("Expected digits");
        }

        if (Peek() == '0')
        {
            _position++;

            if (negative || Peek() is >= '0' and <= '9')
            {
                throw Error("An integer has no leading zero and is not -0", start);
            }

            return 0;
        }

        long value = 0;

        while (Peek() is >= '0' and <= '9')
        {
            value = value * 10 + (_text[_position++] - '0');

            if (value > MaxInteger)
            {
                throw Error("The integer is outside the range of ±(2^53 - 1)", start);
            }
        }

        return negative ? -value : value;
    }

    /// <summary>A string literal in single or double quotes, unescaped.</summary>
    private string ParseString()
    {
        var start = _position;
        var quote = _text[_position++];
        var builder = new System.Text.StringBuilder();

        while (true)
        {
            if (_position >= _text.Length)
            {
                throw Error("The string has no closing quote", start);
            }

            var c = _text[_position];

            if (c == quote)
            {
                _position++;
                return builder.ToString();
            }

            if (c == '\\')
            {
                _position++;
                ParseEscape(quote, builder);
                continue;
            }

            if (c < 0x20)
            {
                throw Error("A control character must be escaped in a string");
            }

            if (char.IsSurrogate(c))
            {
                if (!char.IsHighSurrogate(c) || _position + 1 >= _text.Length || !char.IsLowSurrogate(_text[_position + 1]))
                {
                    throw Error("Unpaired surrogate in a string");
                }

                builder.Append(c).Append(_text[_position + 1]);
                _position += 2;
                continue;
            }

            builder.Append(c);
            _position++;
        }
    }

    private void ParseEscape(char quote, System.Text.StringBuilder builder)
    {
        var c = Peek();
        _position++;

        switch (c)
        {
            case 'b': builder.Append('\b'); return;
            case 'f': builder.Append('\f'); return;
            case 'n': builder.Append('\n'); return;
            case 'r': builder.Append('\r'); return;
            case 't': builder.Append('\t'); return;
            case '/': builder.Append('/'); return;
            case '\\': builder.Append('\\'); return;
            case 'u':
            {
                var unit = ParseHex4();

                if (char.IsHighSurrogate(unit))
                {
                    if (Peek() != '\\' || _position + 1 >= _text.Length || _text[_position + 1] != 'u')
                    {
                        throw Error("A high surrogate escape must be followed by a low surrogate escape");
                    }

                    _position += 2;
                    var low = ParseHex4();

                    if (!char.IsLowSurrogate(low))
                    {
                        throw Error("A high surrogate escape must be followed by a low surrogate escape");
                    }

                    builder.Append(unit).Append(low);
                    return;
                }

                if (char.IsLowSurrogate(unit))
                {
                    throw Error("Unpaired low surrogate escape");
                }

                builder.Append(unit);
                return;
            }
            default:
                if (c == quote && c != '\0')
                {
                    builder.Append(c);
                    return;
                }

                throw Error("Invalid escape in a string", _position - 2);
        }
    }

    private char ParseHex4()
    {
        if (_position + 4 > _text.Length ||
            !ushort.TryParse(_text.AsSpan(_position, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
        {
            throw Error("Expected four hexadecimal digits after '\\u'");
        }

        _position += 4;
        return (char)value;
    }

    #region Filter expressions

    // logical-or-expr = logical-and-expr *(S "||" S logical-and-expr); the operands of an operator must be logical, a
    // single operand is left to the caller (it may be a function argument, such as a literal)
    private PathExpression ParseOr()
    {
        var first = ParseAnd();
        List<PathExpression>? operands = null;

        while (TryConsumeOperator("||"))
        {
            SkipBlank();
            (operands ??= [RequireLogical(first)]).Add(RequireLogical(ParseAnd()));
        }

        return operands == null ? first : new OrExpression(operands.ToArray());
    }

    private PathExpression ParseAnd()
    {
        var first = ParseBasic();
        List<PathExpression>? operands = null;

        while (TryConsumeOperator("&&"))
        {
            SkipBlank();
            (operands ??= [RequireLogical(first)]).Add(RequireLogical(ParseBasic()));
        }

        return operands == null ? first : new AndExpression(operands.ToArray());
    }

    /// <summary>Skips whitespace and consumes an operator, or leaves the position where it was.</summary>
    private bool TryConsumeOperator(string op)
    {
        var start = _position;
        SkipBlank();

        if (string.CompareOrdinal(_text, _position, op, 0, op.Length) == 0)
        {
            _position += op.Length;
            return true;
        }

        _position = start;
        return false;
    }

    // basic-expr = paren-expr / comparison-expr / test-expr
    private PathExpression ParseBasic()
    {
        if (TryConsume('!'))
        {
            SkipBlank();

            if (Peek() == '(')
            {
                return new NotExpression(ParseParenthesized());
            }

            var position = _position;
            var operand = ParsePrimary();

            if (operand is not (QueryExpression or FunctionExpression))
            {
                throw Error("'!' must be followed by a query, a function or a parenthesized expression", position);
            }

            return new NotExpression(RequireLogical(operand, position));
        }

        if (Peek() == '(')
        {
            return ParseParenthesized();
        }

        var leftPosition = _position;
        var left = ParsePrimary();
        var afterLeft = _position;
        SkipBlank();

        if (TryParseComparisonOperator(out var op))
        {
            SkipBlank();
            var rightPosition = _position;
            var right = ParsePrimary();
            return new ComparisonExpression(RequireComparable(left, leftPosition), op, RequireComparable(right, rightPosition));
        }

        _position = afterLeft;
        return left;
    }

    private PathExpression ParseParenthesized()
    {
        _position++; // '('
        Enter();
        SkipBlank();
        var position = _position;
        var expression = RequireLogical(ParseOr(), position);
        SkipBlank();

        if (!TryConsume(')'))
        {
            throw Error("Expected ')'");
        }

        _nesting--;
        return expression;
    }

    private bool TryParseComparisonOperator(out ComparisonOperator op)
    {
        var rest = _text.AsSpan(_position);
        (op, var length) = rest switch
        {
            _ when rest.StartsWith("==") => (ComparisonOperator.Equal, 2),
            _ when rest.StartsWith("!=") => (ComparisonOperator.NotEqual, 2),
            _ when rest.StartsWith("<=") => (ComparisonOperator.LessOrEqual, 2),
            _ when rest.StartsWith(">=") => (ComparisonOperator.GreaterOrEqual, 2),
            _ when rest.StartsWith("<") => (ComparisonOperator.Less, 1),
            _ when rest.StartsWith(">") => (ComparisonOperator.Greater, 1),
            _ => (default, 0)
        };

        _position += length;
        return length > 0;
    }

    /// <summary>A literal, a query (<c>@...</c> or <c>$...</c>) or a function call.</summary>
    private PathExpression ParsePrimary()
    {
        var c = Peek();

        switch (c)
        {
            case '@' or '$':
            {
                _position++;
                Enter();
                var query = new Query(absolute: c == '$', ParseSegments());
                _nesting--;
                return new QueryExpression(query);
            }
            case '\'' or '"':
                return new LiteralExpression(new ToonString(ParseString()));
            case '-' or (>= '0' and <= '9'):
                return new LiteralExpression(ParseNumber());
        }

        if (c is >= 'a' and <= 'z')
        {
            var start = _position;

            while (Peek() is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_')
            {
                _position++;
            }

            var name = _text[start.._position];

            if (Peek() == '(')
            {
                return ParseFunction(name, start);
            }

            switch (name)
            {
                case "true": return new LiteralExpression(ToonBoolean.True);
                case "false": return new LiteralExpression(ToonBoolean.False);
                case "null": return new LiteralExpression(ToonNull.Instance);
            }

            throw Error($"Unexpected '{name}'", start);
        }

        throw Error(_position >= _text.Length ? "The expression ends early" : "Expected a query, a literal or a function");
    }

    /// <summary>number = (int / "-0") [ frac ] [ exp ], with an exponent marker of either case.</summary>
    private ToonNumber ParseNumber()
    {
        var start = _position;
        TryConsume('-');

        if (Peek() == '0')
        {
            _position++;

            if (Peek() is >= '0' and <= '9')
            {
                throw Error("A number has no leading zero", start);
            }
        }
        else if (!ConsumeDigits())
        {
            throw Error("Expected digits", start);
        }

        if (TryConsume('.') && !ConsumeDigits())
        {
            throw Error("Expected digits after '.'");
        }

        if (Peek() is 'e' or 'E')
        {
            _position++;

            if (Peek() is '+' or '-')
            {
                _position++;
            }

            if (!ConsumeDigits())
            {
                throw Error("Expected digits in the exponent");
            }
        }

        // As TOON reads numbers: exact when a decimal holds the value (1e-30 does not; it would round to 0)
        var token = _text.AsSpan(start, _position - start);

        if (Parsing.ToonParser.TryParseNumberParts(token, out var value, out var exact, out var hasExact))
        {
            return hasExact ? new ToonNumber(exact, value) : new ToonNumber(value);
        }

        return new ToonNumber(double.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture));
    }

    private bool ConsumeDigits()
    {
        var start = _position;

        while (Peek() is >= '0' and <= '9')
        {
            _position++;
        }

        return _position > start;
    }

    private PathExpression ParseFunction(string name, int start)
    {
        FunctionKind function;
        ExpressionType[] parameters;
        PathFunction? custom = null;

        switch (name)
        {
            case "length": function = FunctionKind.Length; parameters = [ExpressionType.Value]; break;
            case "count": function = FunctionKind.Count; parameters = [ExpressionType.Nodes]; break;
            case "match": function = FunctionKind.Match; parameters = [ExpressionType.Value, ExpressionType.Value]; break;
            case "search": function = FunctionKind.Search; parameters = [ExpressionType.Value, ExpressionType.Value]; break;
            case "value": function = FunctionKind.Value; parameters = [ExpressionType.Nodes]; break;
            default:
                custom = _limits.StandardFunctionsOnly
                    ? null
                    : PathFunction.Extensions.GetValueOrDefault(name) ?? _limits.Functions?.Find(name);

                if (custom == null)
                {
                    throw Error(_limits.StandardFunctionsOnly && (PathFunction.Extensions.ContainsKey(name) || _limits.Functions?.Find(name) != null)
                                    ? $"'{name}' is not a function of RFC 9535 (StandardFunctionsOnly is set)"
                                    : $"Unknown function '{name}'", start);
                }

                function = FunctionKind.Custom;
                parameters = custom.Parameters;
                break;
        }

        _position++; // '('
        Enter();
        SkipBlank();
        var arguments = new List<PathExpression>(parameters.Length);

        if (!TryConsume(')'))
        {
            while (true)
            {
                var position = _position;

                if (arguments.Count == parameters.Length)
                {
                    throw Error($"{name}() takes {parameters.Length} argument{(parameters.Length == 1 ? "" : "s")}", start);
                }

                arguments.Add(RequireArgument(ParseOr(), parameters[arguments.Count], name, position));
                SkipBlank();

                if (TryConsume(')'))
                {
                    break;
                }

                if (!TryConsume(','))
                {
                    throw Error("Expected ',' or ')'");
                }

                SkipBlank();
            }
        }

        if (arguments.Count != parameters.Length)
        {
            throw Error($"{name}() takes {parameters.Length} argument{(parameters.Length == 1 ? "" : "s")}", start);
        }

        _nesting--;

        // A literal pattern is compiled once, with the path
        if (function is FunctionKind.Match or FunctionKind.Search && arguments[1] is LiteralExpression { Value: var pattern })
        {
            return new FunctionExpression(function, arguments.ToArray())
            {
                HasLiteralPattern = true,
                LiteralPattern = pattern is ToonString text ? ToonPathRegex.Get(text.Value, function == FunctionKind.Match, _limits.MaxPatternLength) : null
            };
        }

        return new FunctionExpression(function, arguments.ToArray()) { Custom = custom };
    }

    #endregion

    #region Well-typedness (RFC 9535 §2.4.3)

    /// <summary>An expression in a logical position: a test (query, or function of LogicalType or NodesType) or a logical operator.</summary>
    private PathExpression RequireLogical(PathExpression expression, int position = -1)
    {
        return expression switch
        {
            LiteralExpression => throw Error("A literal is not a test; compare it with something", position),
            FunctionExpression { ResultType: ExpressionType.Value } => throw Error("A function that returns a value must be compared, not tested", position),
            _ => expression
        };
    }

    /// <summary>A side of a comparison: a literal, a singular query, or a function of ValueType.</summary>
    private PathExpression RequireComparable(PathExpression expression, int position)
    {
        return expression switch
        {
            LiteralExpression => expression,
            QueryExpression { Query.IsSingular: true } => expression,
            QueryExpression => throw Error("Only a singular query (names and indexes only) can be compared", position),
            FunctionExpression { ResultType: ExpressionType.Value } => expression,
            FunctionExpression => throw Error("The result of this function cannot be compared", position),
            _ => throw Error("Expected a literal, a singular query or a function", position)
        };
    }

    private PathExpression RequireArgument(PathExpression expression, ExpressionType parameter, string function, int position)
    {
        var valid = parameter switch
        {
            ExpressionType.Value => expression is LiteralExpression or QueryExpression { Query.IsSingular: true } or
                FunctionExpression { ResultType: ExpressionType.Value },
            ExpressionType.Nodes => expression is QueryExpression,
            _ => expression is not (LiteralExpression or FunctionExpression { ResultType: ExpressionType.Value })
        };

        return valid
            ? expression
            : throw Error(parameter == ExpressionType.Nodes
                              ? $"The argument of {function}() must be a query"
                              : $"The argument of {function}() must be a literal, a singular query or a function that returns a value", position);
    }

    #endregion

    private void Enter()
    {
        if (++_nesting > _limits.MaxNesting)
        {
            throw Error($"The path nests more than {_limits.MaxNesting} levels");
        }

        if (!System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            throw Error("The path nests too deeply for the stack of this thread");
        }
    }

    private char Peek() => _position < _text.Length ? _text[_position] : '\0';

    private bool TryConsume(char c)
    {
        if (_position < _text.Length && _text[_position] == c)
        {
            _position++;
            return true;
        }

        return false;
    }

    /// <summary>Skips the whitespace of the grammar (S): space, tab, line feed and carriage return.</summary>
    private void SkipBlank()
    {
        while (Peek() is ' ' or '\t' or '\n' or '\r')
        {
            _position++;
        }
    }

    private ToonPathException Error(string message, int position = -1) =>
        new(message, _text, position >= 0 ? position : Math.Min(_position, _text.Length));
}
