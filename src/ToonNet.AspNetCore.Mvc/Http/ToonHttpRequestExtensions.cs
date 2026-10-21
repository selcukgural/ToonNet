using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using ToonNet.AspNetCore.Mvc.Formatters;
using ToonNet.Core;
using ToonNet.Core.Serialization;

namespace ToonNet.AspNetCore.Mvc.Http;

/// <summary>
/// Reads TOON request bodies in minimal APIs and middleware, like <c>ReadFromJsonAsync</c> does for JSON:
/// <c>app.MapPost("/users", async (HttpRequest request) =&gt; await request.ReadFromToonAsync&lt;User&gt;())</c>.
/// </summary>
/// <remarks>
/// The request must have a TOON content type (<c>application/toon</c> or <c>text/toon</c>); a charset other than UTF-8
/// is decoded strictly. A body larger than <see cref="ToonFormatterDefaults.MaxRequestBodySize"/> (4 MB, the limit of
/// the MVC formatter) is rejected with <c>413 Payload Too Large</c>, unless the endpoint sets its own limit
/// (<c>[RequestSizeLimit]</c>, <c>.WithMetadata(new RequestSizeLimitAttribute(...))</c> or <c>[DisableRequestSizeLimit]</c>), which the server
/// then enforces. A body with only whitespace reads as the default value, because an empty
/// TOON document would read as an empty object. Invalid TOON throws a <see cref="ToonException"/>.
/// </remarks>
public static class ToonHttpRequestExtensions
{
    /// <summary>Whether the request has a TOON content type (<c>application/toon</c> or <c>text/toon</c>), whatever its charset.</summary>
    /// <param name="request">The request.</param>
    /// <returns>True when the content type is a TOON media type.</returns>
    public static bool HasToonContentType(this HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return TryGetEncoding(request, out _, out var unknownCharset) || unknownCharset != null;
    }

    /// <summary>Reads the request body as TOON, with the application's <see cref="ToonSerializerOptions"/>.</summary>
    /// <typeparam name="T">The type to read.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    ///     The value; the default value when the body is blank, and null when the document is <c>null</c> (an error for a
    ///     non-nullable value type).
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the request does not have a TOON content type or its charset is unknown.</exception>
    /// <exception cref="BadHttpRequestException">Thrown with status 413 when the body is larger than its limit (see the remarks of the class).</exception>
    /// <exception cref="ToonException">Thrown when the body is not valid TOON or does not fit <typeparamref name="T"/>.</exception>
    [RequiresUnreferencedCode(AotMessages.Reflection)]
    [RequiresDynamicCode(AotMessages.Reflection)]
    public static ValueTask<T?> ReadFromToonAsync<T>(this HttpRequest request, CancellationToken cancellationToken = default) =>
        ReadFromToonAsync<T>(request, options: null, cancellationToken);

    /// <summary>Reads the request body as TOON.</summary>
    /// <typeparam name="T">The type to read.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="options">The serializer options; the application's <c>IOptions&lt;ToonSerializerOptions&gt;</c> when null.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    ///     The value; the default value when the body is blank, and null when the document is <c>null</c> (an error for a
    ///     non-nullable value type).
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the request does not have a TOON content type or its charset is unknown.</exception>
    /// <exception cref="BadHttpRequestException">Thrown with status 413 when the body is larger than its limit (see the remarks of the class).</exception>
    /// <exception cref="ToonException">Thrown when the body is not valid TOON or does not fit <typeparamref name="T"/>.</exception>
    [RequiresUnreferencedCode(AotMessages.Reflection)]
    [RequiresDynamicCode(AotMessages.Reflection)]
    public static ValueTask<T?> ReadFromToonAsync<T>(this HttpRequest request, ToonSerializerOptions? options, CancellationToken cancellationToken = default) =>
        ReadAsync(request, options, static (stream, o, ct) => ToonSerializer.DeserializeAsync<T>(stream, o, ct),
                  static (text, o) => ToonSerializer.Deserialize<T>(text, o), cancellationToken);

    /// <summary>
    /// Reads the request body as TOON with generated code only (for trimmed and native AOT applications), with the
    /// application's <see cref="ToonSerializerOptions"/>.
    /// </summary>
    /// <typeparam name="T">The type to read.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="typeInfo">The type info, such as the <c>ToonTypeInfo</c> property of a <c>[ToonSerializable]</c> type.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    ///     The value; the default value when the body is blank, and null when the document is <c>null</c> (an error for a
    ///     non-nullable value type).
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the request does not have a TOON content type or its charset is unknown.</exception>
    /// <exception cref="BadHttpRequestException">Thrown with status 413 when the body is larger than its limit (see the remarks of the class).</exception>
    /// <exception cref="NotSupportedException">Thrown when the generated code is from an older generator that does not apply the options.</exception>
    /// <exception cref="ToonException">Thrown when the body is not valid TOON or does not fit <typeparamref name="T"/>.</exception>
    public static ValueTask<T?> ReadFromToonAsync<T>(this HttpRequest request, ToonTypeInfo<T> typeInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        return ReadAsync(request, options: null, (stream, o, ct) => ToonSerializer.DeserializeAsync(stream, typeInfo, o, ct),
                         (text, o) => ToonSerializer.Deserialize(text, typeInfo, o), cancellationToken);
    }

