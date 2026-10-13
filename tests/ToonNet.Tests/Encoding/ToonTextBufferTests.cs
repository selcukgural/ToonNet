using System.Text;
using ToonNet.Core.Encoding;

namespace ToonNet.Tests.Encoding;

/// <summary>
///     The encoder's chunked character buffer: text written across chunks, truncated back into earlier chunks and
///     encoded to UTF-8 with surrogate pairs split between chunks.
/// </summary>
public class ToonTextBufferTests
{
    private static string Sample(int length)
    {
        var sb = new StringBuilder(length);

        for (var i = 0; sb.Length < length; i++)
        {
            sb.Append(i % 5 == 0 ? "😀" : i % 3 == 0 ? "ç" : "x");
        }

        return sb.ToString(0, length);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(16 * 1024 - 1)]
    [InlineData(16 * 1024 + 1)]
    [InlineData(200_000)]
    [InlineData(3_000_000)]
    public void Text_WrittenAcrossChunks_ComesBackUnchanged(int length)
    {
        var expected = Sample(length);
        using var buffer = new ToonTextBuffer();

        // Mix the append forms so writes start and end at every offset of a chunk
        var position = 0;

        while (position < expected.Length)
        {
            var size = Math.Min(expected.Length - position, (position % 7) switch { 0 => 1, 1 => 3, 2 => 40, 3 => 1000, _ => 9000 });

            if (size == 1)
            {
                buffer.Append(expected[position]);
            }
            else
            {
                var span = buffer.GetSpan(size);
                expected.AsSpan(position, size).CopyTo(span);
                buffer.Advance(size);
            }

            position += size;
        }

        Assert.Equal(expected.Length, buffer.Length);
        Assert.Equal(expected, buffer.ToString());
    }

    [Fact]
    public void Truncate_IntoAnEarlierChunk_KeepsThePrefix()
    {
        var text = Sample(100_000);
        using var buffer = new ToonTextBuffer();
        buffer.Append(text);

        buffer.Truncate(20_000);
        Assert.Equal(text[..20_000], buffer.ToString());

        buffer.Append('-', 50_000).Append("end");
        Assert.Equal(text[..20_000] + new string('-', 50_000) + "end", buffer.ToString());

        buffer.Clear();
        Assert.Equal(0, buffer.Length);
        buffer.Append("again");
        Assert.Equal("again", buffer.ToString());
    }

    [Fact]
    public async Task Utf8Output_WithSurrogatePairsOnChunkBoundaries_MatchesTheEncoding()
    {
        foreach (var offset in new[] { 0, 1, 2, 3 })
        {
            // Put a surrogate pair exactly across the first chunk boundary (16K characters)
            var text = new string('a', 16 * 1024 - 1 + offset % 2) + Sample(70_000);
            using var buffer = new ToonTextBuffer();
            buffer.Append(text.AsSpan(0, 16 * 1024 - 1 + offset % 2));
            buffer.Append(text.AsSpan(16 * 1024 - 1 + offset % 2));

            using var stream = new MemoryStream();
            await buffer.WriteUtf8Async(stream, CancellationToken.None);

            Assert.Equal(System.Text.Encoding.UTF8.GetBytes(text), stream.ToArray());

            using var writer = new StringWriter();
            await buffer.WriteToAsync(writer, CancellationToken.None);
            Assert.Equal(text, writer.ToString());
        }
    }
}
