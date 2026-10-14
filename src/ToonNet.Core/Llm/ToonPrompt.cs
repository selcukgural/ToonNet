using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Core.Llm;

/// <summary>
///     Helpers for using TOON with large language models: compact data for prompts, a template of the shape an answer
///     should have, and reading TOON out of a model's answer.
/// </summary>
/// <example>
///     <code>
///     var prompt = $"""
///         Orders (TOON):
///         {ToonPrompt.Serialize(orders, new ToonPromptOptions { MaxArrayItems = 50 })}
///
///         Answer in a ```toon block with this shape:
///         {ToonPrompt.DescribeShape&lt;Summary&gt;()}
///         """;
///
///     var summary = ToonPrompt.Deserialize&lt;Summary&gt;(modelAnswer);
///     </code>
/// </example>
public static class ToonPrompt
{
    /// <summary>
    ///     Takes the TOON out of a model's answer: the content of the first fenced code block marked <c>toon</c>
    ///     (<c>```toon</c> or <c>~~~toon</c>), else of the first unmarked block, else the whole answer without the blank
    ///     lines around it. A block the answer leaves unclosed (a cut-off answer) runs to the end.
    /// </summary>
    /// <param name="modelOutput">The model's answer.</param>
    /// <returns>The TOON text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="modelOutput"/> is null.</exception>
    public static string Extract(string modelOutput)
    {
        ArgumentNullException.ThrowIfNull(modelOutput);

        var lines = modelOutput.Replace("\r\n", "\n").Split('\n');
        string? unmarked = null;

        for (var i = 0; i < lines.Length; i++)
        {
            if (!TryOpenFence(lines[i], out var fence, out var indent, out var info))
            {
                continue;
            }

            // The block runs to a closing fence of the same character, at least as long
            var end = i + 1;

            while (end < lines.Length && !IsClosingFence(lines[end], fence))
            {
                end++;
            }

            var content = string.Join('\n', lines[(i + 1)..end].Select(line => RemoveIndent(line, indent)));

            if (info.Equals("toon", StringComparison.OrdinalIgnoreCase))
            {
                return content;
            }

            if (info.Length == 0)
            {
                unmarked ??= content;
            }

            i = end;
        }

        return unmarked ?? TrimBlankLines(lines);
    }

    /// <summary>A fence opening a code block: up to three spaces, then three or more backticks or tildes, then an info string.</summary>
    private static bool TryOpenFence(string line, out string fence, out int indent, out string info)
    {
        fence = "";
        info = "";
        indent = 0;

        while (indent < line.Length && indent < 4 && line[indent] == ' ')
        {
            indent++;
        }

        if (indent > 3 || indent >= line.Length || line[indent] is not ('`' or '~'))
        {
            return false;
        }

        var marker = line[indent];
        var length = 0;

        while (indent + length < line.Length && line[indent + length] == marker)
        {
            length++;
        }

        if (length < 3)
        {
            return false;
        }

        var rest = line[(indent + length)..].Trim();

        // A backtick fence's info string cannot contain backticks (CommonMark)
        if (marker == '`' && rest.Contains('`'))
        {
            return false;
        }

        fence = new string(marker, length);
        var space = rest.IndexOfAny([' ', '\t']);
        info = space < 0 ? rest : rest[..space];
        return true;
    }

    private static bool IsClosingFence(string line, string fence)
    {
        var text = line.Trim();
        return text.Length >= fence.Length && text.All(c => c == fence[0]) && line.Length - line.TrimStart(' ').Length <= 3;
    }

    private static string RemoveIndent(string line, int indent)
    {
        var spaces = 0;

        while (spaces < indent && spaces < line.Length && line[spaces] == ' ')
        {
            spaces++;
        }

        return line[spaces..];
    }

    private static string TrimBlankLines(string[] lines)
    {
        var start = 0;
        var end = lines.Length;

        while (start < end && string.IsNullOrWhiteSpace(lines[start]))
        {
            start++;
        }

        while (end > start && string.IsNullOrWhiteSpace(lines[end - 1]))
        {
            end--;
        }

        return string.Join('\n', lines[start..end]);
    }

