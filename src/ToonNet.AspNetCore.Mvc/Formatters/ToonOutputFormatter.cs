using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using ToonNet.AspNetCore.Mvc.Http;
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
    [RequiresUnreferencedCode(AotMessages.Reflection)]
    [RequiresDynamicCode(AotMessages.Reflection)]
    public ToonOutputFormatter(ToonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        // Same media types as the input formatter; the response uses the one the client asked for
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(ToonFormatterDefaults.MediaType));
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(ToonFormatterDefaults.TextMediaType));

        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    /// <summary>
    /// Returns whether the formatter can write the response, also for an <c>Accept</c> media type with a charset
    /// (<c>application/toon; charset=utf-16</c>), which MVC does not match to a media type without one.
    /// </summary>
    /// <param name="context">The formatter context.</param>
    /// <returns>Whether the response can be written as TOON.</returns>
    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (base.CanWriteResult(context))
        {
            return true;
        }

        if (!context.ContentType.HasValue)
        {
            return false;
        }

        var requested = new MediaType(context.ContentType);

        if (!requested.Charset.HasValue || requested.HasWildcard)
        {
            return false;
        }

        foreach (var supported in SupportedMediaTypes)
        {
            var mediaType = new MediaType(supported);

            if (mediaType.Type.Equals(requested.Type, StringComparison.OrdinalIgnoreCase) &&
                mediaType.SubType.Equals(requested.SubType, StringComparison.OrdinalIgnoreCase))
            {
                // The charset picks one of the supported encodings, by its own name: the requested text is never copied
                // into the response header (a value such as "utf-8,text/html" would change the content type). Any other
                // charset gets the media type alone, which MVC writes in UTF-8.
                var encoding = SupportedEncoding(requested.Charset.Value);
                context.ContentType = new StringSegment(encoding == null ? supported : $"{supported}; charset={encoding.WebName}");
                return true;
            }
        }

        return false;
    }

    /// <summary>The supported encoding a charset names, or null.</summary>
    private Encoding? SupportedEncoding(string? charset)
    {
        foreach (var encoding in SupportedEncodings)
        {
            if (string.Equals(encoding.WebName, charset, StringComparison.OrdinalIgnoreCase))
            {
                return encoding;
            }
        }

        return null;
    }

    /// <summary>
    /// Writes the response body by serializing the specified object into TOON format
    /// and writing it to the HTTP response stream asynchronously.
    /// </summary>
    /// <param name="context">The context containing the object to serialize and HTTP response details.</param>
    /// <param name="selectedEncoding">The character encoding used for the serialized response body.</param>
    /// <returns>A task that represents the asynchronous operation of writing the response body.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = AotMessages.AnnotatedConstructor)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = AotMessages.AnnotatedConstructor)]
    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(selectedEncoding);

        var httpContext = context.HttpContext;
        var response = httpContext.Response;

        // A null value (reached when MVC does not turn it into 204 No Content) is written as `null`, because an empty
        // TOON document means an empty object.
        // An asynchronous sequence is collected first: its array header needs the number of items
        var (value, type) = await AsyncEnumerableBuffer.CollectIfAsyncAsync(context.Object, context.Object?.GetType() ?? context.ObjectType ?? typeof(object),
                                                                            httpContext.RequestAborted).ConfigureAwait(false);

        if (selectedEncoding.CodePage == Encoding.UTF8.CodePage)
        {
            // UTF-8 (the common case) is written by the serializer in pooled chunks, without an intermediate string
            await ToonSerializer.SerializeToStreamAsync(type, value, response.Body, _options, httpContext.RequestAborted)
                                .ConfigureAwait(false);
            return;
        }

        // Other encodings: serialize first, then write with the negotiated encoding so the body matches the charset
        var toonString = ToonSerializer.Serialize(value, type, _options);

        var writer = context.WriterFactory(response.Body, selectedEncoding);

        await using var writerDisposal = writer.ConfigureAwait(false);
        await writer.WriteAsync(toonString.AsMemory(), httpContext.RequestAborted).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }
}
