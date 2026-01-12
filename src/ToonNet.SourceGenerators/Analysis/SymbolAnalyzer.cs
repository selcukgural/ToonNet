using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ToonNet.SourceGenerators.Analysis;

/// <summary>
/// Analyzes Roslyn symbols to extract relevant serialization metadata used by the source generator.
/// </summary>
internal sealed class SymbolAnalyzer(Compilation compilation)
{
    /// <summary>
    /// Represents the Roslyn symbol for the [ToonConstructor] attribute used to identify
    /// constructors explicitly marked for serialization in the ToonNet framework.
    /// </summary>
    /// <remarks>
    /// This field holds the result of resolving the "ToonNet.Core.Serialization.Attributes.ToonConstructorAttribute"
    /// metadata type symbol from the provided compilation. It is used to locate and analyze constructors
    /// in classes that have been marked with this attribute.
    /// </remarks>
    private readonly INamedTypeSymbol? _toonConstructorAttr = compilation.GetTypeByMetadataName("ToonNet.Core.Serialization.Attributes.ToonConstructorAttribute");

    /// <summary>
    /// Represents the Roslyn symbol for the [ToonConverter] attribute, used to identify the custom converter type
    /// specified in the attribute's constructor arguments. Typically, this attribute decorates a property to provide
    /// a custom serialization or deserialization logic.
    /// </summary>
    /// <remarks>
    /// - The attribute is expected to reside in the namespace: ToonNet.Core.Serialization.Attributes.
    /// - The type is resolved at runtime using metadata name resolution through Roslyn's `Compilation.GetTypeByMetadataName`.
    /// - It is primarily utilized by methods such as <see cref="GetCustomConverter"/> to analyze and extract converter type information.
    /// </remarks>
    private readonly INamedTypeSymbol? _toonConverterAttr = compilation.GetTypeByMetadataName("ToonNet.Core.Serialization.Attributes.ToonConverterAttribute");

    /// <summary>
    /// Represents the metadata symbol corresponding to the ToonIgnoreAttribute type.
    /// This attribute is used to specify that certain properties or fields are
    /// to be ignored during serialization or related analysis processes. If the
    /// symbol cannot be resolved during compilation, this field will be null.
    /// </summary>
    private readonly INamedTypeSymbol? _toonIgnoreAttr = compilation.GetTypeByMetadataName("ToonNet.Core.Serialization.Attributes.ToonIgnoreAttribute");

    /// <summary>
    /// Represents a cached type symbol reference to the `ToonPropertyAttribute`
    /// used for identifying and analyzing custom property attributes in the analyzed class.
    /// </summary>
    /// <remarks>
    /// This variable holds an instance of `INamedTypeSymbol` resolved using the Roslyn compilation API.
    /// It's utilized to locate `ToonPropertyAttribute` and extract its metadata during code analysis.
    /// If the attribute type is not accessible in the compilation context, this value is null.
    /// </remarks>
    private readonly INamedTypeSymbol? _toonPropertyAttr = compilation.GetTypeByMetadataName("ToonNet.Core.Serialization.Attributes.ToonPropertyAttribute");

    /// <summary>
    /// Represents the Roslyn symbol for the
    /// "ToonNet.Core.Serialization.Attributes.ToonPropertyOrderAttribute" type.
    /// This attribute is used for specifying the order of properties during serialization
    /// in the ToonNet framework.
    /// </summary>
    private readonly INamedTypeSymbol? _toonPropertyOrderAttr = compilation.GetTypeByMetadataName("ToonNet.Core.Serialization.Attributes.ToonPropertyOrderAttribute");

    /// <summary>
    /// Represents the symbol of the ToonSerializableAttribute type, used to mark classes or
    /// members for serialization in the ToonNet framework. This symbol is extracted from the
    /// compilation metadata to identify applicable serialization targets during analysis.
    /// </summary>
    private readonly INamedTypeSymbol? _toonSerializableAttr = compilation.GetTypeByMetadataName("ToonNet.Core.Serialization.Attributes.ToonSerializableAttribute");

    /// <summary>
    /// Analyzes a class symbol and extracts serialization metadata.
    /// </summary>
    /// <param name="classSymbol">
    /// The symbol representing the class to be analyzed.
    /// </param>
    /// <param name="syntax">
    /// The syntax node of the class declaration, if available; otherwise, null.
    /// </param>
    /// <returns>
    /// A <see cref="ClassInfo"/> instance containing metadata about the analyzed class.
    /// </returns>
    public ClassInfo AnalyzeClass(INamedTypeSymbol classSymbol, ClassDeclarationSyntax? syntax)
    {
        var properties = ExtractProperties(classSymbol);
        var attribute = GetToonSerializableAttribute(classSymbol);
        var isPartial = syntax?.Modifiers.Any(m => m.Text == "partial") ?? false;
        var customConstructor = FindCustomConstructor(classSymbol);

        return new ClassInfo
        {
            Name = classSymbol.Name,
            Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
            Symbol = classSymbol,
            Properties = properties,
            Attribute = attribute,
            IsPartial = isPartial,
            CustomConstructor = customConstructor
        };
    }

