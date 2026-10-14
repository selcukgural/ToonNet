using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using ToonNet.Core.Models;
using ToonNet.Core.Paths;

namespace ToonNet.Core.Validation;

/// <summary>
///     Rules a document must follow, written with paths and filter expressions, in the manner of Schematron for XML: each
///     rule selects values with a path and states what must hold for them. Validating a document gives every value that
///     breaks a rule, with its location.
/// </summary>
/// <remarks>
///     <para>
///         Assertions are the expressions of filters (<see cref="ToonPath"/>): <c>@</c> is the value the rule's path
///         selected, <c>$</c> the root, and they may call the standard functions, the ToonNet extensions
///         (<c>starts_with</c>, <c>sum</c>, ...) and the functions of <see cref="ToonPathOptions.Functions"/>.
///     </para>
///     <para>
///         Messages may contain <c>{path}</c> (the location of the value, such as <c>$['orders'][3]</c>) and
///         <c>{value}</c> (the value, shortened). A rule set is thread-safe for validation once its rules are added.
///         Validation is bounded by the limits of the <see cref="ToonPathOptions"/> (steps and time).
///     </para>
/// </remarks>
/// <example>
///     <code>
///     var rules = new ToonRuleSet()
///         .Rule("$.orders[*]", "@.total >= 0", "An order total cannot be negative ({value})")
///         .Rule("$.orders[*]", "@.status == 'open' || @.status == 'closed'", "Unknown status")
///         .Rule("$.orders[*]", "sum(@.lines[*].price) == @.total", "The lines do not add up to the total")
///         .Required("$.customer.email", "The customer needs an email")
///         .Forbidden("$..password", "Passwords must not be sent")
///         .Unique("$.orders[*].id", "Order ids must be unique");
///
///     ToonRuleResult result = rules.Validate(toon);
///     foreach (var violation in result.Violations)
///         Console.WriteLine($"{violation.Path}: {violation.Message}");
///     </code>
/// </example>
public sealed class ToonRuleSet
{
    private readonly List<ToonRule> _rules = [];

    /// <summary>Creates an empty rule set.</summary>
    /// <param name="options">The options of its paths and assertions (functions, limits); defaults when null.</param>
    public ToonRuleSet(ToonPathOptions? options = null)
    {
        Options = options ?? ToonPathOptions.Default;
    }

    /// <summary>The options of the rules' paths and assertions.</summary>
    public ToonPathOptions Options { get; }

    /// <summary>The rules, in the order they were added (and are checked).</summary>
    public IReadOnlyList<ToonRule> Rules => _rules;

    /// <summary>
    ///     Adds a rule: for each value <paramref name="path"/> selects, <paramref name="assertion"/> must hold.
    /// </summary>
    /// <param name="path">The values the rule applies to, such as <c>$.orders[*]</c>.</param>
    /// <param name="assertion">A filter expression, such as <c>@.total &gt;= 0 &amp;&amp; @.currency</c>.</param>
    /// <param name="message">The message of a value that breaks the rule; may contain <c>{path}</c> and <c>{value}</c>.</param>
    /// <param name="severity">Whether a broken rule makes the document invalid (<see cref="ToonRuleSeverity.Error"/>) or is only reported.</param>
    /// <returns>This rule set, to chain rules.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path or the assertion is not valid.</exception>
    public ToonRuleSet Rule(string path, string assertion, string message, ToonRuleSeverity severity = ToonRuleSeverity.Error)
    {
        ArgumentNullException.ThrowIfNull(assertion);
        return Add(new ToonRule(ToonRuleKind.Assertion, Compile(path), assertion, ToonPathParser.ParseExpression(assertion, Options), message, severity));
    }

    /// <summary>Adds a rule: <paramref name="path"/> must select at least one value (<c>$.customer.email</c>).</summary>
    /// <param name="path">The values that must be there.</param>
    /// <param name="message">The message when there are none; may contain <c>{path}</c> (the rule's path).</param>
    /// <param name="severity">Whether a broken rule makes the document invalid.</param>
    /// <returns>This rule set, to chain rules.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    public ToonRuleSet Required(string path, string message, ToonRuleSeverity severity = ToonRuleSeverity.Error) =>
        Add(new ToonRule(ToonRuleKind.Required, Compile(path), null, null, message, severity));

