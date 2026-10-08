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

        Assert.Equal(ToonFormatterDefaults.MediaType, httpContext.Response.ContentType);

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

    private class TestModel
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }
}
