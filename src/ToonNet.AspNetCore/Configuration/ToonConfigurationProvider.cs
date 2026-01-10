using System.Globalization;
using Microsoft.Extensions.Configuration;
using ToonNet.Core;
using ToonNet.Core.Models;
using ToonNet.Core.Parsing;

namespace ToonNet.AspNetCore.Configuration;

/// <summary>
/// Provides a configuration source for TOON files, allowing configuration
/// settings to be loaded into an application from a TOON-formatted file.
/// </summary>
public sealed class ToonConfigurationProvider : FileConfigurationProvider
{
    /// <summary>
    /// Represents a set of configuration options used for parsing TOON configuration files.
    /// </summary>
    private readonly ToonOptions _options;

    /// <summary>
    /// Provides configuration using the TOON file format.
    /// </summary>
    public ToonConfigurationProvider(ToonConfigurationSource source, ToonOptions? options = null) : base(source)
    {
        _options = options ?? ToonOptions.Default;
    }

    /// <summary>
    /// Loads configuration data from the provided stream.
    /// </summary>
    /// <param name="stream">The input stream containing the configuration data.</param>
    public override void Load(Stream stream)
    {
        try
        {
            var parser = new ToonParser(_options);
            // We use the sync Parse method as configuration loading is typically synchronous,
            // But we need to read the stream content first
            
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            
            var doc = parser.Parse(content);
            Data = Flatten(doc);
        }
        catch (ToonParseException ex)
        {
            throw new FormatException($"Failed to parse TOON configuration file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Flattens a ToonDocument into a dictionary of key-value pairs.
    /// </summary>
    /// <param name="doc">The ToonDocument to flatten. This represents the structured configuration data.</param>
    /// <returns>
    /// A dictionary where keys represent the flattened paths of the configuration properties
    /// and values represent the corresponding values as strings or null.
    /// </returns>
    private static Dictionary<string, string?> Flatten(ToonDocument doc)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        
        if (doc.Root is ToonObject rootObj)
        {
            VisitObject(rootObj, string.Empty, data);
        }
        
        return data;
    }

    /// <summary>
    /// Processes a TOON object by visiting its properties and adding flattened key-value pairs to the provided dictionary.
    /// </summary>
    /// <param name="obj">The TOON object to process.</param>
    /// <param name="prefix">The prefix to prepend to the keys of the object's properties.</param>
    /// <param name="data">The dictionary where flattened key-value pairs will be stored.</param>
    private static void VisitObject(ToonObject obj, string prefix, Dictionary<string, string?> data)
    {
        foreach (var property in obj.Properties)
        {
            var key = string.IsNullOrEmpty(prefix) 
                ? property.Key 
                : ConfigurationPath.Combine(prefix, property.Key);
            
            VisitValue(property.Value, key, data);
        }
    }

    /// <summary>
    /// Visits the elements of a <see cref="ToonArray"/> and processes each
    /// element while maintaining a hierarchical key structure.
    /// </summary>
    /// <param name="array">The array to be visited.</param>
    /// <param name="prefix">The key prefix to be used for the elements in the array.</param>
    /// <param name="data">The dictionary to store key-value pairs generated from the array elements.</param>
    private static void VisitArray(ToonArray array, string prefix, Dictionary<string, string?> data)
    {
        for (var i = 0; i < array.Count; i++)
        {
            var key = ConfigurationPath.Combine(prefix, i.ToString());
            VisitValue(array[i], key, data);
        }
    }

    /// <summary>
    /// Processes a ToonValue instance and adds its data representation to the given dictionary.
    /// </summary>
    /// <param name="value">The ToonValue to be processed.</param>
    /// <param name="key">The configuration key associated with the value.</param>
    /// <param name="data">
    /// A dictionary where the processed configuration data will be stored.
    /// Keys and corresponding values are added to this dictionary based on the structure of the ToonValue.
    /// </param>
    private static void VisitValue(ToonValue value, string key, Dictionary<string, string?> data)
    {
        switch (value)
        {
            case ToonObject obj:
                VisitObject(obj, key, data);
                break;
                
            case ToonArray array:
                VisitArray(array, key, data);
                break;
                
            case ToonString str:
                data[key] = str.Value;
                break;
                
            case ToonNumber num:
                data[key] = num.Value.ToString(CultureInfo.InvariantCulture); // Or use invariant culture if needed
                break;
                
            case ToonBoolean boolean:
                data[key] = boolean.Value.ToString();
                break;
                
            case ToonNull:
                data[key] = null;
                break;
        }
    }
}
