using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.SourceGenerators.Tests.Models;
using ToonNet.SourceGenerators.Tests.Models.Other;

namespace ToonNet.SourceGenerators.Tests.Tests;

/// <summary>
/// The generated methods must produce the same TOON as <see cref="ToonSerializer"/> and read it back the same way.
/// </summary>
public class ReflectionParityTests
{
    private static string AssertParity<T>(T value, Func<T, ToonSerializerOptions?, ToonDocument> serialize,
                                          Func<ToonDocument, ToonSerializerOptions?, T> deserialize, ToonSerializerOptions? options = null)
    {
        var encoder = new ToonEncoder(options?.ToonOptions);
        var reflection = ToonSerializer.Serialize(value, options);
        var generated = encoder.Encode(serialize(value, options));

        Assert.Equal(reflection, generated);

        var readBack = deserialize(ToonDocument.Parse(reflection, options?.ToonOptions), options);
        Assert.Equal(reflection, ToonSerializer.Serialize(readBack, options));

        return generated;
    }

    private static CatalogItem CreateCatalogItem()
    {
        var item = new CatalogItem
        {
            Id = 42,
            DisplayName = "Desk lamp, large",
            Tags = [1, 2, 3],
            Scores = [10, 20],
            Stock = new Dictionary<string, int> { ["berlin"] = 4, ["paris"] = 0 },
            Addresses = [new Address { Street = "1 Main St", City = "Springfield", ZipCode = "01234" }],
            Status = Status.Suspended,
            CreatedAt = new DateTime(2026, 10, 8, 12, 30, 0, DateTimeKind.Utc),
            Key = Guid.Parse("4b0e2c55-8d84-4f5e-9b8a-0f3f6a1c2d3e"),
            Big = long.MaxValue,
            Huge = ulong.MaxValue,
            Price = 0.1m + 0.2m,
            Ratio = 0.1,
            Weight = 1.5f,
            Grade = 'A',
            MaybeCount = null,
            Note = "- starts with a dash",
            Secret = "never written"
        };
        item.Bump();
        return item;
    }

    [Fact]
    public void CatalogItem_MatchesReflectionOutput()
    {
        var toon = AssertParity(CreateCatalogItem(), CatalogItem.Serialize, CatalogItem.Deserialize);

        // Collections, enums, dates and nested objects are real TOON structures, not quoted strings
        Assert.StartsWith("Id: 42\n", toon);
        Assert.Contains("Tags[3]: 1,2,3", toon);
        Assert.Contains("Addresses[1]{Street,City,ZipCode}:", toon);
        Assert.Contains("\"display name\": \"Desk lamp, large\"", toon);
        Assert.Contains("Big: 9223372036854775807", toon);
        Assert.Contains("Huge: 18446744073709551615", toon);
        Assert.Contains("Price: 0.3", toon);
        Assert.DoesNotContain("Secret", toon);
    }

    [Fact]
    public void CatalogItem_RoundTripsExactValues()
    {
        var original = CreateCatalogItem();
        var copy = CatalogItem.Deserialize(CatalogItem.Serialize(original));

        Assert.Equal(original.Tags, copy.Tags);
        Assert.Equal(original.Scores, copy.Scores);
        Assert.Equal(original.Stock, copy.Stock);
        Assert.Equal("Springfield", Assert.Single(copy.Addresses).City);
        Assert.Equal(Status.Suspended, copy.Status);
        Assert.Equal(original.CreatedAt, copy.CreatedAt);
        Assert.Equal(original.Key, copy.Key);
        Assert.Equal(long.MaxValue, copy.Big);
        Assert.Equal(ulong.MaxValue, copy.Huge);
        Assert.Equal(0.3m, copy.Price);
        Assert.Equal('A', copy.Grade);
        Assert.Null(copy.MaybeCount);
        Assert.Equal(1, copy.Revision); // private setter
        Assert.Equal("", copy.Secret);
    }

    [Fact]
    public void PositionalRecord_UsesPrimaryConstructor()
    {
        var product = new Product("Lamp", 19.99m, ["home", "light"]);

        AssertParity(product, Product.Serialize, Product.Deserialize);

        var copy = Product.Deserialize(Product.Serialize(product));
        Assert.Equal(product.Name, copy.Name);
        Assert.Equal(product.Price, copy.Price);
        Assert.Equal(product.Tags, copy.Tags);
    }

    [Fact]
    public void RecordStruct_RoundTrips()
    {
        var coordinate = new Coordinate(52.52, 13.405);

        AssertParity(coordinate, Coordinate.Serialize, Coordinate.Deserialize);
        Assert.Equal(coordinate, Coordinate.Deserialize(Coordinate.Serialize(coordinate)));
    }

    [Fact]
    public void Struct_WithInitOnlyProperty_RoundTrips()
    {
        var counter = new Counter { Value = 3, Label = "visits" };

        AssertParity(counter, Counter.Serialize, Counter.Deserialize);
        Assert.Equal(counter, Counter.Deserialize(Counter.Serialize(counter)));
    }

