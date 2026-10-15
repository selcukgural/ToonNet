using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.SourceGenerators.Tests.Tests;

/// <summary>
///     Generated code applies PropertyNameCaseInsensitive, UnmappedMemberHandling and RespectRequiredMembers itself: the
///     same objects and errors as the reflection-based serializer, through every entry point, without reflection.
/// </summary>
public class GeneratedReadingOptionsTests
{
    private static readonly ToonSerializerOptions[] Variants =
    [
        new() { PropertyNameCaseInsensitive = true },
        new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = PropertyNamingPolicy.SnakeCase },
        new() { UnmappedMemberHandling = ToonUnmappedMemberHandling.Disallow },
        new() { UnmappedMemberHandling = ToonUnmappedMemberHandling.Disallow, PropertyNameCaseInsensitive = true },
        new() { RespectRequiredMembers = true },
        new() { RespectRequiredMembers = true, PropertyNameCaseInsensitive = true, UnmappedMemberHandling = ToonUnmappedMemberHandling.Disallow },
        new() { RespectRequiredMembers = true, PropertyNamingPolicy = PropertyNamingPolicy.CamelCase }
    ];

    /// <summary>What reading gives: the object written back as TOON, or the error.</summary>
    private static string Outcome(Func<object?> read)
    {
        try
        {
            return "ok: " + ToonSerializer.Serialize(read(), ReflectionOnly.Options());
        }
        catch (ToonSerializationException ex)
        {
            return $"error: {ex.Message} [{ex.Path}]";
        }
    }

    /// <summary>Reads the text with every options variant through every entry point and compares with reflection.</summary>
    private static void AssertParity<T>(ToonTypeInfo<T> typeInfo, Func<ToonDocument, ToonSerializerOptions, T> generatedDeserialize, params string[] documents)
    {
        foreach (var toon in documents)
        {
            foreach (var options in Variants)
            {
                var expected = Outcome(() => ToonSerializer.Deserialize<T>(toon, ReflectionOnly.Options(options)));
                var label = $"{toon.Replace('\n', '|')} with {Describe(options)}";

                Check("ToonSerializer", Outcome(() => ToonSerializer.Deserialize<T>(toon, options)));
                Check("type info", Outcome(() => ToonSerializer.Deserialize(toon, typeInfo, options)));
                Check("Deserialize", Outcome(() => generatedDeserialize(ToonDocument.Parse(toon), options)));
                Check("tree", Outcome(() => ToonSerializer.DeserializeFromValue<T>(ToonDocument.Parse(toon).Root, options)));

                void Check(string entryPoint, string actual) =>
                    Assert.True(expected == actual, $"{entryPoint}: {label}\nexpected {expected}\nactual   {actual}");
            }
        }
    }

    private static string Describe(ToonSerializerOptions options) =>
        $"ci={options.PropertyNameCaseInsensitive} unmapped={options.UnmappedMemberHandling} required={options.RespectRequiredMembers} policy={options.PropertyNamingPolicy}";

    [Fact]
    public void CaseInsensitiveKeys()
    {
        AssertParity(OptionPurchase.ToonTypeInfo, OptionPurchase.Deserialize,
                     "product: book\nAMOUNT: 2\nlines[2]{sku,PRICE}:\n  a,1.5\n  b,2",
                     "Product: a\nproduct: b",
                     "product: b\nProduct: a",
                     "PRODUCT_NAME: x\nproduct_name: y",
                     "total: 4\nTOTAL: 5\nAmount: 1",
                     "Product: a\nOther: 1",
                     "lines[1]{Sku,Price,Extra}:\n  a,1,2");
    }

    [Fact]
    public void NamesThatDifferOnlyInCase_NeedAnExactMatch()
    {
        AssertParity(OptionClash.ToonTypeInfo, OptionClash.Deserialize,
                     "id: 1\nID: 2\nNAME: x",
                     "Id: 1\niD: 2",
                     "id: 3");
    }

    [Fact]
    public void ConstructorParameters()
    {
        AssertParity(OptionPerson.ToonTypeInfo, OptionPerson.Deserialize,
                     "NAME: Ada\nage: 36\ncity: London",
                     "name: Ada\nName: Bob\nAge: 1",
                     "Name: Ada\nNickname: A",
                     "nickname: A\nAge: 2",
                     "Age: 2",
                     "City: Paris");
    }

    [Fact]
    public void RequiredMembers()
    {
        AssertParity(OptionRequired.ToonTypeInfo, OptionRequired.Deserialize,
                     "Name: a\nAge: 1",
                     "Age: 1",
                     "Name: a",
                     "name: a\nage: 1",
                     "Note: n",
                     "Name: a\nAge: 1\nNote: n\nOther: x");

        AssertParity(OptionSetsRequired.ToonTypeInfo, OptionSetsRequired.Deserialize, "Note: n", "Name: a");
        AssertParity(OptionRequiredRecord.ToonTypeInfo, OptionRequiredRecord.Deserialize, "Code: c\nCount: 1", "Code: c", "Count: 1", "code: c\ncount: 1");
    }

    [Fact]
    public void FixedNamingPolicyAndPropertyNames()
    {
        AssertParity(OptionFixed.ToonTypeInfo, OptionFixed.Deserialize,
                     "orderId: 1\nx-label: a",
                     "ORDERID: 1\nX-LABEL: a",
                     "OrderId: 1",
                     "orderId: 1\nlabel: a");
    }

    [Fact]
    public void AmbiguityThatDependsOnThePolicy()
    {
        // Under snake_case the read-only UserId is named user_id, which makes User_Id ambiguous ignoring case
        AssertParity(OptionPolicyClash.ToonTypeInfo, OptionPolicyClash.Deserialize, "user_id: 5", "User_Id: 6", "USER_ID: 7\nuserId: 8");
    }

    [Fact]
    public void NestedObjectsAndLists()
    {
        AssertParity(OptionOrder.ToonTypeInfo, OptionOrder.Deserialize,
                     "Buyer:\n  name: a\n  age: 1\nLines[1]{SKU,price}:\n  x,1",
                     "Buyer:\n  Name: a\nLines[1]{Sku,Price}:\n  x,1",
                     "Buyer:\n  Name: a\n  Age: 1\n  Extra: 1",
                     "lines[1]{sku,price,more}:\n  x,1,2");
    }

    /// <summary>The reflection-based serializer's metadata cache (private), to prove it was not used.</summary>
    private static ICollection CachedTypes()
    {
        var field = typeof(ToonSerializer).GetField("TypeMetadataCache", BindingFlags.NonPublic | BindingFlags.Static)!;
        return ((IDictionary)field.GetValue(null)!).Keys;
    }

    [Fact]
    public void ReadingWithTheOptions_UsesNoReflection()
    {
        var options = new ToonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, UnmappedMemberHandling = ToonUnmappedMemberHandling.Disallow, RespectRequiredMembers = true
        };

        const string toon = "id: 1\nITEMS[2]{label,QTY}:\n  a,1\n  b,2";

        foreach (var shipment in new[]
                 {
                     ToonSerializer.Deserialize<NoReflectionShipment>(toon, options)!,
                     ToonSerializer.Deserialize(toon, NoReflectionShipment.ToonTypeInfo, options)!,
                     NoReflectionShipment.Deserialize(ToonDocument.Parse(toon), options)
                 })
        {
            Assert.Equal(1, shipment.Id);
            Assert.Equal(["a", "b"], shipment.Items.Select(i => i.Label));
            Assert.Equal(2, shipment.Items[1].Qty);
        }

        var unmapped = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize("Id: 1\nItems[0]:\nOther: 1", NoReflectionShipment.ToonTypeInfo, options));
        Assert.Equal("$.Other", unmapped.Path);

        var missing = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize("Items[0]:", NoReflectionShipment.ToonTypeInfo, options));
        Assert.Contains("'Id'", missing.Message);

        var cached = CachedTypes().Cast<Type>().ToList();
        Assert.DoesNotContain(typeof(NoReflectionShipment), cached);
        Assert.DoesNotContain(typeof(NoReflectionItem), cached);
    }

    [Fact]
    public void CodeThatReachesOlderGeneratedCode_IsNotUsedWithTheOptions()
    {
        LegacyGenerated.Register();
        ModernHolder.Register();
        var options = new ToonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Its own code applies the option, but it calls the older code of LegacyGenerated directly
        var ex = Assert.Throws<NotSupportedException>(() => ToonSerializer.Deserialize("inner:\n  name: x", ToonTypeInfo.GetGenerated<ModernHolder>(), options));
        Assert.Contains("a type it reaches", ex.Message);
        Assert.Throws<NotSupportedException>(() => ToonSerializer.Serialize(new ModernHolder(), ToonTypeInfo.GetGenerated<ModernHolder>(), WithAnyConverter()));
    }

    private static ToonSerializerOptions WithAnyConverter()
    {
        var options = new ToonSerializerOptions();
        options.AddConverter(new NeverConverter());
        return options;
    }

    private sealed class NeverConverter : IToonConverter
    {
        public bool CanConvert(Type type) => false;

        public ToonValue Write(object? value, ToonSerializerOptions options) => throw new InvalidOperationException();

        public object Read(ToonValue value, Type targetType, ToonSerializerOptions options) => throw new InvalidOperationException();
    }

    /// <summary>Stands for code from the current generator whose type holds a <see cref="LegacyGenerated"/> (only the parts the checks use).</summary>
    private sealed class ModernHolder
    {
        public LegacyGenerated? Inner { get; set; }

        public static void Register() =>
            ToonSourceGenerationHelpers.Register<ModernHolder>(static (_, _, _) => new ToonObject(), static (_, _, _) => new ModernHolder(), new Writer(), new Reader());

        private sealed class Writer : ToonObjectWriter<ModernHolder>
        {
            public override int FeatureLevel => 1;
            public override Type[] InlineTypes { get; } = [typeof(LegacyGenerated)];
            public override int PropertyCount => 1;
            public override string GetName(int index, ToonSerializerOptions options) => "inner";
            public override int WriteFields(ModernHolder value, ToonNet.Core.Encoding.ToonWriter writer, int lineDepth, int childDepth, bool firstOnCurrentLine, int depth) => 0;
            public override ToonRowShape GetRowShape(ModernHolder value, ToonNet.Core.Encoding.ToonWriter writer, Span<bool> columns) => ToonRowShape.NotRow;
            public override void WriteRow(ModernHolder value, ToonNet.Core.Encoding.ToonWriter writer, ReadOnlySpan<bool> columns) { }
            public override bool MayBeKeyed(ModernHolder value, ToonNet.Core.Encoding.ToonWriter writer) => false;
            public override ToonPropertyKind GetPropertyKind(int index) => ToonPropertyKind.Object;
            public override bool IsReadOnlyProperty(int index) => false;
            public override object? GetValue(ModernHolder value, int index) => value.Inner;
            public override void WriteValue(ModernHolder value, int index, ToonNet.Core.Encoding.ToonWriter writer) { }
        }

        private sealed class Reader : ToonObjectReader<ModernHolder>
        {
            public override int FeatureLevel => 1;

            public override ModernHolder Read(ToonReader reader, in ToonReaderValue value, int depth) => new();
        }
    }

    [Fact]
    public void CodeFromOlderGenerators_IsReadWithReflection()
    {
        LegacyGenerated.Register();
        var options = new ToonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Its reader matches keys exactly, so it is not used with the option
        Assert.Equal("x", ToonSerializer.Deserialize<LegacyGenerated>("name: x", options)!.Name);
        Assert.Equal("x", ToonSerializer.DeserializeFromValue<LegacyGenerated>(ToonDocument.Parse("name: x").Root, options)!.Name);
        Assert.Null(ToonSerializer.Deserialize<LegacyGenerated>("name: x")!.Name);

        var ex = Assert.Throws<NotSupportedException>(() => ToonSerializer.Deserialize("name: x", ToonTypeInfo.GetGenerated<LegacyGenerated>(), options));
        Assert.Contains("ToonNet.SourceGenerators before 3.3", ex.Message);

        // It does not check required members either, which the default options do since 4.0
        Assert.Throws<NotSupportedException>(() => ToonSerializer.Deserialize("Name: y", ToonTypeInfo.GetGenerated<LegacyGenerated>()));
        var lenient = new ToonSerializerOptions { RespectRequiredMembers = false };
        Assert.Equal("y", ToonSerializer.Deserialize("Name: y", ToonTypeInfo.GetGenerated<LegacyGenerated>(), lenient)!.Name);
    }

    /// <summary>What a 3.2 generator emits for a class with one string property, reduced to the parts the serializer calls.</summary>
    private sealed class LegacyGenerated
    {
        public string? Name { get; set; }

        public static void Register() => ToonSourceGenerationHelpers.Register<LegacyGenerated>(Serialize, Deserialize, null, new Reader());

        private static ToonValue Serialize(LegacyGenerated value, ToonSerializerOptions options, int depth) =>
            ToonSourceGenerationHelpers.SerializeWithReflection(value, options, depth);

        private static LegacyGenerated Deserialize(ToonValue value, ToonSerializerOptions options, int depth)
        {
            if (options.Converters.Count != 0 || ToonSourceGenerationHelpers.ReadsWithReflection(options) || value is not ToonObject obj)
            {
                return ToonSourceGenerationHelpers.DeserializeWithReflection<LegacyGenerated>(value, options, depth);
            }

            return new LegacyGenerated { Name = obj.Properties.TryGetValue("Name", out var name) ? ToonSourceGenerationHelpers.ReadPrimitive<string>(name) : null };
        }

        private sealed class Reader : ToonObjectReader<LegacyGenerated>
        {
            public override LegacyGenerated Read(ToonReader reader, in ToonReaderValue value, int depth)
            {
                var result = new LegacyGenerated();
                var fields = reader.BeginObject(in value);

                while (reader.NextProperty(ref fields, out var key, out var field))
                {
                    if (key == "Name")
                    {
                        result.Name = reader.ReadValue<string>(in field, depth + 1);
                    }
                    else
                    {
                        reader.Skip(in field);
                    }
                }

                return result;
            }
        }
    }
}

