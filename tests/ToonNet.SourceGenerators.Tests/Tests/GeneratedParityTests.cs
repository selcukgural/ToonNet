using System.Globalization;
using ToonNet.Core;
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

public interface IAdminView : IUserView
{
    int Level { get; }
}

public class AdminEntity : IAdminView
{
    public string Name { get; set; } = "";
    public int Level { get; set; } = 3;
    public string Secret { get; set; } = "secret-admin";
}

public struct StructThing : IUserView
{
    public string Name { get; set; }
    public string Secret { get; set; }
}

[ToonSerializable]
public partial class Covariant
{
    public IEnumerable<UserSummary> Seq { get; set; } = [];
    public IReadOnlyList<UserSummary> ReadOnly { get; set; } = [];
    public UserSummary[] Arr { get; set; } = [];
    public IAdminView? Admin { get; set; }
    public IUserView? Thing { get; set; }
}

public class IncludedBase
{
    [ToonInclude]
    public virtual int Level { get; protected set; }
}

[ToonSerializable]
public partial class IncludedDerived : IncludedBase
{
    public override int Level { get => base.Level; protected set => base.Level = value; }
}

[ToonSerializable]
public partial class InPoint
{
    [ToonConstructor]
    public InPoint(in int x, in int y)
    {
        X = x;
        Y = y;
        Via = "in";
    }

    public InPoint() => Via = "default";

    public int X { get; }
    public int Y { get; }
    public string Via { get; }
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

[ToonSerializable]
public partial class Policy
{
    public required string Name { get; init; }

    public required bool RequireMfa { get; init; } = true;

    public required List<string> Tags { get; init; } = ["keep"];
}

[ToonSerializable]
public partial class Measured
{
    public float X { get; set; }

    public List<float> L { get; set; } = [];
}

[ToonSerializable]
public partial class Throwing
{
    public Throwing(int x)
    {
        if (x < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "secret path /etc/x");
        }

        X = x;
    }

    public int X { get; }
}

public class PlainValue
{
    public int P { get; set; } = 3;
}

[ToonSerializable]
public partial class Lazy
{
    public int Reads;

    public IEnumerable<int>? Seq { get; set; }

    public int Counted
    {
        get
        {
            Reads++;
            return 1;
        }
    }

    public object? Any { get; set; }
}

[ToonSerializable]
public partial class NullableHolder
{
    public int? N { get; set; }
}

[ToonSerializable]
public partial class Chain
{
    public int V { get; set; }
    public Chain? Next { get; set; }
}

public class LegsBase
{
    public virtual int Legs { get; set; } = 4;

    [ToonInclude]
    public int Private { get; private set; } = 2;
}

[ToonSerializable]
public partial class LegsGetOnly : LegsBase
{
    public override int Legs => base.Legs;
}

public class HiddenBase
{
    public int Hidden { get; set; } = 1;
    public string Name { get; set; } = "n";
}

[ToonSerializable]
public partial class Hiding : HiddenBase
{
    public new string Hidden { get; set; } = "h";
}

[ToonSerializable]
public partial class CtorBody
{
    public CtorBody() => Name = "ctor";

