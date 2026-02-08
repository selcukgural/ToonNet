using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
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
            (s, e) => new StreamWriter(s, e),
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

    private class TestModel
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }
}
