using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using ToonNet.Core;

namespace ToonNet.AspNetCore.Configuration;

/// <summary>
///     Extension methods for adding <see cref="ToonConfigurationProvider"/>.
/// </summary>
public static class ToonConfigurationExtensions
{
    /// <summary>
    ///     Adds the TOON configuration provider at <paramref name="path"/> to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="path">Path relative to the base path stored in <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddToonFile(this IConfigurationBuilder builder, string path)
    {
        return builder.AddToonFile(provider: null, path: path, optional: false, reloadOnChange: false, options: null);
    }

    /// <summary>
    ///     Adds the TOON configuration provider at <paramref name="path"/> to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="path">Path relative to the base path stored in <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddToonFile(this IConfigurationBuilder builder, string path, bool optional)
    {
        return builder.AddToonFile(provider: null, path: path, optional: optional, reloadOnChange: false, options: null);
    }

    /// <summary>
    ///     Adds the TOON configuration provider at <paramref name="path"/> to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="path">Path relative to the base path stored in <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
    /// <param name="options">Options for parsing the TOON file.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddToonFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange, ToonOptions? options = null)
    {
        return builder.AddToonFile(provider: null, path: path, optional: optional, reloadOnChange: reloadOnChange, options: options);
    }

    /// <summary>
    ///     Adds a TOON configuration source to <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="provider">The <see cref="IFileProvider"/> to use to access the file.</param>
    /// <param name="path">Path relative to the base path stored in <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
    /// <param name="optional">Whether the file is optional.</param>
    /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
    /// <param name="options">Options for parsing the TOON file.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddToonFile(this IConfigurationBuilder builder, IFileProvider? provider, string path, bool optional, bool reloadOnChange, ToonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(path);

        return builder.Add(new ToonConfigurationSource
        {
            FileProvider = provider,
            Path = path,
            Optional = optional,
            ReloadOnChange = reloadOnChange,
            Options = options
        });
    }
}