    public required string Name { get; set; }
    public int X { get; set; }
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
    public void MissingRequiredMembers_KeepTheirInitializers_WhenNotRequired()
    {
        var lenient = new ToonSerializerOptions { RespectRequiredMembers = false };

        foreach (var policy in new[]
                 {
                     ToonSerializer.Deserialize<Policy>("Name: p", lenient)!, ToonSerializer.Deserialize<Policy>("Name: p", ReflectionOnly.Options(lenient))!,
                     ToonSerializer.Deserialize("Name: p", Policy.ToonTypeInfo, lenient)!, Policy.Deserialize(ToonNet.Core.Models.ToonDocument.Parse("Name: p"), lenient)
                 })
        {
            Assert.True(policy.RequireMfa);
            Assert.Equal(["keep"], policy.Tags);
        }

        // With the default options they are required
        Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Policy>("Name: p"));
        Assert.False(ToonSerializer.Deserialize<Policy>("Name: p\nRequireMfa: false\nTags[0]:")!.RequireMfa);
    }

    [Fact]
    public void FloatsBeyondTheirRange_AreErrors_InGeneratedCode()
    {
        foreach (var toon in new[] { "X: 1e39", "L[1]: 1e39" })
        {
            Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Measured>(toon));
            Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Measured>(toon, ReflectionOnly.Options()));
            Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize(toon, Measured.ToonTypeInfo));
        }

        Assert.Equal(3.5f, ToonSerializer.Deserialize<Measured>("X: 3.5")!.X);
    }

    [Fact]
    public void GeneratedCode_CannotBeRegisteredForATypeOfAnotherModule()
    {
        // A delegate from this assembly for a type of the framework
        Assert.Throws<InvalidOperationException>(() => ToonNet.Core.Serialization.ToonSourceGenerationHelpers.Register<Dictionary<string, string>>(
            (_, _, _) => new ToonNet.Core.Models.ToonObject(), null));
        Assert.Equal("a: b", ToonSerializer.Serialize(new Dictionary<string, string> { ["a"] = "b" }));

        // Nor by other code of the type's module: only methods of the type, or of classes nested in it
        Assert.Throws<InvalidOperationException>(() => ToonNet.Core.Serialization.ToonSourceGenerationHelpers.Register<Measured>(
            (_, _, _) => new ToonNet.Core.Models.ToonString("hijacked"), null));
        Assert.Equal("X: 1\nL: []", ToonSerializer.Serialize(new Measured { X = 1 }));

        // A dynamic method created in the type's module has no declaring type
        var dynamic = new System.Reflection.Emit.DynamicMethod("Hijack", typeof(ToonNet.Core.Models.ToonValue),
                                                               [typeof(Measured), typeof(ToonSerializerOptions), typeof(int)], typeof(Measured).Module);
        var il = dynamic.GetILGenerator();
        il.Emit(System.Reflection.Emit.OpCodes.Ldnull);
        il.Emit(System.Reflection.Emit.OpCodes.Ret);
        Assert.Throws<InvalidOperationException>(() => ToonNet.Core.Serialization.ToonSourceGenerationHelpers.Register(
            dynamic.CreateDelegate<Func<Measured, ToonSerializerOptions, int, ToonNet.Core.Models.ToonValue>>(), null));

        // The type's own methods with a writer or reader of other code
        Assert.Throws<InvalidOperationException>(() => ToonNet.Core.Serialization.ToonSourceGenerationHelpers.Register<Measured>(
            Measured.__ToonSerializeValue, Measured.__ToonDeserializeValue, null, new ForeignReader()));
        Assert.Equal(1, ToonSerializer.Deserialize<Measured>("X: 1")!.X);
    }

    private sealed class ForeignReader : ToonNet.Core.Serialization.ToonObjectReader<Measured>
    {
        public override Measured Read(ToonNet.Core.Serialization.ToonReader reader, in ToonNet.Core.Serialization.ToonReaderValue value, int depth) =>
            new() { X = 42 };
    }

    [Fact]
    public void CovariantCollections_InterfacesAndStructs_AreWrittenAsTheirDeclaredTypes()
    {
        UserEntity[] entities = [new() { Name = "a" }, new() { Name = "b" }];
        var value = new Covariant
        {
            Seq = new List<UserEntity>(entities), ReadOnly = entities, Arr = entities, Admin = new AdminEntity { Name = "n" },
            Thing = new StructThing { Name = "s", Secret = "secret-struct" }
        };

        const string expected = "Seq[2]{Name}:\n  a\n  b\nReadOnly[2]{Name}:\n  a\n  b\nArr[2]{Name}:\n  a\n  b\nAdmin:\n  Name: n\n  Level: 3\nThing:\n  Name: s";

        Assert.Equal(expected, ToonSerializer.Serialize(value));
        Assert.Equal(expected, ToonSerializer.Serialize(value, ReflectionOnly.Options()));
        Assert.Equal(expected, ToonSerializer.Serialize(value, Covariant.ToonTypeInfo));
        Assert.Equal(expected, new ToonNet.Core.Encoding.ToonEncoder().Encode(Covariant.Serialize(value)));
        Assert.Equal("[2]{Name}:\n  a\n  b", ToonSerializer.Serialize<IEnumerable<UserSummary>>(new List<UserEntity>(entities)));
        Assert.DoesNotContain("secret", ToonSerializer.Serialize(new List<IUserView> { new StructThing { Name = "x", Secret = "secret" } }));
    }

    [Fact]
    public void ToonInclude_OnAnOverriddenProperty_AndInParameters_ReadAlike()
    {
        Assert.Equal(9, ToonSerializer.Deserialize<IncludedDerived>("Level: 9")!.Level);
        Assert.Equal(9, ToonSerializer.Deserialize<IncludedDerived>("Level: 9", ReflectionOnly.Options())!.Level);

        foreach (var point in new[] { ToonSerializer.Deserialize<InPoint>("X: 3\nY: 4")!, ToonSerializer.Deserialize<InPoint>("X: 3\nY: 4", ReflectionOnly.Options())! })
        {
            Assert.Equal((3, 4, "in"), (point.X, point.Y, point.Via));
        }
    }

    [Fact]
    public void ConstructorExceptions_AreSerializationErrors_WithoutTheirMessage()
    {
        foreach (var read in new Func<Throwing?>[]
                 {
                     () => ToonSerializer.Deserialize<Throwing>("X: -1"), () => ToonSerializer.Deserialize<Throwing>("X: -1", ReflectionOnly.Options()),
                     () => ToonSerializer.Deserialize("X: -1", Throwing.ToonTypeInfo), () => Throwing.Deserialize(ToonNet.Core.Models.ToonDocument.Parse("X: -1"))
                 })
        {
            var ex = Assert.Throws<ToonSerializationException>(read);
            Assert.Contains("The constructor of Throwing threw ArgumentOutOfRangeException", ex.Message);
            Assert.DoesNotContain("secret", ex.Message);
            Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
        }
    }

    [Fact]
    public void NullValues_DoNotCountTowardsTheDepth()
    {
        const string toon = "V: 1\nNext:\n  V: 2\n  Next: null";
        var options = new ToonSerializerOptions { MaxDepth = 1 };

        Assert.Equal(2, ToonSerializer.Deserialize<Chain>(toon, options)!.Next!.V);
        Assert.Equal(2, ToonSerializer.Deserialize<Chain>(toon, ReflectionOnly.Options(options))!.Next!.V);
        Assert.Equal(2, ToonSerializer.Deserialize(toon, Chain.ToonTypeInfo, options)!.Next!.V);
        Assert.Equal(toon, ToonSerializer.Serialize(new Chain { V = 1, Next = new Chain { V = 2 } }, options));
    }

    [Fact]
    public void Overrides_BaseSetters_AndHiddenProperties_ReadAlike()
    {
        foreach (var options in new[] { new ToonSerializerOptions(), ReflectionOnly.Options() })
        {
            var legs = ToonSerializer.Deserialize<LegsGetOnly>("Legs: 2\nPrivate: 7", options)!;
            Assert.Equal((2, 7), (legs.Legs, legs.Private));

            var hiding = new Hiding();
            Assert.Equal("Name: n\nHidden: h", ToonSerializer.Serialize(hiding, options));
            Assert.Equal("x", ToonSerializer.Deserialize<Hiding>("Name: n\nHidden: x", options)!.Hidden);
        }
    }

    [Fact]
    public void MissingRequiredMembers_KeepWhatTheConstructorSet_WhenNotRequired()
    {
        var lenient = new ToonSerializerOptions { RespectRequiredMembers = false };

        Assert.Equal("ctor", ToonSerializer.Deserialize<CtorBody>("X: 1", lenient)!.Name);
        Assert.Equal("ctor", ToonSerializer.Deserialize<CtorBody>("X: 1", ReflectionOnly.Options(lenient))!.Name);
        Assert.Equal("ctor", ToonSerializer.Deserialize("X: 1", CtorBody.ToonTypeInfo, lenient)!.Name);
    }

    [Fact]
    public void NullableValueErrors_NameTheUnderlyingType()
    {
        var generated = Assert.Throws<ToonSerializationException>(() => NullableHolder.Deserialize(ToonNet.Core.Models.ToonDocument.Parse("N: abc")));
        var reflected = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<NullableHolder>("N: abc", ReflectionOnly.Options()));

        Assert.Contains("target type: Int32", generated.Message);
        Assert.Equal(reflected.Message, generated.Message);
    }

    [Fact]
    public void TypeInfoWriting_EnumeratesSequencesOnce_WhenAValueNeedsReflection()
    {
        var enumerations = 0;

        IEnumerable<int> OneShot()
        {
            enumerations++;
            yield return 1;
            yield return 2;
        }

        var lazy = new Lazy { Seq = OneShot(), Any = new PlainValue() };

        Assert.Equal("Seq[2]: 1,2\nCounted: 1\nAny:\n  P: 3", ToonSerializer.Serialize(lazy, Lazy.ToonTypeInfo));
        Assert.Equal((1, 1), (enumerations, lazy.Reads));
    }

    private sealed class DeclaredConverter : IToonConverter
    {
        public bool CanConvert(Type type) => type == typeof(UserSummary);

        public ToonNet.Core.Models.ToonValue Write(object? value, ToonSerializerOptions options) => new ToonNet.Core.Models.ToonString("U:" + ((UserSummary)value!).Name);

        public object Read(ToonNet.Core.Models.ToonValue value, Type targetType, ToonSerializerOptions options) => new UserSummary();
    }

    [Fact]
    public void ComposedTypeInfos_ConvertItemsByTheirDeclaredType()
    {
        var options = new ToonSerializerOptions();
        options.AddConverter(new DeclaredConverter());
        var users = new Dictionary<string, UserSummary> { ["a"] = new UserEntity { Name = "bob" } };

        Assert.Equal("a: \"U:bob\"", ToonSerializer.Serialize(users, ToonTypeInfo.CreateDictionary(UserSummary.ToonTypeInfo), options));
        Assert.Equal("a: \"U:bob\"", ToonSerializer.Serialize(users, options));
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
