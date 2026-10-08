using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
    /// <param name="configureOptions">
    /// An optional action that configures the application's <see cref="ToonSerializerOptions"/> (the same options that
    /// <c>AddToonNet</c> registers and <c>ToonResult</c> uses).
    /// </param>
    /// <returns>The MVC builder instance for chaining further configuration.</returns>
    /// <remarks>
    /// The formatters use <c>IOptions&lt;ToonSerializerOptions&gt;</c> from dependency injection, so settings made with
    /// <c>AddToonNet</c> or <c>services.Configure&lt;ToonSerializerOptions&gt;</c> apply to them too.
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
    /// <param name="configureOptions">
    /// An optional action that configures the application's <see cref="ToonSerializerOptions"/> (see the other overload).
    /// </param>
    /// <param name="maxRequestBodySize">The maximum size, in bytes, of a TOON request body.</param>
    /// <returns>The MVC builder instance for chaining further configuration.</returns>
    public static IMvcBuilder AddToonFormatters(this IMvcBuilder builder, Action<ToonSerializerOptions>? configureOptions, long maxRequestBodySize)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRequestBodySize);

        if (configureOptions != null)
        {
            builder.Services.Configure(configureOptions);
        }

        builder.Services.AddOptions<MvcOptions>().Configure<IOptions<ToonSerializerOptions>>((options, serializerOptions) =>
        {
            options.InputFormatters.Add(new ToonInputFormatter(serializerOptions.Value, maxRequestBodySize));
            options.OutputFormatters.Add(new ToonOutputFormatter(serializerOptions.Value));
        });

        return builder;
    }
}
