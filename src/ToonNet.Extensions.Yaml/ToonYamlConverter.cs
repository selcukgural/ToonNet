using ToonNet.Core.Models;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace ToonNet.Extensions.Yaml;

/// <summary>
///     Provides bidirectional conversion between YAML and TOON formats.
///     Supports YamlDotNet integration for seamless interoperability.
/// </summary>
/// <remarks>
///     This static class acts as a utility for converting data between YAML and TOON formats.
///     It includes methods for serializing and deserializing TOON documents and values,
///     ensuring compatibility with the YamlDotNet library.
/// </remarks>
public static class ToonYamlConverter
{
    /// <summary>
    ///     Converts a YAML string to a TOON document.
    /// </summary>
    /// <param name="yaml">
    ///     The YAML string to convert. This parameter must not be null.
    /// </param>
    /// <returns>
    ///     A <see cref="ToonDocument"/> representing the YAML data.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the <paramref name="yaml"/> parameter is null.
    /// </exception>
    /// <exception cref="YamlException">
    ///     Thrown when YAML parsing fails due to invalid format or other issues.
    /// </exception>
    /// <remarks>
    ///     This method parses the provided YAML string into a <see cref="YamlStream"/> and converts
    ///     its root node into a <see cref="ToonDocument"/>. If the YAML stream contains no documents,
    ///     an empty <see cref="ToonDocument"/> is returned.
    /// </remarks>
    public static ToonDocument FromYaml(string yaml)
    {
        ArgumentNullException.ThrowIfNull(yaml);

        using var reader = new StringReader(yaml);
        var yamlStream = new YamlStream();
        yamlStream.Load(reader);

        if (yamlStream.Documents.Count == 0)
        {
            return new ToonDocument();
        }

        var rootNode = yamlStream.Documents[0].RootNode;
        var toonValue = ConvertYamlNodeToToonValue(rootNode);
        return new ToonDocument(toonValue);
    }

    /// <summary>
    ///     Converts a TOON document to a YAML string.
    /// </summary>
    /// <param name="document">
    ///     The <see cref="ToonDocument"/> to convert. This parameter must not be null.
    /// </param>
    /// <returns>
    ///     A YAML string representation of the TOON document.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the <paramref name="document"/> parameter is null.
    /// </exception>
    /// <remarks>
    ///     This method serializes the root value of the provided <see cref="ToonDocument"/> into a YAML string.
    /// </remarks>
    public static string ToYaml(ToonDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return ToYaml(document.Root);
    }

    /// <summary>
    ///     Converts a TOON value to a YAML string.
    /// </summary>
    /// <param name="value">
    ///     The TOON value to convert. This parameter must not be null.
    /// </param>
    /// <returns>
    ///     A YAML string representation of the TOON value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when the <paramref name="value"/> parameter is null.
    /// </exception>
    /// <remarks>
    ///     This method uses the YamlDotNet library to serialize the provided TOON value into a YAML string.
    ///     The TOON value is first converted to a plain .NET object using <see cref="ConvertToonValueToObject"/>.
    ///     The resulting object is then serialized into YAML format using a configured serializer.
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var toonValue = new ToonString("example");
    ///     var yamlString = ToonYamlConverter.ToYaml(toonValue);
    ///     Console.WriteLine(yamlString);
    ///     </code>
    /// </example>
    public static string ToYaml(ToonValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var serializer = new SerializerBuilder().WithIndentedSequences().Build();

        var obj = ConvertToonValueToObject(value);
        return serializer.Serialize(obj);
    }

    #region YAML to TOON Conversion

