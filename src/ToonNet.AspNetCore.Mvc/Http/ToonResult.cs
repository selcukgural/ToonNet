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
    /// <remarks>
    /// A null value is written as <c>null</c> (like <c>Results.Json(null)</c>); an empty TOON body would mean an
    /// empty object.
    /// </remarks>
    /// <param name="httpContext">
    /// The <see cref="HttpContext"/> containing the HTTP response to write to.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operate completion.
    /// </returns>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // ToonSerializer writes UTF-8 without a byte order mark
        httpContext.Response.ContentType = $"{ToonFormatterDefaults.MediaType}; charset=utf-8";

        // Resolve options from DI if not provided
        var options = _options ?? httpContext.RequestServices.GetService<IOptions<ToonSerializerOptions>>()?.Value ?? ToonSerializerOptions.Default;

        // A null value is written as the TOON document `null`; an empty body would read back as an empty object
        var objectType = _value?.GetType() ?? typeof(object);
        await ToonSerializer.SerializeToStreamAsync(objectType, _value, httpContext.Response.Body, options, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }
}
