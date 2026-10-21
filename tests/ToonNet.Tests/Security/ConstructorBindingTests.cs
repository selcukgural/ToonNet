using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.Tests.Security;

/// <summary>
///     Constructor parameters are read from documents only through the properties they match, as in System.Text.Json:
///     not when their member is kept out of documents, and not as values of their own.
/// </summary>
public class ConstructorBindingTests
{
    public sealed record User(string Name, [property: ToonIgnore] bool IsAdmin = false);

    public sealed class InternalRole(string name, bool isAdmin)
    {
        public string Name { get; } = name;

        internal bool IsAdmin { get; } = isAdmin;
    }

    public sealed class FieldRole
    {
        private readonly bool isAdmin;

        public FieldRole(string name, bool isAdmin)
        {
            Name = name;
            this.isAdmin = isAdmin;
        }

        public string Name { get; }

        public bool Admin() => isAdmin;
    }

    public sealed class Login(string user, string pass)
    {
        public string User { get; } = user;

        public string Secret => pass;
    }

    public sealed class MarkedLogin
    {
        [ToonConstructor]
        public MarkedLogin(string user, string pass)
        {
            User = user;
            Pass = pass;
        }

        public string User { get; }

        [ToonIgnore]
        public string Pass { get; }
    }

    public sealed class OwnValue
    {
        [ToonConstructor]
        public OwnValue(string user, string pass)
        {
            User = user;
            Pass = pass;
        }

        public string User { get; }

        public string Read() => Pass;

        private string Pass { get; }
    }

    public sealed class Marked
    {
        [ToonConstructor]
        public Marked(int amount, string currency)
        {
            Amount = amount;
            Text = $"{amount} {currency}";
        }

        public int Amount { get; }

        public string Text { get; }
    }

    public sealed class Attachment
    {
        public string Name { get; set; } = "";
        public FileInfo? File { get; set; }
    }

    public sealed class Upload
    {
        public Stream? Content { get; set; }
    }

    public sealed class Callback
    {
        public Action? OnDone { get; set; }
    }

    public sealed class Described
    {
        public Type? Kind { get; set; }
    }

    private static T? Read<T>(string toon, ToonSerializerOptions? options = null)
    {
        // The text reader and the tree reader agree
        var direct = ToonSerializer.Deserialize<T>(toon, options);
        var tree = ToonSerializer.DeserializeFromValue<T>(ToonDocument.Parse(toon).Root, options);
        Assert.Equivalent(direct, tree, strict: true);
        return direct;
    }

