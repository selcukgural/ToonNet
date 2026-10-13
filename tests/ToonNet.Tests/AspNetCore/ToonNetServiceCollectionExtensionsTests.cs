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
    [Theory]
    [InlineData("tab", '\t')]
    [InlineData("\t", '\t')]
    [InlineData("Pipe", '|')]
    [InlineData(",", ',')]
    public void AddToonNet_WithConfiguration_BindsDelimiterNamesAndCharacters(string configured, char expected)
    {
        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(new Dictionary<string, string?> { ["ToonNet:ToonOptions:Delimiter"] = configured })
                            .Build();

        var services = new ServiceCollection();
        services.AddToonNet(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.Equal(expected, provider.GetRequiredService<IOptions<ToonOptions>>().Value.Delimiter);
    }

    [Fact]
    public void AddToonNet_WithConfiguration_BindsOptionsAndRegistersServices()
    {
        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(new Dictionary<string, string?>
                            {
                                ["ToonNet:ToonOptions:AllowExtendedLimits"] = "true",
                                ["ToonNet:ToonOptions:MaxDepth"] = "500",
                                ["ToonNet:ToonOptions:IndentSize"] = "4",
                                ["ToonNet:ToonOptions:Delimiter"] = "pipe",
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
        Assert.Equal('|', toonOptions.Delimiter);
        Assert.False(toonOptions.StrictMode);
        Assert.True(toonOptions.AllowExtendedLimits);

        var serializerOptions = provider.GetRequiredService<IOptions<ToonSerializerOptions>>().Value;
        Assert.True(serializerOptions.AllowExtendedLimits);
        Assert.Equal(500, serializerOptions.MaxDepth);
        Assert.True(serializerOptions.IgnoreNullValues);
        Assert.Equal(PropertyNamingPolicy.CamelCase, serializerOptions.PropertyNamingPolicy);
        Assert.True(ReferenceEquals(serializerOptions.ToonOptions, toonOptions));

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

    [Fact]
    public void AddToonNet_SerializerDelegateReplacingToonOptions_IsKept()
    {
        var services = new ServiceCollection();
        services.AddToonNet(toon => toon.IndentSize = 4, serializer => serializer.ToonOptions = new ToonOptions { Delimiter = '|' });

        using var provider = services.BuildServiceProvider();

        var serializerOptions = provider.GetRequiredService<IOptions<ToonSerializerOptions>>().Value;
        Assert.Equal('|', serializerOptions.ToonOptions.Delimiter);
        Assert.Equal(4, provider.GetRequiredService<IOptions<ToonOptions>>().Value.IndentSize);
    }

    [Fact]
    public void AddToonNet_SerializerDelegateChangingToonOptions_ChangesTheSharedOptions()
    {
        var services = new ServiceCollection();
        services.AddToonNet(configureToonOptions: null, serializer => serializer.ToonOptions.IndentSize = 4);

        using var provider = services.BuildServiceProvider();

        var serializerOptions = provider.GetRequiredService<IOptions<ToonSerializerOptions>>().Value;
        Assert.Equal(4, serializerOptions.ToonOptions.IndentSize);
        Assert.Same(provider.GetRequiredService<IOptions<ToonOptions>>().Value, serializerOptions.ToonOptions);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConfigureSerializerOptions_BeforeOrAfterAddToonNet_KeepsToonOptions(bool configureFirst)
    {
        var services = new ServiceCollection();

        if (configureFirst)
        {
            services.Configure<ToonSerializerOptions>(o => o.ToonOptions = new ToonOptions { StrictMode = false });
            services.AddToonNet();
        }
        else
        {
            services.AddToonNet();
            services.Configure<ToonSerializerOptions>(o => o.ToonOptions = new ToonOptions { StrictMode = false });
        }

        using var provider = services.BuildServiceProvider();

        Assert.False(provider.GetRequiredService<IOptions<ToonSerializerOptions>>().Value.ToonOptions.StrictMode);
    }

    [Fact]
    public void AddToonNet_CalledTwice_RegistersTheLinkOnce()
    {
        var services = new ServiceCollection();
        services.AddToonNet();
        services.AddToonNet();

        Assert.Single(services, d => d.ServiceType == typeof(IConfigureOptions<ToonSerializerOptions>) &&
                                     d.ImplementationType?.Name == "ToonOptionsLink");
    }
}
