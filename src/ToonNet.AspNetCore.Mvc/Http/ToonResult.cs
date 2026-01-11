using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ToonNet.AspNetCore.Mvc.Formatters;
using ToonNet.Core.Serialization;

namespace ToonNet.AspNetCore.Mvc.Http;

/// <summary>
/// Represents a result that writes TOON encoded content as an <see cref="IResult"/>.
/// </summary>
public sealed class ToonResult : IResult
{
    /// <summary>
    /// Represents the value to be serialized in the <see cref="ToonResult"/> response.
    /// This value is typically an object that will be encoded using the TOON serialization format
    /// specified by <see cref="ToonSerializerOptions"/>.
    /// </summary>
    private readonly object? _value;

    /// <summary>
    /// Represents the serialization options for the ToonResult output.
    /// When provided, these options determine the behavior of TOON serialization such as
    /// property naming policies, null value handling, type information inclusion, and more.
    /// </summary>
    private readonly ToonSerializerOptions? _options;

    /// <summary>
    /// An <see cref="IResult"/> that writes TOON encoded content.
    /// </summary>
    public ToonResult(object? value, ToonSerializerOptions? options = null)
    {
        _value = value;
        _options = options;
    }

    /// <summary>
    /// Executes the result operation of the current context asynchronously.
    /// </summary>
    /// <param name="httpContext">
    /// The <see cref="HttpContext"/> containing the HTTP response to write to.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operate completion.
    /// </returns>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.ContentType = ToonFormatterDefaults.MediaType;

        if (_value is null)
        {
            return;
        }

        // Resolve options from DI if not provided
        var options = _options ?? httpContext.RequestServices.GetService<IOptions<ToonSerializerOptions>>()?.Value ?? ToonSerializerOptions.Default;

        // Use non-generic SerializeToStreamAsync overload (eliminates reflection overhead)
        var objectType = _value.GetType();
        await ToonSerializer.SerializeToStreamAsync(objectType, _value, httpContext.Response.Body, options, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }
}
