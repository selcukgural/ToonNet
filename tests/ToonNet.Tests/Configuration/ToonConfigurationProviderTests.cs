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

        var path = Path.GetTempFileName();
        File.WriteAllText(path, toon);

        try
        {
            var builder = new ConfigurationBuilder();
            // Use absolute path logic by implementing a custom FileProvider or setting BasePath to root
            // FileConfigurationProvider combines BasePath + Path.
            // If path is absolute, standard FileProvider handles it? No, PhysicalFileProvider is scoped to root.
            
            // Simpler fix for test: Set provider to null (default) but ensure we handle absolute paths correctly 
            // OR simpler: write to current directory
            
            var fileName = "test_config_" + Guid.NewGuid() + ".toon";
            File.WriteAllText(fileName, toon);
            
            try 
            {
                builder.AddToonFile(fileName);
                var config = builder.Build();

                Assert.Equal("Information", config["Logging:LogLevel:Default"]);
                Assert.Equal("Warning", config["Logging:LogLevel:Microsoft"]);
                Assert.Equal("*", config["AllowedHosts"]);
                Assert.Equal("Server=.;Database=Db;", config["ConnectionStrings:Default"]);
                Assert.Equal("True", config["FeatureToggles:IsEnabled"]);
                Assert.Equal("42", config["FeatureToggles:Count"]);
            }
            finally
            {
                if (File.Exists(fileName)) File.Delete(fileName);
            }
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
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
}