    [Fact]
    public void ParametersOfIgnoredProperties_AreNotRead()
    {
        Assert.Equal(new User("a"), Read<User>("Name: a\nIsAdmin: true"));
        Assert.Equal(new User("a"), Read<User>("Name: a\nisadmin: true", new ToonSerializerOptions { PropertyNameCaseInsensitive = true }));

        // Their key is not mapped
        var strict = new ToonSerializerOptions { UnmappedMemberHandling = ToonUnmappedMemberHandling.Disallow };
        var ex = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<User>("Name: a\nIsAdmin: true", strict));
        Assert.Contains("IsAdmin", ex.Message);
        Assert.Throws<ToonSerializationException>(() => ToonSerializer.DeserializeFromValue<User>(ToonDocument.Parse("Name: a\nIsAdmin: true").Root, strict));
    }

    [Fact]
    public void ParametersOfNonPublicMembers_AreNotRead()
    {
        Assert.False(Read<InternalRole>("Name: a\nisAdmin: true")!.IsAdmin);
        Assert.False(Read<FieldRole>("Name: a\nisAdmin: true")!.Admin());
        Assert.Equal("a", Read<FieldRole>("Name: a\nisAdmin: true")!.Name);

        // Even with [ToonConstructor]: the member's own rules decide
        Assert.Null(Read<OwnValue>("User: u\npass: p")!.Read());
        Assert.Null(Read<MarkedLogin>("User: u\npass: p")!.Pass);
    }

    [Fact]
    public void ParametersWithoutAMember_AreAnError_UnlessTheConstructorIsMarked()
    {
        const string message = "The parameter 'pass' of the constructor of Login matches no public property. " +
                               "Add a property with its name, or mark the constructor [ToonConstructor] to read the parameter from the key 'pass'.";

        Assert.Equal(message, Assert.Throws<InvalidOperationException>(() => ToonSerializer.Deserialize<Login>("User: u\npass: p")).Message);
        Assert.Equal(message, Assert.Throws<InvalidOperationException>(() => ToonSerializer.DeserializeFromValue<Login>(ToonDocument.Parse("User: u").Root)).Message);

        // Writing is not affected
        Assert.Equal("User: u\nSecret: p", ToonSerializer.Serialize(new Login("u", "p")));

        // A marked constructor reads it from its own key, as before
        var marked = Read<Marked>("Amount: 5\ncurrency: EUR")!;
        Assert.Equal("5 EUR", marked.Text);
    }

    [Fact]
    public void FileSystemEntries_AreNotCreatedFromDocuments()
    {
        // A FileInfo built from a document set the attributes and times of the file it named
        var path = Path.GetTempFileName();

        try
        {
            File.SetLastWriteTimeUtc(path, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var toon = $"Name: x\nFile:\n  fileName: \"{path.Replace("\\", "\\\\")}\"\n  IsReadOnly: true\n  LastWriteTimeUtc: 1999-01-01T00:00:00Z";

            var ex = Assert.Throws<NotSupportedException>(() => ToonSerializer.Deserialize<Attachment>(toon));
            Assert.Equal("Serializing and deserializing System.IO.FileInfo is not supported: it is a reflection, delegate, stream, file system, process or handle type. Register a converter to write it as a value.", ex.Message);
            Assert.Throws<NotSupportedException>(() => ToonSerializer.DeserializeFromValue<Attachment>(ToonDocument.Parse(toon).Root));

            Assert.False(new FileInfo(path).IsReadOnly);
            Assert.Equal(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), File.GetLastWriteTimeUtc(path));
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }
    }

    [Fact]
    public void StreamsDelegatesAndReflectionTypes_AreNotSupported()
    {
        // A MemoryStream with a capacity from the document reserved it; a FileStream could truncate a file
        Assert.Throws<NotSupportedException>(() => ToonSerializer.Deserialize<Upload>("Content:\n  Capacity: 1500000000"));
        Assert.Throws<NotSupportedException>(() => ToonSerializer.Deserialize<Callback>("OnDone:\n  Method: x"));
        Assert.Throws<NotSupportedException>(() => ToonSerializer.Deserialize<Described>("Kind:\n  Name: x"));
        Assert.Throws<NotSupportedException>(() => ToonSerializer.Deserialize<System.Diagnostics.ProcessStartInfo>("FileName: /bin/sh"));

        // Writing them walked reflection graphs until a depth or cycle error
        Assert.Throws<NotSupportedException>(() => ToonSerializer.Serialize(new Described { Kind = typeof(int) }));
        Assert.Throws<NotSupportedException>(() => ToonSerializer.Serialize(new Upload { Content = new MemoryStream() }));
        Assert.Throws<NotSupportedException>(() => ToonSerializer.Serialize(new Callback { OnDone = () => { } }));

        // Null values and ignored properties do not matter, and a converter of the options still writes them
        Assert.Equal("Kind: null", ToonSerializer.Serialize(new Described()));
        var options = new ToonSerializerOptions();
        options.Converters.Add(new TypeNameConverter());
        Assert.Equal("Kind: System.Int32", ToonSerializer.Serialize(new Described { Kind = typeof(int) }, options));
    }

    private sealed class TypeNameConverter : ToonConverter<Type>
    {
        public override ToonValue? Write(Type? value, ToonSerializerOptions options) => value == null ? ToonNull.Instance : new ToonString(value.FullName!);

        public override Type? Read(ToonValue value, ToonSerializerOptions options) => null;
    }
    public sealed class Contact
    {
        private string _email = "";

        public string Name { get; set; } = "";

        public string Email
        {
            get => _email;
            set => _email = value.Contains('@') ? value : throw new ArgumentException($"Invalid email '{value}' for tenant db=prod-01");
        }

        private int _age;

        public int Age
        {
            get => _age;
            set => _age = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "secret range");
        }
    }

    [Fact]
    public void SetterExceptions_AreReportedWithoutTheirMessage()
    {
        // The message of a setter's check (the value, server names) reached logs and clients, and was a server error
        foreach (var toon in new[] { "Name: a\nEmail: alice-ssn-123", "Name: a\nAge: -5" })
        {
            var property = toon.Contains("Email") ? "Email" : "Age";
            var expected = $"The setter of {property} threw {(property == "Email" ? "ArgumentException" : "ArgumentOutOfRangeException")} (path: $.{property}, target type: Contact)";

            var direct = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Contact>(toon));
            var tree = Assert.Throws<ToonSerializationException>(() => ToonSerializer.DeserializeFromValue<Contact>(ToonDocument.Parse(toon).Root));

            foreach (var ex in new[] { direct, tree })
            {
                Assert.Equal(expected, ex.Message);
                Assert.IsAssignableFrom<ArgumentException>(ex.InnerException);
                Assert.Equal("$." + property, ex.Path);
            }
        }

        // Values set through the general path (converters in the options) too
        var options = new ToonSerializerOptions();
        options.Converters.Add(new TypeNameConverter());
        Assert.Equal("The setter of Email threw ArgumentException (path: $.Email, target type: Contact)",
                     Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Contact>("Email: x", options)).Message);

        // Nested, the path leads to it
        var list = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<List<Contact>>("[1]:\n  - Name: a\n    Email: x"));
        Assert.Equal("The setter of Email threw ArgumentException (path: $[0].Email, target type: Contact)", list.Message);
    }
}