    /// <summary>
    ///     Converts a YamlNode to a ToonValue.
    /// </summary>
    /// <param name="node">
    ///     The YAML node to convert. This parameter must not be null.
    /// </param>
    /// <returns>
    ///     A <see cref="ToonValue"/> representing the YAML node.
    /// </returns>
    /// <exception cref="YamlException">
    ///     Thrown when an unsupported YAML node type is encountered.
    /// </exception>
    /// <remarks>
    ///     This method uses a switch expression to determine the type of the provided YAML node
    ///     and delegates the conversion to the appropriate helper method. Supported node types
    ///     include scalar, mapping, and sequence nodes. If the node type is unsupported, a
    ///     <see cref="YamlException"/> is thrown.
    /// </remarks>
    private static ToonValue ConvertYamlNodeToToonValue(YamlNode node)
    {
        return node switch
        {
            YamlScalarNode scalar   => ConvertYamlScalar(scalar),
            YamlMappingNode mapping => ConvertYamlMapping(mapping),
            YamlSequenceNode seq    => ConvertYamlSequence(seq),
            _                       => throw new YamlException($"Unsupported YAML node type: {node.GetType().Name}")
        };
    }

    /// <summary>
    ///     Converts a YamlScalarNode to a ToonValue.
    /// </summary>
    /// <param name="scalar">
    ///     The YAML scalar node to convert. This parameter must not be null.
    /// </param>
    /// <returns>
    ///     A <see cref="ToonValue"/> representing the scalar value. The scalar can be converted
    ///     to a null, boolean, number, or string value depending on its content.
    /// </returns>
    /// <remarks>
    ///     This method handles special cases for null/empty values, boolean values, and numeric
    ///     values. If the scalar does not match any of these cases, it is treated as a string.
    /// </remarks>
    private static ToonValue ConvertYamlScalar(YamlScalarNode scalar)
    {
        var value = scalar.Value;

        // Handle null/empty
        if (string.IsNullOrEmpty(value) || value == "~" || value == "null")
        {
            return ToonNull.Instance;
        }

        switch (value)
        {
            // Handle booleans
            case "true":
            case "yes":
            case "on":
                return new ToonBoolean(true);
            case "false":
            case "no":
            case "off":
                return new ToonBoolean(false);
        }

        // Handle numbers
        if (double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var number))
        {
            return new ToonNumber(number);
        }

