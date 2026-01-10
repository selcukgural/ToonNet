using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace ToonNet.SourceGenerators.Analysis;

/// <summary>
/// Analyzes Roslyn symbols to extract serialization metadata.
/// </summary>
internal sealed class SymbolAnalyzer
{
    private readonly Compilation _compilation;
    private readonly INamedTypeSymbol? _toonSerializableAttr;
    private readonly INamedTypeSymbol? _toonIgnoreAttr;
    private readonly INamedTypeSymbol? _toonPropertyAttr;
    private readonly INamedTypeSymbol? _toonPropertyOrderAttr;

    public SymbolAnalyzer(Compilation compilation)
    {
        _compilation = compilation;
        _toonSerializableAttr = compilation.GetTypeByMetadataName(
            "ToonNet.Core.Serialization.Attributes.ToonSerializableAttribute");
        _toonIgnoreAttr = compilation.GetTypeByMetadataName(
            "ToonNet.Core.Serialization.Attributes.ToonIgnoreAttribute");
        _toonPropertyAttr = compilation.GetTypeByMetadataName(
            "ToonNet.Core.Serialization.Attributes.ToonPropertyAttribute");
        _toonPropertyOrderAttr = compilation.GetTypeByMetadataName(
            "ToonNet.Core.Serialization.Attributes.ToonPropertyOrderAttribute");
    }

    /// <summary>
    /// Analyzes a class symbol and extracts serialization metadata.
    /// </summary>
    public ClassInfo AnalyzeClass(INamedTypeSymbol classSymbol, ClassDeclarationSyntax? syntax)
    {
        var properties = ExtractProperties(classSymbol);
        var attribute = GetToonSerializableAttribute(classSymbol);
        var isPartial = syntax?.Modifiers.Any(m => m.Text == "partial") ?? false;

        return new ClassInfo
        {
            Name = classSymbol.Name,
            Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
            Symbol = classSymbol,
            Properties = properties,
            Attribute = attribute,
            IsPartial = isPartial
        };
    }

    /// <summary>
    /// Extracts all serializable properties from a class.
    /// </summary>
    private ImmutableArray<PropertyInfo> ExtractProperties(INamedTypeSymbol classSymbol)
    {
        var properties = new List<PropertyInfo>();

        foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            // Skip properties without getter and setter
            if (member.GetMethod is null || member.SetMethod is null)
                continue;

            var isIgnored = HasAttribute(member, _toonIgnoreAttr);
            
            var info = new PropertyInfo
            {
                Name = member.Name,
                Symbol = member,
                Type = member.Type,
                CustomName = GetCustomPropertyName(member),
                Order = GetPropertyOrder(member),
                IsIgnored = isIgnored
            };

            properties.Add(info);
        }

        return properties
            .Where(p => !p.IsIgnored)
            .OrderBy(p => p.Order)
            .ThenBy(p => p.Name)
            .ToImmutableArray();
    }

    /// <summary>
    /// Gets the custom property name from [ToonProperty("name")] attribute.
    /// </summary>
    private string? GetCustomPropertyName(IPropertySymbol prop)
    {
        var attr = FindAttribute(prop, _toonPropertyAttr);
        if (attr?.ConstructorArguments.Length > 0)
        {
            var value = attr.ConstructorArguments[0].Value;
            return value as string;
        }
        return null;
    }

    /// <summary>
    /// Gets the property order from [ToonPropertyOrder(order)] attribute.
    /// </summary>
    private int GetPropertyOrder(IPropertySymbol prop)
    {
        var attr = FindAttribute(prop, _toonPropertyOrderAttr);
        if (attr?.ConstructorArguments.Length > 0)
        {
            var value = attr.ConstructorArguments[0].Value;
            if (value is int order)
                return order;
        }
        return int.MaxValue;
    }

    /// <summary>
    /// Gets the [ToonSerializable] attribute from a class.
    /// </summary>
    private AttributeData? GetToonSerializableAttribute(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ToonSerializableAttribute");
    }

    /// <summary>
    /// Checks if a symbol has a specific attribute.
    /// </summary>
    private bool HasAttribute(ISymbol symbol, INamedTypeSymbol? attributeType)
    {
        return attributeType is not null && symbol.GetAttributes()
            .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType));
    }

    /// <summary>
    /// Finds an attribute by type.
    /// </summary>
    private AttributeData? FindAttribute(ISymbol symbol, INamedTypeSymbol? attributeType)
    {
        if (attributeType is null) return null;
        return symbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType));
    }
}
