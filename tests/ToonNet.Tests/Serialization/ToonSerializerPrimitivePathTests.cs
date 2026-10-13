using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Serialization;

/// <summary>
///     Primitive properties and collection items are read and written through typed delegates when no converter
///     applies; these tests check that the results and errors match the general path.
/// </summary>
public class ToonSerializerPrimitivePathTests
{
    public class AllPrimitives
    {
        public string? Text { get; set; }
        public bool Flag { get; set; }
        public char Letter { get; set; }
        public int Int { get; set; }
        public long Long { get; set; }
        public short Short { get; set; }
        public byte Byte { get; set; }
        public sbyte SByte { get; set; }
        public ushort UShort { get; set; }
        public uint UInt { get; set; }
        public ulong ULong { get; set; }
        public double Double { get; set; }
        public float Float { get; set; }
        public decimal Decimal { get; set; }
        public DateTime Date { get; set; }
        public DateTimeOffset Offset { get; set; }
        public Guid Id { get; set; }
        public int? NullableInt { get; set; }
        public double? NullableDouble { get; set; }
        public bool? NullableFlag { get; set; }
        public List<int> Ints { get; set; } = [];
        public List<int?> NullableInts { get; set; } = [];
        public string?[] Texts { get; set; } = [];
        public int ReadOnly => Int + 1;
    }

    private static AllPrimitives Sample() => new()
    {
        Text = "a: b", Flag = true, Letter = 'x', Int = int.MinValue, Long = long.MaxValue, Short = -5, Byte = 255,
        SByte = -128, UShort = 65535, UInt = uint.MaxValue, ULong = ulong.MaxValue, Double = 0.1, Float = 1.5f,
        Decimal = 12.50m, Date = new DateTime(2026, 10, 13, 1, 2, 3, DateTimeKind.Utc),
        Offset = new DateTimeOffset(2026, 10, 13, 1, 2, 3, TimeSpan.FromHours(3)), Id = Guid.Parse("c56a4180-65aa-42ec-a945-5fd21dec0538"),
        NullableInt = 7, NullableDouble = null, NullableFlag = false, Ints = [1, -2, 3], NullableInts = [1, null], Texts = ["x", null]
    };

    /// <summary>Options whose converter list is not empty, which turns the typed path off everywhere.</summary>
    private static ToonSerializerOptions GeneralPathOptions(ToonSerializerOptions? options = null)
    {
        options ??= new ToonSerializerOptions();
        options.AddConverter(new NeverConverter());
        return options;
    }

    [Fact]
    public void TypedPath_MatchesTheGeneralPath()
    {
        var sample = Sample();
        var toon = ToonSerializer.Serialize(sample);

        Assert.Equal(ToonSerializer.Serialize(sample, GeneralPathOptions()), toon);
        Assert.Equal(ToonSerializer.Serialize(new AllPrimitives(), GeneralPathOptions()), ToonSerializer.Serialize(new AllPrimitives()));

        var back = ToonSerializer.Deserialize<AllPrimitives>(toon)!;
        Assert.Equivalent(ToonSerializer.Deserialize<AllPrimitives>(toon, GeneralPathOptions()), back, strict: true);
        Assert.Equivalent(sample, back, strict: true);
        Assert.Equal(toon, ToonSerializer.Serialize(back));
    }

    [Fact]
    public void TypedPath_HonorsIgnoreNullValues()
    {
        var options = new ToonSerializerOptions { IgnoreNullValues = true };
        var toon = ToonSerializer.Serialize(new AllPrimitives(), options);

        Assert.Equal(ToonSerializer.Serialize(new AllPrimitives(), GeneralPathOptions(new ToonSerializerOptions { IgnoreNullValues = true })), toon);
        Assert.DoesNotContain("Text:", toon);
        Assert.DoesNotContain("NullableInt:", toon);
        Assert.Contains("Text: null", ToonSerializer.Serialize(new AllPrimitives()));
    }

    [Theory]
    [InlineData("Int: 2147483648", "$.Int", "outside the range of Int32")]
    [InlineData("Byte: -1", "$.Byte", "outside the range of Byte")]
    [InlineData("Int: 1.5", "$.Int", "not an integer")]
    [InlineData("Float: 1e300", "$.Float", "outside the range of Single")]
    [InlineData("Flag: 1", "$.Flag", "Cannot convert Number to Boolean")]
    [InlineData("Int: null", "$.Int", "Cannot convert null to the non-nullable type Int32")]
    [InlineData("Ints[2]: 1,x", "$.Ints[1]", "The string is not a number")]
    public void TypedPath_FallsBackForErrors(string toon, string path, string message)
    {
        var ex = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<AllPrimitives>(toon));

        Assert.Equal(path, ex.Path);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void TypedPath_FallsBackForConvertibleValues()
    {
        var copy = ToonSerializer.Deserialize<AllPrimitives>("Text: 123\nInt: \"42\"\nNullableInts[3]: 1,null,\"3\"\nFlag: \"true\"")!;

        Assert.Equal(("123", 42, true), (copy.Text, copy.Int, copy.Flag));
        Assert.Equal([1, null, 3], copy.NullableInts);
    }

    [Fact]
    public void Converters_DisableTheTypedPath()
    {
        var options = new ToonSerializerOptions();
        options.AddConverter(new DoublingIntConverter());

        var toon = ToonSerializer.Serialize(new AllPrimitives { Int = 21, Ints = [1] }, options);

        Assert.Contains("Int: 42", toon);
        Assert.Contains("Ints[1]: 2", toon);
        Assert.Equal(21, ToonSerializer.Deserialize<AllPrimitives>("Int: 42", options)!.Int);
        Assert.Equal([1], ToonSerializer.Deserialize<AllPrimitives>("Ints[1]: 2", options)!.Ints);
    }

    private sealed class NeverConverter : IToonConverter
    {
        public bool CanConvert(Type type) => false;

        public ToonValue Write(object? value, ToonSerializerOptions options) => throw new NotSupportedException();

        public object Read(ToonValue value, Type targetType, ToonSerializerOptions options) => throw new NotSupportedException();
    }

    private sealed class DoublingIntConverter : ToonConverter<int>
    {
        public override ToonValue Write(int value, ToonSerializerOptions options) => new ToonNumber((long)value * 2);

        public override int Read(ToonValue value, ToonSerializerOptions options) => (int)((ToonNumber)value).Value / 2;
    }
}
