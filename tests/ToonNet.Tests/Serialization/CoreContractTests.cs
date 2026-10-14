using System.Globalization;
using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;
using ToonNet.Core.Validation;

namespace ToonNet.Tests.Serialization;

/// <summary>Promises of the public API that no other test checked.</summary>
public class CoreContractTests
{
    #region Files are not truncated by a failure

    [Fact]
    public async Task SerializeToFileAsync_Failure_KeepsTheFile()
    {
        var path = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(path, "old");

            await Assert.ThrowsAsync<ToonEncodingException>(() => ToonSerializer.SerializeToFileAsync(new { V = "\uD800" }, path).AsTask());
            Assert.Equal("old", await File.ReadAllTextAsync(path));

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ToonSerializer.SerializeToFileAsync(new { V = 1 }, path, cancellationToken: cancelled.Token).AsTask());
            Assert.Equal("old", await File.ReadAllTextAsync(path));

            await ToonSerializer.SerializeToFileAsync(new { V = 1 }, path);
            Assert.Equal("V: 1", await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Encoder_AsyncMethods()
    {
        var document = ToonDocument.Parse("a[2]: 1,2");
        var encoder = new ToonEncoder();

        Assert.Equal("a[2]: 1,2", await encoder.EncodeAsync(document));

        var stream = new MemoryStream();
        await encoder.EncodeToStreamAsync(document, stream);
        Assert.Equal("a[2]: 1,2", System.Text.Encoding.UTF8.GetString(stream.ToArray()));

        var path = Path.GetTempFileName();

        try
        {
            await encoder.EncodeToFileAsync(document, path);
            Assert.Equal("a[2]: 1,2", await File.ReadAllTextAsync(path));

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => encoder.EncodeToFileAsync(document, path, cancelled.Token));
            Assert.Equal("a[2]: 1,2", await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task StreamAndFileMethods_HonourCancellation()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var token = cancelled.Token;
        var bytes = "a: 1"u8.ToArray();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ToonSerializer.DeserializeAsync<object>(new MemoryStream(bytes), cancellationToken: token).AsTask());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ToonSerializer.DeserializeFromStreamAsync<object>(new MemoryStream(bytes), cancellationToken: token).AsTask());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ToonSerializer.SerializeAsync(new MemoryStream(), new { A = 1 }, cancellationToken: token).AsTask());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ToonSerializer.DeserializeAtAsync<int>(new MemoryStream(bytes), "$.a", cancellationToken: token).AsTask());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ToonSerializer.DeserializeAllAsync<int>(new MemoryStream(bytes), "$.a", cancellationToken: token).AsTask());

        var path = Path.GetTempFileName();

        try
        {
            await File.WriteAllBytesAsync(path, bytes);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ToonSerializer.DeserializeFromFileAsync<object>(path, cancellationToken: token).AsTask());
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion

    #region Validator

    [Fact]
    public void Validator_ReportsInvalidOptions_AndZeroBasedPositions()
    {
        var invalid = ToonValidator.Validate("a: 1", new ToonOptions { MaxDepth = 300 });
        Assert.False(invalid.IsValid);
        Assert.Equal(ToonValidationErrorCodes.OptionsInvalid, Assert.Single(invalid.Errors).Code);

        var duplicate = Assert.Single(ToonValidator.Validate("a: 1\na: 2").Errors);
        Assert.Equal((1, 0), (duplicate.LineNumber, duplicate.ColumnNumber)); // line 2, column 1
    }

    #endregion

    #region Converters

    public sealed class Point
    {
        public int X { get; set; }
    }

    private sealed class Prefix(string prefix) : ToonConverter<Point>
    {
        public override ToonValue? Write(Point? value, ToonSerializerOptions options) => value == null ? null : new ToonString(prefix + value.X);

        public override Point? Read(ToonValue value, ToonSerializerOptions options) =>
            new() { X = int.Parse(value.GetValue<string>()[prefix.Length..], CultureInfo.InvariantCulture) };
    }

    private sealed class AttributePrefix() : ToonConverter<Point>
    {
        public override ToonValue? Write(Point? value, ToonSerializerOptions options) => new ToonString("attr" + value!.X);

        public override Point? Read(ToonValue value, ToonSerializerOptions options) => new() { X = int.Parse(value.GetValue<string>()[4..], CultureInfo.InvariantCulture) };
    }

    private sealed class Nothing : ToonConverter<Point>
    {
        public override ToonValue? Write(Point? value, ToonSerializerOptions options) => null;

        public override Point? Read(ToonValue value, ToonSerializerOptions options) => null;
    }

    public sealed class Holder
    {
        [ToonConverter(typeof(AttributePrefix))]
        public Point? Marked { get; set; }

        public Point? Plain { get; set; }
    }

    [Fact]
    public void Converters_Precedence_AndPlaces()
    {
        var options = new ToonSerializerOptions();
        options.AddConverter(new Prefix("first"));
        options.AddConverter(new Prefix("second"));

        // The property's attribute wins over the options; the first converter of the options wins over later ones
        Assert.Equal("Marked: attr1\nPlain: first2", ToonSerializer.Serialize(new Holder { Marked = new() { X = 1 }, Plain = new() { X = 2 } }, options));

        // In lists, arrays, dictionary values and table cells
        Assert.Equal("[2]: first1,first2", ToonSerializer.Serialize(new List<Point> { new() { X = 1 }, new() { X = 2 } }, options));
        Assert.Equal("[1]: first3", ToonSerializer.Serialize(new[] { new Point { X = 3 } }, options));
        Assert.Equal("k: first4", ToonSerializer.Serialize(new Dictionary<string, Point> { ["k"] = new() { X = 4 } }, options));
        Assert.Equal("[2]{Plain}:\n  first5\n  first6", ToonSerializer.Serialize(new[] { new { Plain = new Point { X = 5 } }, new { Plain = new Point { X = 6 } } }, options));

        // And back
        var read = ToonSerializer.Deserialize<Holder>("Marked: attr7\nPlain: first8", options)!;
        Assert.Equal((7, 8), (read.Marked!.X, read.Plain!.X));
        Assert.Equal([1, 2], ToonSerializer.Deserialize<List<Point>>("[2]: first1,first2", options)!.Select(p => p.X));
    }

    [Fact]
    public void Converters_ReturningNull_WithIgnoreNullValues_DropTheField()
    {
        var options = new ToonSerializerOptions { IgnoreNullValues = true };
        options.AddConverter(new Nothing());

        Assert.Equal("Marked: attr1", ToonSerializer.Serialize(new Holder { Marked = new() { X = 1 }, Plain = new() { X = 2 } }, options));
    }

    #endregion

    #region Property collection and document writer

    [Fact]
    public void PropertyCollection_EnumerationAndCollectionMembers()
    {
        var obj = ToonDocument.Parse("a: 1\nb: 2").Root.AsObject();
        var properties = obj.Properties;

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var (key, _) in properties)
            {
                properties.Remove(key);
            }
        });

        // The enumerator is a struct: through the interface, Reset applies to the same one
        using var enumerator = ((IEnumerable<KeyValuePair<string, ToonValue>>)properties).GetEnumerator();
        Assert.True(enumerator.MoveNext());
        enumerator.Reset();
        Assert.True(enumerator.MoveNext());
        Assert.Equal("b", enumerator.Current.Key); // "a" was removed above

        ICollection<KeyValuePair<string, ToonValue>> collection = properties;
        collection.Add(new KeyValuePair<string, ToonValue>("c", new ToonNumber(3L)));
        Assert.Contains(collection, kv => kv.Key == "c");
        Assert.False(collection.IsReadOnly);

        var copy = new KeyValuePair<string, ToonValue>[collection.Count];
        collection.CopyTo(copy, 0);
        Assert.Equal(["b", "c"], copy.Select(kv => kv.Key));
        Assert.True(collection.Remove(copy[1]));
        Assert.Equal(["b"], properties.Keys);
    }

    [Fact]
    public async Task DocumentWriter_Overloads()
    {
        var text = new StringWriter();

        await using (var writer = new ToonDocumentWriter(text))
        {
            writer.WriteStartObject();
            writer.WriteString("span", "abc".AsSpan());
            writer.WriteString("none", (string?)null);
            writer.WriteNumber("big", ulong.MaxValue);
            writer.WriteNumber("f", 0.1f);
            writer.WriteStartArray("v", 3);
            writer.WriteStringValue("x".AsSpan());
            writer.WriteNumberValue(ulong.MaxValue);
            writer.WriteNumberValue(1.5f);
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.Flush();
            await writer.DisposeAsync();
        }

        Assert.Equal("span: abc\nnone: null\nbig: 18446744073709551615\nf: 0.1\nv[3]: x,18446744073709551615,1.5", text.ToString());
    }

    #endregion
}