    [Fact]
    public void RequiredAndInitOnlyMembers_AreSet()
    {
        var account = new Account { Email = "a@example.com", Plan = "pro" };
        account.Login();

        AssertParity(account, Account.Serialize, Account.Deserialize);

        var copy = Account.Deserialize(Account.Serialize(account));
        Assert.Equal("a@example.com", copy.Email);
        Assert.Equal("pro", copy.Plan);
        Assert.Equal(1, copy.Logins);
    }

    [Fact]
    public void MissingInitOnlyKey_KeepsInitializerValue()
    {
        var copy = Account.Deserialize(ToonDocument.Parse("Email: b@example.com"));

        Assert.Equal("b@example.com", copy.Email);
        Assert.Equal("free", copy.Plan);
    }

    [Fact]
    public void InheritedProperties_AreWrittenBaseFirst_AndPrivateBaseSetterIsRestored()
    {
        var customer = new Customer { Id = 7, Name = "Ada" };
        customer.Touch(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));

        var toon = AssertParity(customer, Customer.Serialize, Customer.Deserialize);
        Assert.StartsWith("Id: 7\nCreated:", toon);

        var copy = Customer.Deserialize(Customer.Serialize(customer));
        Assert.Equal(customer.Created, copy.Created);
        Assert.Equal("customer", copy.Kind);
    }

    [Fact]
    public void GenericType_RoundTrips()
    {
        var box = new Box<List<int>> { Value = [1, 2], Label = "numbers" };

        AssertParity(box, Box<List<int>>.Serialize, Box<List<int>>.Deserialize);
        Assert.Equal([1, 2], Box<List<int>>.Deserialize(Box<List<int>>.Serialize(box)).Value);
    }

    [Fact]
    public void NestedAndGlobalNamespaceTypes_RoundTrip()
    {
        AssertParity(new Outer.Inner { A = 1 }, Outer.Inner.Serialize, Outer.Inner.Deserialize);
        AssertParity(new GlobalNamespaceModel { Name = "global" }, GlobalNamespaceModel.Serialize, GlobalNamespaceModel.Deserialize);
    }

    [Fact]
    public void PropertiesOfTypesFromOtherNamespaces_RoundTrip()
    {
        var shipment = new Shipment
        {
            Code = "S-1",
            Destination = new Address { City = "Oslo" },
            Parcel = new Parcel { WeightKg = 2.5 }
        };

        AssertParity(shipment, Shipment.Serialize, Shipment.Deserialize);
        Assert.Equal(2.5, Shipment.Deserialize(Shipment.Serialize(shipment)).Parcel!.WeightKg);
    }

    [Fact]
    public void ConstructorDefaults_AreUsedForMissingKeys()
    {
        var copy = Positional.Deserialize(ToonDocument.Parse("Name: x\nMode: manual"));

        Assert.Equal("x", copy.Name);
        Assert.Equal(7, copy.Count);
        Assert.Equal("manual", copy.Mode);
        AssertParity(copy, Positional.Serialize, Positional.Deserialize);
    }

    [Theory]
    [InlineData(PropertyNamingPolicy.Default)]
    [InlineData(PropertyNamingPolicy.CamelCase)]
    [InlineData(PropertyNamingPolicy.SnakeCase)]
    [InlineData(PropertyNamingPolicy.LowerCase)]
    public void RuntimeNamingPolicy_IsHonoured(PropertyNamingPolicy policy)
    {
        var options = new ToonSerializerOptions { PropertyNamingPolicy = policy };
        var contact = new Contact { FirstName = "Ada", PhoneNumber = "555" };

        AssertParity(contact, Contact.Serialize, Contact.Deserialize, options);
    }

    [Fact]
    public void IgnoreNullValues_AndReadOnlyOption_AreHonoured()
    {
        var options = new ToonSerializerOptions { IgnoreNullValues = true, IncludeReadOnlyProperties = false };

        var toon = AssertParity(CreateCatalogItem(), CatalogItem.Serialize, CatalogItem.Deserialize, options);

        Assert.DoesNotContain("MaybeCount", toon);
        Assert.DoesNotContain("TagCount", toon);
    }

    [Fact]
    public void ConvertersInOptions_AreHonoured()
    {
        var options = new ToonSerializerOptions();
        options.AddConverter(new IntAsStringConverter());

        var toon = AssertParity(new Outer.Inner { A = 5 }, Outer.Inner.Serialize, Outer.Inner.Deserialize, options);

        Assert.Equal("A: #5", toon);
        Assert.Equal(5, Outer.Inner.Deserialize(ToonDocument.Parse(toon), options).A);
    }

    [Fact]
    public void InvalidValue_FailsLikeReflection()
    {
        const string toon = "Name: Ada\nAge: not-a-number";

        var reflection = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Person>(toon));
        var generated = Assert.Throws<ToonSerializationException>(() => Person.Deserialize(ToonDocument.Parse(toon)));

        Assert.Equal(reflection.TargetType, generated.TargetType);
    }

    [Fact]
    public void NonObjectRoot_FailsLikeReflection()
    {
        Assert.Throws<ToonSerializationException>(() => Person.Deserialize(ToonDocument.Parse("[2]: 1,2")));
    }
}
