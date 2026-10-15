using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Paths;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Serialization;

public class DeserializeAtTests
{
    public sealed class Order
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "";
        public decimal Total { get; set; }
        public Address? Ship { get; set; }
    }

    public sealed class Address
    {
        public string City { get; set; } = "";
        public string Zip { get; set; } = "";
    }

    private const string Envelope = """
                                    status: ok
                                    page:
                                      number: 2
                                      size: 3
                                    data:
                                      orders[3]{Id,Customer,Total,Ship{City,Zip}}:
                                        1,Ada,9.5,Oslo,0150
                                        2,Bob,12,Rome,00100
                                        3,Cy,7,Kyiv,01001
                                      byId[2:]{Customer,Total}:
                                        a: Ada,9.5
                                        b: Bob,12
                                      notes[2]:
                                        - Id: 4
                                          Customer: Di
                                          Total: 1
                                        - first
                                    after: done
                                    """;

    public static TheoryData<string, Type> Targets() => new()
    {
        { "$.status", typeof(string) },
        { "$.page", typeof(Dictionary<string, int>) },
        { "$.page.size", typeof(int) },
        { "$.data.orders", typeof(List<Order>) },
        { "$.data.orders[1]", typeof(Order) },
        { "$.data.orders[-1].Ship", typeof(Address) },
        { "$.data.orders[0].Ship.Zip", typeof(string) },
        { "$.data.byId", typeof(Dictionary<string, Order>) },
        { "$.data.byId.b", typeof(Order) },
        { "$.data.notes[0]", typeof(Order) },
        { "$.data.notes[1]", typeof(string) },
        { "$['data']['orders'][2]['Customer']", typeof(string) },
        { "$.after", typeof(string) },
        { "$", typeof(Dictionary<string, object>) }
    };

    /// <summary>The value at the path converts as the tree's value at the path does.</summary>
    [Theory]
    [MemberData(nameof(Targets))]
    public void ReadsLikeTheTree(string path, Type type)
    {
        foreach (var strict in (bool[])[true, false])
        {
            var options = new ToonSerializerOptions { ToonOptions = new ToonOptions { StrictMode = strict } };
            var tree = ToonDocument.Parse(Envelope, options.ToonOptions).Root.SelectSingle(path)!;
            var expected = ToonSerializer.Serialize(ToonSerializer.DeserializeFromValue(tree, type, options), type, options);

            Assert.Equal(expected, ToonSerializer.Serialize(ToonSerializer.DeserializeAt(Envelope, path, type, options), type, options));
        }
    }

    [Fact]
    public void Overloads()
    {
        var utf8 = System.Text.Encoding.UTF8.GetBytes(Envelope);

        Assert.Equal("Bob", ToonSerializer.DeserializeAt<Order>(Envelope, "$.data.orders[1]")!.Customer);
        Assert.Equal("Bob", ToonSerializer.DeserializeAt<Order>(utf8, "$.data.orders[1]")!.Customer);
        Assert.Equal(3, ToonSerializer.DeserializeAt<int>(utf8, "$.page.size"));
    }

    [Fact]
    public async Task Overloads_Async()
    {
        var orders = await ToonSerializer.DeserializeAtAsync<List<Order>>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(Envelope)), "$.data.orders");

        Assert.Equal(["Ada", "Bob", "Cy"], orders!.Select(o => o.Customer));
    }

    [Theory]
    [InlineData("$.missing")]
    [InlineData("$.data.orders[3]")]
    [InlineData("$.data.orders[-4]")]
    [InlineData("$.status.x")]
    [InlineData("$.page[0]")]
    [InlineData("$.data.orders.Id")]
    public void Missing_IsDefault(string path)
    {
        Assert.Null(ToonSerializer.DeserializeAt<Order>(Envelope, path));
        Assert.Equal(0, ToonSerializer.DeserializeAt<int>(Envelope, path));
        Assert.Null(ToonSerializer.DeserializeAt<int?>(Envelope, path, new ToonSerializerOptions { ToonOptions = new ToonOptions { StrictMode = false } }));
    }

    [Fact]
    public void StopsAtTheEndOfTheValue()
    {
        const string text = "first: 1\nsecond: 2\nbroken[3]: a,b";

        Assert.Equal(1, ToonSerializer.DeserializeAt<int>(text, "$.first"));
        Assert.Throws<ToonParseException>(() => ToonSerializer.DeserializeAt<int>(text, "$.missing"));
        Assert.Throws<ToonParseException>(() => ToonSerializer.DeserializeAt<int>("bad[2]: 1\nfirst: 1", "$.first"));
    }

    [Fact]
    public void NonStrictMode_TakesTheLastOfRepeatedKeys()
    {
        var options = new ToonSerializerOptions { ToonOptions = new ToonOptions { StrictMode = false } };

        Assert.Equal(2, ToonSerializer.DeserializeAt<int>("a:\n  b: 1\na:\n  b: 2", "$.a.b", options));

        // Strict mode stops at the end of the value: the repeated key after it, an error, is not read
        Assert.Equal(1, ToonSerializer.DeserializeAt<int>("a:\n  b: 1\na:\n  b: 2", "$.a.b"));
        Assert.Throws<ToonParseException>(() => ToonSerializer.DeserializeAt<int>("a:\n  b: 1\na:\n  b: 2", "$.c"));
    }

    [Fact]
    public void Errors_HaveThePathInTheDocument()
    {
        const string text = "data:\n  orders[2]{Id,Customer}:\n    1,Ada\n    x,Bob";

        foreach (var strict in (bool[])[true, false])
        {
            var options = new ToonSerializerOptions { ToonOptions = new ToonOptions { StrictMode = strict } };

            var ex = Assert.Throws<ToonSerializationException>(() => ToonSerializer.DeserializeAt<List<Order>>(text, "$.data.orders", options));
            Assert.Equal("$.data.orders[1].Id", ex.Path);

            ex = Assert.Throws<ToonSerializationException>(() => ToonSerializer.DeserializeAt<Order>(text, "$.data.orders[-1]", options));
            Assert.Equal("$.data.orders[1].Id", ex.Path);
        }
    }

    [Fact]
    public void Paths_AreChecked()
    {
        Assert.Throws<ToonPathException>(() => ToonSerializer.DeserializeAt<int>(Envelope, "data.orders"));
        var ex = Assert.Throws<ArgumentException>(() => ToonSerializer.DeserializeAt<int>(Envelope, "$.data.orders[*].Id"));
        Assert.StartsWith("The path '$.data.orders[*].Id' can select several values; DeserializeAt needs member names and indexes only.", ex.Message);
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.DeserializeAt<int>(Envelope, null!));
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.DeserializeAt<int>((string)null!, "$"));
    }
}
