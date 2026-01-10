using Microsoft.Extensions.Configuration;
using ToonNet.Core;

namespace ToonNet.AspNetCore.Configuration;

/// <summary>
/// Represents a configuration source for a TOON file, leveraging the functionality
/// of <see cref="FileConfigurationSource"/> to manage configuration data in a TOON-specific format.
/// </summary>
public sealed class ToonConfigurationSource : FileConfigurationSource
{
    /// <summary>
    /// Specifies the configuration options to be used when parsing a TOON file.
    /// </summary>
    public ToonOptions? Options { get; set; }

    /// <inheritdoc />
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        EnsureDefaults(builder);
        return new ToonConfigurationProvider(this, Options);
    }
}
