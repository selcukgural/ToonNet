using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using ToonNet.Core.Serialization;

namespace ToonNet.AspNetCore.Mvc.Formatters;

/// <summary>
///     A <see cref="TextInputFormatter"/> that reads TOON encoded content.
/// </summary>
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

    /// <inheritdoc />
    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(encoding);

        try
        {
            var httpContext = context.HttpContext;
            var request = httpContext.Request;
            
            // Use helper method to avoid reflection - much faster!
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
    ///     Internal helper to deserialize without reflection.
    /// </summary>
    private static async Task<object?> DeserializeFromStreamInternalAsync(
        Type type, 
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
