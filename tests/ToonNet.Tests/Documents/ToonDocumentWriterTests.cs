using System.Buffers;
using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using Text = System.Text;

namespace ToonNet.Tests.Documents;

public class ToonDocumentWriterTests
{
    private static string Write(Action<ToonDocumentWriter> write, ToonOptions? options = null)
    {
        var text = new StringWriter();

        using (var writer = new ToonDocumentWriter(text, options))
        {
            write(writer);
            Assert.True(writer.IsComplete);
        }

        return text.ToString();
    }

    [Fact]
    public void Document_WithEveryForm()
    {
        var text = Write(w =>
        {
            w.WriteStartObject();
            w.WriteString("name", "Spring sale");
            w.WriteNumber("count", 3);
            w.WriteNumber("price", 9.99m);
            w.WriteNumber("ratio", 0.5);
            w.WriteBoolean("active", true);
            w.WriteNull("note");
            w.WriteStartArray("tags", 2);
            w.WriteStringValue("a,b");
            w.WriteStringValue("c");
            w.WriteEndArray();
            w.WriteStartTable("users", 2, ["id", "name"]);
            w.WriteNumberValue(1);
            w.WriteStringValue("Ada");
            w.WriteNumberValue(2);
            w.WriteStringValue("Bob");
            w.WriteEndArray();
            w.WriteStartKeyedTable("servers", 2, ["host", "port"]);
            w.WriteRowKey("eu");
            w.WriteStringValue("eu.example.com");
            w.WriteNumberValue(443);
            w.WriteRowKey("us");
            w.WriteStringValue("us.example.com");
            w.WriteNumberValue(8443);
            w.WriteEndArray();
            w.WriteStartList("items", 4);
            w.WriteNumberValue(1);
            w.WriteStartObject();
            w.WriteString("kind", "box");
            w.WriteStartArray("sizes", 2);
            w.WriteNumberValue(1);
            w.WriteNumberValue(2);
            w.WriteEndArray();
            w.WriteEndObject();
            w.WriteStartObject();
            w.WriteEndObject();
            w.WriteStartArray(2);
            w.WriteBooleanValue(false);
            w.WriteNullValue();
            w.WriteEndArray();
            w.WriteEndArray();
            w.WriteStartObject("address");
            w.WriteString("city", "Oslo");
            w.WriteEndObject();
            w.WriteStartArray("empty", 0);
            w.WriteEndArray();
            w.WriteEndObject();
        });

        Assert.Equal("""
                     name: Spring sale
                     count: 3
                     price: 9.99
                     ratio: 0.5
                     active: true
                     note: null
                     tags[2]: "a,b",c
                     users[2]{id,name}:
                       1,Ada
                       2,Bob
                     servers[2:]{host,port}:
                       eu: eu.example.com,443
                       us: us.example.com,8443
                     items[4]:
                       - 1
                       - kind: box
                         sizes[2]: 1,2
                       -
                       - [2]: false,null
                     address:
                       city: Oslo
                     empty: []
                     """, text);

        // The text is the encoder's for the same data
        Assert.Equal(new ToonEncoder().Encode(ToonDocument.Parse(text)), text);
    }

    [Fact]
    public void RootValues()
    {
        Assert.Equal("", Write(w =>
        {
            w.WriteStartObject();
            w.WriteEndObject();
        }));
        Assert.Equal("[2]: 1,2", Write(w =>
        {
            w.WriteStartArray(2);
            w.WriteNumberValue(1);
            w.WriteNumberValue(2);
            w.WriteEndArray();
        }));
        Assert.Equal("[]", Write(w =>
        {
            w.WriteStartList(0);
            w.WriteEndArray();
        }));
        Assert.Equal("[1]{a}:\n  x", Write(w =>
        {
            w.WriteStartTable(1, ["a"]);
            w.WriteStringValue("x");
            w.WriteEndArray();
        }));
        Assert.Equal("[2:]{a}:\n  k: 1\n  l: 2", Write(w =>
        {
            w.WriteStartKeyedTable(2, ["a"]);
            w.WriteRowKey("k");
            w.WriteNumberValue(1);
            w.WriteRowKey("l");
            w.WriteNumberValue(2);
            w.WriteEndArray();
        }));
        Assert.Equal("hello", Write(w => w.WriteStringValue("hello")));
        Assert.Equal("\"\\uFEFFx\"".Replace("\\uFEFF", "\uFEFF"), Write(w => w.WriteStringValue("\uFEFFx")));
        Assert.Equal("null", Write(w => w.WriteNullValue()));
    }