[ToonSerializable]
public partial class OptionPurchase
{
    public string? Product { get; set; }
    public string? ProductName { get; set; }
    public int Amount { get; set; }
    public List<OptionLine>? Lines { get; set; }
    public int Total => Amount * 2;
}

[ToonSerializable]
public partial class OptionLine
{
    public string? Sku { get; set; }
    public decimal Price { get; set; }
}

[ToonSerializable]
public partial class OptionClash
{
    public int Id { get; set; }

    // ReSharper disable once InconsistentNaming
    public int ID { get; set; }

    public string? Name { get; set; }
}

[ToonSerializable]
public partial record OptionPerson(string Name, int Age, string? Nickname = null)
{
    public string? City { get; set; }
}

[ToonSerializable]
public partial class OptionRequired
{
    public required string Name { get; set; }
    public required int Age { get; init; }
    public string? Note { get; set; }
}

[ToonSerializable]
public partial class OptionSetsRequired
{
    [SetsRequiredMembers]
    public OptionSetsRequired() => Name = "default";

    public required string Name { get; set; }
    public string? Note { get; set; }
}

[ToonSerializable]
public partial record OptionRequiredRecord(string Code)
{
    public required int Count { get; init; }
}

[ToonSerializable(NamingPolicy = PropertyNamingPolicy.CamelCase)]
public partial class OptionFixed
{
    public int OrderId { get; set; }

    [ToonProperty("x-label")]
    public string? Label { get; set; }
}

[ToonSerializable]
public partial class OptionOrder
{
    public OptionRequired? Buyer { get; set; }
    public List<OptionLine> Lines { get; set; } = [];
}

[ToonSerializable(AllowReflectionFallback = false)]
public partial class NoReflectionShipment
{
    public required int Id { get; set; }
    public List<NoReflectionItem> Items { get; set; } = [];
}

[ToonSerializable(AllowReflectionFallback = false)]
public partial class NoReflectionItem
{
    public string Label { get; set; } = "";
    public int Qty { get; set; }
}

[ToonSerializable]
public partial class OptionPolicyClash
{
    [ToonProperty("User_Id")]
    public int A { get; set; }

    public int UserId => 0;
}
