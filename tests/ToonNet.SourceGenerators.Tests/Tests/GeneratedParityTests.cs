using System.Globalization;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.SourceGenerators.Tests.Tests;

public sealed class IntAsHashConverter : ToonConverter<int>
{
    public override ToonValue? Write(int value, ToonSerializerOptions options) => new ToonString("#" + value.ToString(CultureInfo.InvariantCulture));

    public override int Read(ToonValue value, ToonSerializerOptions options) => int.Parse(value.GetValue<string>()[1..], CultureInfo.InvariantCulture);
}

[ToonSerializable]
public partial class PropertyConverted
{
    [ToonConverter(typeof(IntAsHashConverter))]
    public int N { get; set; }

    public int Plain { get; set; }
}

[ToonSerializable]
public partial class Ordered
{
    [ToonPropertyOrder(2)]
    public int A { get; set; } = 1;

    public int B { get; set; } = 2;

    [ToonPropertyOrder(-5)]
    public int C { get; set; } = 3;
}

public enum Permission
{
    Read,
    Write
}

[ToonSerializable]
public partial record WithDefaults(
    int Id,
    double D = -1.5,
    char Q = '\'',
    float F = 2.5f,
    long L = long.MinValue,
    decimal M = 1.10m,
    int? N = 4,
    Permission P = Permission.Write,
    Permission? PN = Permission.Write,
    string S = "x\"y");

[ToonSerializable]
public partial class ConstructorMarked
{
    public ConstructorMarked()
    {
        Origin = "parameterless";
    }

    [ToonConstructor]
    public ConstructorMarked(int x)
    {
        X = x;
        Origin = "marked";
    }

    public int X { get; set; }

    public string Origin { get; private set; }
}

/// <summary>Generated code gives the text and the objects of the reflection-based serializer, feature by feature.</summary>
public class GeneratedParityTests
{
    private static void AssertSameText<T>(T value, string expected)
    {
        Assert.Equal(expected, ToonSerializer.Serialize(value));
        Assert.Equal(expected, ToonSerializer.Serialize(value, ReflectionOnly.Options()));
    }

    [Fact]
    public void PropertyConverters_RunInGeneratedCode()
    {
        AssertSameText(new PropertyConverted { N = 3, Plain = 4 }, "N: \"#3\"\nPlain: 4");

        Assert.Equal(9, ToonSerializer.Deserialize<PropertyConverted>("N: \"#9\"\nPlain: 1")!.N);
        Assert.Equal(9, ToonSerializer.Deserialize<PropertyConverted>("N: \"#9\"\nPlain: 1", ReflectionOnly.Options())!.N);
        Assert.Equal("N: \"#3\"\nPlain: 4", ToonSerializer.Serialize(new PropertyConverted { N = 3, Plain = 4 }, PropertyConverted.ToonTypeInfo));
        Assert.Equal(9, ToonSerializer.Deserialize("N: \"#9\"\nPlain: 1", PropertyConverted.ToonTypeInfo)!.N);
    }

    [Fact]
    public void PropertyOrder_IsApplied()
    {
        AssertSameText(new Ordered(), "C: 3\nB: 2\nA: 1");
        Assert.Equal("C: 3\nB: 2\nA: 1", ToonSerializer.Serialize(new Ordered(), Ordered.ToonTypeInfo));
    }

    [Fact]
    public void ConstructorDefaults_AreTheDeclaredValues()
    {
        var generated = ToonSerializer.Deserialize<WithDefaults>("Id: 1")!;
        var reflected = ToonSerializer.Deserialize<WithDefaults>("Id: 1", ReflectionOnly.Options())!;
        var expected = new WithDefaults(1);

        Assert.Equal(expected, generated);
        Assert.Equal(expected, reflected);
        Assert.Equal(Permission.Write, reflected.PN);
        Assert.Equal(1.10m, generated.M);
        Assert.Equal("1.10", generated.M.ToString(CultureInfo.InvariantCulture)); // the declared scale
    }

    [Fact]
    public void ToonConstructor_IsTheOneUsed()
    {
        Assert.Equal(("marked", 3), Read(ToonSerializer.Deserialize<ConstructorMarked>("X: 3")!));
        Assert.Equal(("marked", 3), Read(ToonSerializer.Deserialize<ConstructorMarked>("X: 3", ReflectionOnly.Options())!));
        Assert.Equal(("marked", 3), Read(ToonSerializer.Deserialize("X: 3", ConstructorMarked.ToonTypeInfo)!));

        static (string, int) Read(ConstructorMarked value) => (value.Origin, value.X);
    }
}
