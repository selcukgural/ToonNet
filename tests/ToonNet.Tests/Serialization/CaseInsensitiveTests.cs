using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Serialization;

/// <summary>PropertyNameCaseInsensitive: keys match names ignoring case, the same in the direct and tree paths.</summary>
public class CaseInsensitiveTests
{
    public sealed class Purchase
    {
        public string? Product { get; set; }
        public int Amount { get; set; }
        public List<Line>? Lines { get; set; }
    }

    public sealed class Line
    {
        public string? Sku { get; set; }
        public decimal Price { get; set; }
    }

    public sealed class Clash
    {
        public int Id { get; set; }

        // ReSharper disable once InconsistentNaming
        public int ID { get; set; }

        public string? Name { get; set; }
    }

    public sealed record Person(string Name, int Age)
    {
        public string? City { get; set; }
    }

    private static readonly ToonSerializerOptions Lenient = new() { PropertyNameCaseInsensitive = true };

    private static T Read<T>(string toon)
    {
        var direct = ToonSerializer.Deserialize<T>(toon, Lenient)!;
        var tree = ToonSerializer.DeserializeFromValue<T>(ToonDocument.Parse(toon, Lenient.ToonOptions).Root, Lenient)!;

        Assert.Equal(ToonSerializer.Serialize(tree), ToonSerializer.Serialize(direct));
        return direct;
    }

    [Fact]
    public void KeysMatchIgnoringCase()
    {
        var purchase = Read<Purchase>("product: book\nAMOUNT: 2\nlines[2]{sku,PRICE}:\n  a,1.5\n  b,2");

        Assert.Equal("book", purchase.Product);
        Assert.Equal(2, purchase.Amount);
        Assert.Equal(["a", "b"], purchase.Lines!.Select(l => l.Sku));
        Assert.Equal(2m, purchase.Lines![1].Price);

        // Off by default: keys match exactly
        Assert.Null(ToonSerializer.Deserialize<Purchase>("product: book")!.Product);
    }

    [Fact]
    public void TheLastMatchingKeyWins()
    {
        Assert.Equal("b", Read<Purchase>("Product: a\nproduct: b").Product);
        Assert.Equal("a", Read<Purchase>("product: b\nProduct: a").Product);
    }

    [Fact]
    public void NamesThatDifferOnlyInCase_NeedAnExactMatch()
    {
        var clash = Read<Clash>("id: 1\nID: 2\nNAME: x");

        Assert.Equal(0, clash.Id);
        Assert.Equal(2, clash.ID);
        Assert.Equal("x", clash.Name);
    }

    [Fact]
    public void ConstructorParametersMatchIgnoringCase()
    {
        var person = Read<Person>("NAME: Ada\nage: 36\ncity: London");

        Assert.Equal(new Person("Ada", 36) { City = "London" }, person);
        Assert.Equal("Bob", Read<Person>("name: Ada\nName: Bob\nAge: 1").Name);
    }
}