    /// <summary>Adds a rule: <paramref name="path"/> must select nothing (<c>$..password</c>).</summary>
    /// <param name="path">The values that must not be there.</param>
    /// <param name="message">The message of each value found; may contain <c>{path}</c> and <c>{value}</c>.</param>
    /// <param name="severity">Whether a broken rule makes the document invalid.</param>
    /// <returns>This rule set, to chain rules.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    public ToonRuleSet Forbidden(string path, string message, ToonRuleSeverity severity = ToonRuleSeverity.Error) =>
        Add(new ToonRule(ToonRuleKind.Forbidden, Compile(path), null, null, message, severity));

    /// <summary>
    ///     Adds a rule: the values <paramref name="path"/> selects must be distinct (<c>$.orders[*].id</c>); each repeated
    ///     value is reported where it repeats. Values are compared as <see cref="ToonValue.DeepEquals"/> does.
    /// </summary>
    /// <param name="path">The values that must differ.</param>
    /// <param name="message">The message of each repeated value; may contain <c>{path}</c> and <c>{value}</c>.</param>
    /// <param name="severity">Whether a broken rule makes the document invalid.</param>
    /// <returns>This rule set, to chain rules.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ToonPathException">Thrown when the path is not valid.</exception>
    public ToonRuleSet Unique(string path, string message, ToonRuleSeverity severity = ToonRuleSeverity.Error) =>
        Add(new ToonRule(ToonRuleKind.Unique, Compile(path), null, null, message, severity));

