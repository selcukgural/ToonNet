 using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Parsing;
using ToonNet.Core.Serialization;

namespace ToonNet.AspNetCore.DependencyInjection;

/// <summary>
/// Provides extension methods for registering ToonNet services with ASP.NET Core dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Represents the default configuration section name used for binding ToonNet options.
    /// </summary>
    private const string DefaultSectionName = "ToonNet";

    /// <summary>
    /// Registers ToonNet services and binds options from configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="sectionName">The root section name. Default is <c>ToonNet</c>.</param>
    /// <returns>The same service collection to allow chaining.</returns>
    public static IServiceCollection AddToonNet(this IServiceCollection services, IConfiguration configuration,
                                                string sectionName = DefaultSectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var rootSection = configuration.GetSection(sectionName);
        var toonOptionsSection = rootSection.GetSection(nameof(ToonOptions));
        var serializerOptionsSection = rootSection.GetSection(nameof(ToonSerializerOptions));

        services.AddOptions<ToonOptions>()
                .Configure(options => ApplyToonOptionsFromConfiguration(options, toonOptionsSection))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddOptions<ToonSerializerOptions>()
                .Configure(options => ApplyToonSerializerOptionsFromConfiguration(options, serializerOptionsSection))
                .Configure<IOptions<ToonOptions>>((options, toonOptions) => options.ToonOptions = toonOptions.Value)
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddSingleton(sp => new ToonParser(sp.GetRequiredService<IOptions<ToonOptions>>().Value));
        services.AddSingleton(sp => new ToonEncoder(sp.GetRequiredService<IOptions<ToonOptions>>().Value));

        return services;
    }

    /// <summary>
    /// Registers ToonNet services and binds default options from configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection to allow chaining.</returns>
    public static IServiceCollection AddToonNet(this IServiceCollection services, Action<ToonOptions>? configureToonOptions,
                                                Action<ToonSerializerOptions>? configureSerializerOptions)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ToonOptions>().Configure(options => configureToonOptions?.Invoke(options)).ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddOptions<ToonSerializerOptions>()
                .Configure(options => configureSerializerOptions?.Invoke(options))
                .Configure<IOptions<ToonOptions>>((options, toonOptions) => options.ToonOptions = toonOptions.Value)
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddSingleton(sp => new ToonParser(sp.GetRequiredService<IOptions<ToonOptions>>().Value));
        services.AddSingleton(sp => new ToonEncoder(sp.GetRequiredService<IOptions<ToonOptions>>().Value));

        return services;
    }

    /// <summary>
    /// Registers ToonNet services with default configurations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection to allow chaining.</returns>
    public static IServiceCollection AddToonNet(this IServiceCollection services)
    {
        return services.AddToonNet(configureToonOptions: null, configureSerializerOptions: null);
    }

    /// <summary>
    /// Configures the provided <c>ToonOptions</c> instance with values from the specified configuration section.
    /// </summary>
    /// <param name="options">The <c>ToonOptions</c> instance to configure.</param>
    /// <param name="section">The configuration section containing the values.</param>
    private static void ApplyToonOptionsFromConfiguration(ToonOptions options, IConfigurationSection section)
    {
        if (!section.Exists())
        {
            return;
        }

        var allowExtendedLimits = section.GetValue<bool?>(nameof(ToonOptions.AllowExtendedLimits));
        if (allowExtendedLimits.HasValue)
        {
            options.AllowExtendedLimits = allowExtendedLimits.Value;
        }

        var indentSize = section.GetValue<int?>(nameof(ToonOptions.IndentSize));
        if (indentSize.HasValue)
        {
            options.IndentSize = indentSize.Value;
        }

        var delimiterString = section.GetValue<string?>(nameof(ToonOptions.Delimiter));
        if (!string.IsNullOrWhiteSpace(delimiterString))
        {
            options.Delimiter = delimiterString[0];
        }

        var strictMode = section.GetValue<bool?>(nameof(ToonOptions.StrictMode));
        if (strictMode.HasValue)
        {
            options.StrictMode = strictMode.Value;
        }

        var maxDepth = section.GetValue<int?>(nameof(ToonOptions.MaxDepth));
        if (maxDepth.HasValue)
        {
            options.MaxDepth = maxDepth.Value;
        }
    }

    /// <summary>
    /// Configures the specified <c>ToonSerializerOptions</c> instance with values from the provided configuration section.
    /// </summary>
    /// <param name="options">The <c>ToonSerializerOptions</c> instance to configure.</param>
    /// <param name="section">The configuration section containing serializer option values.</param>
    private static void ApplyToonSerializerOptionsFromConfiguration(ToonSerializerOptions options, IConfigurationSection section)
    {
        if (!section.Exists())
        {
            return;
        }

        var allowExtendedLimits = section.GetValue<bool?>(nameof(ToonSerializerOptions.AllowExtendedLimits));
        if (allowExtendedLimits.HasValue)
        {
            options.AllowExtendedLimits = allowExtendedLimits.Value;
        }

        var ignoreNullValues = section.GetValue<bool?>(nameof(ToonSerializerOptions.IgnoreNullValues));
        if (ignoreNullValues.HasValue)
        {
            options.IgnoreNullValues = ignoreNullValues.Value;
        }

        var includeTypeInformation = section.GetValue<bool?>(nameof(ToonSerializerOptions.IncludeTypeInformation));
        if (includeTypeInformation.HasValue)
        {
            options.IncludeTypeInformation = includeTypeInformation.Value;
        }

        var publicOnly = section.GetValue<bool?>(nameof(ToonSerializerOptions.PublicOnly));
        if (publicOnly.HasValue)
        {
            options.PublicOnly = publicOnly.Value;
        }

        var includeReadOnlyProperties = section.GetValue<bool?>(nameof(ToonSerializerOptions.IncludeReadOnlyProperties));
        if (includeReadOnlyProperties.HasValue)
        {
            options.IncludeReadOnlyProperties = includeReadOnlyProperties.Value;
        }

        var maxDepth = section.GetValue<int?>(nameof(ToonSerializerOptions.MaxDepth));
        if (maxDepth.HasValue)
        {
            options.MaxDepth = maxDepth.Value;
        }

        var namingPolicy = section.GetValue<string?>(nameof(ToonSerializerOptions.PropertyNamingPolicy));
        if (!string.IsNullOrWhiteSpace(namingPolicy) && Enum.TryParse<PropertyNamingPolicy>(namingPolicy, ignoreCase: true, out var policy))
        {
            options.PropertyNamingPolicy = policy;
        }
    }
}
