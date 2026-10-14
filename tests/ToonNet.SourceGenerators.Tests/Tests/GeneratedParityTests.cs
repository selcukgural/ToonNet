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

[ToonSerializable]
public partial class Member
{
    public string Name { get; set; } = "";

    public bool IsAdmin { get; private set; }

    public decimal Balance { get; protected set; }

    public int Level { get; internal set; }

    [ToonInclude]
    public string Team { get; private set; } = "";

    public void Promote() => IsAdmin = true;
}

public interface IUserView
{
    string Name { get; }
}

[ToonSerializable]
public partial class UserSummary : IUserView
{
    public string Name { get; set; } = "";
}

[ToonSerializable]
public partial class UserEntity : UserSummary
{
    public string PasswordHash { get; set; } = "secret-hash";

    public bool IsAdmin { get; set; } = true;
}

public class PlainSummary
{
    public string Name { get; set; } = "";
}

public class PlainEntity : PlainSummary
{
    public string PasswordHash { get; set; } = "secret-hash";
}

[ToonSerializable]
public partial class Envelope
{
    public UserSummary? User { get; set; }

    public List<UserSummary> Users { get; set; } = [];

    public Dictionary<string, UserSummary> ById { get; set; } = [];

    public IUserView? View { get; set; }

    public PlainSummary? Plain { get; set; }

    public object? Anything { get; set; }
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
    public void NonPublicSetters_AreNotSetFromDocuments_UnlessIncluded()
    {
        const string toon = "Name: bob\nIsAdmin: true\nBalance: 1000000\nLevel: 9\nTeam: red";

        foreach (var member in new[]
                 {
                     ToonSerializer.Deserialize<Member>(toon)!, ToonSerializer.Deserialize<Member>(toon, ReflectionOnly.Options())!,
                     ToonSerializer.Deserialize(toon, Member.ToonTypeInfo)!, ToonSerializer.Deserialize<Member>("name: bob\nisadmin: true",
                         new ToonSerializerOptions { PropertyNameCaseInsensitive = true })!
                 })
        {
            Assert.Equal("bob", member.Name);
            Assert.False(member.IsAdmin);
            Assert.Equal(0m, member.Balance);
            Assert.Equal(0, member.Level);
        }

        Assert.Equal("red", ToonSerializer.Deserialize<Member>(toon)!.Team);
        Assert.Equal("red", ToonSerializer.Deserialize<Member>(toon, ReflectionOnly.Options())!.Team);

        // They are still written (as read-only properties are)
        var promoted = new Member { Name = "a" };
        promoted.Promote();
        AssertSameText(promoted, "Name: a\nIsAdmin: true\nBalance: 0\nLevel: 0\nTeam: \"\"");
    }

    [Fact]
    public void DerivedObjects_DoNotWriteTheMembersTheirDeclaredTypeLacks()
    {
        var entity = new UserEntity { Name = "bob" };
        var envelope = new Envelope
        {
            User = entity, Users = [entity, new UserSummary { Name = "ann" }], ById = { ["b"] = entity }, View = entity,
            Plain = new PlainEntity { Name = "pat" }
        };

        const string expected = "User:\n  Name: bob\nUsers[2]{Name}:\n  bob\n  ann\nById:\n  b:\n    Name: bob\nView:\n  Name: bob\nPlain:\n  Name: pat\nAnything: null";
        string[] texts =
        [
            ToonSerializer.Serialize(envelope), ToonSerializer.Serialize(envelope, ReflectionOnly.Options()),
            ToonSerializer.Serialize(envelope, Envelope.ToonTypeInfo), new ToonNet.Core.Encoding.ToonEncoder().Encode(Envelope.Serialize(envelope)),
            ToonSerializer.SerializeToValue(envelope).ToString()!, ToonNet.Core.Llm.ToonPrompt.Serialize(envelope)
        ];

        foreach (var text in texts)
        {
            Assert.DoesNotContain("secret", text);
            Assert.DoesNotContain("IsAdmin", text);
        }

        Assert.Equal(expected, texts[0]);
        Assert.Equal(expected, texts[1]);
        Assert.Equal(expected, texts[2]);
        Assert.Equal(expected, texts[3]);

        // The root too, by the type argument
        Assert.Equal("Name: bob", ToonSerializer.Serialize<UserSummary>(entity));
        Assert.Equal("Name: bob", ToonSerializer.Serialize(entity, UserSummary.ToonTypeInfo));
        Assert.Equal("[1]{Name}:\n  bob", ToonSerializer.Serialize<IUserView[]>([entity]));

        // A value declared as object, and the option, write the runtime type
        Assert.Contains("PasswordHash: secret-hash", ToonSerializer.Serialize(new Envelope { Anything = entity }));
        var runtime = new ToonSerializerOptions { SerializeRuntimeTypes = true };
        Assert.Contains("PasswordHash: secret-hash", ToonSerializer.Serialize(envelope, runtime));
        Assert.Equal(ToonSerializer.Serialize(envelope, ReflectionOnly.Options(runtime)), ToonSerializer.Serialize(envelope, runtime));
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