    /// <summary>Reads the request body as TOON.</summary>
    /// <param name="request">The request.</param>
    /// <param name="type">The type to read.</param>
    /// <param name="options">The serializer options; the application's <c>IOptions&lt;ToonSerializerOptions&gt;</c> when null.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    ///     The value; the default value of <paramref name="type"/> when the body is blank, and null when the document is
    ///     <c>null</c> (an error for a non-nullable value type).
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the request does not have a TOON content type or its charset is unknown.</exception>
    /// <exception cref="BadHttpRequestException">Thrown with status 413 when the body is larger than its limit (see the remarks of the class).</exception>
    /// <exception cref="ToonException">Thrown when the body is not valid TOON or does not fit <paramref name="type"/>.</exception>
    [RequiresUnreferencedCode(AotMessages.Reflection)]
    [RequiresDynamicCode(AotMessages.Reflection)]
    public static async ValueTask<object?> ReadFromToonAsync(this HttpRequest request, Type type, ToonSerializerOptions? options = null,
                                                             CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(type);

        var value = await ReadAsync(request, options, (stream, o, ct) => ToonSerializer.DeserializeAsync(stream, type, o, ct),
                                    (text, o) => ToonSerializer.Deserialize(text, type, o), cancellationToken).ConfigureAwait(false);

        return value ?? (type.IsValueType && Nullable.GetUnderlyingType(type) == null ? Activator.CreateInstance(type) : null);
    }

    private static async ValueTask<T?> ReadAsync<T>(HttpRequest request, ToonSerializerOptions? options,
                                                    Func<Stream, ToonSerializerOptions, CancellationToken, ValueTask<T?>> readUtf8,
                                                    Func<string, ToonSerializerOptions, T?> readText, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetEncoding(request, out var encoding, out var charset))
        {
            throw new InvalidOperationException(charset == null
                ? $"Unable to read the request as TOON because the request content type '{request.ContentType}' is not a TOON content type."
                : $"Unable to read the request as TOON because the charset '{charset}' is not supported.");
        }

        options ??= request.HttpContext.RequestServices?.GetService<IOptions<ToonSerializerOptions>>()?.Value ?? ToonSerializerOptions.Default;

        // An endpoint with a size limit of its own has that limit (enforced here too: a server or test host may not
        // apply it); others the limit of the MVC formatter, because the text and the tree of a TOON body take many times
        // its size in memory
        var limit = request.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IRequestSizeLimitMetadata>() is { } own
            ? own.MaxRequestBodySize ?? long.MaxValue
            : ToonFormatterDefaults.MaxRequestBodySize;

        if (request.ContentLength > limit)
        {
            throw PayloadTooLarge(limit);
        }

        // The wrapper enforces the limit and notes whether the body has more than whitespace
        var body = new LimitedReadStream(request.Body, limit, () => PayloadTooLarge(limit));
        T? value;

        // UTF-8 bodies are checked for content as they are read; others once decoded
        string? content = null;
        bool HasContent() => content == null ? body.HasContent : RequestBody.HasContent(content);

        try
        {
            if (encoding.CodePage == Encoding.UTF8.CodePage)
            {
                value = await readUtf8(body, options, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                content = await RequestBody.ReadTextAsync(body, encoding, cancellationToken).ConfigureAwait(false);
                value = readText(content, options);
            }
        }
        catch (ToonException) when (!HasContent())
        {
            // Whitespace that is not valid TOON (a tab in strict mode) is still a blank body
            return default;
        }
        catch (DecoderFallbackException ex)
        {
            throw new ToonException($"The request body is not valid {encoding.WebName}.", ex);
        }

        return HasContent() ? value : default;
    }

    private static BadHttpRequestException PayloadTooLarge(long limit) =>
        new($"The TOON request body exceeds the maximum allowed size of {limit} bytes.", StatusCodes.Status413PayloadTooLarge);

    /// <summary>The encoding of a request with a TOON content type (UTF-8 when it has no charset).</summary>
    /// <param name="request">The request.</param>
    /// <param name="encoding">The encoding.</param>
    /// <param name="unknownCharset">The charset when the content type is TOON but the charset is unknown; otherwise null.</param>
    private static bool TryGetEncoding(HttpRequest request, out Encoding encoding, out string? unknownCharset)
    {
        encoding = Encoding.UTF8;
        unknownCharset = null;

        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType) ||
            !(contentType.MediaType.Equals(ToonFormatterDefaults.MediaType, StringComparison.OrdinalIgnoreCase) ||
              contentType.MediaType.Equals(ToonFormatterDefaults.TextMediaType, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (!contentType.Charset.HasValue)
        {
            return true;
        }

        try
        {
            encoding = Encoding.GetEncoding(contentType.Charset.Value!.Trim('"'));

            // The encodings of the MVC formatter only: a body that a proxy or firewall in front reads as UTF-8 must not
            // mean something else here (utf-32, an EBCDIC code page)
            if (encoding.CodePage is 65001 or 1200)
            {
                return true;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            // An unknown name, or one .NET knows but does not support (utf-7): the client chose it, so it is not a 500
        }

        encoding = Encoding.UTF8;
        unknownCharset = contentType.Charset.Value;
        return false;
    }
}
