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
                                ["ToonNet:ToonSerializerOptions:PropertyNamingPolicy"] = "CamelCase",
                                ["ToonNet:ToonSerializerOptions:PropertyNameCaseInsensitive"] = "true",
                                ["ToonNet:ToonSerializerOptions:RespectRequiredMembers"] = "true",
                                ["ToonNet:ToonSerializerOptions:UnmappedMemberHandling"] = "disallow"
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
        Assert.True(serializerOptions.PropertyNameCaseInsensitive);
        Assert.True(serializerOptions.RespectRequiredMembers);
        Assert.Equal(ToonUnmappedMemberHandling.Disallow, serializerOptions.UnmappedMemberHandling);
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
                                ["ToonNet:ToonOptions:IndentSize"] = "0" // Must be a positive number of spaces.
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

    [Fact]
    public void AddToonNet_CalledTwice_RegistersServicesAndValidationOnce()
    {
        var services = new ServiceCollection();
        services.AddToonNet();
        services.AddToonNet(o => o.MaxDepth = 7);

        Assert.Single(services, d => d.ServiceType == typeof(ToonEncoder));
        Assert.Single(services, d => d.ServiceType == typeof(IValidateOptions<ToonOptions>));
        Assert.Single(services, d => d.ServiceType == typeof(IValidateOptions<ToonSerializerOptions>));

        using var provider = services.BuildServiceProvider();
        Assert.Equal(7, provider.GetRequiredService<IOptions<ToonSerializerOptions>>().Value.MaxDepth);
    }

    [Fact]
    public void AddToonNet_WithSerializerDelegate_ValidatesTheOptions()
    {
        var services = new ServiceCollection();
        services.AddToonNet(o => o.MaxDepth = 500); // above 200 without AllowExtendedLimits

        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<ToonSerializerOptions>>().Value);
    }

    [Theory]
    [InlineData("SnakeCas")]
    [InlineData("7")]
    [InlineData("1")]                     // a number of a defined policy
    [InlineData("CamelCase,SnakeCase")]   // Enum.TryParse combines the list into another policy
    public void AddToonNet_WithMisspelledNamingPolicy_Throws(string policy)
    {
        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(new Dictionary<string, string?> { ["ToonNet:ToonSerializerOptions:PropertyNamingPolicy"] = policy })
                            .Build();

        var services = new ServiceCollection();
        services.AddToonNet(configuration);
        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IOptions<ToonSerializerOptions>>().Value);
        Assert.Contains("PropertyNamingPolicy", ex.Message);
    }

    [Theory]
    [InlineData(" camelCase ", "PropertyNamingPolicy", "CamelCase")]
    [InlineData("disallow", "UnmappedMemberHandling", "Disallow")]
    public void AddToonNet_WithConfiguration_ReadsEnumNamesInAnyCase(string configured, string key, string expected)
    {
        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(new Dictionary<string, string?> { [$"ToonNet:ToonSerializerOptions:{key}"] = configured })
                            .Build();

        var services = new ServiceCollection();
        services.AddToonNet(configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ToonSerializerOptions>>().Value;

        Assert.Equal(expected, key == "PropertyNamingPolicy" ? options.PropertyNamingPolicy.ToString() : options.UnmappedMemberHandling.ToString());
    }

    [Theory]
    [InlineData("Skip,Disallow")]
    [InlineData("1")]
    public void AddToonNet_WithAListOrNumberForUnmappedMembers_Throws(string configured)
    {
        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(new Dictionary<string, string?> { ["ToonNet:ToonSerializerOptions:UnmappedMemberHandling"] = configured })
                            .Build();

        var services = new ServiceCollection();
        services.AddToonNet(configuration);
        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IOptions<ToonSerializerOptions>>().Value);
        Assert.Equal($"Invalid ToonNet:ToonSerializerOptions:UnmappedMemberHandling value '{configured}'. Expected one of: Skip, Disallow.", ex.Message);
    }

    [Theory]
    [InlineData("pipes")]
    [InlineData(";")]
    [InlineData(",|")]
    public void AddToonNet_WithUnknownDelimiter_Throws(string delimiter)
    {
        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(new Dictionary<string, string?> { ["ToonNet:ToonOptions:Delimiter"] = delimiter })
                            .Build();

        var services = new ServiceCollection();
        services.AddToonNet(configuration);
        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IOptions<ToonOptions>>().Value);
        Assert.Contains("ToonNet:ToonOptions:Delimiter", ex.Message);
        Assert.Contains($"'{delimiter}'", ex.Message);
    }

    [Fact]
    public void AddToonNet_WithConfiguration_BindsSerializeRuntimeTypes_AndRequiredMembers()
    {
        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(new Dictionary<string, string?>
                            {
                                ["ToonNet:ToonSerializerOptions:SerializeRuntimeTypes"] = "true",
                                ["ToonNet:ToonSerializerOptions:RespectRequiredMembers"] = "false"
                            })
                            .Build();

        var services = new ServiceCollection();
        services.AddToonNet(configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ToonSerializerOptions>>().Value;

        Assert.True(options.SerializeRuntimeTypes);
        Assert.False(options.RespectRequiredMembers);
        Assert.True(new ToonSerializerOptions().RespectRequiredMembers);
    }

    [Fact]
    public void AddToonNet_WithConfiguration_BindsIncludeReadOnlyProperties()
    {
        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(new Dictionary<string, string?> { ["ToonNet:ToonSerializerOptions:IncludeReadOnlyProperties"] = "false" })
                            .Build();

        var services = new ServiceCollection();
        services.AddToonNet(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.True(new ToonSerializerOptions().IncludeReadOnlyProperties);
        Assert.False(provider.GetRequiredService<IOptions<ToonSerializerOptions>>().Value.IncludeReadOnlyProperties);
        Assert.False(provider.GetRequiredService<IOptions<ToonSerializerOptions>>().Value.SerializeRuntimeTypes);
    }

    [Fact]
    public void AddToonNet_WithoutTheSection_KeepsTheDefaults()
    {
        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(new Dictionary<string, string?> { ["Other:MaxDepth"] = "3" })
                            .Build();

        var services = new ServiceCollection();
        services.AddToonNet(configuration);
        using var provider = services.BuildServiceProvider();

        var toonOptions = provider.GetRequiredService<IOptions<ToonOptions>>().Value;
        var serializerOptions = provider.GetRequiredService<IOptions<ToonSerializerOptions>>().Value;
        var defaults = new ToonOptions();
        var serializerDefaults = new ToonSerializerOptions();

        Assert.Equal((defaults.Delimiter, defaults.IndentSize, defaults.MaxDepth, defaults.StrictMode),
                     (toonOptions.Delimiter, toonOptions.IndentSize, toonOptions.MaxDepth, toonOptions.StrictMode));
        Assert.Equal((serializerDefaults.MaxDepth, serializerDefaults.IgnoreNullValues, serializerDefaults.PropertyNamingPolicy),
                     (serializerOptions.MaxDepth, serializerOptions.IgnoreNullValues, serializerOptions.PropertyNamingPolicy));
    }
}
