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
            
            var method = typeof(ToonSerializer).GetMethod(nameof(ToonSerializer.DeserializeFromStreamAsync), 
                [typeof(Stream), typeof(ToonSerializerOptions), typeof(CancellationToken)]);
                
            if (method == null)
            {
                return await InputFormatterResult.FailureAsync();
            }

            var genericMethod = method.MakeGenericMethod(context.ModelType);
            
            var task = (Task)genericMethod.Invoke(null, [request.Body, _options, httpContext.RequestAborted])!;
            await task.ConfigureAwait(false);
            
            var resultProperty = task.GetType().GetProperty("Result");
            var model = resultProperty?.GetValue(task);

            return await InputFormatterResult.SuccessAsync(model);
        }
        catch (Exception ex)
        {
            context.ModelState.TryAddModelError(string.Empty, ex.Message);
            return await InputFormatterResult.FailureAsync();
        }
    }
}
