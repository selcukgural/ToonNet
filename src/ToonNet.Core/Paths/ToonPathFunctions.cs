using ToonNet.Core.Models;

namespace ToonNet.Core.Paths;

/// <summary>
///     Functions for the filters of a <see cref="ToonPath"/>, beyond the five of RFC 9535 and the extensions ToonNet has
///     built in: predicates (<c>[?is_vip(@.customer)]</c>) and functions that return a value
///     (<c>[?discount(@.total) &gt; 10]</c>), over values or over the values a query selects.
/// </summary>
/// <remarks>
///     <para>
///         Arguments follow the rules of RFC 9535 §2.4: a value argument is a literal, a singular query (<c>@.name</c>) or a
///         function that returns a value, and is null when the query selects nothing; a nodes argument is any query, and
///         gets the values it selects. Names are lowercase letters, digits and underscores, starting with a letter, and
///         cannot be those of the standard or built-in functions.
///     </para>
///     <para>
///         Register functions before the paths that use them are compiled; a compiled path keeps the functions it found.
///         The functions are called from the threads that evaluate paths, so they should not change shared state. An
///         exception a function throws is not caught: check the kind of a value (<c>value is ToonObject obj</c>) before
///         reading it as one. Passing a value to a function spends steps of the evaluation budget in proportion to its
///         size; the function's own work is not counted.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     var functions = new ToonPathFunctions()
///         .AddPredicate("is_vip", customer => customer is ToonObject c &amp;&amp; c["tier"] is ToonString { Value: "gold" })
///         .AddFunction("net", total => total is ToonNumber n ? new ToonNumber(n.Value / 1.2) : null);
///
///     var path = ToonPath.Parse("$.orders[?is_vip(@.customer) &amp;&amp; net(@.total) &gt; 100]", new ToonPathOptions { Functions = functions });
///     </code>
/// </example>
public sealed class ToonPathFunctions
{
    // Replaced on each change, so paths being compiled on other threads read a consistent set
    private volatile Dictionary<string, PathFunction> _functions = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    /// <summary>The names of the registered functions.</summary>
    public IReadOnlyCollection<string> Names => _functions.Keys;

    /// <summary>Registers a predicate of one value: <c>[?name(@.member)]</c>.</summary>
    /// <param name="name">The function name (lowercase letters, digits, underscores).</param>
    /// <param name="predicate">The predicate; its argument is null when the query selects nothing.</param>
    /// <returns>This set, to chain registrations.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is not valid, is built in, or is already registered.</exception>
    public ToonPathFunctions AddPredicate(string name, Func<ToonValue?, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Add(name, [ExpressionType.Value], ExpressionType.Logical, args => predicate((ToonValue?)args[0]));
    }

    /// <summary>Registers a predicate of two values: <c>[?name(@.a, 'text')]</c>.</summary>
    /// <param name="name">The function name (lowercase letters, digits, underscores).</param>
    /// <param name="predicate">The predicate; an argument is null when its query selects nothing.</param>
    /// <returns>This set, to chain registrations.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is not valid, is built in, or is already registered.</exception>
    public ToonPathFunctions AddPredicate(string name, Func<ToonValue?, ToonValue?, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Add(name, [ExpressionType.Value, ExpressionType.Value], ExpressionType.Logical,
                   args => predicate((ToonValue?)args[0], (ToonValue?)args[1]));
    }

    /// <summary>Registers a predicate of the values a query selects: <c>[?name(@.lines[*].qty)]</c>.</summary>
    /// <param name="name">The function name (lowercase letters, digits, underscores).</param>
    /// <param name="predicate">The predicate.</param>
    /// <returns>This set, to chain registrations.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is not valid, is built in, or is already registered.</exception>
    public ToonPathFunctions AddNodesPredicate(string name, Func<IReadOnlyList<ToonValue>, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Add(name, [ExpressionType.Nodes], ExpressionType.Logical, args => predicate((IReadOnlyList<ToonValue>)args[0]!));
    }

    /// <summary>Registers a function of one value that returns a value, to be compared: <c>[?name(@.member) &gt; 10]</c>.</summary>
    /// <param name="name">The function name (lowercase letters, digits, underscores).</param>
    /// <param name="function">The function; its argument is null when the query selects nothing, and it returns null for no value.</param>
    /// <returns>This set, to chain registrations.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is not valid, is built in, or is already registered.</exception>
    public ToonPathFunctions AddFunction(string name, Func<ToonValue?, ToonValue?> function)
    {
        ArgumentNullException.ThrowIfNull(function);
        return Add(name, [ExpressionType.Value], ExpressionType.Value, args => function((ToonValue?)args[0]));
    }

