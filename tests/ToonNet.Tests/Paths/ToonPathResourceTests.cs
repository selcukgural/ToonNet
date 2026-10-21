using System.Diagnostics;
using System.Text;
using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;
using ToonNet.Core.Paths;
using ToonNet.Core.Serialization;
using ToonNet.Core.Validation;

namespace ToonNet.Tests.Paths;

/// <summary>Work and memory of paths, rules and edits on documents built to make them expensive.</summary>
[Collection(MeasurementCollection.Name)]
public class ToonPathResourceTests
{
    private static readonly ToonPathOptions Strict = new()
    {
        MaxLength = 256, MaxNesting = 8, MaxSteps = 100_000, MaxStepsPerValue = 4, Timeout = TimeSpan.FromMilliseconds(200)
    };

    private static void Fast(Action action, int milliseconds = 1_000)
    {
        var watch = Stopwatch.StartNew();
        action();
        Assert.True(watch.ElapsedMilliseconds < milliseconds, $"took {watch.ElapsedMilliseconds} ms");
    }

    private static void LimitedQuickly(Action action) => Fast(() => Assert.Throws<ToonPathLimitException>(action));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PatternsWithLargeAutomata_SpendForTheirSize_BeforeTheyRun(bool defaults)
    {
        // [a-z]{300} has 300 states: linear time, but microseconds per character (.NET 8 rejects [a-z]{1000} as too large)
        var options = defaults ? ToonPathOptions.Default : Strict;
        var text = new string('a', 1_500_000) + "b";
        var tree = new ToonArray([new ToonObject { ["s"] = new ToonString(text) }]);

        LimitedQuickly(() => ToonPath.Parse("$[?search(@.s, '[a-z]*a[a-z]{300}b')]", options).Select(tree));
        LimitedQuickly(() => ToonPath.Parse("$.x[?search(@.s, '[a-z]*a[a-z]{300}b')]", options).SelectFrom("x[1]:\n  - s: " + text + "\n"));

        // The same pattern taken from the document
        var fromDocument = new ToonArray([new ToonObject { ["s"] = new ToonString(text), ["p"] = new ToonString("[a-z]*a[a-z]{300}b") }]);
        LimitedQuickly(() => ToonPath.Parse("$[?search(@.s, @.p)]", options).Select(fromDocument));
    }

    [Fact]
    public void LightPatterns_OnManyValues_StayWithinTheDefaultLimits()
    {
        var items = "items[10000]{e,phone}:\n" + string.Join("\n", Enumerable.Range(0, 10_000).Select(i => $"  user{i}@example.com,555-{i % 10000:D4}"));

        Assert.Equal(10_000, ToonPath.Parse("$.items[?match(@.e, '[a-z0-9]+@[a-z]+\\\\.com')]").SelectFrom(items).Count);
        Assert.Equal(10_000, ToonPath.Parse("$.items[?match(@.phone, '[0-9]{3}-[0-9]{4}')]").Select(ToonDocument.Parse(items).Root).Count);
        Assert.Equal(1, ToonPath.Parse("$.items[?search(@.e, '^user42@')]").SelectFrom(items).Count);
    }

    [Theory]
    [InlineData("(a{1000}){1000}")]
    [InlineData("((a{100}){100}){100}")]
    public void PatternsBeyondTheLargestAutomaton_AreInvalid_AndMatchNothing(string pattern)
    {
        Fast(() => Assert.Empty(ToonPath.Parse($"$[?search(@, '{pattern}')]").Select(new ToonArray([new ToonString("aaaa")]))));
    }

    [Fact]
    public void Patterns_KeepNoLargeStateBetweenEvaluations()
    {
        var random = new Random(3);
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var toon = "s: " + new string(Enumerable.Range(0, 200_000).Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray()) + "\n";
        var generous = new ToonPathOptions { MaxSteps = 1_000_000_000, Timeout = TimeSpan.FromMinutes(1) };

        static long Live()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return GC.GetTotalMemory(true);
        }