    private ToonPath Compile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return ToonPath.Parse(path, Options);
    }

    private ToonRuleSet Add(ToonRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule.Message);
        _rules.Add(rule);
        return this;
    }

    /// <summary>Checks a document against the rules.</summary>
    /// <param name="toon">The TOON text.</param>
    /// <param name="options">Parsing options; defaults when null.</param>
    /// <returns>The values that break rules, in the order of the rules.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toon"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the text is not valid TOON.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the rules take more steps than the options allow.</exception>
    /// <exception cref="TimeoutException">Thrown when the rules take longer than the options allow.</exception>
    public ToonRuleResult Validate(string toon, ToonOptions? options = null) => Validate(ToonDocument.Parse(toon, options).Root);

    /// <summary>Checks a UTF-8 encoded document against the rules.</summary>
    /// <param name="utf8Toon">The UTF-8 encoded TOON text.</param>
    /// <param name="options">Parsing options; defaults when null.</param>
    /// <returns>The values that break rules, in the order of the rules.</returns>
    /// <exception cref="ToonParseException">Thrown when the text is not valid UTF-8 or not valid TOON.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the rules take more steps than the options allow.</exception>
    /// <exception cref="TimeoutException">Thrown when the rules take longer than the options allow.</exception>
    public ToonRuleResult Validate(ReadOnlySpan<byte> utf8Toon, ToonOptions? options = null) => Validate(ToonDocument.Parse(utf8Toon, options).Root);

    /// <summary>Checks a tree against the rules, with <paramref name="root"/> as <c>$</c>.</summary>
    /// <param name="root">The value to check, such as <see cref="ToonDocument.Root"/>.</param>
    /// <returns>The values that break rules, in the order of the rules.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="root"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the rules take more steps than the options allow.</exception>
    /// <exception cref="TimeoutException">Thrown when the rules take longer than the options allow.</exception>
    public ToonRuleResult Validate(ToonValue root)
    {
        ArgumentNullException.ThrowIfNull(root);

        // One budget for all the rules
        var work = new EvaluationBudget(root, Options);
        var violations = new List<ToonRuleViolation>();

        foreach (var rule in _rules)
        {
            var nodes = ToonPathEvaluator.Select(rule.Query.Query, root, root, track: true, work);

            switch (rule.Kind)
            {
                case ToonRuleKind.Assertion:
                    foreach (var node in nodes)
                    {
                        if (!ToonPathEvaluator.Test(rule.Expression!, root, node.Value, work))
                        {
                            violations.Add(Violation(rule, node));
                        }
                    }

                    break;
                case ToonRuleKind.Required:
                    if (nodes.Count == 0)
                    {
                        violations.Add(new ToonRuleViolation(rule, rule.Path, Format(rule.Message, rule.Path, null), null));
                    }

                    break;
                case ToonRuleKind.Forbidden:
                    foreach (var node in nodes)
                    {
                        violations.Add(Violation(rule, node));
                    }

                    break;
                case ToonRuleKind.Unique:
                    var seen = new HashSet<ToonValue>(DataComparer.Instance);

                    foreach (var node in nodes)
                    {
                        work.Spend();

                        if (!seen.Add(node.Value))
                        {
                            violations.Add(Violation(rule, node));
                        }
                    }

                    break;
            }
        }

        return new ToonRuleResult(violations);
    }

    private static ToonRuleViolation Violation(ToonRule rule, ToonPathEvaluator.Node node)
    {
        var path = ToonPathEvaluator.Location.Format(node.Location);
        return new ToonRuleViolation(rule, path, Format(rule.Message, path, node.Value), node.Value);
    }

    private static string Format(string message, string path, ToonValue? value)
    {
        if (!message.Contains('{'))
        {
            return message;
        }

        return message.Replace("{path}", path, StringComparison.Ordinal)
                      .Replace("{value}", value == null ? "" : Describe(value), StringComparison.Ordinal);
    }

    /// <summary>A short text of a value for a message: a primitive as written, an object or array by its size.</summary>
    private static string Describe(ToonValue value)
    {
        var text = value switch
        {
            ToonObject obj => $"an object of {obj.Properties.Count} member{(obj.Properties.Count == 1 ? "" : "s")}",
            ToonArray array => $"an array of {array.Count} item{(array.Count == 1 ? "" : "s")}",
            ToonString s => s.Value,
            _ => value.ToString() ?? ""
        };

        return text.Length <= 100 ? text : text[..100] + "...";
    }

    /// <summary>Equality of values by their data, as <see cref="ToonValue.DeepEquals"/>, with a hash that agrees with it.</summary>
    private sealed class DataComparer : IEqualityComparer<ToonValue>
    {
        public static readonly DataComparer Instance = new();

        public bool Equals(ToonValue? x, ToonValue? y) => ToonValue.DeepEquals(x, y);

        public int GetHashCode(ToonValue value)
        {
            switch (value)
            {
                case ToonString s:
                    return s.Value.GetHashCode(StringComparison.Ordinal);
                case ToonBoolean b:
                    return b.Value ? 1 : 2;
                case ToonNumber n:
                    // Equal numbers have the same canonical text, whether they are known exactly or not
                    return n.ToString().GetHashCode(StringComparison.Ordinal);
                case ToonObject obj:
                {
                    RuntimeHelpers.EnsureSufficientExecutionStack();
                    var hash = obj.Properties.Count;

                    foreach (var (key, member) in obj.Properties)
                    {
                        hash ^= HashCode.Combine(key.GetHashCode(StringComparison.Ordinal), GetHashCode(member)); // any order
                    }

                    return hash;
                }
                case ToonArray array:
                {
                    RuntimeHelpers.EnsureSufficientExecutionStack();
                    var hash = new HashCode();

                    foreach (var item in array.Items)
                    {
                        hash.Add(GetHashCode(item));
                    }

                    return hash.ToHashCode();
                }
                default:
                    return 0;
            }
        }
    }
}

/// <summary>Whether a broken rule makes a document invalid.</summary>
public enum ToonRuleSeverity
{
    /// <summary>The document is invalid (<see cref="ToonRuleResult.IsValid"/> is false).</summary>
    Error,

    /// <summary>The violation is reported, and the document stays valid.</summary>
    Warning
}

