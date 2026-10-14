using System.Text;
using Microsoft.AspNetCore.Http;
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
/// is decoded strictly. The size of the body is limited by the server (<c>MaxRequestBodySize</c>, or
/// <c>[RequestSizeLimit]</c> on the endpoint). A body with only whitespace reads as the default value, because an empty
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
    /// <returns>The value, or the default value when the document is <c>null</c> or the body is blank.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the request does not have a TOON content type or its charset is unknown.</exception>
    /// <exception cref="ToonException">Thrown when the body is not valid TOON or does not fit <typeparamref name="T"/>.</exception>
    public static ValueTask<T?> ReadFromToonAsync<T>(this HttpRequest request, CancellationToken cancellationToken = default) =>
        ReadFromToonAsync<T>(request, options: null, cancellationToken);

    /// <summary>Reads the request body as TOON.</summary>
    /// <typeparam name="T">The type to read.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="options">The serializer options; the application's <c>IOptions&lt;ToonSerializerOptions&gt;</c> when null.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The value, or the default value when the document is <c>null</c> or the body is blank.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the request does not have a TOON content type or its charset is unknown.</exception>
    /// <exception cref="ToonException">Thrown when the body is not valid TOON or does not fit <typeparamref name="T"/>.</exception>
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
    /// <returns>The value, or the default value when the document is <c>null</c> or the body is blank.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the request does not have a TOON content type or its charset is unknown.</exception>
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
    /// <returns>The value, or the default value of <paramref name="type"/> when the document is <c>null</c> or the body is blank.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the request does not have a TOON content type or its charset is unknown.</exception>
    /// <exception cref="ToonException">Thrown when the body is not valid TOON or does not fit <paramref name="type"/>.</exception>
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

        // The server limits the size; the wrapper notes whether the body has more than whitespace
        var body = new LimitedReadStream(request.Body, long.MaxValue, static () => new InvalidOperationException());
        T? value;

        try
        {
            value = encoding.CodePage == Encoding.UTF8.CodePage
                ? await readUtf8(body, options, cancellationToken).ConfigureAwait(false)
                : readText(await RequestBody.ReadTextAsync(body, encoding, cancellationToken).ConfigureAwait(false), options);
        }
        catch (ToonException) when (!body.HasContent)
        {
            // Whitespace that is not valid TOON (a tab in strict mode) is still a blank body
            return default;
        }
        catch (DecoderFallbackException ex)
        {
            throw new ToonException($"The request body is not valid {encoding.WebName}.", ex);
        }

        return body.HasContent ? value : default;
    }

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
            return true;
        }
        catch (ArgumentException)
        {
            unknownCharset = contentType.Charset.Value;
            return false;
        }
    }
}
