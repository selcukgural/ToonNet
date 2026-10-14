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
        Assert.Contains("Duplicate field 'a'", duplicate.Message);

        // With several repeated names, the first one is reported, as for short lists
        var first = Assert.Throws<ToonParseException>(() => ToonDocument.Parse($"[0]{{x,{fields},y,y,x}}:"));
        Assert.Contains("Duplicate field 'x'", first.Message);
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
}
