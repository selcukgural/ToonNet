using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.SourceGenerators.Tests.Tests;

[ToonSerializable]
public sealed partial record BindingUser(string Name, [property: ToonIgnore] bool IsAdmin = false);

[ToonSerializable]
public sealed partial class BindingInternalRole(string name, bool isAdmin)
{
    public string Name { get; } = name;

    internal bool IsAdmin { get; } = isAdmin;
}

[ToonSerializable]
public sealed partial class BindingMarked
{
    [ToonConstructor]
    public BindingMarked(int amount, string currency)
    {
        Amount = amount;
        Text = $"{amount} {currency}";
    }

    public int Amount { get; }

    public string Text { get; }
}

/// <summary>The parameter 'pass' matches no property (TOON012, which the project does not report).</summary>
[ToonSerializable]
public sealed partial class BindingLogin(string user, string pass)
{
    public string User { get; } = user;

    public string Secret => pass;
}

/// <summary>Generated readers bind constructor parameters as ToonSerializer does.</summary>
public class GeneratedConstructorBindingTests
{
    /// <summary>The generated text and tree readers and the reflection-based reader give the same object.</summary>
    private static T? Read<T>(string toon, ToonSerializerOptions? options = null)
    {
        options ??= new ToonSerializerOptions();
        var fallbacks = ToonSerializer.DirectReadFallbacks;
        var generated = ToonSerializer.Deserialize<T>(toon, options);
        Assert.Equal(fallbacks, ToonSerializer.DirectReadFallbacks);

        var tree = ToonSerializer.DeserializeFromValue<T>(ToonDocument.Parse(toon).Root, options);
        var reflection = ToonSerializer.Deserialize<T>(toon, ReflectionOnly.Options(options));
        Assert.Equivalent(reflection, generated, strict: true);
        Assert.Equivalent(reflection, tree, strict: true);
        return generated;
    }

    [Fact]
    public void ParametersOfIgnoredOrNonPublicProperties_AreNotRead()
    {
        Assert.Equal(new BindingUser("a"), Read<BindingUser>("Name: a\nIsAdmin: true"));
        Assert.Equal(new BindingUser("a"), Read<BindingUser>("Name: a\nisadmin: true", new ToonSerializerOptions { PropertyNameCaseInsensitive = true }));
        Assert.False(Read<BindingInternalRole>("Name: a\nisAdmin: true")!.IsAdmin);

        // The key is not mapped
        var strict = new ToonSerializerOptions { UnmappedMemberHandling = ToonUnmappedMemberHandling.Disallow };
        Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<BindingUser>("Name: a\nIsAdmin: true", strict));
        Assert.Throws<ToonSerializationException>(() => ToonSerializer.DeserializeFromValue<BindingUser>(ToonDocument.Parse("Name: a\nIsAdmin: true").Root, strict));
        Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<BindingUser>("Name: a\nIsAdmin: true", ReflectionOnly.Options(strict)));
    }

    [Fact]
    public void ParametersWithoutAMember_AreAnError_UnlessTheConstructorIsMarked()
    {
        const string message = "The parameter 'pass' of the constructor of BindingLogin matches no public property. " +
                               "Add a property with its name, or mark the constructor [ToonConstructor] to read the parameter from the key 'pass'.";

        Assert.Equal(message, Assert.Throws<InvalidOperationException>(() => ToonSerializer.Deserialize<BindingLogin>("User: u\npass: p")).Message);
        Assert.Equal(message, Assert.Throws<InvalidOperationException>(() => ToonSerializer.DeserializeFromValue<BindingLogin>(ToonDocument.Parse("User: u").Root)).Message);
        Assert.Equal(message, Assert.Throws<InvalidOperationException>(() => ToonSerializer.Deserialize("User: u", BindingLogin.ToonTypeInfo)).Message);
        Assert.Equal(message, Assert.Throws<InvalidOperationException>(() => ToonSerializer.Deserialize<BindingLogin>("User: u", ReflectionOnly.Options())).Message);
        Assert.Null(ToonSerializer.Deserialize<BindingLogin>("null"));

        Assert.Equal("User: u\nSecret: p", ToonSerializer.Serialize(new BindingLogin("u", "p")));
        Assert.Equal("5 EUR", Read<BindingMarked>("Amount: 5\ncurrency: EUR")!.Text);
    }
}
