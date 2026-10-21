using System.Buffers;
using System.Diagnostics;
using System.Text;
using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Parsing;

/// <summary>
///     Untrusted input must cost memory and time in proportion to its size: declared lengths, line breaks, field lists and
///     errors must not make the reader reserve, keep or leak more than the document holds.
/// </summary>
public class ResourceLimitTests
{
    private static long Allocated(Action action)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private const long MiB = 1024 * 1024;

    [Fact]
    public void DeclaredLengths_DoNotReserveMemoryForItemsThatAreNotThere()
    {
        // Nested lists with huge declared lengths, followed by many blank lines
        var nested = new StringBuilder("[1000000]:\n");

        for (var depth = 1; depth < 40; depth++)
        {
            nested.Append(' ', depth * 2).Append("- [1000000]:\n");
        }

        nested.Append('\n', 200_000);
        var nestedText = nested.ToString();

        var allocated = Allocated(() => Record.Exception(() => ToonSerializer.Deserialize<object>(nestedText)));
        Assert.True(allocated < 40 * MiB, $"{allocated / MiB} MiB for a {nestedText.Length / 1024} KB document");

        // Many sibling arrays with huge declared lengths (non-strict mode does not check the counts)
        var siblings = new StringBuilder();

        for (var i = 0; i < 20_000; i++)
        {
            siblings.Append('k').Append(i).Append("[99999999]:\n");
        }

        var siblingText = siblings.ToString();
        var options = new ToonOptions { StrictMode = false };

        allocated = Allocated(() => ToonDocument.Parse(siblingText, options));
        Assert.True(allocated < 60 * MiB, $"{allocated / MiB} MiB for a {siblingText.Length / 1024} KB document");
    }

    [Fact]
    public void Tables_ReserveTheirRowsExactly()
    {
        var text = "rows[5000]{a,b}:\n" + string.Concat(Enumerable.Range(0, 5000).Select(i => $"  {i},x\n"));
        var rows = (ToonArray)((ToonObject)ToonDocument.Parse(text).Root).Properties["rows"];

        Assert.Equal(5000, rows.Count);
    }

    [Fact]
    public void HonestListLengths_AreReservedAtOnce()
    {
        // 10,000 items in one array (40 KB), not a series of growing arrays that ends on the large object heap
        var text = "[10000]:\n" + string.Concat(Enumerable.Range(0, 10_000).Select(i => $"  - {i}\n"));
        ToonSerializer.Deserialize<List<int>>(text);

        var allocated = Allocated(() => ToonSerializer.Deserialize<List<int>>(text));
        Assert.True(allocated < 64 * 1024, $"{allocated / 1024} KB for 10,000 integers");
    }

    [Fact]
    public void LineBreaks_DoNotInflateTheLineTable()
    {
        var bytes = new byte[4 * MiB];
        bytes.AsSpan().Fill((byte)'\n');
        bytes[0] = (byte)'a';
        bytes[1] = (byte)':';
        bytes[2] = (byte)' ';
        bytes[3] = (byte)'1';

        // The decoded text is 8 MiB; the line table must not add a multiple of that
        var allocated = Allocated(() => ToonSerializer.Deserialize<Dictionary<string, int>>(bytes));
        Assert.True(allocated < 24 * MiB, $"{allocated / MiB} MiB for 4 MiB of line breaks");
    }

    [Fact]
    public void LongFieldLists_AreCheckedForDuplicatesInLinearTime()
    {
        var fields = string.Join(",", Enumerable.Range(0, 60_000).Select(i => $"f{i:D6}"));
        var stopwatch = Stopwatch.StartNew();

        ToonDocument.Parse($"[0]{{{fields}}}:");
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"{stopwatch.Elapsed.TotalSeconds:F1} s for 60,000 fields");

        var duplicate = Assert.Throws<ToonParseException>(() => ToonDocument.Parse($"[0]{{a,{fields},b,a}}:"));
        Assert.Contains("Duplicate field \"a\"", duplicate.Message);

