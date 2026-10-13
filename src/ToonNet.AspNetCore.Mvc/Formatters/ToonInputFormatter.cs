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
    private const int ReadBufferSize = 16 * 1024;

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
        string content;

        try
        {
            content = await ReadBodyAsync(httpContext.Request, encoding, httpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (DecoderFallbackException)
        {
            // Ill-formed UTF-8 is an error, not text with U+FFFD in it (TOON spec §4)
            context.ModelState.TryAddModelError(context.ModelName, "The request body is not valid UTF-8.");
            return await InputFormatterResult.FailureAsync().ConfigureAwait(false);
        }

        try
        {
            var model = ToonSerializer.Deserialize(content, context.ModelType, _options);
            return await InputFormatterResult.SuccessAsync(model).ConfigureAwait(false);
        }
        catch (ToonException ex)
        {
            context.ModelState.TryAddModelError(context.ModelName, ex.Message);
            return await InputFormatterResult.FailureAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads the request body as text, rejecting bodies larger than the configured limit.
    /// </summary>
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private async Task<string> ReadBodyAsync(HttpRequest request, Encoding encoding, CancellationToken cancellationToken)
    {
        if (request.ContentLength > _maxRequestBodySize)
        {
            throw CreatePayloadTooLargeException();
        }

        // Decode while reading instead of copying the body into a MemoryStream first; the wrapper enforces the limit
        var body = new LimitedReadStream(request.Body, _maxRequestBodySize, CreatePayloadTooLargeException);
        // UTF-8 bodies are decoded strictly, so ill-formed bytes throw instead of becoming U+FFFD
        var decoder = encoding.CodePage == Encoding.UTF8.CodePage ? StrictUtf8 : encoding;
        using var reader = new StreamReader(body, decoder, detectEncodingFromByteOrderMarks: true, ReadBufferSize, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     A read-only view of a stream that throws once more than a given number of bytes has been read.
    /// </summary>
    private sealed class LimitedReadStream(Stream inner, long limit, Func<Exception> createLimitException) : Stream
    {
        private long _read;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => Count(inner.Read(buffer, offset, count));

        public override int Read(Span<byte> buffer) => Count(inner.Read(buffer));

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            Count(await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false));

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private int Count(int read)
        {
            _read += read;
            return _read > limit ? throw createLimitException() : read;
        }
    }

    private BadHttpRequestException CreatePayloadTooLargeException()
    {
        return new BadHttpRequestException($"The TOON request body exceeds the maximum allowed size of {_maxRequestBodySize} bytes.",
                                           StatusCodes.Status413PayloadTooLarge);
    }
}
