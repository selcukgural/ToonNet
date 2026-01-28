using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
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

    /// <summary>
    ///     Initializes a new instance of <see cref="ToonInputFormatter"/>.
    /// </summary>
    /// <param name="options">Options for deserialization.</param>
    public ToonInputFormatter(ToonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

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
    /// otherwise, returns a failure result.
    /// </returns>
    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(encoding);

        try
        {
            var httpContext = context.HttpContext;
            var request = httpContext.Request;
            
            var model = await DeserializeFromStreamInternalAsync(
                context.ModelType, 
                request.Body, 
                _options, 
                httpContext.RequestAborted
            ).ConfigureAwait(false);

            return await InputFormatterResult.SuccessAsync(model);
        }
        catch (Exception ex)
        {
            context.ModelState.TryAddModelError(string.Empty, ex.Message);
            return await InputFormatterResult.FailureAsync();
        }
    }

    /// <summary>
    /// Deserializes the content of a stream into an object of the specified type using the provided options.
    /// </summary>
    /// <param name="type">The target type of the deserialization.</param>
    /// <param name="stream">The input stream containing the serialized data.</param>
    /// <param name="options">The options to control deserialization behavior.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The deserialized object, or null if the stream is empty.</returns>
    private static async Task<object?> DeserializeFromStreamInternalAsync(Type type, 
                                                                          Stream stream, 
                                                                          ToonSerializerOptions options, 
                                                                          CancellationToken cancellationToken)
    {
        // Read stream to string
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        
        // Use non-generic Deserialize method (no reflection needed)
        return ToonSerializer.Deserialize(content, type, options);
    }
}
