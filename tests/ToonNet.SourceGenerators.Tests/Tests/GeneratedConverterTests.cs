using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using ToonNet.Core.Encoding;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.SourceGenerators.Tests.Tests;

/// <summary>
///     Generated code applies the converters of the options itself, so the ToonTypeInfo overloads support them: the
///     same text and objects as the reflection-based serializer through every entry point, without reflection.
/// </summary>
public class GeneratedConverterTests
{
    private static ToonSerializerOptions With(params IToonConverter[] converters)
    {
        var options = new ToonSerializerOptions();

        foreach (var converter in converters)
        {
            options.AddConverter(converter);
        }

        return options;
    }

    private static IEnumerable<ToonSerializerOptions> ConverterSets()
    {
        yield return With(new DateConverter());
        yield return With(new MoneyConverter());
        yield return With(new NullableIntConverter());
        yield return With(new TagsConverter());
        yield return With(new SecretConverter());
        yield return With(new TenfoldIntConverter());
        yield return With(new UpperStringConverter(), new MoneyConverter());
        yield return With(new LineConverter());
        yield return With(new DateConverter(), new MoneyConverter(), new TagsConverter(), new SecretConverter());
        yield return With(new GuidConverter()); // applies to nothing the order holds
    }

    private static ConvertedOrder Order() => new()
    {
        Id = "o-1",
        Placed = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc),
        Priority = 3,
        Total = new Money(12.5m, "EUR"),
        Lines =
        [
            new() { Sku = "a", Price = 1.5m, Shipped = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc) },
            new() { Sku = "b", Price = 2m, Shipped = null }
        ],
        Notes = new Dictionary<int, string> { [7] = "seven", [8] = "eight" },
        Tags = ["x", "y"],
        Hidden = new Secret("s"),
        ByCode = new Dictionary<string, Money> { ["a"] = new(1m, "USD") },
        Amounts = [new Money(1m, "EUR"), new Money(2m, "TRY")]
    };

    /// <summary>The value written back by the reflection-based serializer, to compare what was read.</summary>
    private static string Canonical<T>(T value, ToonSerializerOptions options) => ToonSerializer.Serialize(value, ReflectionOnly.Options(options));

    private static void AssertParity<T>(T value, ToonTypeInfo<T> typeInfo, Func<T, ToonSerializerOptions, ToonDocument>? serialize,
                                        Func<ToonDocument, ToonSerializerOptions, T>? deserialize)
    {
        foreach (var options in ConverterSets())
        {
            var label = string.Join(", ", options.Converters.Select(c => c.GetType().Name));
            var expected = ToonSerializer.Serialize(value, ReflectionOnly.Options(options));

            Assert.True(expected == ToonSerializer.Serialize(value, options), $"ToonSerializer with {label}");
            Assert.True(expected == ToonSerializer.Serialize(value, typeInfo, options), $"type info with {label}");
            Assert.True(Encoding.UTF8.GetBytes(expected).AsSpan().SequenceEqual(ToonSerializer.SerializeToUtf8Bytes(value, typeInfo, options)),
                        $"UTF-8 type info with {label}");

            if (serialize != null)
            {
                Assert.True(expected == new ToonEncoder(options.ToonOptions).Encode(serialize(value, options)), $"Serialize with {label}");
            }

            var reference = Canonical(ToonSerializer.Deserialize<T>(expected, ReflectionOnly.Options(options)), options);

            Assert.True(reference == Canonical(ToonSerializer.Deserialize<T>(expected, options), options), $"read ToonSerializer with {label}");
            Assert.True(reference == Canonical(ToonSerializer.Deserialize(expected, typeInfo, options), options), $"read type info with {label}");
            Assert.True(reference == Canonical(ToonSerializer.DeserializeFromValue<T>(ToonDocument.Parse(expected).Root, options), options),
                        $"read tree with {label}");

            if (deserialize != null)
            {
                Assert.True(reference == Canonical(deserialize(ToonDocument.Parse(expected), options), options), $"Deserialize with {label}");
            }
        }
    }

    [Fact]
    public void Objects() => AssertParity(Order(), ConvertedOrder.ToonTypeInfo, ConvertedOrder.Serialize, ConvertedOrder.Deserialize);

    [Fact]
    public void ObjectsWithNulls() =>
        AssertParity(new ConvertedOrder { Id = "o", Lines = [new ConvertedLine()], Tags = [] }, ConvertedOrder.ToonTypeInfo, ConvertedOrder.Serialize,
                     ConvertedOrder.Deserialize);

    [Fact]
    public void ComposedTypeInfos()
    {
        AssertParity(Order().Lines, ToonTypeInfo.CreateList(ConvertedLine.ToonTypeInfo), null, null);
        AssertParity(Order().Lines.ToArray(), ToonTypeInfo.CreateArray(ConvertedLine.ToonTypeInfo), null, null);
        AssertParity(new Dictionary<string, ConvertedLine> { ["a"] = Order().Lines[0] }, ToonTypeInfo.CreateDictionary(ConvertedLine.ToonTypeInfo), null, null);
        AssertParity(new List<ConvertedOrder> { Order(), new() { Id = "o2" } }, ToonTypeInfo.CreateList(ConvertedOrder.ToonTypeInfo), null, null);
    }

    [Fact]
    public void AConverterForTheRootType_WritesAndReadsTheRoot()
    {
        var options = With(new LineConverter());
        var line = Order().Lines[0];

        Assert.Equal("a@1.5", ToonSerializer.Serialize(line, ConvertedLine.ToonTypeInfo, options));
        Assert.Equal("a@1.5", new ToonEncoder().Encode(ConvertedLine.Serialize(line, options)));
        Assert.Equal(1.5m, ToonSerializer.Deserialize("a@1.5", ConvertedLine.ToonTypeInfo, options)!.Price);
        Assert.Equal(1.5m, ConvertedLine.Deserialize(ToonDocument.Parse("a@1.5"), options).Price);
    }

    [Fact]
    public void ItemsAreConvertedByTheirDeclaredType()
    {
        // The converter of the item type applies to derived items too (since 4.0 items are declared as the item type)
        var options = With(new ExactShapeConverter());
        var shapes = new ShapeBox { Main = new ConvertedShape { Name = "m" }, Items = [new ConvertedShape { Name = "a" }, new DerivedShape { Name = "b", Sides = 3 }] };
        var expected = ToonSerializer.Serialize(shapes, ReflectionOnly.Options(options));

        Assert.Contains("shape:m", expected);
        Assert.Contains("shape:b", expected);
        Assert.DoesNotContain("Sides", expected);
        Assert.Equal(expected, ToonSerializer.Serialize(shapes, options));
        Assert.Equal(expected, ToonSerializer.Serialize(shapes, ShapeBox.ToonTypeInfo, options));
        Assert.Equal(expected, new ToonEncoder().Encode(ShapeBox.Serialize(shapes, options)));
    }

    private sealed class ExactShapeConverter : IToonConverter
    {
        public bool CanConvert(Type type) => type == typeof(ConvertedShape);

        public ToonValue Write(object? value, ToonSerializerOptions options) => new ToonString("shape:" + ((ConvertedShape)value!).Name);

        public object Read(ToonValue value, Type targetType, ToonSerializerOptions options) => new ConvertedShape { Name = ((ToonString)value).Value[6..] };
    }

    /// <summary>The reflection-based serializer's metadata cache (private), to prove it was not used.</summary>
    private static ICollection CachedTypes()
    {
        var field = typeof(ToonSerializer).GetField("TypeMetadataCache", BindingFlags.NonPublic | BindingFlags.Static)!;
        return ((IDictionary)field.GetValue(null)!).Keys;
    }

    [Fact]
    public void TypeInfosWithConverters_UseNoReflection()
    {
        var options = With(new DateConverter(), new AotMoneyConverter());
        var invoice = new AotInvoice
        {
            Number = 1,
            Issued = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            Total = new AotMoney(9m, "EUR"),
            Items = [new AotItem { Name = "a", Price = new AotMoney(1m, "EUR") }, new AotItem { Name = "b", Price = new AotMoney(8m, "EUR") }]
        };

        var text = ToonSerializer.Serialize(invoice, AotInvoice.ToonTypeInfo, options);
        Assert.Contains("Issued: \"20260102\"", text);
        Assert.Contains("9 EUR", text);

        var read = ToonSerializer.Deserialize(text, AotInvoice.ToonTypeInfo, options)!;
        Assert.Equal(invoice.Issued, read.Issued);
        Assert.Equal(invoice.Total, read.Total);
        Assert.Equal(invoice.Items.Select(i => i.Price), read.Items.Select(i => i.Price));
        Assert.Equal(text, new ToonEncoder().Encode(AotInvoice.Serialize(AotInvoice.Deserialize(ToonDocument.Parse(text), options), options)));

        var cached = CachedTypes().Cast<Type>().ToList();
        Assert.DoesNotContain(typeof(AotInvoice), cached);
        Assert.DoesNotContain(typeof(AotItem), cached);
        Assert.DoesNotContain(typeof(AotMoney), cached);
    }

    private sealed class DateConverter : ToonConverter<DateTime>
    {
        public override ToonValue Write(DateTime value, ToonSerializerOptions options) =>
            new ToonString(value.ToString("yyyyMMdd", CultureInfo.InvariantCulture));

        public override DateTime Read(ToonValue value, ToonSerializerOptions options) =>
            DateTime.SpecifyKind(DateTime.ParseExact(((ToonString)value).Value, "yyyyMMdd", CultureInfo.InvariantCulture), DateTimeKind.Utc);
    }

    private sealed class MoneyConverter : ToonConverter<Money>
    {
        public override ToonValue Write(Money value, ToonSerializerOptions options) =>
            new ToonString(value.Amount.ToString(CultureInfo.InvariantCulture) + " " + value.Currency);

        public override Money Read(ToonValue value, ToonSerializerOptions options)
        {
            var parts = ((ToonString)value).Value.Split(' ');
            return new Money(decimal.Parse(parts[0], CultureInfo.InvariantCulture), parts[1]);
        }
    }

    private sealed class AotMoneyConverter : ToonConverter<AotMoney>
    {
        public override ToonValue Write(AotMoney value, ToonSerializerOptions options) =>
            new ToonString(value.Amount.ToString(CultureInfo.InvariantCulture) + " " + value.Currency);

        public override AotMoney Read(ToonValue value, ToonSerializerOptions options)
        {
            var parts = ((ToonString)value).Value.Split(' ');
            return new AotMoney(decimal.Parse(parts[0], CultureInfo.InvariantCulture), parts[1]);
        }
    }

    /// <summary>A converter for <c>int?</c> only (int values are not converted).</summary>
    private sealed class NullableIntConverter : IToonConverter
    {
        public bool CanConvert(Type type) => type == typeof(int?);

        public ToonValue Write(object? value, ToonSerializerOptions options) => new ToonString("p" + value);

        public object? Read(ToonValue value, Type targetType, ToonSerializerOptions options) =>
            value is ToonString s ? int.Parse(s.Value[1..], CultureInfo.InvariantCulture) : null;
    }

    private sealed class TagsConverter : ToonConverter<List<string>>
    {
        public override ToonValue Write(List<string>? value, ToonSerializerOptions options) => new ToonString(string.Join("|", value!));

        public override List<string> Read(ToonValue value, ToonSerializerOptions options) =>
            value is ToonString { Value.Length: > 0 } s ? [.. s.Value.Split('|')] : [];
    }

    /// <summary>Leaves secrets out (a null result), and reads nothing back.</summary>
    private sealed class SecretConverter : ToonConverter<Secret>
    {
        public override ToonValue? Write(Secret? value, ToonSerializerOptions options) => null;

        public override Secret? Read(ToonValue value, ToonSerializerOptions options) => new("read");
    }

    /// <summary>Writes ints times ten; reads numbers and strings (dictionary keys) back.</summary>
    private sealed class TenfoldIntConverter : ToonConverter<int>
    {
        public override ToonValue Write(int value, ToonSerializerOptions options) => new ToonNumber(value * 10L);

        public override int Read(ToonValue value, ToonSerializerOptions options) =>
            value is ToonString s ? int.Parse(s.Value, CultureInfo.InvariantCulture) : (int)((ToonNumber)value).Value / 10;
    }

    private sealed class UpperStringConverter : ToonConverter<string>
    {
        public override ToonValue Write(string? value, ToonSerializerOptions options) => new ToonString(value!.ToUpperInvariant());

        public override string Read(ToonValue value, ToonSerializerOptions options) => value is ToonString s ? s.Value.ToLowerInvariant() : null!;
    }

    private sealed class LineConverter : ToonConverter<ConvertedLine>
    {
        public override ToonValue Write(ConvertedLine? value, ToonSerializerOptions options) =>
            new ToonString(value!.Sku + "@" + value.Price.ToString(CultureInfo.InvariantCulture));

        public override ConvertedLine Read(ToonValue value, ToonSerializerOptions options)
        {
            var parts = ((ToonString)value).Value.Split('@');
            return new ConvertedLine { Sku = parts[0], Price = decimal.Parse(parts[1], CultureInfo.InvariantCulture) };
        }
    }

    private sealed class GuidConverter : ToonConverter<Guid>
    {
        public override ToonValue Write(Guid value, ToonSerializerOptions options) => new ToonString(value.ToString("N"));

        public override Guid Read(ToonValue value, ToonSerializerOptions options) => Guid.Parse(((ToonString)value).Value);
    }
}