    /// <summary>
    ///     Parses the TOON of a model's answer (see <see cref="Extract"/>) leniently: in non-strict mode (§14.4), so a
    ///     length the model miscounted, a repeated key or a misplaced indentation level does not fail the answer.
    /// </summary>
    /// <param name="modelOutput">The model's answer.</param>
    /// <param name="options">Parsing options; strict mode is turned off.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="modelOutput"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the answer is not TOON even in non-strict mode.</exception>
    public static ToonDocument Parse(string modelOutput, ToonOptions? options = null)
    {
        return ToonDocument.Parse(Extract(modelOutput), (options ?? ToonOptions.Default).NonStrict());
    }

    /// <summary>
    ///     Deserializes the TOON of a model's answer (see <see cref="Extract"/>) leniently, in non-strict mode (§14.4).
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="modelOutput">The model's answer.</param>
    /// <param name="options">Optional deserialization options; strict mode is turned off.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="modelOutput"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the answer is not TOON even in non-strict mode.</exception>
    /// <exception cref="ToonSerializationException">Thrown when the answer does not fit <typeparamref name="T"/>.</exception>
    [RequiresUnreferencedCode(ToonSerializer.ReflectionMessage)]
    [RequiresDynamicCode(ToonSerializer.ReflectionMessage)]
    public static T? Deserialize<T>(string modelOutput, ToonSerializerOptions? options = null)
    {
        return ToonSerializer.Deserialize<T>(Extract(modelOutput), Lenient(options));
    }

    /// <summary>
    ///     Deserializes the TOON of a model's answer leniently with the type info of its type, without reflection; see
    ///     <see cref="Deserialize{T}(string, ToonSerializerOptions)"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="modelOutput">The model's answer.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options; strict mode is turned off.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="modelOutput"/> or <paramref name="typeInfo"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the answer is not TOON even in non-strict mode.</exception>
    /// <exception cref="ToonSerializationException">Thrown when the answer does not fit <typeparamref name="T"/>.</exception>
    public static T? Deserialize<T>(string modelOutput, ToonTypeInfo<T> typeInfo, ToonSerializerOptions? options = null)
    {
        return ToonSerializer.Deserialize(Extract(modelOutput), typeInfo, Lenient(options));
    }

    /// <summary>
    ///     Reads the TOON of a model's answer leniently (see <see cref="Deserialize{T}(string, ToonSerializerOptions)"/>),
    ///     checks it against rules, and deserializes it when it follows them. The exception's message lists what to fix,
    ///     ready to send back to the model.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="modelOutput">The model's answer.</param>
    /// <param name="rules">The rules the answer must follow.</param>
    /// <param name="options">Optional deserialization options; strict mode is turned off.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="modelOutput"/> or <paramref name="rules"/> is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the answer is not TOON even in non-strict mode.</exception>
    /// <exception cref="ToonRuleException">Thrown when the answer breaks a rule of severity error.</exception>
    /// <exception cref="ToonSerializationException">Thrown when the answer does not fit <typeparamref name="T"/>.</exception>
    [RequiresUnreferencedCode(ToonSerializer.ReflectionMessage)]
    [RequiresDynamicCode(ToonSerializer.ReflectionMessage)]
    public static T? DeserializeValidated<T>(string modelOutput, Validation.ToonRuleSet rules, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var lenient = Lenient(options);
        var root = ToonDocument.Parse(Extract(modelOutput), lenient.ToonOptions).Root;

        rules.Validate(root).ThrowIfInvalid();
        return ToonSerializer.DeserializeFromValue<T>(root, lenient);
    }

