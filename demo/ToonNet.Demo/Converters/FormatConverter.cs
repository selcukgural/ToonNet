using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ToonNet.Core.Serialization;

namespace ToonNet.Demo.Converters;

/// <summary>
/// Handles conversions between TOON, JSON, and YAML formats
/// </summary>
public static class FormatConverter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    /// <summary>
    /// Converts object to TOON format
    /// </summary>
    public static string ToToon<T>(T obj)
    {
        return ToonSerializer.Serialize(obj);
    }

    /// <summary>
    /// Converts object to JSON format
    /// </summary>
    public static string ToJson<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, JsonOptions);
    }

    /// <summary>
    /// Converts object to YAML format
    /// </summary>
    public static string ToYaml<T>(T obj)
    {
        return YamlSerializer.Serialize(obj);
    }

    /// <summary>
    /// Converts TOON to JSON
    /// </summary>
    public static string ToonToJson<T>(string toonString)
    {
        var obj = ToonSerializer.Deserialize<T>(toonString);
        return ToJson(obj);
    }

    /// <summary>
    /// Converts TOON to YAML
    /// </summary>
    public static string ToonToYaml<T>(string toonString)
    {
        var obj = ToonSerializer.Deserialize<T>(toonString);
        return ToYaml(obj);
    }

    /// <summary>
    /// Converts JSON to TOON
    /// </summary>
    public static string JsonToToon<T>(string jsonString)
    {
        var obj = JsonSerializer.Deserialize<T>(jsonString, JsonOptions);
        return ToToon(obj);
    }

    /// <summary>
    /// Converts YAML to TOON
    /// </summary>
    public static string YamlToToon<T>(string yamlString)
    {
        var obj = YamlDeserializer.Deserialize<T>(yamlString);
        return ToToon(obj);
    }

    /// <summary>
    /// Converts JSON to YAML
    /// </summary>
    public static string JsonToYaml<T>(string jsonString)
    {
        var obj = JsonSerializer.Deserialize<T>(jsonString, JsonOptions);
        return ToYaml(obj);
    }

    /// <summary>
    /// Converts YAML to JSON
    /// </summary>
    public static string YamlToJson<T>(string yamlString)
    {
        var obj = YamlDeserializer.Deserialize<T>(yamlString);
        return ToJson(obj);
    }
}