    /// <summary>Registers a function of two values that returns a value.</summary>
    /// <param name="name">The function name (lowercase letters, digits, underscores).</param>
    /// <param name="function">The function; an argument is null when its query selects nothing, and it returns null for no value.</param>
    /// <returns>This set, to chain registrations.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is not valid, is built in, or is already registered.</exception>
    public ToonPathFunctions AddFunction(string name, Func<ToonValue?, ToonValue?, ToonValue?> function)
    {
        ArgumentNullException.ThrowIfNull(function);
        return Add(name, [ExpressionType.Value, ExpressionType.Value], ExpressionType.Value,
                   args => function((ToonValue?)args[0], (ToonValue?)args[1]));
    }

    /// <summary>Registers a function of the values a query selects that returns a value: <c>[?name(@.lines[*].price) &gt; 100]</c>.</summary>
    /// <param name="name">The function name (lowercase letters, digits, underscores).</param>
    /// <param name="function">The function; it returns null for no value.</param>
    /// <returns>This set, to chain registrations.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is not valid, is built in, or is already registered.</exception>
    public ToonPathFunctions AddNodesFunction(string name, Func<IReadOnlyList<ToonValue>, ToonValue?> function)
    {
        ArgumentNullException.ThrowIfNull(function);
        return Add(name, [ExpressionType.Nodes], ExpressionType.Value, args => function((IReadOnlyList<ToonValue>)args[0]!));
    }

    private ToonPathFunctions Add(string name, ExpressionType[] parameters, ExpressionType result, Func<object?[], object?> invoke)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (!IsValidName(name))
        {
            throw new ArgumentException($"'{name}' is not a function name: lowercase letters, digits and underscores, starting with a letter.", nameof(name));
        }

        if (PathFunction.IsBuiltIn(name))
        {
            throw new ArgumentException($"'{name}' is a built-in function.", nameof(name));
        }

        lock (_gate)
        {
            if (_functions.ContainsKey(name))
            {
                throw new ArgumentException($"A function named '{name}' is already registered.", nameof(name));
            }

            _functions = new Dictionary<string, PathFunction>(_functions, StringComparer.Ordinal)
            {
                [name] = new PathFunction(name, parameters, result, invoke)
            };
        }

        return this;
    }

    internal PathFunction? Find(string name) => _functions.GetValueOrDefault(name);

    private static bool IsValidName(string name) =>
        name.Length > 0 && name[0] is >= 'a' and <= 'z' && name.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_');
}

/// <summary>
///     A function of a filter other than the five of RFC 9535: a ToonNet extension or a registered function. Arguments are
///     <see cref="ToonValue"/>s (null for Nothing) or, for nodes parameters, lists of the selected values; the result is a
///     <see cref="ToonValue"/> (null for Nothing) or a boolean.
/// </summary>
internal sealed class PathFunction(string name, ExpressionType[] parameters, ExpressionType result, Func<object?[], object?> invoke)
{
    public string Name { get; } = name;

    public ExpressionType[] Parameters { get; } = parameters;

    public ExpressionType Result { get; } = result;

    public Func<object?[], object?> Invoke { get; } = invoke;

    /// <summary>For the extensions a token filter tests on text: the string test they make.</summary>
    public StringTest Test { get; init; }

    internal enum StringTest : byte
    {
        None,
        StartsWith,
        EndsWith,
        Contains,
        EqualsIgnoreCase
    }

    private static readonly string[] Standard = ["length", "count", "match", "search", "value"];