    /// <summary>
    ///     Reads the TOON of a model's answer leniently, checks it against rules, and deserializes it with the type info of
    ///     its type, without reflection; see <see cref="DeserializeValidated{T}(string, Validation.ToonRuleSet, ToonSerializerOptions)"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="modelOutput">The model's answer.</param>
    /// <param name="rules">The rules the answer must follow.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">Optional deserialization options; strict mode is turned off.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ToonParseException">Thrown when the answer is not TOON even in non-strict mode.</exception>
    /// <exception cref="ToonRuleException">Thrown when the answer breaks a rule of severity error.</exception>
    /// <exception cref="ToonSerializationException">Thrown when the answer does not fit <typeparamref name="T"/>.</exception>
    public static T? DeserializeValidated<T>(string modelOutput, Validation.ToonRuleSet rules, ToonTypeInfo<T> typeInfo,
                                             ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var lenient = Lenient(options);
        var root = ToonDocument.Parse(Extract(modelOutput), lenient.ToonOptions).Root;

        rules.Validate(root).ThrowIfInvalid();
        return ToonSerializer.DeserializeFromValue(root, typeInfo, lenient);
    }

    private static ToonSerializerOptions Lenient(ToonSerializerOptions? options)
    {
        options ??= ToonSerializerOptions.Default;
        return options.ToonOptions.StrictMode ? options.With(options.ToonOptions.NonStrict()) : options;
    }

    /// <summary>
    ///     A TOON template of the documents <typeparamref name="T"/> is written as, to show a model the shape its answer
    ///     should have: a placeholder for each value (<c>&lt;integer&gt;</c>, <c>&lt;string&gt;</c>, <c>&lt;Draft|Active&gt;</c>,
    ///     <c>&lt;date-time&gt;</c>) and <c>N</c> for each length, with the property names, order and forms of the
    ///     serializer.
    /// </summary>
    /// <typeparam name="T">The type of the answer.</typeparam>
    /// <param name="options">Optional serialization options (naming policy, delimiter, indentation).</param>
    /// <returns>The template, such as <c>orders[N]{id,customer,total}:\n  &lt;integer&gt;,&lt;string&gt;,&lt;number&gt;</c>.</returns>
    [RequiresUnreferencedCode(ToonSerializer.ReflectionMessage)]
    [RequiresDynamicCode(ToonSerializer.ReflectionMessage)]
    public static string DescribeShape<T>(ToonSerializerOptions? options = null)
    {
        return ToonSerializer.DescribeShape(typeof(T), options ?? ToonSerializerOptions.Default);
    }

    /// <summary>
    ///     Serializes a value for a prompt: like <see cref="ToonSerializer.Serialize{T}(T, ToonSerializerOptions)"/>, with
    ///     long arrays shortened to their first <see cref="ToonPromptOptions.MaxArrayItems"/> items. A shortened array
    ///     declares the items it has, and a comment line after it says how many were left out (<c># 380 more items</c>),
    ///     so the text is still valid TOON and the model knows the data is incomplete.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="options">The prompt options; without a limit, the text is that of <see cref="ToonSerializer.Serialize{T}(T, ToonSerializerOptions)"/>.</param>
    /// <returns>The TOON text.</returns>
    [RequiresUnreferencedCode(ToonSerializer.ReflectionMessage)]
    [RequiresDynamicCode(ToonSerializer.ReflectionMessage)]
    public static string Serialize<T>(T? value, ToonPromptOptions? options = null)
    {
        var serializerOptions = options?.SerializerOptions ?? ToonSerializerOptions.Default;

        if (options?.MaxArrayItems is not { } max)
        {
            return ToonSerializer.Serialize(value, serializerOptions);
        }

        return Encode(ToonSerializer.SerializeToValue(value, serializerOptions), max, serializerOptions.ToonOptions);
    }

