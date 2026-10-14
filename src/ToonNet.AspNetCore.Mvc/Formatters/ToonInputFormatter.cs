using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using ToonNet.Core;
using ToonNet.Core.Serialization;

namespace ToonNet.AspNetCore.Mvc.Formatters;

/// <summary>
/// A <see cref="TextInputFormatter"/> that processes incoming TOON-encoded content and deserializes it into .NET objects.
/// </summary>
/// <remarks>
/// This formatter supports TOON-specific media types as defined by <see cref="ToonFormatterDefaults"/> and ensures compatibility
/// with UTF-8 and Unicode encodings. The deserialization behavior can be customized using <see cref="ToonSerializerOptions"/>.
/// </remarks>
public sealed class ToonInputFormatter : TextInputFormatter
{
    private readonly ToonSerializerOptions _options;
    private readonly long _maxRequestBodySize;

    /// <summary>
    ///     Initializes a new instance of <see cref="ToonInputFormatter"/> that accepts request bodies
    ///     up to <see cref="ToonFormatterDefaults.MaxRequestBodySize"/> bytes.
    /// </summary>
    /// <param name="options">Options for deserialization.</param>
    public ToonInputFormatter(ToonSerializerOptions options) : this(options, ToonFormatterDefaults.MaxRequestBodySize) { }

    /// <summary>
    ///     Initializes a new instance of <see cref="ToonInputFormatter"/> with a custom request body size limit.
    /// </summary>
    /// <param name="options">Options for deserialization.</param>
    /// <param name="maxRequestBodySize">The maximum size of the request body in bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxRequestBodySize"/> is not positive.</exception>
    public ToonInputFormatter(ToonSerializerOptions options, long maxRequestBodySize)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRequestBodySize);
        _maxRequestBodySize = maxRequestBodySize;

        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(ToonFormatterDefaults.MediaType));
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(ToonFormatterDefaults.TextMediaType));
        
        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    /// <summary>
    /// Asynchronously reads the request body, deserializes it into a .NET object, and returns the result.
    /// </summary>
    /// <param name="context">The context containing information about the request to process.</param>
    /// <param name="encoding">The character encoding of the request body.</param>
    /// <returns>
    /// A <see cref="Task"/> that, when completed, contains an <see cref="InputFormatterResult"/> representing
    /// the deserialization outcome.
    /// Returns a successful result with the deserialized object if deserialization is successful;
    /// otherwise, returns a failure result with the TOON error added to the model state.
    /// </returns>
    /// <exception cref="BadHttpRequestException">
    /// Thrown with status code 413 when the request body exceeds the configured size limit.
    /// </exception>
    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(encoding);

        var httpContext = context.HttpContext;
        var request = httpContext.Request;

        if (request.ContentLength > _maxRequestBodySize)
        {
            throw CreatePayloadTooLargeException();
        }

        // Decode while reading instead of copying the body into a MemoryStream first; the wrapper enforces the limit
        var body = new LimitedReadStream(request.Body, _maxRequestBodySize, CreatePayloadTooLargeException);

        try
        {
            object? model;

            if (encoding.CodePage == Encoding.UTF8.CodePage)
            {
                // UTF-8 (the common case) is decoded strictly into a pooled buffer and parsed there, without a string;
                // ill-formed bytes are an error, not text with U+FFFD in it (TOON spec §4)
                model = await ToonSerializer.DeserializeFromStreamAsync(body, context.ModelType, _options, httpContext.RequestAborted)
                                            .ConfigureAwait(false);
            }
            else
            {
                var content = await RequestBody.ReadTextAsync(body, encoding, httpContext.RequestAborted).ConfigureAwait(false);
                model = ToonSerializer.Deserialize(content, context.ModelType, _options);
            }

            return body.HasContent ? await InputFormatterResult.SuccessAsync(model).ConfigureAwait(false) : await NoContentAsync(context).ConfigureAwait(false);
        }
        catch (ToonException) when (!body.HasContent)
        {
            // Whitespace that is not valid TOON (a tab in strict mode) is still no content
            return await NoContentAsync(context).ConfigureAwait(false);
        }
        catch (DecoderFallbackException)
        {
            // Ill-formed text in another charset is an error too, not text with U+FFFD in it
            context.ModelState.TryAddModelError(context.ModelName, $"The request body is not valid {encoding.WebName}.");
            return await InputFormatterResult.FailureAsync().ConfigureAwait(false);
        }
        catch (ToonException ex)
        {
            context.ModelState.TryAddModelError(context.ModelName, ex.Message);
            return await InputFormatterResult.FailureAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     A body with only whitespace binds nothing, as MVC handles Content-Length: 0 (an empty TOON document would bind
    ///     an empty object): the default value when the model allows empty input, no value otherwise.
    /// </summary>
    private Task<InputFormatterResult> NoContentAsync(InputFormatterContext context) =>
        context.TreatEmptyInputAsDefaultValue
            ? InputFormatterResult.SuccessAsync(GetDefaultValueForType(context.ModelType))
            : InputFormatterResult.NoValueAsync();

    private BadHttpRequestException CreatePayloadTooLargeException()
    {
        return new BadHttpRequestException($"The TOON request body exceeds the maximum allowed size of {_maxRequestBodySize} bytes.",
                                           StatusCodes.Status413PayloadTooLarge);
    }
}
