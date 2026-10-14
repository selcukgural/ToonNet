using System.Diagnostics.CodeAnalysis;
using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Serialization;

/// <summary>
///     UnmappedMemberHandling.Disallow and RespectRequiredMembers: the same results and errors in the direct and
///     tree-based paths, and nothing changes with the defaults.
/// </summary>
public class StrictMemberTests
{
    public sealed class Account
    {
        public required string Name { get; init; }
        public int Age { get; set; }
        public string Display => $"{Name} ({Age})";
        public List<Address>? Addresses { get; set; }
    }

    public sealed class Address
    {
        public required string City { get; set; }
        public string? Street { get; set; }
    }

    public sealed record Person(string Name, int Age)
    {
        public required string Email { get; init; }
    }

    public sealed class FromConstructor
    {
        [SetsRequiredMembers]
        public FromConstructor(string code) => Code = code;

        public required string Code { get; init; }
    }

    private static readonly ToonSerializerOptions Disallow = new() { UnmappedMemberHandling = ToonUnmappedMemberHandling.Disallow };
    private static readonly ToonSerializerOptions Required = new() { RespectRequiredMembers = true };
    private static readonly ToonSerializerOptions Both = new() { UnmappedMemberHandling = ToonUnmappedMemberHandling.Disallow, RespectRequiredMembers = true };

    private static T Read<T>(string toon, ToonSerializerOptions options)
    {
        var direct = ToonSerializer.Deserialize<T>(toon, options)!;
        var tree = ToonSerializer.DeserializeFromValue<T>(ToonDocument.Parse(toon).Root, options)!;

        Assert.Equal(ToonSerializer.Serialize(tree), ToonSerializer.Serialize(direct));
        return direct;
    }

    private static ToonSerializationException Fails<T>(string toon, ToonSerializerOptions options)
    {
        var direct = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<T>(toon, options));
        var tree = Assert.Throws<ToonSerializationException>(() => ToonSerializer.DeserializeFromValue<T>(ToonDocument.Parse(toon).Root, options));

        Assert.Equal(tree.Message, direct.Message);
        return direct;
    }

    [Fact]
    public void Defaults_SkipUnknownKeysAndMissingRequiredProperties()
    {
        var account = ToonSerializer.Deserialize<Account>("Age: 3\nUnknown: 1")!;

        Assert.Null(account.Name);
        Assert.Equal(3, account.Age);
    }

    [Fact]
    public void UnknownKeys_AreErrors_WithTheirPath()
    {
        var ex = Fails<Account>("Name: a\nNmae: b", Disallow);
        Assert.Equal("The key matches no property or constructor parameter (path: $.Nmae, target type: Account)", ex.Message);

        ex = Fails<Account>("Name: a\nAddresses[1]{City,Zip}:\n  x,1", Disallow);
        Assert.Equal("$.Addresses[0].Zip", ex.Path);

        Fails<Person>("Name: a\nAge: 1\nEmail: e\nPhone: 1", Disallow);
    }

    [Fact]
    public void KnownKeys_ReadOnlyProperties_AndConstructorParameters_AreAccepted()
    {
        // The serializer writes read-only properties (Display), so its output reads back
        var account = new Account { Name = "a", Age = 2, Addresses = [new Address { City = "c" }] };
        var text = ToonSerializer.Serialize(account);
        Assert.Contains("Display:", text);
        Assert.Equal(text, ToonSerializer.Serialize(Read<Account>(text, Both)));

        var person = Read<Person>("Name: a\nAge: 1\nEmail: e", Both);
        Assert.Equal(new Person("a", 1) { Email = "e" }, person);
    }

    [Fact]
    public void Disallow_FollowsNamingPolicyAndCaseInsensitiveReading()
    {
        var camel = new ToonSerializerOptions { UnmappedMemberHandling = ToonUnmappedMemberHandling.Disallow, PropertyNamingPolicy = PropertyNamingPolicy.CamelCase };
        Assert.Equal(2, Read<Account>("name: a\nage: 2", camel).Age);
        Fails<Account>("Name: a", camel);

        var lenient = new ToonSerializerOptions { UnmappedMemberHandling = ToonUnmappedMemberHandling.Disallow, PropertyNameCaseInsensitive = true };
        Assert.Equal(2, Read<Account>("NAME: a\nage: 2", lenient).Age);
    }

    [Fact]
    public void MissingRequiredProperties_AreErrors()
    {
        var ex = Fails<Account>("Age: 3", Required);
        Assert.Equal("The required property 'Name' is missing (path: $, target type: Account)", ex.Message);

        ex = Fails<Account>("Name: a\nAddresses[1]{Street}:\n  s", Required);
        Assert.Equal("$.Addresses[0]", ex.Path);

        Fails<Person>("Name: a\nAge: 1", Required);
        Assert.Equal("e", Read<Person>("Name: a\nAge: 1\nEmail: e", Required).Email);

        var camel = new ToonSerializerOptions { RespectRequiredMembers = true, PropertyNamingPolicy = PropertyNamingPolicy.SnakeCase };
        Assert.Contains("'name'", Fails<Account>("age: 1", camel).Message);
    }

    [Fact]
    public void RequiredProperties_SetByTheConstructor_AreNotChecked()
    {
        Assert.Equal("x", Read<FromConstructor>("code: x", new ToonSerializerOptions { RespectRequiredMembers = true, PropertyNameCaseInsensitive = true }).Code);
        Assert.Null(ToonSerializer.Deserialize<FromConstructor>("Other: 1", Required)!.Code);
    }

    [Fact]
    public void NullCountsAsPresent()
    {
        Assert.Null(Read<Account>("Name: null", Required).Name);
    }

    [Fact]
    public void UndefinedHandling_FailsValidation()
    {
        var options = new ToonSerializerOptions { UnmappedMemberHandling = (ToonUnmappedMemberHandling)7 };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        Assert.False(System.ComponentModel.DataAnnotations.Validator.TryValidateObject(options, new(options), results, validateAllProperties: true));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ToonSerializerOptions.UnmappedMemberHandling)));
    }
}