    /// <summary>
    ///     Serializes a value for a prompt with the type info of its type, without reflection; see
    ///     <see cref="Serialize{T}(T, ToonPromptOptions)"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="options">The prompt options.</param>
    /// <returns>The TOON text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeInfo"/> is null.</exception>
    public static string Serialize<T>(T? value, ToonTypeInfo<T> typeInfo, ToonPromptOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        var serializerOptions = options?.SerializerOptions ?? ToonSerializerOptions.Default;
        var text = ToonSerializer.Serialize(value, typeInfo, serializerOptions);

        return options?.MaxArrayItems is { } max
            ? Encode(ToonDocument.Parse(text, serializerOptions.ToonOptions).Root, max, serializerOptions.ToonOptions)
            : text;
    }

    /// <summary>
    ///     Writes a tree for a prompt, with long arrays shortened to <paramref name="maxArrayItems"/> items and a comment
    ///     after each (<c># 380 more items</c>); the tree is not changed. See <see cref="Serialize{T}(T, ToonPromptOptions)"/>.
    /// </summary>
    /// <param name="value">The value, such as <see cref="ToonDocument.Root"/>.</param>
    /// <param name="maxArrayItems">The most items an array keeps (at least 1).</param>
    /// <param name="options">Encoding options; defaults when null.</param>
    /// <returns>The TOON text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxArrayItems"/> is less than 1.</exception>
    public static string Encode(ToonValue value, int maxArrayItems, ToonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxArrayItems, 1);

        var omitted = new Dictionary<ToonArray, int>(ReferenceEqualityComparer.Instance);
        var shortened = Shorten(value, maxArrayItems, omitted);

        options ??= ToonOptions.Default;
        _ = options.ValidatedMaxDepth;

        using var buffer = new ToonTextBuffer();
        var writer = new ToonWriter(buffer, options);
        new PromptWriter(writer, omitted).WriteRoot(shortened);
        return buffer.ToString();
    }

    /// <summary>Writes a comment line after each shortened array, saying how many items were left out.</summary>
    private sealed class PromptWriter(ToonWriter writer, Dictionary<ToonArray, int> omitted) : ToonDomWriter(writer)
    {
        private readonly ToonWriter _writer = writer;

        protected override void WriteAfterArray(ToonArray array, int childDepth)
        {
            if (omitted.TryGetValue(array, out var count))
            {
                // A comment line, which parsers drop (§5.1), at the depth of the items
                _writer.StartLine(childDepth);
                _writer.Write(count == 1 ? "# 1 more item" : $"# {count.ToString(System.Globalization.CultureInfo.InvariantCulture)} more items");
            }
        }
    }

    /// <summary>A copy of the parts of the tree with long arrays, shortened; other parts are shared.</summary>
    private static ToonValue Shorten(ToonValue value, int max, Dictionary<ToonArray, int> omitted)
    {
        switch (value)
        {
            case ToonArray array:
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
                var count = Math.Min(array.Items.Count, max);
                var items = new List<ToonValue>(count);

                for (var i = 0; i < count; i++)
                {
                    items.Add(Shorten(array.Items[i], max, omitted));
                }

                var copy = new ToonArray(items);

                if (array.Items.Count > max)
                {
                    omitted[copy] = array.Items.Count - max;
                }

                return copy;
            }
            case ToonObject obj:
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
                var copy = new ToonObject(obj.Properties.Count);

                foreach (var (key, member) in obj.Properties)
                {
                    copy.Properties.Add(key, Shorten(member, max, omitted));
                }

                return copy;
            }
            default:
                return value;
        }
    }
}

/// <summary>Options of <see cref="ToonPrompt.Serialize{T}(T, ToonPromptOptions)"/>.</summary>
public sealed class ToonPromptOptions
{
    private int? _maxArrayItems;

    /// <summary>
    ///     The most items an array keeps (at least 1); the rest are left out, with a comment saying how many. Null (the
    ///     default) keeps all items.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is less than 1.</exception>
    public int? MaxArrayItems
    {
        get => _maxArrayItems;
        set
        {
            if (value is < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "MaxArrayItems must be at least 1");
            }

            _maxArrayItems = value;
        }
    }

    /// <summary>The serializer options (naming, delimiter, indentation); defaults when null.</summary>
    public ToonSerializerOptions? SerializerOptions { get; set; }
}