public sealed record Secret(string Value);

[ToonSerializable]
public partial record struct Money(decimal Amount, string Currency);

[ToonSerializable]
public partial class ConvertedLine
{
    public string Sku { get; set; } = "";
    public decimal Price { get; set; }
    public DateTime? Shipped { get; set; }
}

[ToonSerializable]
public partial class ConvertedOrder
{
    public string Id { get; set; } = "";
    public DateTime Placed { get; set; }
    public int? Priority { get; set; }
    public Money Total { get; set; }
    public List<ConvertedLine> Lines { get; set; } = [];
    public Dictionary<int, string> Notes { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public Secret? Hidden { get; set; }
    public Dictionary<string, Money> ByCode { get; set; } = [];
    public Money[] Amounts { get; set; } = [];
}

[ToonSerializable(AllowReflectionFallback = false)]
public partial class AotInvoice
{
    public int Number { get; set; }
    public DateTime Issued { get; set; }
    public AotMoney Total { get; set; }
    public List<AotItem> Items { get; set; } = [];
}

[ToonSerializable(AllowReflectionFallback = false)]
public partial class AotItem
{
    public string Name { get; set; } = "";
    public AotMoney Price { get; set; }
}

[ToonSerializable(AllowReflectionFallback = false)]
public partial record struct AotMoney(decimal Amount, string Currency);

[ToonSerializable]
public partial class ConvertedShape
{
    public string Name { get; set; } = "";
}

public sealed class DerivedShape : ConvertedShape
{
    public int Sides { get; set; }
}

[ToonSerializable]
public partial class ShapeBox
{
    public ConvertedShape? Main { get; set; }
    public List<ConvertedShape> Items { get; set; } = [];
}
