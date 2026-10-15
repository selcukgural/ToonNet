 using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ToonNet.Core;
using ToonNet.Core.Encoding;
using ToonNet.Core.Serialization;

namespace ToonNet.AspNetCore.DependencyInjection;

/// <summary>
/// Provides extension methods for registering ToonNet services with ASP.NET Core dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Represents the default section name used for binding configuration options related to ToonNet.
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

        services.AddOptions<ToonOptions>().Configure(options => ApplyToonOptionsFromConfiguration(options, toonOptionsSection));
        services.AddOptions<ToonSerializerOptions>().Configure(options => ApplyToonSerializerOptionsFromConfiguration(options, serializerOptionsSection));

        return AddServices(services);
    }

    /// <summary>
    /// Registers ToonNet services and binds options from configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureToonOptions">An optional action to configure Toon options.</param>
    /// <param name="configureSerializerOptions">An optional action to configure serialization options.</param>
    /// <returns>The same service collection to allow chaining.</returns>
    public static IServiceCollection AddToonNet(this IServiceCollection services, Action<ToonOptions>? configureToonOptions,
                                                Action<ToonSerializerOptions>? configureSerializerOptions)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configureToonOptions != null)
        {
            services.AddOptions<ToonOptions>().Configure(configureToonOptions);
        }

        if (configureSerializerOptions != null)
        {
            services.AddOptions<ToonSerializerOptions>().Configure(configureSerializerOptions);
        }

        return AddServices(services);
    }

    /// <summary>
    /// Registers ToonNet services and configures the serializer options, for example
    /// <c>services.AddToonNet(o =&gt; o.PropertyNamingPolicy = PropertyNamingPolicy.CamelCase)</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureSerializerOptions">An action to configure serialization options.</param>
    /// <returns>The same service collection to allow chaining.</returns>
    public static IServiceCollection AddToonNet(this IServiceCollection services, Action<ToonSerializerOptions> configureSerializerOptions)
    {
        ArgumentNullException.ThrowIfNull(configureSerializerOptions);
        return services.AddToonNet(configureToonOptions: null, configureSerializerOptions);
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
    /// Registers the services and option validation once, however often <c>AddToonNet</c> is called.
    /// </summary>
    private static IServiceCollection AddServices(IServiceCollection services)
    {
        // The options check themselves (IValidatableObject; their setters check ranges), so no reflection over
        // data annotations is needed, which trimming and NativeAOT cannot follow
        if (!services.Any(d => d.ServiceType == typeof(IValidateOptions<ToonOptions>)))
        {
            services.AddSingleton<IValidateOptions<ToonOptions>, SelfValidatingOptions<ToonOptions>>();
            services.AddOptions<ToonOptions>().ValidateOnStart();
        }

        if (!services.Any(d => d.ServiceType == typeof(IValidateOptions<ToonSerializerOptions>)))
        {
            services.AddSingleton<IValidateOptions<ToonSerializerOptions>, SelfValidatingOptions<ToonSerializerOptions>>();
            services.AddOptions<ToonSerializerOptions>().ValidateOnStart();
        }

        LinkToonOptions(services);
        services.TryAddSingleton(sp => new ToonEncoder(sp.GetRequiredService<IOptions<ToonOptions>>().Value));

        return services;
    }

    /// <summary>
    /// Sets <see cref="ToonSerializerOptions.ToonOptions"/> to the registered <see cref="ToonOptions"/> before any other
    /// configuration of the serializer options runs, so a delegate that sets or changes
    /// <see cref="ToonSerializerOptions.ToonOptions"/> (in <c>AddToonNet</c>, <c>AddToonFormatters</c> or
    /// <c>Configure&lt;ToonSerializerOptions&gt;</c>, registered before or after) is not overwritten.
    /// </summary>
    private static void LinkToonOptions(IServiceCollection services)
    {
        if (services.Any(d => d.ImplementationType == typeof(ToonOptionsLink)))
        {
            return;
        }

        // Options configurations run in registration order
        services.Insert(0, ServiceDescriptor.Singleton<IConfigureOptions<ToonSerializerOptions>, ToonOptionsLink>());
    }

    /// <summary>Runs <see cref="IValidatableObject.Validate"/> of the options when they are created.</summary>
    private sealed class SelfValidatingOptions<TOptions> : IValidateOptions<TOptions> where TOptions : class, IValidatableObject
    {
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The options are ToonOptions or ToonSerializerOptions, whose members the DynamicDependency keeps; their Validate does not use the context.")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ToonOptions))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ToonSerializerOptions))]
        public ValidateOptionsResult Validate(string? name, TOptions options)
        {
#if NET10_0_OR_GREATER
            var context = new ValidationContext(options, typeof(TOptions).Name, serviceProvider: null, items: null); // with a name: no reflection
#else
            var context = new ValidationContext(options);
#endif
            var errors = options.Validate(context)
                                .Select(result => $"{typeof(TOptions).Name}: {result.ErrorMessage}")
                                .ToList();

            return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
        }
    }

    private sealed class ToonOptionsLink(IOptions<ToonOptions> toonOptions) : IConfigureOptions<ToonSerializerOptions>
    {
        public void Configure(ToonSerializerOptions options) => options.ToonOptions = toonOptions.Value;
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

        var allowExtendedLimits = GetBoolean(section, nameof(ToonOptions.AllowExtendedLimits));

        if (allowExtendedLimits.HasValue)
        {
            options.AllowExtendedLimits = allowExtendedLimits.Value;
        }

        var indentSize = GetInt32(section, nameof(ToonOptions.IndentSize));
        if (indentSize.HasValue)
        {
            options.IndentSize = indentSize.Value;
        }

        // Accepts the character itself (",", "|", a literal tab) or the names "comma", "tab" and "pipe"
        var delimiterString = section[nameof(ToonOptions.Delimiter)];
        if (!string.IsNullOrEmpty(delimiterString))
        {
            // Anything else (a misspelled name, several characters) fails at startup instead of keeping its first character
            options.Delimiter = delimiterString.ToLowerInvariant() switch
            {
                "comma" or ","   => ',',
                "tab" or "\t"    => '\t',
                "pipe" or "|"    => '|',
                _ => throw new InvalidOperationException(
                    $"Invalid {section.Path}:{nameof(ToonOptions.Delimiter)} value '{delimiterString}'. " +
                    "Expected one of: comma, tab, pipe, or the character itself (',', a tab, '|').")
            };
        }

        var strictMode = GetBoolean(section, nameof(ToonOptions.StrictMode));
        if (strictMode.HasValue)
        {
            options.StrictMode = strictMode.Value;
        }

        var maxDepth = GetInt32(section, nameof(ToonOptions.MaxDepth));
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

        var allowExtendedLimits = GetBoolean(section, nameof(ToonSerializerOptions.AllowExtendedLimits));

        if (allowExtendedLimits.HasValue)
        {
            options.AllowExtendedLimits = allowExtendedLimits.Value;
        }

        var ignoreNullValues = GetBoolean(section, nameof(ToonSerializerOptions.IgnoreNullValues));
        if (ignoreNullValues.HasValue)
        {
            options.IgnoreNullValues = ignoreNullValues.Value;
        }

        var includeReadOnlyProperties = GetBoolean(section, nameof(ToonSerializerOptions.IncludeReadOnlyProperties));
        if (includeReadOnlyProperties.HasValue)
        {
            options.IncludeReadOnlyProperties = includeReadOnlyProperties.Value;
        }

        var serializeRuntimeTypes = GetBoolean(section, nameof(ToonSerializerOptions.SerializeRuntimeTypes));
        if (serializeRuntimeTypes.HasValue)
        {
            options.SerializeRuntimeTypes = serializeRuntimeTypes.Value;
        }

        var maxDepth = GetInt32(section, nameof(ToonSerializerOptions.MaxDepth));
        if (maxDepth.HasValue)
        {
            options.MaxDepth = maxDepth.Value;
        }

        var namingPolicy = section[nameof(ToonSerializerOptions.PropertyNamingPolicy)];
        if (!string.IsNullOrWhiteSpace(namingPolicy))
        {
            // A misspelled policy fails at startup instead of silently keeping the default
            options.PropertyNamingPolicy = TryParseName<PropertyNamingPolicy>(namingPolicy, out var policy)
                ? policy
                : throw new InvalidOperationException(
                    $"Invalid {section.Path}:{nameof(ToonSerializerOptions.PropertyNamingPolicy)} value '{namingPolicy}'. " +
                    $"Expected one of: {string.Join(", ", Enum.GetNames<PropertyNamingPolicy>())}.");
        }

        var caseInsensitive = GetBoolean(section, nameof(ToonSerializerOptions.PropertyNameCaseInsensitive));
        if (caseInsensitive.HasValue)
        {
            options.PropertyNameCaseInsensitive = caseInsensitive.Value;
        }

        var respectRequired = GetBoolean(section, nameof(ToonSerializerOptions.RespectRequiredMembers));
        if (respectRequired.HasValue)
        {
            options.RespectRequiredMembers = respectRequired.Value;
        }

        var unmapped = section[nameof(ToonSerializerOptions.UnmappedMemberHandling)];
        if (!string.IsNullOrWhiteSpace(unmapped))
        {
            options.UnmappedMemberHandling = TryParseName<ToonUnmappedMemberHandling>(unmapped, out var handling)
                ? handling
                : throw new InvalidOperationException(
                    $"Invalid {section.Path}:{nameof(ToonSerializerOptions.UnmappedMemberHandling)} value '{unmapped}'. " +
                    $"Expected one of: {string.Join(", ", Enum.GetNames<ToonUnmappedMemberHandling>())}.");
        }
    }

    /// <summary>
    ///     One of the names of the enum, in any case; not a number or a comma-separated list, which
    ///     <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/> would combine into another value.
    /// </summary>
    private static bool TryParseName<TEnum>(string text, out TEnum value) where TEnum : struct, Enum
    {
        var name = text.Trim();

        foreach (var candidate in Enum.GetNames<TEnum>())
        {
            if (string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase))
            {
                value = Enum.Parse<TEnum>(candidate);
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>A boolean setting (<c>true</c>/<c>false</c> in any case), or null when it is not set.</summary>
    private static bool? GetBoolean(IConfigurationSection section, string key)
    {
        var text = section[key];

        return string.IsNullOrWhiteSpace(text) ? null
            : bool.TryParse(text.Trim(), out var value) ? value
            : throw InvalidValue(section, key, text, "true or false");
    }

    /// <summary>An integer setting, or null when it is not set.</summary>
    private static int? GetInt32(IConfigurationSection section, string key)
    {
        var text = section[key];

        return string.IsNullOrWhiteSpace(text) ? null
            : int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value
            : throw InvalidValue(section, key, text, "an integer");
    }

    private static InvalidOperationException InvalidValue(IConfigurationSection section, string key, string text, string expected) =>
        new($"Invalid {section.Path}:{key} value '{text}'. Expected {expected}.");
}
