using Microsoft.Extensions.Configuration;
using ToonNet.AspNetCore.Configuration;

namespace ToonNet.Tests.Configuration;

public class ToonConfigurationProviderTests
{
    [Fact]
    public void Load_ValidToon_ParsesConfiguration()
    {
        var toon = """
                   Logging:
                     LogLevel:
                       Default: Information
                       Microsoft: Warning
                   AllowedHosts: *
                   ConnectionStrings:
                     Default: "Server=.;Database=Db;"
                   FeatureToggles:
                     IsEnabled: true
                     Count: 42
                   """;

        // A relative path is found from the current directory
        var fileName = "test_config_" + Guid.NewGuid() + ".toon";
        File.WriteAllText(fileName, toon);

        try
        {
            var config = new ConfigurationBuilder().AddToonFile(fileName).Build();

            Assert.Equal("Information", config["Logging:LogLevel:Default"]);
            Assert.Equal("Warning", config["Logging:LogLevel:Microsoft"]);
            Assert.Equal("*", config["AllowedHosts"]);
            Assert.Equal("Server=.;Database=Db;", config["ConnectionStrings:Default"]);
            Assert.Equal("True", config["FeatureToggles:IsEnabled"]);
            Assert.Equal("42", config["FeatureToggles:Count"]);
        }
        finally
        {
            File.Delete(fileName);
        }
    }

