using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ToonNet.AspNetCore.Mvc.Formatters;
using ToonNet.Core.Serialization;

namespace ToonNet.AspNetCore.Mvc.Http;

/// <summary>
/// Represents a result that writes TOON encoded content as an <see cref="IResult"/>.
/// </summary>
public sealed class ToonResult : IResult, IStatusCodeHttpResult, IValueHttpResult, IContentTypeHttpResult
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
    public ToonResult(object? value, ToonSerializerOptions? options = null) : this(value, options, null)
    {
    }

    /// <summary>
    /// An <see cref="IResult"/> that writes TOON encoded content with the given status code.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Serializer options; the application's <c>IOptions&lt;ToonSerializerOptions&gt;</c> when null.</param>
    /// <param name="statusCode">The response status code; the response's current status code (200 by default) when null.</param>
    public ToonResult(object? value, ToonSerializerOptions? options, int? statusCode)
    {
        _value = value;
        _options = options;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the status code written to the response, or null to leave the current one (200 by default).
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Gets the value written to the response.
    /// </summary>
    public object? Value => _value;

    /// <summary>
    /// Gets the content type of the response.
    /// </summary>
    public string ContentType => $"{ToonFormatterDefaults.MediaType}; charset=utf-8";

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

        if (StatusCode is { } statusCode)
        {
            httpContext.Response.StatusCode = statusCode;
        }

        // ToonSerializer writes UTF-8 without a byte order mark
        httpContext.Response.ContentType = ContentType;

        // Resolve options from DI if not provided
        var options = _options ?? httpContext.RequestServices.GetService<IOptions<ToonSerializerOptions>>()?.Value ?? ToonSerializerOptions.Default;

        // A null value is written as the TOON document `null`; an empty body would read back as an empty object. An
        // asynchronous sequence is collected first (its array header needs the number of items)
        var (value, objectType) = await AsyncEnumerableBuffer.CollectIfAsyncAsync(_value, _value?.GetType() ?? typeof(object), httpContext.RequestAborted)
                                                              .ConfigureAwait(false);
        await ToonSerializer.SerializeToStreamAsync(objectType, value, httpContext.Response.Body, options, httpContext.RequestAborted)
            .ConfigureAwait(false);
    }
}
