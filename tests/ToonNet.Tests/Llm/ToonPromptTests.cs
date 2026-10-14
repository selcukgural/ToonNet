using ToonNet.Core;
using ToonNet.Core.Llm;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Llm;

public class ToonPromptTests
{
    public enum Status
    {
        Draft,
        Active
    }

    public sealed class Line
    {
        public string Sku { get; set; } = "";
        public int Quantity { get; set; }
    }

    public sealed class Address
    {
        public string City { get; set; } = "";
        public string? Zip { get; set; }
    }

    public sealed class Order
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "";
        public decimal Total { get; set; }
        public Status Status { get; set; }
        public DateTime? ShippedAt { get; set; }
        public List<string> Tags { get; set; } = [];
        public List<Line> Lines { get; set; } = [];
        public Address Ship { get; set; } = new();
        public Dictionary<string, int> Stock { get; set; } = [];
        public List<Order> Related { get; set; } = [];
    }

    [Fact]
    public void Extract_TakesTheToonBlock()
    {
        const string answer = """
                              Sure! Here is the summary:

                              ```json
                              {"not": "this"}
                              ```

                              ```toon
                              total: 3
                              items[2]: a,b
                              ```

                              Let me know if you need more.
                              """;

        Assert.Equal("total: 3\nitems[2]: a,b", ToonPrompt.Extract(answer));
    }

    [Theory]
    [InlineData("```\na: 1\n```", "a: 1")]
    [InlineData("```TOON\r\na: 1\r\n```\r\n", "a: 1")]
    [InlineData("~~~toon\na: 1\n~~~", "a: 1")]
    [InlineData("````toon\na: 1\n```\nb: 2\n````", "a: 1\n```\nb: 2")]
    [InlineData("  ```toon\n  a:\n    b: 1\n  ```", "a:\n  b: 1")]
    [InlineData("```toon\na: 1\nb: 2", "a: 1\nb: 2")]
    [InlineData("\n\na: 1\n  b: 2\n\n", "a: 1\n  b: 2")]
    [InlineData("```python\nx = 1\n```\na: 1", "```python\nx = 1\n```\na: 1")]
    [InlineData("``` toon extra\na: 1\n```", "a: 1")]
    public void Extract_Forms(string answer, string expected)
    {
        Assert.Equal(expected, ToonPrompt.Extract(answer));
    }

    [Fact]
    public void Deserialize_IsLenient()
    {
        // The model miscounted the rows and repeated a key
        const string answer = "```toon\nId: 1\nId: 2\nTags[3]: a,b\nLines[1]{Sku,Quantity}:\n  x,1\n  y,2\n```";

        var order = ToonPrompt.Deserialize<Order>(answer)!;

        Assert.Equal(2, order.Id);
        Assert.Equal(["a", "b"], order.Tags);
        Assert.Equal(["x", "y"], order.Lines.Select(l => l.Sku));
        Assert.Throws<ToonParseException>(() => ToonSerializer.Deserialize<Order>(ToonPrompt.Extract(answer)));
        Assert.Equal(2, ToonPrompt.Parse(answer).Root["Id"]!.GetValue<int>());

        // The caller's options are not changed
        var options = new ToonSerializerOptions();
        ToonPrompt.Deserialize<Order>(answer, options);
        Assert.True(options.ToonOptions.StrictMode);
    }

    [Fact]
    public void DescribeShape_ShowsTheSerializersForms()
    {
        const string expected = """
                                Id: <integer>
                                Customer: <string>
                                Total: <number>
                                Status: <Draft|Active>
                                ShippedAt: <date-time|null>
                                Tags[N]: <string>,...
                                Lines[N]{Sku,Quantity}:
                                  <string>,<integer>
                                Ship:
                                  City: <string>
                                  Zip: <string|null>
                                Stock:
                                  <key>: <integer>
                                Related[N]:
                                  - <Order>
                                """;

        Assert.Equal(expected, ToonPrompt.DescribeShape<Order>());
    }

    [Fact]
    public void DescribeShape_RootsAndOptions()
    {
        Assert.Equal("[N]{sku,quantity}:\n  <string>,<integer>",
                     ToonPrompt.DescribeShape<List<Line>>(new ToonSerializerOptions { PropertyNamingPolicy = PropertyNamingPolicy.CamelCase }));
        Assert.Equal("[N|]{Sku|Quantity}:\n    <string>|<integer>",
                     ToonPrompt.DescribeShape<Line[]>(new ToonSerializerOptions { ToonOptions = new ToonOptions { Delimiter = '|', IndentSize = 4 } }));
        Assert.Equal("<integer>", ToonPrompt.DescribeShape<int>());
        Assert.Equal("[N]: <uuid>,...", ToonPrompt.DescribeShape<List<Guid>>());
        Assert.Equal("[N]:\n  - [N]: <integer>,...", ToonPrompt.DescribeShape<List<int[]>>());
        Assert.Equal("[N]:\n  - City: <string>\n    Zip: <string|null>\n    Lines[N]{Sku,Quantity}:\n      <string>,<integer>",
                     ToonPrompt.DescribeShape<List<Depot>>());
    }

    public sealed class Depot
    {
        public string City { get; set; } = "";
        public string? Zip { get; set; }
        public List<Line> Lines { get; set; } = [];
    }

    [Fact]
    public void Serialize_ShortensLongArrays()
    {
        var data = new
        {
            Orders = Enumerable.Range(1, 5).Select(i => new Line { Sku = "s" + i, Quantity = i }).ToList(),
            Tags = new[] { "a", "b", "c" },
            Nested = new[] { new[] { 1, 2, 3, 4 } },
            Few = new[] { 1 }
        };

        var text = ToonPrompt.Serialize(data, new ToonPromptOptions { MaxArrayItems = 2 });

        Assert.Equal("""
                     Orders[2]{Sku,Quantity}:
                       s1,1
                       s2,2
                       # 3 more items
                     Tags[2]: a,b
                       # 1 more item
                     Nested[1]:
                       - [2]: 1,2
                         # 2 more items
                     Few[1]: 1
                     """, text);

        // Valid TOON in strict mode: the comments are dropped
        var parsed = ToonDocument.Parse(text).Root;
        Assert.Equal(2, parsed["Orders"]!.AsArray().Count);
        Assert.Equal(ToonSerializer.Serialize(data), ToonPrompt.Serialize(data));
        Assert.Equal(ToonSerializer.Serialize(data), ToonPrompt.Serialize(data, new ToonPromptOptions { MaxArrayItems = 10 }));
    }

    [Fact]
    public void Encode_DoesNotChangeTheTree()
    {
        var root = ToonDocument.Parse("[4]: 1,2,3,4").Root;

        Assert.Equal("[1]: 1\n  # 3 more items", ToonPrompt.Encode(root, 1));
        Assert.Equal(4, root.AsArray().Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => ToonPrompt.Encode(root, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ToonPromptOptions { MaxArrayItems = 0 });
    }

    public sealed class SelfList : List<SelfList>;

    public sealed class SelfMap : Dictionary<string, SelfMap>;

    public sealed class Empty;

    public sealed class Holder
    {
        public List<Empty> Items { get; set; } = [];
        public SelfList Nested { get; set; } = [];
    }

    [Fact]
    public void DescribeShape_RecursiveCollections_AndEmptyTypes()
    {
        Assert.Equal("[N]:\n  - <SelfList>", ToonPrompt.DescribeShape<SelfList>());
        Assert.Equal("<key>: <SelfMap>", ToonPrompt.DescribeShape<SelfMap>());
        Assert.Equal("[N]:\n  -", ToonPrompt.DescribeShape<List<Empty>>());
        Assert.Equal("Items[N]:\n  -\nNested[N]:\n  - <SelfList>", ToonPrompt.DescribeShape<Holder>());
    }

    [Fact]
    public void DeserializeValidated_ChecksTheRules()
    {
        var rules = new ToonNet.Core.Validation.ToonRuleSet()
            .Rule("$.Lines[*]", "@.Quantity > 0", "Quantity must be positive")
            .Required("$.Customer", "Customer is missing");

        var good = ToonPrompt.DeserializeValidated<Order>("```toon\nCustomer: Ada\nLines[1]{Sku,Quantity}:\n  a,2\n```", rules)!;
        Assert.Equal("Ada", good.Customer);

        var ex = Assert.Throws<ToonRuleException>(() =>
            ToonPrompt.DeserializeValidated<Order>("Lines[2]{Sku,Quantity}:\n  a,0\n  b,1", rules));
        Assert.Equal("The document has 2 rule violations: $['Lines'][0]: Quantity must be positive; $.Customer: Customer is missing", ex.Message);
    }
}