    /// <summary>The functions ToonNet adds to RFC 9535, unless <see cref="ToonPathOptions.StandardFunctionsOnly"/> is set.</summary>
    internal static readonly Dictionary<string, PathFunction> Extensions = new PathFunction[]
    {
        new("starts_with", [ExpressionType.Value, ExpressionType.Value], ExpressionType.Logical,
            args => args is [ToonString text, ToonString prefix] && text.Value.StartsWith(prefix.Value, StringComparison.Ordinal))
        {
            Test = StringTest.StartsWith
        },
        new("ends_with", [ExpressionType.Value, ExpressionType.Value], ExpressionType.Logical,
            args => args is [ToonString text, ToonString suffix] && text.Value.EndsWith(suffix.Value, StringComparison.Ordinal))
        {
            Test = StringTest.EndsWith
        },
        new("contains", [ExpressionType.Value, ExpressionType.Value], ExpressionType.Logical, args => args switch
        {
            [ToonString text, ToonString part] => text.Value.Contains(part.Value, StringComparison.Ordinal),
            [ToonArray array, ToonValue item] => array.Items.Exists(i => ToonValue.DeepEquals(i, item)),
            _ => false
        })
        {
            Test = StringTest.Contains
        },
        new("equals_ignore_case", [ExpressionType.Value, ExpressionType.Value], ExpressionType.Logical,
            args => args is [ToonString a, ToonString b] && string.Equals(a.Value, b.Value, StringComparison.OrdinalIgnoreCase))
        {
            Test = StringTest.EqualsIgnoreCase
        },
        new("lower", [ExpressionType.Value], ExpressionType.Value,
            args => args[0] is ToonString text ? new ToonString(text.Value.ToLowerInvariant()) : null),
        new("upper", [ExpressionType.Value], ExpressionType.Value,
            args => args[0] is ToonString text ? new ToonString(text.Value.ToUpperInvariant()) : null),
        new("trim", [ExpressionType.Value], ExpressionType.Value,
            args => args[0] is ToonString text ? new ToonString(text.Value.Trim()) : null),
        new("type", [ExpressionType.Value], ExpressionType.Value, args => args[0] switch
        {
            not ToonValue => null,
            ToonValue value => new ToonString(value.ValueType switch
            {
                ToonValueType.Null => "null",
                ToonValueType.Boolean => "boolean",
                ToonValueType.Number => "number",
                ToonValueType.String => "string",
                ToonValueType.Object => "object",
                _ => "array"
            })
        }),
        new("sum", [ExpressionType.Nodes], ExpressionType.Value, args => Aggregate((IReadOnlyList<ToonValue>)args[0]!, Aggregation.Sum)),
        new("min", [ExpressionType.Nodes], ExpressionType.Value, args => Aggregate((IReadOnlyList<ToonValue>)args[0]!, Aggregation.Min)),
        new("max", [ExpressionType.Nodes], ExpressionType.Value, args => Aggregate((IReadOnlyList<ToonValue>)args[0]!, Aggregation.Max)),
        new("avg", [ExpressionType.Nodes], ExpressionType.Value, args => Aggregate((IReadOnlyList<ToonValue>)args[0]!, Aggregation.Average))
    }.ToDictionary(f => f.Name, StringComparer.Ordinal);

    internal static bool IsBuiltIn(string name) => Array.IndexOf(Standard, name) >= 0 || Extensions.ContainsKey(name);

    private enum Aggregation : byte
    {
        Sum,
        Min,
        Max,
        Average
    }

    /// <summary>
    ///     The sum, minimum, maximum or average of numbers: exact (decimal) while every number is and the result fits,
    ///     otherwise as doubles. Nothing when a value is not a number or the result is not finite, and for the minimum,
    ///     maximum and average of no values; the sum of none is 0.
    /// </summary>
    private static ToonValue? Aggregate(IReadOnlyList<ToonValue> values, Aggregation aggregation)
    {
        if (values.Count == 0)
        {
            return aggregation == Aggregation.Sum ? new ToonNumber(0L) : null;
        }

        var numbers = new ToonNumber[values.Count];

        for (var i = 0; i < numbers.Length; i++)
        {
            if (values[i] is not ToonNumber { IsFinite: true } number)
            {
                return null;
            }

            numbers[i] = number;
        }

        if (TryAggregateExactly(numbers, aggregation, out var exact))
        {
            return new ToonNumber(exact);
        }

        // An average of doubles adds the parts, so large values do not overflow
        var result = aggregation switch
        {
            Aggregation.Min => numbers.Min(n => n.Value),
            Aggregation.Max => numbers.Max(n => n.Value),
            Aggregation.Sum => numbers.Sum(n => n.Value),
            _ => numbers.Sum(n => n.Value / numbers.Length)
        };

        return double.IsFinite(result) ? new ToonNumber(result) : null;
    }

    private static bool TryAggregateExactly(ToonNumber[] numbers, Aggregation aggregation, out decimal result)
    {
        result = 0;

        foreach (var number in numbers)
        {
            if (number.DecimalValue == null)
            {
                return false;
            }
        }

        try
        {
            result = aggregation switch
            {
                Aggregation.Min => numbers.Min(n => n.DecimalValue!.Value),
                Aggregation.Max => numbers.Max(n => n.DecimalValue!.Value),
                Aggregation.Sum => numbers.Sum(n => n.DecimalValue!.Value),
                _ => numbers.Sum(n => n.DecimalValue!.Value) / numbers.Length
            };

            return true;
        }
        catch (OverflowException) when (aggregation == Aggregation.Average)
        {
            // The sum does not fit: average the differences from the first value, which stay small for values that are
            // close (and are exact for equal ones)
            try
            {
                var first = numbers[0].DecimalValue!.Value;
                result = first + numbers.Sum(n => (n.DecimalValue!.Value - first) / numbers.Length);
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}
