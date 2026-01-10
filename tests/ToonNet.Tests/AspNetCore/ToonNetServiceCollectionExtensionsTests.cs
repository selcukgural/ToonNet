using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ToonNet.AspNetCore.DependencyInjection;
using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Parsing;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.AspNetCore;

/// <summary>
///     Tests for ASP.NET Core dependency injection integration.
/// </summary>
public sealed class ToonNetServiceCollectionExtensionsTests
{
    /// <summary>
    ///     Ensures options are bound from configuration and core services are registered.
    /// </summary>
    [Fact]
    public void AddToonNet_WithConfiguration_BindsOptionsAndRegistersServices()
    {
        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(new Dictionary<string, string?>
                            {
                                ["ToonNet:ToonOptions:AllowExtendedLimits"] = "true",
                                ["ToonNet:ToonOptions:MaxDepth"] = "500",
                                ["ToonNet:ToonOptions:IndentSize"] = "4",
                                ["ToonNet:ToonOptions:Delimiter"] = ";",
                                ["ToonNet:ToonOptions:StrictMode"] = "false",

                                ["ToonNet:ToonSerializerOptions:AllowExtendedLimits"] = "true",
                                ["ToonNet:ToonSerializerOptions:MaxDepth"] = "500",
                                ["ToonNet:ToonSerializerOptions:IgnoreNullValues"] = "true",
                                ["ToonNet:ToonSerializerOptions:PropertyNamingPolicy"] = "CamelCase"
                            })
                            .Build();

        var services = new ServiceCollection();
        services.AddToonNet(configuration);

        using var provider = services.BuildServiceProvider();

        var toonOptions = provider.GetRequiredService<IOptions<ToonOptions>>().Value;
        Assert.Equal(4, toonOptions.IndentSize);
        Assert.Equal(500, toonOptions.MaxDepth);
        Assert.Equal(';', toonOptions.Delimiter);
        Assert.False(toonOptions.StrictMode);
        Assert.True(toonOptions.AllowExtendedLimits);

        var serializerOptions = provider.GetRequiredService<IOptions<ToonSerializerOptions>>().Value;
        Assert.True(serializerOptions.AllowExtendedLimits);
        Assert.Equal(500, serializerOptions.MaxDepth);
        Assert.True(serializerOptions.IgnoreNullValues);
        Assert.Equal(PropertyNamingPolicy.CamelCase, serializerOptions.PropertyNamingPolicy);
        Assert.True(ReferenceEquals(serializerOptions.ToonOptions, toonOptions));

        Assert.NotNull(provider.GetRequiredService<ToonParser>());
        Assert.NotNull(provider.GetRequiredService<ToonEncoder>());
    }

    /// <summary>
    ///     Ensures invalid configuration fails fast when options are accessed.
    /// </summary>
    [Fact]
    public void AddToonNet_WithInvalidIndentSize_Throws()
    {
        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(new Dictionary<string, string?>
                            {
                                ["ToonNet:ToonOptions:IndentSize"] = "3" // Must be even per TOON specification.
                            })
                            .Build();

        var services = new ServiceCollection();
        services.AddToonNet(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetRequiredService<IOptions<ToonOptions>>().Value);
    }
}