        // Default to string
        return new ToonString(value);
    }

    /// <summary>
    ///     Converts a YamlMappingNode to a ToonObject.
    /// </summary>
    /// <param name="mapping">
    ///     The YAML mapping node to convert. This parameter must not be null.
    /// </param>
    /// <returns>
    ///     A <see cref="ToonObject"/> containing the key-value pairs from the YAML mapping node.
    /// </returns>
    /// <remarks>
    ///     This method iterates through the children of the provided <see cref="YamlMappingNode"/> and converts
    ///     each key-value pair into a corresponding entry in the <see cref="ToonObject"/>. Keys must be scalar nodes
    ///     and non-empty strings; otherwise, they are skipped. Values are recursively converted using
    ///     <see cref="ConvertYamlNodeToToonValue"/>.
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var yamlMapping = new YamlMappingNode
    ///     {
    ///         { new YamlScalarNode("key1"), new YamlScalarNode("value1") },
    ///         { new YamlScalarNode("key2"), new YamlScalarNode("value2") }
    ///     };
    ///     var toonObject = ConvertYamlMapping(yamlMapping);
    ///     Console.WriteLine(toonObject["key1"]); // Outputs: value1
    ///     </code>
    /// </example>
    private static ToonObject ConvertYamlMapping(YamlMappingNode mapping)
    {
        var toonObject = new ToonObject();

        foreach (var entry in mapping.Children)
        {
            if (entry.Key is not YamlScalarNode keyNode)
            {
                continue;
            }

            var key = keyNode.Value;

            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            var value = ConvertYamlNodeToToonValue(entry.Value);
            toonObject[key] = value;
        }

        return toonObject;
    }

    /// <summary>
    ///     Converts a YamlSequenceNode to a ToonArray.
    /// </summary>
    /// <param name="sequence">
    ///     The YAML sequence node to convert. This parameter must not be null.
    /// </param>
    /// <returns>
    ///     A <see cref="ToonArray"/> containing the sequence items. Each item in the sequence
    ///     is converted to a <see cref="ToonValue"/> using the <see cref="ConvertYamlNodeToToonValue"/> method.
    /// </returns>
    /// <remarks>
    ///     This method iterates through the children of the provided <see cref="YamlSequenceNode"/> and converts
    ///     each child node into a corresponding <see cref="ToonValue"/>. The resulting values are added to a
    ///     <see cref="ToonArray"/> which is then returned. This method assumes that the input sequence node is valid
    ///     and does not perform additional validation.
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var yamlSequence = new YamlSequenceNode
    ///     {
    ///         new YamlScalarNode("item1"),
    ///         new YamlScalarNode("item2")
    ///     };
    ///     var toonArray = ConvertYamlSequence(yamlSequence);
    ///     Console.WriteLine(toonArray[0]); // Outputs: item1
    ///     </code>
    /// </example>
    private static ToonArray ConvertYamlSequence(YamlSequenceNode sequence)
    {
        var items = new List<ToonValue>();

        foreach (var child in sequence.Children)
        {
            items.Add(ConvertYamlNodeToToonValue(child));
        }

        return new ToonArray(items);
    }

    #endregion

    #region TOON to YAML Conversion

    /// <summary>
    ///     Converts a ToonValue to a plain .NET object for YamlDotNet serialization.
    /// </summary>
    /// <param name="value">
    ///     The TOON value to convert. This parameter must not be null.
    /// </param>
    /// <returns>
    ///     A plain .NET object representing the TOON value. The returned object can be:
    ///     - null for <see cref="ToonNull"/>
    ///     - a boolean for <see cref="ToonBoolean"/>
    ///     - a double for <see cref="ToonNumber"/>
    ///     - a string for <see cref="ToonString"/>
    ///     - a Dictionary for <see cref="ToonObject"/>
    ///     - a List for <see cref="ToonArray"/>
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the provided TOON value type is unsupported.
    /// </exception>
    /// <remarks>
    ///     This method uses a switch expression to determine the type of the provided TOON value
    ///     and converts it to the corresponding .NET object. Unsupported TOON value types will
    ///     result in an exception.
    /// </remarks>
    private static object? ConvertToonValueToObject(ToonValue value)
    {
        return value switch
        {
            ToonNull      => null,
            ToonBoolean b => b.Value,
            ToonNumber n  => n.Value,
            ToonString s  => s.Value,
            ToonObject o  => ConvertToonObjectToDict(o),
            ToonArray a   => ConvertToonArrayToList(a),
            _             => throw new InvalidOperationException($"Unsupported TOON value type: {value.GetType().Name}")
        };
    }

    /// <summary>
    ///     Converts a ToonObject to a Dictionary for YAML serialization.
    /// </summary>
    /// <param name="obj">
    ///     The TOON object to convert. This parameter must not be null.
    /// </param>
    /// <returns>
    ///     A Dictionary where the keys are strings and the values are plain .NET objects
    ///     representing the properties of the TOON object.
    /// </returns>
    /// <remarks>
    ///     This method iterates through the properties of the provided TOON object and converts
    ///     each property value to a plain .NET object using <see cref="ConvertToonValueToObject"/>.
    /// </remarks>
    private static Dictionary<string, object?> ConvertToonObjectToDict(ToonObject obj)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var (key, value) in obj.Properties)
        {
            dict[key] = ConvertToonValueToObject(value);
        }

        return dict;
    }

    /// <summary>
    ///     Converts a ToonArray to a List for YAML serialization.
    /// </summary>
    /// <param name="array">
    ///     The TOON array to convert. This parameter must not be null.
    /// </param>
    /// <returns>
    ///     A List of plain .NET objects representing the items in the TOON array.
    /// </returns>
    /// <remarks>
    ///     This method iterates through the items of the provided TOON array and converts
    ///     each item to a plain .NET object using <see cref="ConvertToonValueToObject"/>.
    /// </remarks>
    private static List<object?> ConvertToonArrayToList(ToonArray array)
    {
        var list = new List<object?>();

        foreach (var item in array.Items)
        {
            list.Add(ConvertToonValueToObject(item));
        }

        return list;
    }

    #endregion
}