using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ToonNet.SourceGenerators.Analysis;

/// <summary>
///     Analyzes Roslyn symbols to extract serialization metadata.
/// </summary>
internal sealed class SymbolAnalyzer(Compilation compilation)
{
    private readonly INamedTypeSymbol? _toonConstructorAttr = compilation.GetTypeByMetadataName("ToonNet.Core.Serialization.Attributes.ToonConstructorAttribute");
    private readonly INamedTypeSymbol? _toonConverterAttr = compilation.GetTypeByMetadataName("ToonNet.Core.Serialization.Attributes.ToonConverterAttribute");
    private readonly INamedTypeSymbol? _toonIgnoreAttr = compilation.GetTypeByMetadataName("ToonNet.Core.Serialization.Attributes.ToonIgnoreAttribute");
    private readonly INamedTypeSymbol? _toonPropertyAttr = compilation.GetTypeByMetadataName("ToonNet.Core.Serialization.Attributes.ToonPropertyAttribute");
    private readonly INamedTypeSymbol? _toonPropertyOrderAttr = compilation.GetTypeByMetadataName("ToonNet.Core.Serialization.Attributes.ToonPropertyOrderAttribute");
    private readonly INamedTypeSymbol? _toonSerializableAttr = compilation.GetTypeByMetadataName("ToonNet.Core.Serialization.Attributes.ToonSerializableAttribute");

    /// <summary>
    ///     Analyzes a class symbol and extracts serialization metadata.
    /// </summary>
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
    ///     Extracts all serializable properties from a class.
    /// </summary>
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
    ///     Gets the custom property name from [ToonProperty("name")] attribute.
    /// </summary>
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
    ///     Gets the custom converter type from [ToonConverter(typeof(...))] attribute.
    /// </summary>
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
    ///     Gets the property order from [ToonPropertyOrder(order)] attribute.
    /// </summary>
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
    ///     Gets the [ToonSerializable] attribute from a class.
    /// </summary>
    private static AttributeData? GetToonSerializableAttribute(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "ToonSerializableAttribute");
    }

    /// <summary>
    ///     Checks if a symbol has a specific attribute.
    /// </summary>
    private static bool HasAttribute(ISymbol symbol, INamedTypeSymbol? attributeType)
    {
        return attributeType is not null && symbol.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType));
    }

    /// <summary>
    ///     Finds an attribute by type.
    /// </summary>
    private static AttributeData? FindAttribute(ISymbol symbol, INamedTypeSymbol? attributeType)
    {
        return attributeType is null ? null : symbol.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType));
    }

    /// <summary>
    ///     Finds the constructor marked with the [ToonConstructor] attribute.
    /// </summary>
    private IMethodSymbol? FindCustomConstructor(INamedTypeSymbol classSymbol)
    {
        return classSymbol.Constructors.FirstOrDefault(c => HasAttribute(c, _toonConstructorAttr));
    }
}