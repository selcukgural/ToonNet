using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Serialization;

/// <summary>
/// Tests for the ToonValue-level APIs: SerializeToValue, DeserializeFromValue and ToonDocument.Parse.
/// </summary>
public class ToonSerializerValueApiTests
{
    private sealed class Order
    {
        public string Id { get; set; } = "";
        public List<int> Quantities { get; set; } = [];
        public string? Note { get; set; }
    }

    [Fact]
    public void SerializeToValue_ReturnsStructuredValue()
    {
        var value = ToonSerializer.SerializeToValue(new Order { Id = "A-1", Quantities = [1, 2] });

        var obj = Assert.IsType<ToonObject>(value);
        Assert.Equal("A-1", Assert.IsType<ToonString>(obj["Id"]).Value);
        Assert.Equal(2, Assert.IsType<ToonArray>(obj["Quantities"]).Items.Count);
        Assert.IsType<ToonNull>(obj["Note"]);
    }

    [Fact]
    public void SerializeToValue_NullRoot_ReturnsToonNull_EvenWhenNullsAreIgnored()
    {
        var options = new ToonSerializerOptions { IgnoreNullValues = true };

        Assert.Same(ToonNull.Instance, ToonSerializer.SerializeToValue<Order>(null, options));
    }

    [Fact]
    public void SerializeToValue_EncodesLikeSerialize()
    {
        var order = new Order { Id = "A-1", Quantities = [1, 2], Note = "rush" };

        var encoded = new ToonNet.Core.Encoding.ToonEncoder().Encode(new ToonDocument(ToonSerializer.SerializeToValue(order)));

        Assert.Equal(ToonSerializer.Serialize(order), encoded);
    }

    [Fact]
    public void DeserializeFromValue_ReadsParsedDocument()
    {
        var document = ToonDocument.Parse("Id: A-1\nQuantities[2]: 3,4\nNote: null");

        var order = ToonSerializer.DeserializeFromValue<Order>(document.Root);

        Assert.NotNull(order);
        Assert.Equal("A-1", order.Id);
        Assert.Equal([3, 4], order.Quantities);
        Assert.Null(order.Note);
    }

    [Fact]
    public void DeserializeFromValue_ReadsNestedValue()
    {
        var document = ToonDocument.Parse("order:\n  Id: B-2\n  Quantities[1]: 9");

        var order = ToonSerializer.DeserializeFromValue<Order>(document.AsObject()["order"]!);

        Assert.Equal("B-2", order!.Id);
    }

    [Fact]
    public void DeserializeFromValue_ValidatesArguments()
    {
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.DeserializeFromValue<Order>(null!));
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.DeserializeFromValue(ToonNull.Instance, (Type)null!));
        Assert.Throws<ArgumentNullException>(() => ToonSerializer.SerializeToValue(1, (Type)null!));
    }

    [Fact]
    public void DeserializeFromValue_InvalidValue_Throws()
    {
        Assert.Throws<ToonSerializationException>(() => ToonSerializer.DeserializeFromValue<int>(new ToonString("abc")));
    }

    [Fact]
    public void ToonDocumentParse_UsesOptions()
    {
        Assert.Throws<ToonParseException>(() => ToonDocument.Parse("a:\n   b: 1"));

        var lenient = ToonDocument.Parse("a:\n   b: 1", new ToonOptions { StrictMode = false });
        Assert.IsType<ToonObject>(lenient.Root);
    }

    [Fact]
    public void ToonDocumentParse_NullInput_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ToonDocument.Parse(null!));
    }
}