    [Fact]
    public void Load_NestedArrays_ParsesCorrectly()
    {
        var toon = """
                   Endpoints[2]:
                     - 127.0.0.1
                     - localhost
                   Users[2]:
                     - Name: Alice
                       Role: Admin
                     - Name: Bob
                       Role: User
                   """;

        var fileName = "test_nested_" + Guid.NewGuid() + ".toon";
        File.WriteAllText(fileName, toon);

        try
        {
            var builder = new ConfigurationBuilder();
            builder.AddToonFile(fileName);
            var config = builder.Build();

            Assert.Equal("127.0.0.1", config["Endpoints:0"]);
            Assert.Equal("localhost", config["Endpoints:1"]);
            
            Assert.Equal("Alice", config["Users:0:Name"]);
            Assert.Equal("Admin", config["Users:0:Role"]);
            Assert.Equal("Bob", config["Users:1:Name"]);
            Assert.Equal("User", config["Users:1:Role"]);
        }
        finally
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
        }
    }

    [Fact]
    public void AddToonFile_AbsolutePath_IsFound()
    {
        var path = Path.Combine(Path.GetTempPath(), "toon_config_" + Guid.NewGuid() + ".toon");
        File.WriteAllText(path, "Name: absolute");

        try
        {
            var config = new ConfigurationBuilder().AddToonFile(path).Build();

            Assert.Equal("absolute", config["Name"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Numbers_KeepTheirExactValue_InCanonicalForm()
    {
        var path = Path.Combine(Path.GetTempPath(), "toon_config_" + Guid.NewGuid() + ".toon");
        File.WriteAllText(path, "Big: 12345678901234567890\nPrice: 1299.99\nScale: 1.50\nTiny: 0.000001");

        try
        {
            var config = new ConfigurationBuilder().AddToonFile(path).Build();

            Assert.Equal("12345678901234567890", config["Big"]);
            Assert.Equal(12345678901234567890UL, config.GetValue<ulong>("Big"));
            Assert.Equal("1299.99", config["Price"]);
            Assert.Equal("1.5", config["Scale"]); // canonical form (spec §2), same value
            Assert.Equal(0.000001m, config.GetValue<decimal>("Tiny"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static ToonConfigurationProvider LoadProvider(string toon)
    {
        var provider = new ToonConfigurationProvider(new ToonConfigurationSource());
        provider.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(toon)));
        return provider;
    }

    [Fact]
    public void Load_IllFormedUtf8_Throws()
    {
        var provider = new ToonConfigurationProvider(new ToonConfigurationSource());

        var ex = Assert.Throws<FormatException>(() => provider.Load(new MemoryStream([(byte)'a', (byte)':', (byte)' ', 0xFF])));

        Assert.Contains("not valid UTF-8", ex.Message);
    }

    [Theory]
    [InlineData("Port: 1\nport: 2")]
    [InlineData("A:\n  b: 1\na:\n  B: 2")]
    public void Load_KeysDifferingOnlyInCase_Throw(string toon)
    {
        var ex = Assert.Throws<FormatException>(() => LoadProvider(toon));

        Assert.Contains("duplicate key", ex.Message);
    }

    [Theory]
    [InlineData("[2]: 1,2")]
    [InlineData("hello")]
    public void Load_NonObjectRoot_Throws(string toon)
    {
        Assert.Throws<FormatException>(() => LoadProvider(toon));
    }

    private static string TempFile(string toon) => TempFile(System.Text.Encoding.UTF8.GetBytes(toon));

    private static string TempFile(byte[] content)
    {
        var path = Path.Combine(Path.GetTempPath(), "toon_config_" + Guid.NewGuid() + ".toon");
        File.WriteAllBytes(path, content);
        return path;
    }

    [Fact]
    public void Load_EmptyObjectsAndArrays_OverrideValuesFromEarlierSources_AsInJson()
    {
        var path = TempFile("Hosts: []\nFeature:\nOther: 1");

        try
        {
            var earlier = new Dictionary<string, string?> { ["Hosts:0"] = "a", ["Feature"] = "on" };
            var toon = new ConfigurationBuilder().AddInMemoryCollection(earlier).AddToonFile(path).Build();
            var json = new ConfigurationBuilder().AddInMemoryCollection(earlier)
                                                 .AddJsonStream(new MemoryStream("""{"Hosts":[],"Feature":{},"Other":1}"""u8.ToArray()))
                                                 .Build();

            // The later empty object replaces the value; children of other sources stay, as with JSON files (whose empty
            // arrays are null before .NET 10 and an empty string from it, so only the TOON value is checked for them)
            Assert.Null(toon["Feature"]);
            Assert.Equal(json["Feature"], toon["Feature"]);
            Assert.Null(toon["Hosts"]);
            Assert.Equal("a", toon["Hosts:0"]);
            Assert.Equal(json["Hosts:0"], toon["Hosts:0"]);
            Assert.Equal("1", toon["Other"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_NullValues_AreNull()
    {
        var provider = LoadProvider("A: null\nB: \"null\"\nC[2]: null,x");

        Assert.True(provider.TryGet("A", out var a));
        Assert.Null(a);
        Assert.True(provider.TryGet("B", out var b));
        Assert.Equal("null", b);
        Assert.True(provider.TryGet("C:0", out var c0));
        Assert.Null(c0);
    }

    [Fact]
    public void Build_MalformedFile_ThrowsWithTheFileError()
    {
        var path = TempFile("A: 1\n  B: [unclosed");

        try
        {
            var ex = Assert.ThrowsAny<Exception>(() => new ConfigurationBuilder().AddToonFile(path).Build());
            var format = ex as FormatException ?? Assert.IsType<FormatException>(ex.InnerException);
            Assert.StartsWith("Failed to parse TOON configuration file", format.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MissingFiles_ThrowUnlessOptional()
    {
        var path = Path.Combine(Path.GetTempPath(), "missing_" + Guid.NewGuid() + ".toon");

        Assert.Throws<FileNotFoundException>(() => new ConfigurationBuilder().AddToonFile(path).Build());
        Assert.Empty(new ConfigurationBuilder().AddToonFile(path, optional: true).Build().AsEnumerable());
    }

    [Fact]
    public void Reload_ReadsTheChangedFile()
    {
        var path = TempFile("Level: 1");

        try
        {
            var config = new ConfigurationBuilder().AddToonFile(path, optional: false, reloadOnChange: false).Build();
            Assert.Equal("1", config["Level"]);

            File.WriteAllText(path, "Level: 2\nAdded: yes");
            config.Reload();

            Assert.Equal("2", config["Level"]);
            Assert.Equal("yes", config["Added"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_Utf8ByteOrderMark_IsSkipped_AndUtf16ByteOrderMark_SelectsUtf16()
    {
        var provider = new ToonConfigurationProvider(new ToonConfigurationSource());

        provider.Load(new MemoryStream([0xEF, 0xBB, 0xBF, .. "Name: ü"u8]));
        Assert.True(provider.TryGet("Name", out var utf8));
        Assert.Equal("ü", utf8);

        // StreamReader detects the byte order mark of a file saved as UTF-16
        provider.Load(new MemoryStream([.. System.Text.Encoding.Unicode.GetPreamble(), .. System.Text.Encoding.Unicode.GetBytes("Name: ü")]));
        Assert.True(provider.TryGet("Name", out var utf16));
        Assert.Equal("ü", utf16);
    }

    [Fact]
    public void ToonOptions_AreUsedForParsing()
    {
        // Tab indentation is an error in strict mode only
        var strict = new ToonConfigurationProvider(new ToonConfigurationSource());
        Assert.Throws<FormatException>(() => strict.Load(new MemoryStream("A:\n\tB: 1"u8.ToArray())));

        var lenient = new ToonConfigurationProvider(new ToonConfigurationSource(), new ToonNet.Core.ToonOptions { StrictMode = false });
        lenient.Load(new MemoryStream("A:\n\tB: 1"u8.ToArray()));
        Assert.True(lenient.TryGet("A:B", out var b));
        Assert.Equal("1", b);
    }
}
