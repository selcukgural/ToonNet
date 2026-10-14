using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.SourceGenerators.Tests.Tests;

/// <summary>
///     With converters in the options, generated code is still used for types where no converter applies to anything it
///     converts itself; the text and the objects must be the same as with reflection only, straight from text.
/// </summary>
public class ConverterGeneratedCodeTests
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

    private static void AssertSame<T>(T value, ToonSerializerOptions options)
    {
        var reflection = ReflectionOnly.Options(options);
        var expected = ToonSerializer.Serialize(value, reflection);

        var writeFallbacks = ToonSerializer.DirectWriteFallbacks;
        Assert.Equal(expected, ToonSerializer.Serialize(value, options));
        Assert.Equal(writeFallbacks, ToonSerializer.DirectWriteFallbacks);

        var readFallbacks = ToonSerializer.DirectReadFallbacks;
        var read = ToonSerializer.Deserialize<T>(expected, options);
        Assert.Equal(readFallbacks, ToonSerializer.DirectReadFallbacks);
        Assert.Equal(expected, ToonSerializer.Serialize(read, reflection));
    }

    private static BoxSize Size(int w, int h) => new() { Width = w, Height = h };

    private static ParcelDepot Depot() => new()
    {
        Parcels = [new() { Code = "a", Box = Size(1, 2), Inner = Size(3, 4) }, new() { Code = "b", Box = Size(5, 6) }],
        Sizes = new Dictionary<string, BoxSize> { ["s"] = Size(1, 1), ["m"] = Size(2, 2) },
        Plan = new ShippingPlan { Small = Size(1, 1), Large = Size(9, 9) },
        Note = "n"
    };

    private static List<WriterRow> Rows() =>
    [
        new() { Id = 1, Name = "a", Score = 1.5, Mood = Mood.Busy, When = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), Flag = true },
        new() { Id = 2, Name = null, Score = null }
    ];

    [Fact]
    public void UnrelatedConverter_KeepsGeneratedCode()
    {
        var options = With(new GuidConverter());
        var check = ToonSerializer.GetConverterCheck(options)!;

        Assert.True(check.AllowsGeneratedCode(typeof(ParcelDepot)));
        Assert.True(check.AllowsGeneratedCode(typeof(WriterRow)));

        AssertSame(Depot(), options);
        AssertSame(Rows(), options);
    }

    [Fact]
    public void ConverterForAnInlineType_TurnsGeneratedCodeOff_ForTheTypesThatReachIt()
    {
        // int is converted by BoxSize's generated code, which ParcelDepot's generated code reaches
        var options = With(new HashIntConverter());
        var check = ToonSerializer.GetConverterCheck(options)!;

        Assert.False(check.AllowsGeneratedCode(typeof(BoxSize)));
        Assert.False(check.AllowsGeneratedCode(typeof(ParcelDepot)));
        Assert.False(check.AllowsGeneratedCode(typeof(WriterRow)));
        Assert.False(check.AllowsGeneratedCode(typeof(Collections))); // int?[]: through the underlying type

        AssertSame(Depot(), options);
        AssertSame(Rows(), options);
        AssertSame(new Collections(), options);
    }

    [Fact]
    public void ConverterForAGeneratedType_IsUsedForItsValues()
    {
        var options = With(new BoxSizeConverter());
        var check = ToonSerializer.GetConverterCheck(options)!;

        Assert.False(check.AllowsGeneratedCode(typeof(BoxSize)));
        Assert.False(check.AllowsGeneratedCode(typeof(ParcelDepot)));
        Assert.True(check.AllowsGeneratedCode(typeof(WriterRow)));

        AssertSame(Depot(), options);
        Assert.Contains("Box: 1x2", ToonSerializer.Serialize(Depot().Parcels[0], options));
    }

    [Fact]
    public void ChangingTheConverters_ChecksAgain()
    {
        var options = With(new GuidConverter());
        Assert.True(ToonSerializer.GetConverterCheck(options)!.AllowsGeneratedCode(typeof(WriterRow)));

        options.AddConverter(new UpperStringConverter());
        Assert.False(ToonSerializer.GetConverterCheck(options)!.AllowsGeneratedCode(typeof(WriterRow)));
        AssertSame(Rows(), options);

        options.Converters.Clear();
        Assert.Null(ToonSerializer.GetConverterCheck(options));
    }

    private sealed class GuidConverter : ToonConverter<Guid>
    {
        public override ToonValue Write(Guid value, ToonSerializerOptions options) => new ToonString(value.ToString("N"));

        public override Guid Read(ToonValue value, ToonSerializerOptions options) => Guid.Parse(((ToonString)value).Value);
    }

    private sealed class HashIntConverter : ToonConverter<int>
    {
        public override ToonValue Write(int value, ToonSerializerOptions options) => new ToonString("#" + value);

        public override int Read(ToonValue value, ToonSerializerOptions options) =>
            value is ToonString s ? int.Parse(s.Value.TrimStart('#')) : (int)((ToonNumber)value).Value;
    }

    private sealed class UpperStringConverter : ToonConverter<string>
    {
        public override ToonValue Write(string? value, ToonSerializerOptions options) =>
            value is null ? ToonNull.Instance : new ToonString(value.ToUpperInvariant());

        public override string Read(ToonValue value, ToonSerializerOptions options) => value is ToonString s ? s.Value : null!;
    }

    private sealed class BoxSizeConverter : ToonConverter<BoxSize>
    {
        public override ToonValue Write(BoxSize? value, ToonSerializerOptions options) =>
            value is null ? ToonNull.Instance : new ToonString($"{value.Width}x{value.Height}");

        public override BoxSize Read(ToonValue value, ToonSerializerOptions options)
        {
            if (value is not ToonString s)
            {
                return null!;
            }

            var parts = s.Value.Split('x');
            return new BoxSize { Width = int.Parse(parts[0]), Height = int.Parse(parts[1]) };
        }
    }
}