        // With several repeated names, the first one is reported, as for short lists
        var first = Assert.Throws<ToonParseException>(() => ToonDocument.Parse($"[0]{{x,{fields},y,y,x}}:"));
        Assert.Contains("Duplicate field \"x\"", first.Message);
    }

    [Fact]
    public void InvalidUtf8_DoesNotLeaveDecodedTextInThePool()
    {
        const string secret = "SECRET-TOKEN-4711";
        byte[] bytes = [.. System.Text.Encoding.UTF8.GetBytes(new string('S', 3000) + secret), 0xFF];

        Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<Dictionary<string, string>>(bytes));
        Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<Dictionary<string, string>>(new MemoryStream(bytes)));
        Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<Dictionary<string, string>>(new TrickleStream(bytes, 1000)));

        // The same thread rents the buffers it returned
        for (var size = 1024; size <= 64 * 1024; size *= 2)
        {
            var rented = ArrayPool<char>.Shared.Rent(size);

            try
            {
                Assert.True(rented.AsSpan().IndexOf(secret.AsSpan()) < 0, $"document text left in a pooled array of {rented.Length}");
            }
            finally
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }

    [Fact]
    public void EmptyRoots_HaveNoLineNumber()
    {
        using var reader = new ToonDocumentReader("\n  \n");
        Assert.True(reader.Read());
        Assert.Equal(0, reader.LineNumber);
    }
    /// <summary>A table whose header nests <paramref name="levels"/> field groups (<c>{a{a{...}}}</c>) around one field.</summary>
    private static string NestedGroups(int levels, int rows)
    {
        var text = new StringBuilder("t[").Append(rows).Append(']');
        text.Insert(text.Length, "{a", levels).Append('}', levels).Append(':');

        for (var i = 0; i < rows; i++)
        {
            text.Append("\n  1");
        }

        return text.ToString();
    }

    [Fact]
    public void NestedFieldGroups_MakeObjectsInProportionToTheDocument()
    {
        // 97 objects per row of one character: 9.7 million objects from 400 KB
        var text = NestedGroups(97, 100_000);
        const string message = "The nested field groups of the rows make more than 200150 objects";

        var allocated = Allocated(() => Assert.StartsWith(message, Assert.Throws<ToonParseException>(() => ToonDocument.Parse(text)).Message));
        Assert.True(allocated < 100 * MiB, $"{allocated / MiB} MiB");

        Assert.StartsWith(message, Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<object>(text)).Message);

        using (var reader = new ToonDocumentReader(text))
        {
            Assert.StartsWith(message, Assert.Throws<ToonParseException>(() =>
            {
                while (reader.Read())
                {
                }
            }).Message);
        }

        using (var reader = new ToonDocumentReader(text))
        {
            Assert.True(reader.Read());
            Assert.StartsWith(message, Assert.Throws<ToonParseException>(() => reader.Skip()).Message);
        }

        var error = Assert.Single(ToonNet.Core.Validation.ToonValidator.Validate(text).Errors);
        Assert.StartsWith(message, error.Message);

        // Keyed tables count their entry rows the same way
        var keyed = new StringBuilder("t[100000:]{a{a{a{a{a{a{a{a{a{a}}}}}}}}}}:");

        for (var i = 0; i < 100_000; i++)
        {
            keyed.Append("\n  k").Append(i).Append(": 1");
        }

        Assert.StartsWith("The nested field groups of the rows make more than", Assert.Throws<ToonParseException>(() => ToonDocument.Parse(keyed.ToString())).Message);
    }

    [Fact]
    public void NestedFieldGroups_WithinTheBudget_AreRead()
    {
        // Two groups per row of one character: one object per two characters, the most a document may have
        var text = NestedGroups(3, 100_000);
        var rows = ToonDocument.Parse(text).Root.AsObject()["t"]!.AsArray();

        Assert.Equal(100_000, rows.Count);
        var last = rows[99_999].AsObject();
        Assert.Equal(1, Assert.Single(last.Properties).Value.AsObject()["a"]!.AsObject()["a"]!.GetValue<int>());

        // A short document may have more per character
        Assert.Equal(600, ToonDocument.Parse(NestedGroups(97, 600)).Root.AsObject()["t"]!.AsArray().Count);
    }

    [Fact]
    public void NestedFieldGroups_ReserveTheEntriesOfTheirOwnLevel()
    {
        // Each object reserved the entries of all the groups below it: 97 levels took a hundred times the memory
        var text = NestedGroups(97, 600);

        var allocated = Allocated(() => ToonDocument.Parse(text));

        Assert.True(allocated < 15 * MiB, $"{allocated / MiB} MiB");
    }
    [Fact]
    public void WideRowsOfEmptyCells_UseMemoryInProportionToTheDocument()
    {
        // A thousand empty cells per row, one character each: every cell was a string of its own, and every row a hash
        // index of its keys (548 MB for 3.8 MB)
        var text = new StringBuilder("t[4000]{");
        text.AppendJoin(',', Enumerable.Range(0, 1000).Select(i => "f" + i)).Append("}:");
        var row = "\n  " + new string(',', 999);

        for (var i = 0; i < 4000; i++)
        {
            text.Append(row);
        }

        var input = text.ToString();
        ToonDocument? document = null;
        var allocated = Allocated(() => document = ToonDocument.Parse(input));

        Assert.True(allocated < 100 * MiB, $"{allocated / MiB} MiB");

        // Lookups by key still work on wide rows
        var last = document!.Root.AsObject()["t"]!.AsArray()[3999].AsObject();
        Assert.Equal(1000, last.Properties.Count);
        Assert.Equal("", last["f999"]!.GetValue<string>());
        Assert.Equal("f500", last.Properties.Keys[500]);
        Assert.False(last.Properties.ContainsKey("f1000"));
    }

    [Fact]
    public void WideRows_InNonStrictMode_TakeTheLastCellOfARepeatedField()
    {
        var fields = string.Join(',', Enumerable.Range(0, 20).Select(i => "f" + i)) + ",f3";
        var cells = string.Join(',', Enumerable.Range(0, 20)) + ",99";
        var row = ToonDocument.Parse($"t[1]{{{fields}}}:\n  {cells}", new ToonOptions { StrictMode = false }).Root.AsObject()["t"]!.AsArray()[0].AsObject();

        Assert.Equal(20, row.Properties.Count);
        Assert.Equal(99, row["f3"]!.GetValue<int>());
        Assert.Equal("f3", row.Properties.Keys[3]);
    }
}
