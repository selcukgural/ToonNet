using ToonNet.Core.Encoding;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.Tests.Security;

/// <summary>
///     Objects are written as the type they are declared as, so members of a derived type (a password hash) do not reach
///     a response through a narrower type: on every path, the tables of the text writer included.
/// </summary>
public class DeclaredTypeWritingTests
{
    public class Summary
    {
        public string Name { get; set; } = "n";
    }

    public class Entity : Summary
    {
        public string PasswordHash { get; set; } = "SECRET";
    }

    public class Order
    {
        public int Id { get; set; }
        public Summary? Owner { get; set; }
    }

    public class OrderWithMap
    {
        public int Id { get; set; }
        public Dictionary<string, Summary> People { get; set; } = [];
    }

    public interface IAccount
    {
        string Name { get; }
        string PasswordHash { get; }
    }

    public class Account : IAccount
    {
        public string Name { get; set; } = "n";

        [ToonIgnore]
        public string PasswordHash { get; set; } = "SECRET";
    }

    public class ExplicitAccount : IAccount
    {
        public string Name { get; set; } = "n";

        [ToonIgnore]
        string IAccount.PasswordHash => "SECRET";
    }

    public class OpenAccount : IAccount
    {
        public string Name { get; set; } = "n";
        public string PasswordHash { get; set; } = "hash";
    }

    public class AccountHolder
    {
        public IAccount? Account { get; set; }
        public List<IAccount>? Accounts { get; set; }
    }

    /// <summary>The text of each writing path: direct text, UTF-8, and the tree.</summary>
    private static void AssertWritten<T>(string expected, T value, ToonSerializerOptions? options = null)
    {
        options ??= new ToonSerializerOptions();
        Assert.Equal(expected, ToonSerializer.Serialize(value, options));
        Assert.Equal(expected, System.Text.Encoding.UTF8.GetString(ToonSerializer.SerializeToUtf8Bytes(value, options)));
        Assert.Equal(expected, new ToonEncoder(options.ToonOptions).Encode(new Core.Models.ToonDocument(ToonSerializer.SerializeToValue(value, options))));
    }

    [Fact]
    public void TableRows_WriteObjectPropertiesAsTheirDeclaredType()
    {
        List<Order> orders = [new() { Id = 1, Owner = new Entity() }, new() { Id = 2, Owner = new Entity { Name = "m" } }];

        AssertWritten("[2]{Id,Owner{Name}}:\n  1,n\n  2,m", orders);
        AssertWritten("[2]{Id,Owner{Name,PasswordHash}}:\n  1,n,SECRET\n  2,m,SECRET", orders, new ToonSerializerOptions { SerializeRuntimeTypes = true });
    }

    [Fact]
    public void TableRows_WriteDictionaryValuesAsTheDictionarysValueType()
    {
        List<Dictionary<string, Summary>> rows = [new() { ["a"] = new Entity() }, new() { ["a"] = new Entity { Name = "m" } }];
        AssertWritten("[2]{a{Name}}:\n  n\n  m", rows);

        var nested = new Dictionary<string, Dictionary<string, Summary>> { ["x"] = new() { ["a"] = new Entity() }, ["y"] = new() { ["a"] = new Entity() } };
        AssertWritten("[2:]{a{Name}}:\n  x: n\n  y: n", nested);

        List<OrderWithMap> orders = [new() { Id = 1, People = new() { ["a"] = new Entity() } }, new() { Id = 2, People = new() { ["a"] = new Entity() } }];
        AssertWritten("[2]{Id,People{a{Name}}}:\n  1,n\n  2,n", orders);

        // Values declared as object are written as what they are
        List<Dictionary<string, object>> objects = [new() { ["a"] = new Entity() }, new() { ["a"] = new Entity() }];
        AssertWritten("[2]{a{Name,PasswordHash}}:\n  n,SECRET\n  n,SECRET", objects);
    }

    [Fact]
    public void ValuesWrittenAsAnInterface_LeaveOutWhatTheirClassIgnores()
    {
        // The interface has PasswordHash; the class that implements it marks it [ToonIgnore]
        AssertWritten("Account:\n  Name: n\nAccounts: null", new AccountHolder { Account = new Account() });
        AssertWritten("Account: null\nAccounts[2]{Name}:\n  n\n  n", new AccountHolder { Accounts = [new Account(), new ExplicitAccount()] });
        AssertWritten("Name: n", (IAccount)new Account());
        AssertWritten("Name: n", (IAccount)new ExplicitAccount());

        // A class that does not ignore it writes it, as declared by the interface
        AssertWritten("Account:\n  Name: n\n  PasswordHash: hash\nAccounts: null", new AccountHolder { Account = new OpenAccount() });

        // Objects of the classes in one list are written each with their own view
        AssertWritten("Account: null\nAccounts[2]:\n  - Name: n\n  - Name: n\n    PasswordHash: hash", new AccountHolder { Accounts = [new Account(), new OpenAccount()] });
    }
}
