using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ToonNet.AspNetCore.Mvc.Formatters;
using ToonNet.Core.Serialization;

namespace ToonNet.AspNetCore.Mvc.DependencyInjection;

/// <summary>
/// Provides extension methods for integrating TOON serialization formatters into the ASP.NET Core MVC pipeline.
/// </summary>
public static class ToonMvcBuilderExtensions
{
    /// <summary>
    /// Adds TOON input and output formatters to MVC.
    /// </summary>
    /// <param name="builder">The MVC builder instance.</param>
    /// <param name="configureOptions">An optional action to configure TOON serializer options.</param>
    /// <returns>The MVC builder instance for chaining further configuration.</returns>
    /// <remarks>
    /// Request bodies are limited to <see cref="ToonFormatterDefaults.MaxRequestBodySize"/> bytes.
    /// </remarks>
    public static IMvcBuilder AddToonFormatters(this IMvcBuilder builder, Action<ToonSerializerOptions>? configureOptions = null)
    {
        return AddToonFormatters(builder, configureOptions, ToonFormatterDefaults.MaxRequestBodySize);
    }

    /// <summary>
    /// Adds TOON input and output formatters to MVC with a custom request body size limit.
    /// </summary>
    /// <param name="builder">The MVC builder instance.</param>
    /// <param name="configureOptions">An optional action to configure TOON serializer options.</param>
    /// <param name="maxRequestBodySize">The maximum size, in bytes, of a TOON request body.</param>
    /// <returns>The MVC builder instance for chaining further configuration.</returns>
    public static IMvcBuilder AddToonFormatters(this IMvcBuilder builder, Action<ToonSerializerOptions>? configureOptions, long maxRequestBodySize)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRequestBodySize);

        builder.Services.Configure<MvcOptions>(options =>
        {
            var serializerOptions = new ToonSerializerOptions();
            configureOptions?.Invoke(serializerOptions);
            
            // Add Input Formatter
            options.InputFormatters.Add(new ToonInputFormatter(serializerOptions, maxRequestBodySize));
            
            // Add Output Formatter
            options.OutputFormatters.Add(new ToonOutputFormatter(serializerOptions));
        });

        return builder;
    }
}
