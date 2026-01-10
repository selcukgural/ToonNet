using Microsoft.AspNetCore.Http;
using ToonNet.Core.Serialization;

namespace ToonNet.AspNetCore.Mvc.Http;

/// <summary>
/// Provides extension methods for working with TOON-formatted HTTP results in ASP.NET Core applications.
/// </summary>
public static class ToonHttpResultExtensions
{
    /// <summary>
    /// Creates a <see cref="ToonResult"/> that serializes the specified <paramref name="value"/> to TOON format.
    /// </summary>
    /// <param name="resultExtensions">The result extensions.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Optional serializer options.</param>
    /// <returns>The created <see cref="ToonResult"/>.</returns>
    public static IResult Toon(this IResultExtensions resultExtensions, object? value, ToonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(resultExtensions);
        return new ToonResult(value, options);
    }
}
