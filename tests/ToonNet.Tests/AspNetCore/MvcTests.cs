using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using ToonNet.AspNetCore.DependencyInjection;
using ToonNet.AspNetCore.Mvc.DependencyInjection;
using ToonNet.AspNetCore.Mvc.Formatters;
using ToonNet.AspNetCore.Mvc.Http;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.AspNetCore;

public class MvcTests
{
    [Fact]
    public async Task ToonInputFormatter_ReadsBody_ReturnsObject()
    {
        var options = new ToonSerializerOptions();
        var formatter = new ToonInputFormatter(options);

        var content = """
                      Name: Test
                      Age: 10
                      """;
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Body = stream,
                ContentType = ToonFormatterDefaults.MediaType
            }
        };

        var context = new InputFormatterContext(
            httpContext,
            nameof(TestModel),
            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary(),
            new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider().GetMetadataForType(typeof(TestModel)),
            (s, e) => new StreamReader(s, e)
        );

        var result = await formatter.ReadAsync(context);

        Assert.False(result.HasError);
        var model = Assert.IsType<TestModel>(result.Model);
        Assert.Equal("Test", model.Name);
        Assert.Equal(10, model.Age);
    }

    [Fact]
    public async Task ToonOutputFormatter_WritesBody_CorrectContentType()
    {
        var options = new ToonSerializerOptions();
        var formatter = new ToonOutputFormatter(options);

        var model = new TestModel { Name = "Output", Age = 20 };
        var httpContext = new DefaultHttpContext();
        var stream = new MemoryStream();
        httpContext.Response.Body = stream;

        var context = new OutputFormatterWriteContext(
            httpContext,
            (s, e) => new StreamWriter(s, e, leaveOpen: true),
            typeof(TestModel),
            model
        );

        await formatter.WriteAsync(context);

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        Assert.Contains("Name: Output", content);
        Assert.Contains("Age: 20", content);
    }

    [Fact]
    public async Task ToonResult_ExecuteAsync_WritesToResponse()
    {
        var model = new TestModel { Name = "Minimal", Age = 30 };
        var result = new ToonResult(model);
        
        var httpContext = new DefaultHttpContext();
        var stream = new MemoryStream();
        httpContext.Response.Body = stream;

        // Mock DI for Options
        var services = new ServiceCollection();
        services.AddOptions();
        httpContext.RequestServices = services.BuildServiceProvider();

        await result.ExecuteAsync(httpContext);

        Assert.Equal("application/toon; charset=utf-8", httpContext.Response.ContentType);

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        Assert.Contains("Name: Minimal", content);
        Assert.Contains("Age: 30", content);
    }

    private static InputFormatterContext CreateInputContext(byte[] body, string contentType, long? contentLength = null)
    {
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Body = new MemoryStream(body),
                ContentType = contentType,
                ContentLength = contentLength
            }
        };

        return new InputFormatterContext(
            httpContext,
            nameof(TestModel),
            new ModelStateDictionary(),
            new EmptyModelMetadataProvider().GetMetadataForType(typeof(TestModel)),
            (s, e) => new StreamReader(s, e));
    }

    [Fact]
    public async Task ToonInputFormatter_BodyOverLimit_Throws413()
    {
        var formatter = new ToonInputFormatter(new ToonSerializerOptions(), maxRequestBodySize: 64);
        var body = System.Text.Encoding.UTF8.GetBytes("Name: " + new string('x', 100));

        var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => formatter.ReadAsync(CreateInputContext(body, ToonFormatterDefaults.MediaType)));

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
    }

    [Fact]
    public async Task ToonInputFormatter_BodyExactlyAtLimit_IsRead()
    {
        var body = System.Text.Encoding.UTF8.GetBytes("Name: " + new string('x', 58));
        var atLimit = new ToonInputFormatter(new ToonSerializerOptions(), maxRequestBodySize: body.Length);
        var belowLimit = new ToonInputFormatter(new ToonSerializerOptions(), maxRequestBodySize: body.Length - 1);

        var result = await atLimit.ReadAsync(CreateInputContext(body, ToonFormatterDefaults.MediaType));

        Assert.False(result.HasError);
        Assert.Equal(new string('x', 58), ((TestModel)result.Model!).Name);
        await Assert.ThrowsAsync<BadHttpRequestException>(() => belowLimit.ReadAsync(CreateInputContext(body, ToonFormatterDefaults.MediaType)));
    }

    [Fact]
    public async Task ToonInputFormatter_DeclaredContentLengthOverLimit_Throws413BeforeReading()
    {
        var formatter = new ToonInputFormatter(new ToonSerializerOptions(), maxRequestBodySize: 64);
        var body = System.Text.Encoding.UTF8.GetBytes("Name: a");

        var ex = await Assert.ThrowsAsync<BadHttpRequestException>(
            () => formatter.ReadAsync(CreateInputContext(body, ToonFormatterDefaults.MediaType, contentLength: 1_000)));

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
    }

    [Fact]
    public void ToonInputFormatter_DefaultLimit_IsFourMegabytes()
    {
        Assert.Equal(4 * 1024 * 1024, ToonFormatterDefaults.MaxRequestBodySize);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ToonInputFormatter(new ToonSerializerOptions(), 0));
    }

    [Fact]
    public async Task ToonInputFormatter_DeeplyNestedBody_ReturnsModelErrorInsteadOfCrashing()
    {
        var sb = new System.Text.StringBuilder();

        for (var i = 0; i < 2_000; i++)
        {
            sb.Append(' ', i * 2).Append("k:\n");
        }

        var formatter = new ToonInputFormatter(new ToonSerializerOptions(), maxRequestBodySize: 16 * 1024 * 1024);
        var context = CreateInputContext(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), ToonFormatterDefaults.MediaType);

        var result = await formatter.ReadAsync(context);

        Assert.True(result.HasError);
        Assert.False(context.ModelState.IsValid);
    }

    [Fact]
    public async Task ToonInputFormatter_IllFormedUtf8_ReturnsModelError()
    {
        // 0xC3 starts a two-byte sequence that 0x28 does not continue (TOON spec §4: an error, not U+FFFD)
        var formatter = new ToonInputFormatter(new ToonSerializerOptions());
        var body = new byte[] { (byte)'N', (byte)'a', (byte)'m', (byte)'e', (byte)':', (byte)' ', 0xC3, 0x28 };
        var context = CreateInputContext(body, ToonFormatterDefaults.MediaType);

        var result = await formatter.ReadAsync(context);

        Assert.True(result.HasError);
        Assert.Contains("not valid UTF-8", context.ModelState[nameof(TestModel)]!.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task ToonInputFormatter_Utf8Body_WithByteOrderMark_IsRead()
    {
        var formatter = new ToonInputFormatter(new ToonSerializerOptions());
        byte[] body = [0xEF, 0xBB, 0xBF, .. System.Text.Encoding.UTF8.GetBytes("Name: Çağrı 😀\nAge: 5")];

        var result = await formatter.ReadAsync(CreateInputContext(body, ToonFormatterDefaults.MediaType));

        Assert.False(result.HasError);
        var model = Assert.IsType<TestModel>(result.Model);
        Assert.Equal("Çağrı 😀", model.Name);
        Assert.Equal(5, model.Age);
    }

    [Fact]
    public async Task ToonInputFormatter_LargeUtf8Body_IsReadInChunks()
    {
        // Larger than the read buffer, so the body is decoded in several reads
        var formatter = new ToonInputFormatter(new ToonSerializerOptions());
        var name = string.Concat(Enumerable.Repeat("ğüşİöç😀", 5_000));
        var body = System.Text.Encoding.UTF8.GetBytes($"Name: {name}\nAge: 7");

        var result = await formatter.ReadAsync(CreateInputContext(body, ToonFormatterDefaults.MediaType));

        Assert.False(result.HasError);
        Assert.Equal(name, Assert.IsType<TestModel>(result.Model).Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n\n")]
    [InlineData("  \r\n\t")]
    [InlineData("\uFEFF")]
    public async Task ToonInputFormatter_BlankBody_IsNoValue_LikeAnEmptyBody(string body)
    {
        // Without Content-Length (a chunked request), the formatter sees the body; whitespace binds nothing
        var formatter = new ToonInputFormatter(new ToonSerializerOptions());
        var bytes = System.Text.Encoding.UTF8.GetBytes(body);

        var result = await formatter.ReadRequestBodyAsync(CreateInputContext(bytes, ToonFormatterDefaults.MediaType), System.Text.Encoding.UTF8);
        Assert.False(result.IsModelSet);
        Assert.False(result.HasError);

        var lenient = new InputFormatterContext(CreateInputContext(bytes, ToonFormatterDefaults.MediaType).HttpContext, nameof(TestModel),
                                                new ModelStateDictionary(), new EmptyModelMetadataProvider().GetMetadataForType(typeof(TestModel)),
                                                (s, e) => new StreamReader(s, e), treatEmptyInputAsDefaultValue: true);
        var defaulted = await formatter.ReadRequestBodyAsync(lenient, System.Text.Encoding.UTF8);
        Assert.True(defaulted.IsModelSet);
        Assert.Null(defaulted.Model);
    }

    [Fact]
    public async Task ToonInputFormatter_IllFormedUtf16_ReturnsModelError()
    {
        var formatter = new ToonInputFormatter(new ToonSerializerOptions());
        byte[] body = [(byte)'N', 0, (byte)':', 0, (byte)' ', 0, 0x00, 0xD8]; // "N: " and a lone high surrogate
        var context = CreateInputContext(body, ToonFormatterDefaults.MediaType + "; charset=utf-16");

        var result = await formatter.ReadAsync(context);

        Assert.True(result.HasError);
        Assert.Contains("not valid utf-16", context.ModelState[nameof(TestModel)]!.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task ToonInputFormatter_Utf16Body_UsesRequestEncoding()
    {
        var formatter = new ToonInputFormatter(new ToonSerializerOptions());
        var body = System.Text.Encoding.Unicode.GetBytes("Name: Çağrı\nAge: 5");

        var result = await formatter.ReadAsync(CreateInputContext(body, ToonFormatterDefaults.MediaType + "; charset=utf-16"));

        Assert.False(result.HasError);
        var model = Assert.IsType<TestModel>(result.Model);
        Assert.Equal("Çağrı", model.Name);
        Assert.Equal(5, model.Age);
    }

    [Fact]
    public async Task ToonOutputFormatter_Utf16Selected_WritesUtf16()
    {
        var formatter = new ToonOutputFormatter(new ToonSerializerOptions());
        var httpContext = new DefaultHttpContext();
        var stream = new MemoryStream();
        httpContext.Response.Body = stream;

        var context = new OutputFormatterWriteContext(
            httpContext,
            (s, e) => new StreamWriter(s, e, leaveOpen: true),
            typeof(TestModel),
            new TestModel { Name = "Çağrı", Age = 1 });

        await formatter.WriteResponseBodyAsync(context, System.Text.Encoding.Unicode);

        var content = System.Text.Encoding.Unicode.GetString(stream.ToArray()).TrimStart('\uFEFF');
        Assert.Contains("Name: Çağrı", content);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToonOutputFormatter_Utf8Selected_StreamsTheSerializerOutput(bool nullValue)
    {
        var formatter = new ToonOutputFormatter(new ToonSerializerOptions());
        var httpContext = new DefaultHttpContext();
        var stream = new MemoryStream();
        httpContext.Response.Body = stream;
        var model = nullValue ? null : new TestModel { Name = "Çağrı 😀 " + new string('x', 40_000), Age = 1 };

        // UTF-8 output goes straight to the body, without the text writer
        var context = new OutputFormatterWriteContext(httpContext, (_, _) => throw new InvalidOperationException("writer used"),
                                                      typeof(TestModel), model);

        await formatter.WriteResponseBodyAsync(context, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var expected = nullValue ? "null" : ToonSerializer.Serialize(model);
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes(expected), stream.ToArray());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddToonFormatters_UsesTheSharedSerializerOptions(bool configureThroughAddToonNet)
    {
        var services = new ServiceCollection();

        if (configureThroughAddToonNet)
        {
            services.AddToonNet(configureToonOptions: null, configureSerializerOptions: o => o.PropertyNamingPolicy = PropertyNamingPolicy.CamelCase);
            services.AddMvc().AddToonFormatters();
        }
        else
        {
            services.AddMvc().AddToonFormatters(o => o.PropertyNamingPolicy = PropertyNamingPolicy.CamelCase);
        }

        await using var provider = services.BuildServiceProvider();
        var mvcOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions>>().Value;
        var formatter = Assert.Single(mvcOptions.OutputFormatters.OfType<ToonOutputFormatter>());
        Assert.Single(mvcOptions.InputFormatters.OfType<ToonInputFormatter>());

        var httpContext = new DefaultHttpContext { RequestServices = provider };
        var stream = new MemoryStream();
        httpContext.Response.Body = stream;
        var model = new TestModel { Name = "Shared", Age = 1 };

        await formatter.WriteAsync(new OutputFormatterWriteContext(httpContext, (s, e) => new StreamWriter(s, e, leaveOpen: true), typeof(TestModel), model));
        // The test writer factory (StreamWriter with Encoding.UTF8) adds a BOM; MVC's own writer does not
        var formatterOutput = System.Text.Encoding.UTF8.GetString(stream.ToArray()).TrimStart('\uFEFF');

        // ToonResult resolves the same options from DI
        var resultStream = new MemoryStream();
        httpContext.Response.Body = resultStream;
        await new ToonResult(model).ExecuteAsync(httpContext);

        Assert.Equal("name: Shared\nage: 1", formatterOutput);
        Assert.Equal(formatterOutput, System.Text.Encoding.UTF8.GetString(resultStream.ToArray()));
    }

    private static async Task<string> ExecuteToonResultAsync(object? value, ToonSerializerOptions? options = null)
    {
        var httpContext = new DefaultHttpContext { RequestServices = new ServiceCollection().AddOptions().BuildServiceProvider() };
        var stream = new MemoryStream();
        httpContext.Response.Body = stream;

        await new ToonResult(value, options).ExecuteAsync(httpContext);

        Assert.Equal("application/toon; charset=utf-8", httpContext.Response.ContentType);
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static async IAsyncEnumerable<TestModel> ModelsAsync()
    {
        for (var i = 1; i <= 2; i++)
        {
            await Task.Yield();
            yield return new TestModel { Name = $"n{i}", Age = i };
        }
    }

    [Fact]
    public async Task AsyncSequences_AreWrittenAsArrays()
    {
        var expected = ToonSerializer.Serialize(new List<TestModel> { new() { Name = "n1", Age = 1 }, new() { Name = "n2", Age = 2 } });

        Assert.Equal(expected, await ExecuteToonResultAsync(ModelsAsync()));

        var formatter = new ToonOutputFormatter(new ToonSerializerOptions());
        var httpContext = new DefaultHttpContext();
        var stream = new MemoryStream();
        httpContext.Response.Body = stream;
        var context = new OutputFormatterWriteContext(httpContext, (s, e) => new StreamWriter(s, e, leaveOpen: true),
                                                      typeof(IAsyncEnumerable<TestModel>), ModelsAsync());

        await formatter.WriteResponseBodyAsync(context, System.Text.Encoding.UTF8);
        Assert.Equal(expected, System.Text.Encoding.UTF8.GetString(stream.ToArray()));

        // The serializer itself does not write an asynchronous sequence as an empty object
        Assert.Throws<ToonNet.Core.ToonSerializationException>(() => ToonSerializer.Serialize(ModelsAsync()));
        Assert.Throws<ToonNet.Core.ToonSerializationException>(() => ToonSerializer.Serialize(new { Items = ModelsAsync() }));
    }

    [Fact]
    public async Task ToonResult_StatusCode_IsWritten()
    {
        var httpContext = new DefaultHttpContext { RequestServices = new ServiceCollection().AddOptions().BuildServiceProvider() };
        httpContext.Response.Body = new MemoryStream();

        var result = (ToonResult)Results.Extensions.Toon(new TestModel { Name = "New", Age = 1 }, StatusCodes.Status201Created);
        await result.ExecuteAsync(httpContext);

        Assert.Equal(201, httpContext.Response.StatusCode);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("application/toon; charset=utf-8", result.ContentType);
        Assert.IsType<TestModel>(result.Value);
        Assert.Null(new ToonResult(null).StatusCode);
    }

    [Fact]
    public async Task ToonResult_NullValue_WritesToonNull()
    {
        // An empty body would read back as an empty object
        Assert.Equal("null", await ExecuteToonResultAsync(null));
        Assert.Equal("null", await ExecuteToonResultAsync(null, new ToonSerializerOptions { IgnoreNullValues = true }));
        Assert.Null(ToonSerializer.Deserialize<TestModel>("null"));
    }

    [Fact]
    public async Task ToonOutputFormatter_NullObject_WritesToonNull()
    {
        var formatter = new ToonOutputFormatter(new ToonSerializerOptions());
        var httpContext = new DefaultHttpContext();
        var stream = new MemoryStream();
        httpContext.Response.Body = stream;

        var context = new OutputFormatterWriteContext(httpContext, (s, e) => new StreamWriter(s, e, leaveOpen: true), typeof(TestModel), null);
        await formatter.WriteResponseBodyAsync(context, new System.Text.UTF8Encoding(false));

        Assert.Equal("null", System.Text.Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Theory]
    [InlineData("text/toon", "text/toon; charset=utf-8")]
    [InlineData("application/toon", "application/toon; charset=utf-8")]
    [InlineData("text/toon, application/toon", "text/toon; charset=utf-8")]
    public async Task ToonOutputFormatter_AcceptHeader_SelectsToonMediaType(string accept, string expectedContentType)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvc(o => o.ReturnHttpNotAcceptable = true).AddToonFormatters();
        await using var provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = provider };
        httpContext.Request.Headers.Accept = accept;
        var body = new MemoryStream();
        httpContext.Response.Body = body;

        var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(),
                                                                       new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        await new Microsoft.AspNetCore.Mvc.ObjectResult(new TestModel { Name = "A", Age = 1 }).ExecuteResultAsync(actionContext);

        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal(expectedContentType, httpContext.Response.ContentType);
        Assert.Equal("Name: A\nAge: 1", System.Text.Encoding.UTF8.GetString(body.ToArray()));
    }

    private class TestModel
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }
}