        var before = Live();

        // Distinct heavy patterns (one compiled each evaluation) and many light ones (a small shared cache)
        for (var i = 0; i < 4; i++)
        {
            ToonPath.Parse("$[?search(@, '(" + string.Join("|", alphabet.Select(c => c + ".{30}" + c)) + ")!" + i + "')]", generous).SelectFrom(toon);
        }

        for (var i = 0; i < 100; i++)
        {
            ToonPath.Parse("$[?search(@, '(" + string.Join("|", alphabet.Take(6).Select(c => c + ".{4}" + c)) + ")!" + i + "')]").SelectFrom(toon);
        }

        var retained = (Live() - before) / 1_048_576;
        Assert.True(retained < 64, $"{retained} MB kept after the evaluations");
    }

    [Fact]
    public void SelectMatches_SpendsForThePathsItFormats()
    {
        // Deep values with long keys: the path of each match is long, and $..*..* has many matches
        var text = new StringBuilder();

        for (var d = 0; d < 60; d++)
        {
            text.Append(' ', 2 * d).Append(new string((char)('a' + d % 26), 20_000)).Append(d).Append(":\n");
        }

        text.Append(' ', 120).Append("v: 1\n");
        var root = ToonDocument.Parse(text.ToString()).Root;

        LimitedQuickly(() => ToonPath.Parse("$..*..*", Strict).SelectMatches(root));
        LimitedQuickly(() => ToonPath.Parse("$..*..*..*").SelectMatches(root));

        // Plain selections of the same nodes are cheap
        Assert.Equal(61, ToonPath.Parse("$..*", Strict).Select(root).Count);
    }

    [Fact]
    public void TextEvaluation_CountsLines_NotCharacters_ForItsBudget()
    {
        // A long string bought steps (one per character) that nested descendant segments then spent
        var text = new StringBuilder("pad: \"").Append('x', 8_000_000).Append("\"\nd:\n");

        for (var i = 1; i <= 95; i++)
        {
            text.Append(' ', 2 * i).Append("k:\n");
        }

        text.Append(' ', 192).Append("v: 1\n");
        var toon = text.ToString();
        var path = ToonPath.Parse("$.d..*..*..*..*..*");

        // The steps the document buys: MaxSteps, MaxStepsPerValue per value (line), and enough to read the 8 million
        // characters twice (a step per 16), about 2 million in all; a step per character would be over 9 million
        foreach (var budget in new[] { new EvaluationBudget((ToonText)toon, ToonPathOptions.Default), new EvaluationBudget(ToonDocument.Parse(toon).Root, ToonPathOptions.Default) })
        {
            var steps = 0L;

            void SpendAll()
            {
                while (true)
                {
                    budget.Spend();
                    steps++;
                }
            }

            Assert.Throws<ToonPathLimitException>(SpendAll);
            Assert.InRange(steps, 1_000_000 + 2 * (8_000_000 / 16), 1_000_000 + 2 * (8_000_000 / 16) + 16 * 200 + 1_000);
        }

        // An upper bound on the time only (shared CI runners are several times slower than a workstation)
        Fast(() => Assert.Throws<ToonPathLimitException>(() => path.SelectFrom(toon)), 5_000);
        Fast(() => Assert.Throws<ToonPathLimitException>(() => ToonSerializer.DeserializeAll<int?>(toon, "$.d..*..*..*..*..*")), 5_000);
        Fast(() => Assert.Throws<ToonPathLimitException>(() => path.Select(ToonDocument.Parse(toon).Root)), 5_000);
    }

    [Theory]
    [InlineData(',')]
    [InlineData('|')]
    public void TextEvaluation_CountsTheItemsOfInlineArraysAndTheCellsOfTables(char delimiter)
    {
        var d = delimiter == ',' ? "" : delimiter.ToString();
        var inline = $"a[100000{d}]: " + string.Join(delimiter, Enumerable.Range(0, 100_000));
        var table = $"t[50000{d}]{{x{delimiter}y{delimiter}z}}:\n" + string.Join('\n', Enumerable.Range(0, 50_000).Select(i => $"  {i}{delimiter}{i}{delimiter}{i}"));
        var filter = string.Join(" && ", Enumerable.Range(1, 8).Select(i => $"@ != {i}")) + " && search(@, '9')";

        var inlinePath = ToonPath.Parse($"$.a[?{filter}]");
        Assert.Equal(inlinePath.Select(ToonDocument.Parse(inline).Root).Count, inlinePath.SelectFrom(inline).Count);

        var tablePath = ToonPath.Parse($"$.t[?{filter.Replace("@", "@.x")}].y");
        Assert.Equal(tablePath.Select(ToonDocument.Parse(table).Root).Count, tablePath.SelectFrom(table).Count);

        // Delimiters inside quotes and in plain strings are not values
        Assert.Equal(1, EvaluationBudget.CountTextValues("a[1]: \"x,y,z\""));
        Assert.Equal(1, EvaluationBudget.CountTextValues("pad: ,,,,,,,,"));
        Assert.Equal(3, EvaluationBudget.CountTextValues("a[3]: 1,2,3")); // the line and its two delimiters
    }

    [Fact]
    public void RemoveAll_AndMergePatch_RemoveManyMembers_InLinearTime()
    {
        var toon = string.Join("\n", Enumerable.Range(0, 80_000).Select(i => $"k{i}: {i}"));

        var root = ToonDocument.Parse(toon).Root;
        Fast(() => Assert.Equal(80_000, root.RemoveAll("$.*")));
        Assert.Empty(root.AsObject().Properties);

        // Every other member: odd keys hold strings
        var mixed = string.Join("\n", Enumerable.Range(0, 80_000).Select(i => i % 2 == 0 ? $"k{i}: {i}" : $"k{i}: x"));
        root = ToonDocument.Parse(mixed).Root;
        Fast(() => Assert.Equal(40_000, root.RemoveAll("$[?type(@) == 'string']")));
        Assert.Equal(Enumerable.Range(0, 40_000).Select(i => $"k{i * 2}"), root.AsObject().Properties.Keys);
        Assert.Equal(78_000, root["k78000"]!.GetValue<int>());

        var patch = new ToonObject();

        for (var i = 0; i < 40_000; i++)
        {
            patch[$"k{i}"] = ToonNull.Instance;
        }

        patch["added"] = new ToonString("x");
        var target = ToonDocument.Parse(toon).Root;
        Fast(() => target = ToonMergePatch.Apply(target, patch));
        Assert.Equal(40_001, target.AsObject().Properties.Count);
        Assert.Equal(["k40000", "k40001"], target.AsObject().Properties.Keys.Take(2));
        Assert.Equal("added", target.AsObject().Properties.Keys.Last());

        // Items of arrays: every other one, in one pass
        var array = ToonDocument.Parse("[100000]: " + string.Join(",", Enumerable.Range(0, 100_000).Select(i => i % 2 == 0 ? "x" : i.ToString()))).Root;
        Fast(() => Assert.Equal(50_000, array.RemoveAll("$[?type(@) == 'string']")));
        Assert.Equal(Enumerable.Range(0, 50_000).Select(i => i * 2 + 1), array.AsArray().Items.Select(v => v.GetValue<int>()));
    }

    [Fact]
    public void ErrorText_EscapesAndShortensKeysFromTheDocument()
    {
        var forged = Assert.Throws<ToonSerializationException>(() => ToonSerializer.DeserializeAll<int>("\"x\\n2026-10-14 ERROR forged\": abc\n", "$.*"));
        Assert.DoesNotContain('\n', forged.Message);
        Assert.Contains(@"$['x\n2026-10-14 ERROR forged']", forged.Message);

        var path = Assert.Throws<ToonPathException>(() => ToonPath.Parse("$[?@.a\n\n2026-10-14 INFO login ok\n]"));
        Assert.DoesNotContain('\n', path.Message);

        var longKey = new string('k', 1_000_000);
        var shortened = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Dictionary<string, int>>($"\"{longKey}\": x"));
        Assert.True(shortened.Message.Length < 300, $"{shortened.Message.Length} characters");
        Assert.Contains("$['" + new string('k', 64) + "'...]", shortened.Message);

        // Bidirectional controls, C1 controls (the 8-bit terminal escape) and invisible characters are escaped
        foreach (var (key, escaped) in new[] { ("a\u202Eb", @"a\u202Eb"), ("a\u009Bb", @"a\u009Bb"), ("a\u200Bb", @"a\u200Bb"), ("a\uFEFFb", @"a\uFEFFb") })
        {
            var ex = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Dictionary<string, int>>($"\"{key}\": x"));
            Assert.Contains(escaped, ex.Message);
            Assert.DoesNotContain(key, ex.Message);
        }
    }

    [Fact]
    public void Rules_CannotBeChangedThroughTheList_AndCanBeAddedWhileValidating()
    {
        var rules = new ToonRuleSet().Rule("$.a", "@ > 0", "bad");

        Assert.Throws<NotSupportedException>(() => ((IList<ToonRule>)rules.Rules).Clear());
        Assert.IsNotType<List<ToonRule>>(rules.Rules);

        var adding = Task.Run(() =>
        {
            for (var i = 0; i < 2_000; i++)
            {
                rules.Rule("$.a", "@ > -1", "never");
            }
        });

        while (!adding.IsCompleted)
        {
            Assert.Single(rules.Validate("a: 0").Errors, e => e.Message == "bad");
        }

        adding.Wait();
        Assert.Equal(2_001, rules.Rules.Count);
    }
    [Fact]
    public void ManyLightPatterns_AreCompiledOncePerEvaluation()
    {
        // 17 patterns pushed each other out of the shared cache of 16, and each match compiled one again (10 s for 300 KB)
        var items = new StringBuilder("a[20000]:");

        for (var i = 0; i < 20_000; i++)
        {
            items.Append("\n  - v: x").Append(i % 32);
        }

        var tree = ToonDocument.Parse(items.ToString()).Root;

        foreach (var count in new[] { 17, 32 })
        {
            var filter = string.Join("||", Enumerable.Range(0, count).Select(i => $"match(@.v,'x{i}')"));
            var path = ToonPath.Parse($"$.a[?{filter}]");
            IReadOnlyList<ToonValue>? selected = null;

            Fast(() => selected = path.Select(tree));
            Assert.Equal(count * 625, selected!.Count);
            Fast(() => Assert.Equal(selected.Count, path.SelectFrom(items.ToString()).Count));
        }
    }

    [Fact]
    public void Paths_HaveAtMost32PatternLiterals()
    {
        var filter = string.Join("||", Enumerable.Range(0, 33).Select(i => $"search(@,'p{i}')"));

        var path = $"$[?{filter}]";
        var ex = Assert.Throws<ToonPathException>(() => ToonPath.Parse(path));

        Assert.Equal("A path has at most 32 pattern literals", ex.Reason);
        Assert.Equal(path.IndexOf("search(@,'p32')", StringComparison.Ordinal), ex.Position);
        Assert.NotNull(ToonPath.Parse("$[?" + string.Join("||", Enumerable.Range(0, 32).Select(i => $"search(@,'p{i}')")) + "]"));
    }

    [Fact]
    public void PatternsFromTheDocument_SpendForCompilingThem()
    {
        // Each value with a pattern of its own: compiling one takes as long as thousands of steps
        var items = string.Join("\n", Enumerable.Range(0, 2_000).Select(i => $"  - v: abc\n    p: \"[a-c]+{i}\""));
        var text = "a[2000]:\n" + items;

        LimitedQuickly(() => ToonPath.Parse("$.a[?match(@.v, @.p)]").Select(ToonDocument.Parse(text).Root));
        LimitedQuickly(() => ToonPath.Parse("$.a[?match(@.v, @.p)]").SelectFrom(text));
    }

    [Fact]
    public void RepeatedSelectors_CannotKeepMoreNodesThanTheDocumentHas()
    {
        // Duplicates are kept (RFC 9535): 2,000 wildcards over 100,000 items kept 16 nodes per value until the steps ran out
        var tree = new ToonObject { ["a"] = new ToonArray(Enumerable.Range(0, 100_000).Select(i => (ToonValue)new ToonNumber(i)).ToList()) };
        var path = ToonPath.Parse("$.a[" + string.Join(',', Enumerable.Repeat("*", 2_000)) + "]");

        var ex = Assert.Throws<ToonPathLimitException>(() => path.Select(tree));
        Assert.StartsWith("The path selects more than 1200004 values at once", ex.Message);
        Assert.False(ex.IsTimeout);
        Assert.Throws<ToonPathLimitException>(() => path.SelectMatches(tree));

        // Up to two per value, beyond the first million
        Assert.Equal(1_200_000, ToonPath.Parse("$.a[" + string.Join(',', Enumerable.Repeat("*", 12)) + "]").Select(tree).Count);
    }

    [Fact]
    public void TextEvaluation_LooksUpTheKeysOfFiltersOfManyNames()
    {
        // Ten objects of 50,000 members and a filter of 6,000 names: the text evaluator compared every key with every name
        var text = new StringBuilder("items[10]:\n");

        for (var o = 0; o < 10; o++)
        {
            for (var m = 0; m < 50_000; m++)
            {
                text.Append(m == 0 ? "  - " : "    ").Append('k').Append(m.ToString("D5")).Append(": 1\n");
            }
        }

        var toon = text.ToString();
        var options = new ToonPathOptions { MaxLength = 200_000 };
        var filter = string.Join("||", Enumerable.Range(0, 6_000).Select(i => $"@.x{i}==2").Append("@.k49999==1"));
        var path = ToonPath.Parse($"$.items[?{filter}]", options);

        // Within the default limits and the time of a single name, with the tree's result
        Fast(() => Assert.Equal(10, path.SelectFrom(toon).Count));
        Assert.Equal(10, path.Select(ToonDocument.Parse(toon).Root).Count);

        // A filter reading a member through a function (length) reads only the members it names, also with many names
        var lengths = ToonPath.Parse("$.items[?" + string.Join("||", Enumerable.Range(0, 6_000).Select(i => $"length(@.x{i})>9")) + "||@.k00007>0]", options);
        Fast(() => Assert.Equal(10, lengths.SelectFrom(toon).Count));
    }

    [Fact]
    public void TextEvaluation_SpendsForTheOperandsOfFilters()
    {
        // 160 contains() operands per member value: each reads the value, which was free in the text evaluator
        var value = new string('a', 1_000);
        var toon = "items[2000]:\n" + string.Concat(Enumerable.Repeat($"  - s: {value}\n", 2_000));
        var filter = string.Join("||", Enumerable.Range(0, 160).Select(i => $"contains(@.s,'z{i}')"));
        var path = ToonPath.Parse($"$.items[?{filter}]");

        LimitedQuickly(() => path.SelectFrom(toon));
        LimitedQuickly(() => path.Select(ToonDocument.Parse(toon).Root));

        // Many operands on short values: each operand is a step
        var shortValues = "items[20000]:\n" + string.Concat(Enumerable.Repeat("  - s: abc\n", 20_000));
        LimitedQuickly(() => path.SelectFrom(shortValues));
        LimitedQuickly(() => path.Select(ToonDocument.Parse(shortValues).Root));
    }

    [Fact]
    public void Contains_SpendsForTheLengthOfThePart()
    {
        // Looking for a part of 4,000 characters in a text of a million compares it at every position
        var haystack = string.Concat(Enumerable.Repeat("ab", 500_000));
        var needle = string.Concat(Enumerable.Repeat("ab", 2_000)) + "aa";
        var tree = new ToonArray([new ToonObject { ["s"] = new ToonString(haystack) }]);
        var path = ToonPath.Parse($"$[?contains(@.s, '{needle}')]");

        LimitedQuickly(() => path.Select(tree));
        LimitedQuickly(() => ToonPath.Parse($"$.x[?contains(@.s, '{needle}')]").SelectFrom("x[1]:\n  - s: " + haystack + "\n"));
        Assert.Single(ToonPath.Parse("$[?contains(@.s, 'abab')]").Select(tree));
    }

    [Fact]
    public void PathsInMessages_AreEscapedAndShortened()
    {
        // Normalized paths escape only C0 controls (RFC 9535); messages escape the rest and cut long paths
        var key = "k\u0085ERROR forged\u2028line\u202Etxt";
        var tree = new ToonObject { [key] = -1, [new string('L', 100_000)] = -1 };
        var result = new ToonRuleSet().Rule("$.*", "@ >= 0", "negative at {path}").Validate(tree);

        var first = result.Violations[0];
        Assert.Equal($"$['{key}']", first.Path);
        Assert.Equal(@"negative at $['k\u0085ERROR forged\u2028line\u202Etxt']", first.Message);
        Assert.Equal(@"$['k\u0085ERROR forged\u2028line\u202Etxt']: negative at $['k\u0085ERROR forged\u2028line\u202Etxt']", first.ToString());

        var second = result.Violations[1];
        Assert.Equal(100_005, second.Path.Length);
        Assert.Equal("negative at $['" + new string('L', 61) + "..." + new string('L', 158) + "']", second.Message);

        var thrown = Assert.Throws<ToonRuleException>(() => result.ThrowIfInvalid());
        Assert.DoesNotContain('\u2028', thrown.Message);
        Assert.True(thrown.Message.Length < 1_000, $"{thrown.Message.Length} characters");

        var unexpected = Assert.Throws<ToonPathException>(() => ToonPath.Parse("$\u202E"));
        Assert.Equal(@"Unexpected '\u202E'", unexpected.Reason);

        var several = Assert.Throws<InvalidOperationException>(() => ToonPath.Parse("$['a\u2028b'].*").SetValue(tree, 1));
        Assert.DoesNotContain('\u2028', several.Message);
        Assert.Contains(@"$['a\u2028b'].*", several.Message);
    }

    [Fact]
    public void CompilingPatterns_CostsTheSame_WhateverRanBefore()
    {
        // The compile was charged only when the shared cache missed: a small budget failed or not depending on whether
        // another evaluation had compiled the pattern
        var tree = new ToonArray([new ToonObject { ["a"] = "xyz" }]);
        var small = new ToonPathOptions { MaxSteps = 3_000, MaxStepsPerValue = 0 };

        Assert.Single(ToonPath.Parse("$[?match(@.a, 'x.*')]").Select(tree));
        Assert.Throws<ToonPathLimitException>(() => ToonPath.Parse("$[?match(@.a, 'x.*')]", small).Select(tree));

        var fromDocument = new ToonArray([new ToonObject { ["a"] = "xyz", ["p"] = "x.*" }]);
        Assert.Single(ToonPath.Parse("$[?match(@.a, @.p)]").Select(fromDocument));
        Assert.Throws<ToonPathLimitException>(() => ToonPath.Parse("$[?match(@.a, @.p)]", small).Select(fromDocument));

        // Once per evaluation: the same pattern on many values is compiled once
        var many = new ToonArray(Enumerable.Range(0, 1_000).Select(_ => (ToonValue)new ToonObject { ["a"] = "xyz", ["p"] = "x.*" }).ToList());
        Assert.Equal(1_000, ToonPath.Parse("$[?match(@.a, @.p)]", new ToonPathOptions { MaxSteps = 50_000, MaxStepsPerValue = 0 }).Select(many).Count);
    }
}