    /// <summary>
    /// Extracts all serializable properties from a class symbol.
    /// </summary>
    /// <param name="classSymbol">
    /// The class symbol containing the properties to be analyzed for serialization.
    /// </param>
    /// <returns>
    /// An immutable array of <see cref="PropertyInfo"/> containing metadata about the serializable properties
    /// in the class.
    /// </returns>
    private ImmutableArray<PropertyInfo> ExtractProperties(INamedTypeSymbol classSymbol)
    {
        var properties = new List<PropertyInfo>();

        foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            // Skip properties without getter and setter
            if (member.GetMethod is null || member.SetMethod is null)
            {
                continue;
            }

            var isIgnored = HasAttribute(member, _toonIgnoreAttr);
            var customConverter = GetCustomConverter(member);
            var isNestedSerializable = HasAttribute(member.Type, _toonSerializableAttr);

            var info = new PropertyInfo
            {
                Name = member.Name,
                Symbol = member,
                Type = member.Type,
                CustomName = GetCustomPropertyName(member),
                Order = GetPropertyOrder(member),
                IsIgnored = isIgnored,
                CustomConverter = customConverter,
                IsNestedSerializable = isNestedSerializable
            };

            properties.Add(info);
        }

        return properties.Where(p => !p.IsIgnored).OrderBy(p => p.Order).ThenBy(p => p.Name).ToImmutableArray();
    }

    /// <summary>
    /// Retrieves the custom property name defined in the [ToonProperty("name")] attribute of a property.
    /// </summary>
    /// <param name="prop">
    /// The property symbol to analyze for custom property name attributes.
    /// </param>
    /// <returns>
    /// The custom property name specified in the attribute, or null if no custom name is defined.
    /// </returns>
    private string? GetCustomPropertyName(IPropertySymbol prop)
    {
        var attr = FindAttribute(prop, _toonPropertyAttr);

        if (!(attr?.ConstructorArguments.Length > 0))
        {
            return null;
        }

        var value = attr.ConstructorArguments[0].Value;
        return value as string;

    }

    /// <summary>
    /// Gets the custom converter type from the <c>ToonConverterAttribute</c> applied to a given property.
    /// </summary>
    /// <param name="prop">
    /// The property symbol to inspect for the <c>ToonConverterAttribute</c>.
    /// </param>
    /// <returns>
    /// The type symbol representing the custom converter specified in the attribute,
    /// or <c>null</c> if no valid converter is specified.
    /// </returns>
    private ITypeSymbol? GetCustomConverter(IPropertySymbol prop)
    {
        var attr = FindAttribute(prop, _toonConverterAttr);

        if (!(attr?.ConstructorArguments.Length > 0))
        {
            return null;
        }

        var value = attr.ConstructorArguments[0].Value;

        if (value is ITypeSymbol converterType)
        {
            return converterType;
        }

        return null;
    }

    /// <summary>
    /// Retrieves the property order specified by the [ToonPropertyOrder] attribute on a given property symbol.
    /// </summary>
    /// <param name="prop">
    /// The property symbol from which to retrieve the order value.
    /// </param>
    /// <returns>
    /// The order value specified in the [ToonPropertyOrder] attribute, or <see cref="int.MaxValue"/> if the attribute
    /// is not applied or the value is not set.
    /// </returns>
    private int GetPropertyOrder(IPropertySymbol prop)
    {
        var attr = FindAttribute(prop, _toonPropertyOrderAttr);

        if (!(attr?.ConstructorArguments.Length > 0))
        {
            return int.MaxValue;
        }

        var value = attr.ConstructorArguments[0].Value;

        if (value is int order)
        {
            return order;
        }

        return int.MaxValue;
    }

    /// <summary>
    /// Retrieves the [ToonSerializable] attribute from the specified class symbol, if it exists.
    /// </summary>
    /// <param name="classSymbol">
    /// The class symbol from which to extract the ToonSerializable attribute.
    /// </param>
    /// <returns>
    /// An <see cref="Microsoft.CodeAnalysis.AttributeData"/> representing the [ToonSerializable] attribute,
    /// or null if the attribute is not present on the class symbol.
    /// </returns>
    private static AttributeData? GetToonSerializableAttribute(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "ToonSerializableAttribute");
    }

    /// <summary>
    /// Checks if a symbol has a specific attribute.
    /// </summary>
    /// <param name="symbol">
    /// The symbol to inspect for the attribute.
    /// </param>
    /// <param name="attributeType">
    /// The type of the attribute to check for.
    /// If null, the method will return false.
    /// </param>
    /// <returns>
    /// True if the symbol has the specified attribute; otherwise, false.
    /// </returns>
    private static bool HasAttribute(ISymbol symbol, INamedTypeSymbol? attributeType)
    {
        return attributeType is not null && symbol.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType));
    }

    /// <summary>
    /// Finds an attribute of the specified type applied to the given symbol.
    /// </summary>
    /// <param name="symbol">
    /// The symbol to inspect for an attribute.
    /// </param>
    /// <param name="attributeType">
    /// The type of the attribute to find.
    /// </param>
    /// <returns>
    /// An instance of <see cref="AttributeData"/> representing the found attribute, or null if no matching attribute is found.
    /// </returns>
    private static AttributeData? FindAttribute(ISymbol symbol, INamedTypeSymbol? attributeType)
    {
        return attributeType is null ? null : symbol.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType));
    }

    /// <summary>
    /// Finds the constructor in a class symbol that is marked with the [ToonConstructor] attribute.
    /// </summary>
    /// <param name="classSymbol">
    /// The class symbol to search for a custom constructor.
    /// </param>
    /// <returns>
    /// The constructor symbol marked with the [ToonConstructor] attribute, or null if no such constructor is found.
    /// </returns>
    private IMethodSymbol? FindCustomConstructor(INamedTypeSymbol classSymbol)
    {
        return classSymbol.Constructors.FirstOrDefault(c => HasAttribute(c, _toonConstructorAttr));
    }
}