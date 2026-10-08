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

    private static ToonValue Nest(int levels)
    {
        ToonValue value = new ToonNumber(1);

        for (var i = 0; i < levels; i++)
        {
            value = i % 2 == 0 ? new ToonObject { ["a"] = value } : new ToonArray([value]);
        }

        return value;
    }

    [Fact]
    public void DeserializeFromValue_ObjectTarget_EnforcesMaxDepth()
    {
        var options = new ToonSerializerOptions { MaxDepth = 10 };

        Assert.NotNull(ToonSerializer.DeserializeFromValue<object>(Nest(8), options));
        Assert.Throws<ToonParseException>(() => ToonSerializer.DeserializeFromValue<object>(Nest(20), options));
    }

    [Fact]
    public void DeserializeFromValue_UntypedDictionaryValues_EnforceMaxDepth()
    {
        var options = new ToonSerializerOptions { MaxDepth = 10 };
        var root = new ToonObject { ["x"] = Nest(20) };

        Assert.Throws<ToonParseException>(() => ToonSerializer.DeserializeFromValue<Dictionary<string, object>>(root, options));
    }

    [Fact]
    public void DeserializeFromValue_ObjectTarget_EnforcesDefaultMaxDepth()
    {
        Assert.Throws<ToonParseException>(() => ToonSerializer.DeserializeFromValue<object>(Nest(10_000)));
    }

    [Theory]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(int))]
    [InlineData(typeof(string))]
    public void DeserializeFromValue_ObjectToScalarType_ThrowsClearError(Type targetType)
    {
        var ex = Assert.Throws<ToonSerializationException>(() => ToonSerializer.DeserializeFromValue(new ToonObject { ["a"] = new ToonNumber(1) }, targetType));

        Assert.Contains($"Cannot convert Object to {targetType.Name}", ex.Message);
        Assert.Equal(targetType, ex.TargetType);
        Assert.Equal("$", ex.Path);
    }

    [Fact]
    public void Deserialize_NumberToDateTimeProperty_ReportsPath()
    {
        var ex = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Dated>("When: 5"));

        Assert.Contains("Cannot convert Number to DateTime", ex.Message);
        Assert.Equal("$.When", ex.Path);
    }

    private sealed class Dated
    {
        public DateTime When { get; set; }
    }
}
