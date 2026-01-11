using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using ToonNet.Core.Serialization;

namespace ToonNet.AspNetCore.Mvc.Formatters;

/// <summary>
/// A formatter that writes responses in the TOON serialized format, extending <see cref="TextOutputFormatter"/>.
/// </summary>
public sealed class ToonOutputFormatter : TextOutputFormatter
{
    /// <summary>
    /// Holds the configuration settings required for TOON serialization processing
    /// within the <see cref="ToonOutputFormatter"/>. This includes options
    /// that govern serialization rules such as property naming conventions,
    /// handling of null values, depth restrictions, and custom converter support.
    /// </summary>
    private readonly ToonSerializerOptions _options;

    /// <summary>
    /// A formatter that outputs TOON encoded content for ASP.NET Core MVC responses.
    /// </summary>
    /// <param name="options">Configuration options for TOON serialization.</param>
    public ToonOutputFormatter(ToonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(ToonFormatterDefaults.MediaType));
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(ToonFormatterDefaults.TextMediaType));
        
        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    /// <summary>
    /// Writes the response body by serializing the specified object into TOON format
    /// and writing it to the HTTP response stream asynchronously.
    /// </summary>
    /// <param name="context">The context containing the object to serialize and HTTP response details.</param>
    /// <param name="selectedEncoding">The character encoding used for the serialized response body.</param>
    /// <returns>A task that represents the asynchronous operation of writing the response body.</returns>
    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(selectedEncoding);

        var httpContext = context.HttpContext;
        var response = httpContext.Response;

        if (context.Object == null)
        {
            return;
        }

        // Use non-generic overload to avoid reflection - much faster!
        var objectType = context.Object.GetType();
        await ToonSerializer.SerializeToStreamAsync(
            objectType,
            context.Object,
            response.Body,
            _options,
            httpContext.RequestAborted
        ).ConfigureAwait(false);
    }
}