/// <summary>What a rule checks.</summary>
public enum ToonRuleKind
{
    /// <summary>An assertion holds for each selected value.</summary>
    Assertion,

    /// <summary>The path selects at least one value.</summary>
    Required,

    /// <summary>The path selects nothing.</summary>
    Forbidden,

    /// <summary>The selected values are distinct.</summary>
    Unique
}

/// <summary>A rule of a <see cref="ToonRuleSet"/>.</summary>
public sealed class ToonRule
{
    internal ToonRule(ToonRuleKind kind, ToonPath query, string? assertion, PathExpression? expression, string message, ToonRuleSeverity severity)
    {
        Kind = kind;
        Query = query;
        Assertion = assertion;
        Expression = expression;
        Message = message;
        Severity = severity;
    }

    /// <summary>What the rule checks.</summary>
    public ToonRuleKind Kind { get; }

    /// <summary>The path of the values the rule applies to.</summary>
    public string Path => Query.Text;

    /// <summary>The assertion of an <see cref="ToonRuleKind.Assertion"/> rule, or null.</summary>
    public string? Assertion { get; }

    /// <summary>The message of a violation, with its placeholders.</summary>
    public string Message { get; }

    /// <summary>Whether a violation makes the document invalid.</summary>
    public ToonRuleSeverity Severity { get; }

    internal ToonPath Query { get; }

    internal PathExpression? Expression { get; }

    /// <summary>The rule as text: <c>$.orders[*]: @.total &gt;= 0</c>.</summary>
    public override string ToString() => Kind switch
    {
        ToonRuleKind.Assertion => $"{Path}: {Assertion}",
        _ => $"{Path}: {Kind.ToString().ToLowerInvariant()}"
    };
}

/// <summary>A value that breaks a rule.</summary>
/// <param name="Rule">The rule.</param>
/// <param name="Path">The normalized path of the value (<c>$['orders'][3]</c>), or the rule's path for a missing value.</param>
/// <param name="Message">The rule's message, with its placeholders filled in.</param>
/// <param name="Value">The value, or null for a missing value.</param>
public sealed record ToonRuleViolation(ToonRule Rule, string Path, string Message, ToonValue? Value)
{
    /// <summary>The severity of the rule.</summary>
    public ToonRuleSeverity Severity => Rule.Severity;

    /// <summary>The violation as text: <c>$['orders'][3]: An order total cannot be negative</c>.</summary>
    public override string ToString() => $"{Path}: {Message}";
}

/// <summary>The result of checking a document against a <see cref="ToonRuleSet"/>.</summary>
public sealed class ToonRuleResult
{
    internal ToonRuleResult(IReadOnlyList<ToonRuleViolation> violations)
    {
        Violations = violations;
    }

    /// <summary>Whether no rule of severity <see cref="ToonRuleSeverity.Error"/> was broken.</summary>
    public bool IsValid => Violations.All(v => v.Severity != ToonRuleSeverity.Error);

    /// <summary>Every value that breaks a rule, errors and warnings, in the order of the rules.</summary>
    public IReadOnlyList<ToonRuleViolation> Violations { get; }

    /// <summary>The violations of rules of severity <see cref="ToonRuleSeverity.Error"/>.</summary>
    public IEnumerable<ToonRuleViolation> Errors => Violations.Where(v => v.Severity == ToonRuleSeverity.Error);

    /// <summary>The violations of rules of severity <see cref="ToonRuleSeverity.Warning"/>.</summary>
    public IEnumerable<ToonRuleViolation> Warnings => Violations.Where(v => v.Severity == ToonRuleSeverity.Warning);

    /// <summary>Throws when a rule of severity <see cref="ToonRuleSeverity.Error"/> was broken.</summary>
    /// <exception cref="ToonRuleException">Thrown when the document is not valid, with its violations.</exception>
    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            throw new ToonRuleException(Errors.ToList());
        }
    }

    /// <summary>The violations, one per line.</summary>
    public override string ToString() => string.Join('\n', Violations);
}