    [Fact]
    public void Numbers_UseTheCanonicalForms()
    {
        var text = Write(w =>
        {
            w.WriteStartArray(7);
            w.WriteNumberValue(long.MinValue);
            w.WriteNumberValue(ulong.MaxValue);
            w.WriteNumberValue(1.50m);
            w.WriteNumberValue(1e21);
            w.WriteNumberValue(double.NaN);
            w.WriteNumberValue(0.1f);
            w.WriteNumberValue(-0.0);
            w.WriteEndArray();
        });

        Assert.Equal("[7]: -9223372036854775808,18446744073709551615,1.5,1e+21,null,0.1,0", text);
    }

    [Fact]
    public void OptionsAndQuoting_FollowTheEncoder()
    {
        var options = new ToonOptions { Delimiter = '|', IndentSize = 4 };
        var text = Write(w =>
        {
            w.WriteStartObject();
            w.WriteStartTable("rows", 1, ["a b", "c"]);
            w.WriteStringValue("x|y");
            w.WriteStringValue("1,2");
            w.WriteEndArray();
            w.WriteStartObject("nested");
            w.WriteString("key: odd", "true");
            w.WriteEndObject();
            w.WriteEndObject();
        }, options);

        Assert.Equal("rows[1|]{\"a b\"|c}:\n    \"x|y\"|1,2\nnested:\n    \"key: odd\": \"true\"", text);
        Assert.Equal(new ToonEncoder(options).Encode(ToonDocument.Parse(text, options)), text);
    }

    [Fact]
    public void ToonValueTrees_AreWrittenInTheEncodersForm()
    {
        var tree = ToonDocument.Parse("orders[2]{id,customer{name}}:\n  1,Ada\n  2,Bob").Root;
        var orders = ((ToonObject)tree).Properties["orders"];

        var text = Write(w =>
        {
            w.WriteStartObject();
            w.WriteValue("orders", orders);
            w.WriteStartList("more", 1);
            w.WriteValue(orders);
            w.WriteEndArray();
            w.WriteEndObject();
        });

        Assert.Equal(new ToonEncoder().Encode(new ToonDocument(new ToonObject
        {
            ["orders"] = orders,
            ["more"] = new ToonArray([orders])
        })), text);
    }

    [Fact]
    public void Destinations_GetTheSameText()
    {
        static void WriteDocument(ToonDocumentWriter w)
        {
            w.WriteStartObject();
            w.WriteString("text", "ünï 😀");
            w.WriteEndObject();
        }

        var expected = Write(WriteDocument);
        var bytes = Text.Encoding.UTF8.GetBytes(expected);

        var bufferWriter = new ArrayBufferWriter<byte>();
        using (var writer = new ToonDocumentWriter(bufferWriter))
        {
            WriteDocument(writer);
        }

        Assert.Equal(bytes, bufferWriter.WrittenSpan.ToArray());

        var stream = new MemoryStream();
        using (var writer = new ToonDocumentWriter(stream))
        {
            WriteDocument(writer);
        }

        Assert.Equal(bytes, stream.ToArray());
    }

    [Fact]
    public async Task Flushing_KeepsTheTextWhole()
    {
        // A large table flushed every 100 rows, synchronously and asynchronously
        var stream = new MemoryStream();
        var asyncStream = new MemoryStream();

        using (var writer = new ToonDocumentWriter(stream))
        await using (var asyncWriter = new ToonDocumentWriter(asyncStream))
        {
            foreach (var w in new[] { writer, asyncWriter })
            {
                w.WriteStartObject();
                w.WriteStartTable("rows", 1000, ["id", "label"]);
            }

            for (var i = 0; i < 1000; i++)
            {
                foreach (var w in new[] { writer, asyncWriter })
                {
                    w.WriteNumberValue(i);
                    w.WriteStringValue($"row-{i}-ğ");
                }

                if (i % 100 == 99)
                {
                    writer.Flush();
                    await asyncWriter.FlushAsync();
                }
            }

            foreach (var w in new[] { writer, asyncWriter })
            {
                w.WriteEndArray();
                w.WriteEndObject();
            }
        }

        var text = Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal(text, Text.Encoding.UTF8.GetString(asyncStream.ToArray()));

        var rows = (ToonArray)((ToonObject)ToonDocument.Parse(text).Root).Properties["rows"];
        Assert.Equal(1000, rows.Count);
        Assert.Equal("row-999-ğ", ((ToonString)((ToonObject)rows[999]).Properties["label"]).Value);
    }

