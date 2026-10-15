using ToonNet.Core;
using ToonNet.Core.Parsing;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Documents;

public class ToonDocumentReaderDeserializeTests
{
    public sealed class Order
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "";
        public decimal Total { get; set; }
        public List<string> Tags { get; set; } = [];
    }

    private const string Document = """
                                    version: 3
                                    orders[2]:
                                      - Id: 1
                                        Customer: Ada
                                        Total: 9.5
                                        Tags[2]: a,b
                                      - Id: 2
                                        Customer: Bob
                                        Total: 12
                                        Tags[0]:
                                    after: done
                                    """;

    /// <summary>Moves the reader to the property with the given name at any depth.</summary>
    private static void MoveTo(ToonDocumentReader reader, string name)
    {
        while (reader.Read())
        {
            if (reader.TokenType == ToonTokenType.PropertyName && reader.GetPropertyName() == name)
            {
                return;
            }
        }

        throw new InvalidOperationException(name + " not found");
    }

    private static List<string> Rest(ToonDocumentReader reader)
    {
        var tokens = new List<string>();

        while (reader.Read())
        {
            tokens.Add(reader.TokenType == ToonTokenType.PropertyName ? reader.GetPropertyName() : reader.TokenType.ToString());
        }

        return tokens;
    }

    [Fact]
    public void ReadsThePropertyValue_AndTheReaderContinues()
    {
        using var reader = new ToonDocumentReader(Document);
        MoveTo(reader, "orders");

        var fallbacks = ToonSerializer.DirectReadFallbacks;
        var orders = ToonSerializer.Deserialize<List<Order>>(reader)!;

        Assert.Equal(fallbacks, ToonSerializer.DirectReadFallbacks);
        Assert.Equal(ToonTokenType.EndArray, reader.TokenType);
        Assert.Equal([1, 2], orders.Select(o => o.Id));
        Assert.Equal(["a", "b"], orders[0].Tags);
        Assert.Equal(["after", "String", "EndObject"], Rest(reader));
    }

    [Fact]
    public void ReadsItemsOneByOne()
    {
        using var reader = new ToonDocumentReader(Document);
        MoveTo(reader, "orders");
        reader.Read(); // StartArray
        var orders = new List<Order>();

        while (reader.Read() && reader.TokenType != ToonTokenType.EndArray)
        {
            orders.Add(ToonSerializer.Deserialize<Order>(reader)!);
            Assert.Equal(ToonTokenType.EndObject, reader.TokenType);
        }

        Assert.Equal(["Ada", "Bob"], orders.Select(o => o.Customer));
        Assert.Equal(["after", "String", "EndObject"], Rest(reader));
    }

    [Fact]
    public void ReadsPrimitivesAndTheRoot()
    {
        using (var reader = new ToonDocumentReader(Document))
        {
            MoveTo(reader, "version");
            Assert.Equal(3, ToonSerializer.Deserialize<int>(reader));
            Assert.Equal(ToonTokenType.Number, reader.TokenType);
            reader.Read();
            Assert.Equal("orders", reader.GetPropertyName());
        }

        using (var reader = new ToonDocumentReader("Id: 7\nCustomer: Cy\nTotal: 1"))
        {
            // A reader that has not read starts at the root
            Assert.Equal(7, ToonSerializer.Deserialize<Order>(reader)!.Id);
            Assert.False(reader.Read());
        }
    }

    public sealed record Point(int X, int Y);

    [Fact]
    public void FallsBackToTheTree_AndTheReaderContinues()
    {
        // A repeated key (non-strict mode) for a constructor parameter makes direct reading give up after reading into
        // the value; the parser returns to the start of the value and the tree-based path reads it
        const string text = """
                            first[1]: x
                            point:
                              list[2]{a,b}:
                                1,2
                                3,4
                              X: 1
                              Y: 2
                              X: 3
                            rest[2]{a,b}:
                              1,2
                              3,4
                            """;
        using var reader = new ToonDocumentReader(text, new ToonOptions { StrictMode = false });
        MoveTo(reader, "point");

        var fallbacks = ToonSerializer.DirectReadFallbacks;
        var point = ToonSerializer.Deserialize<Point>(reader);

        Assert.Equal(fallbacks + 1, ToonSerializer.DirectReadFallbacks);
        Assert.Equal(new Point(3, 2), point);
        Assert.Equal(["rest", "StartArray", "StartObject", "a", "Number", "b", "Number", "EndObject", "StartObject", "a", "Number", "b", "Number",
                      "EndObject", "EndArray", "EndObject"], Rest(reader));
    }

    /// <summary>Saves a checkpoint at the reader's value, reads a few fields into it, returns, and skips the value.</summary>
    private static void ReadPartlyAndRestore(ToonDocumentReader reader, int steps)
    {
        var parser = reader.Parser;
        var value = reader.BeginValue("test");
        var checkpoint = parser.Save(value);
        var cursor = parser.BeginObject(value);

        for (var i = 0; i < steps && parser.NextProperty(ref cursor, out _, out var field); i++)
        {
            if (field.NodeKind == ToonNodeKind.Object)
            {
                var inner = parser.BeginObject(field);
                parser.NextProperty(ref inner, out _, out _);
                break;
            }
        }

        parser.Restore(checkpoint);
        parser.Skip(value);
        reader.EndValue();
    }

    [Fact]
    public void Checkpoint_KeepsDuplicateChecksOfTheEnclosingObject()
    {
        // Keys read inside the value before the checkpoint is restored must not count as keys of the outer object
        using var reader = new ToonDocumentReader("order:\n  Id: 1\n  Customer: x\nId: 2");
        MoveTo(reader, "order");

        ReadPartlyAndRestore(reader, steps: 2);

        Assert.Equal(["Id", "Number", "EndObject"], Rest(reader));
    }

    [Fact]
    public void Checkpoint_KeepsTheDepthLimit()
    {
        // Each restore must leave the nesting depth where it was, or reading many values would exceed the limit
        var text = string.Join('\n', Enumerable.Range(0, 20).Select(i => $"o{i}:\n  inner:\n    a: {i}"));
        using var reader = new ToonDocumentReader(text, new ToonOptions { MaxDepth = 3 });
        var count = 0;

        while (reader.Read())
        {
            if (reader.TokenType == ToonTokenType.PropertyName)
            {
                ReadPartlyAndRestore(reader, steps: 1);
                count++;
            }
        }

        Assert.Equal(20, count);
    }

    [Fact]
    public void Errors_AreTheSerializers()
    {
        using var reader = new ToonDocumentReader("order:\n  Id: abc");
        MoveTo(reader, "order");

        var ex = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Order>(reader));
        var expected = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Order>("Id: abc"));
        Assert.Equal(expected.Message, ex.Message);
    }

    [Fact]
    public void ParseErrors_InTheValue()
    {
        using var reader = new ToonDocumentReader("order:\n  Tags[3]: a,b\n  Id: 1");
        MoveTo(reader, "order");

        Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<Order>(reader));
    }

    [Fact]
    public void Arguments_AndPosition_AreChecked()
    {
        using var reader = new ToonDocumentReader("a: 1");
        reader.Read();
        reader.Read();
        reader.Skip();
        reader.Read(); // EndObject

        Assert.Equal("Deserialize needs a property name or the start of a value, not EndObject.",
                     Assert.Throws<InvalidOperationException>(() => ToonSerializer.Deserialize<int>(reader)).Message);
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.Deserialize<int>((ToonDocumentReader)null!));
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.Deserialize(reader, null!));

        reader.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ToonSerializer.Deserialize<int>(reader));
    }
}
