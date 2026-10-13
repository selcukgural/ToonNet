using System.Text.Json;
using ToonNet.Core.Serialization;
using ToonNet.Shared;

namespace ToonNet.Tests.Serialization;

/// <summary>
///     The naming policies follow System.Text.Json, so acronyms stay together.
/// </summary>
public class ToonNamingTests
{
    public static TheoryData<string> Names => new()
    {
        "FirstName", "URL", "URLValue", "HTTPServer", "IOStream", "ID", "UserID", "Address2Line", "Version2", "X", "x",
        "already_snake", "_private", "ABCDef", "MyHTTPRequestV2", "Ünïcode", "IPhone", ""
    };

    [Theory]
    [MemberData(nameof(Names))]
    public void CamelCase_MatchesSystemTextJson(string name)
    {
        Assert.Equal(JsonNamingPolicy.CamelCase.ConvertName(name), ToonNaming.ToCamelCase(name));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void SnakeCase_MatchesSystemTextJson(string name)
    {
        Assert.Equal(JsonNamingPolicy.SnakeCaseLower.ConvertName(name), ToonNaming.ToSnakeCase(name));
    }

    [Fact]
    public void Serializer_KeepsAcronymsTogether()
    {
        var value = new { HTTPServer = 1, URLValue = 2 };

        Assert.Equal("http_server: 1\nurl_value: 2",
                     ToonSerializer.Serialize(value, new ToonSerializerOptions { PropertyNamingPolicy = PropertyNamingPolicy.SnakeCase }));
        Assert.Equal("httpServer: 1\nurlValue: 2",
                     ToonSerializer.Serialize(value, new ToonSerializerOptions { PropertyNamingPolicy = PropertyNamingPolicy.CamelCase }));
    }
}