    public static TheoryData<string, Action<ToonDocumentWriter>> Misuses => new()
    {
        { "property at the root", w => w.WriteString("a", "b") },
        { "value in an object", w => { w.WriteStartObject(); w.WriteStringValue("x"); } },
        { "object in an inline array", w => { w.WriteStartArray(1); w.WriteStartObject(); } },
        { "object in a table", w => { w.WriteStartTable(1, ["a"]); w.WriteStartObject(); } },
        { "table in a list", w => { w.WriteStartList(1); w.WriteStartTable(1, ["a"]); } },
        { "too many items", w => { w.WriteStartArray(1); w.WriteNumberValue(1); w.WriteNumberValue(2); } },
        { "too few items", w => { w.WriteStartArray(2); w.WriteNumberValue(1); w.WriteEndArray(); } },
        { "incomplete row", w => { w.WriteStartTable(1, ["a", "b"]); w.WriteNumberValue(1); w.WriteEndArray(); } },
        { "keyed row without key", w => { w.WriteStartKeyedTable(1, ["a"]); w.WriteNumberValue(1); } },
        { "keyed row key twice", w => { w.WriteStartKeyedTable(2, ["a", "b"]); w.WriteRowKey("k"); w.WriteNumberValue(1); w.WriteRowKey("l"); } },
        { "row key outside a keyed table", w => { w.WriteStartTable(1, ["a"]); w.WriteRowKey("k"); } },
        { "end object for an array", w => { w.WriteStartArray(0); w.WriteEndObject(); } },
        { "end array for an object", w => { w.WriteStartObject(); w.WriteEndArray(); } },
        { "end without start", w => w.WriteEndObject() },
        { "second root", w => { w.WriteNullValue(); w.WriteNullValue(); } },
        { "value after the root object", w => { w.WriteStartObject(); w.WriteEndObject(); w.WriteNumberValue(1); } },
        { "tree object in a table", w => { w.WriteStartTable(1, ["a"]); w.WriteValue(new ToonObject()); } }
    };

    [Theory]
    [MemberData(nameof(Misuses))]
    public void Misuse_Throws(string name, Action<ToonDocumentWriter> misuse)
    {
        using var writer = new ToonDocumentWriter(new StringWriter());
        Assert.True(Assert.ThrowsAny<InvalidOperationException>(() => misuse(writer)) is not ObjectDisposedException, name);
    }

    [Fact]
    public void InvalidArguments_Throw()
    {
        using var writer = new ToonDocumentWriter(new StringWriter());
        writer.WriteStartObject();

        Assert.Throws<ArgumentNullException>(() => writer.WriteString(null!, "x"));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteStartArray("a", -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteStartKeyedTable("a", 0, ["x"]));
        Assert.Throws<ArgumentException>(() => writer.WriteStartTable("a", 1, []));
        Assert.Throws<ArgumentException>(() => writer.WriteStartTable("a", 1, ["x", "x"]));
        Assert.Throws<ArgumentException>(() => writer.WriteStartTable("a", 1, ["x", null!]));
        Assert.Throws<ArgumentNullException>(() => new ToonDocumentWriter((Stream)null!));
        Assert.Throws<ToonEncodingException>(() => writer.WriteString("bad", "\uD800"));
    }

    [Fact]
    public void Depth_IsLimitedByTheOptions()
    {
        using var writer = new ToonDocumentWriter(new StringWriter(), new ToonOptions { MaxDepth = 2 });
        writer.WriteStartObject();
        writer.WriteStartObject("a");
        writer.WriteStartObject("b");

        Assert.Throws<ToonEncodingException>(() => writer.WriteStartObject("c"));
    }

    [Fact]
    public void Disposed_Throws()
    {
        var writer = new ToonDocumentWriter(new StringWriter());
        writer.Dispose();
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteNullValue());
    }
}
