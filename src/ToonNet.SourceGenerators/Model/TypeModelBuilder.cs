using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ToonNet.SourceGenerators.Utilities;

namespace ToonNet.SourceGenerators.Model;

/// <summary>
/// Builds a <see cref="TypeModel"/> from a <c>[ToonSerializable]</c> type symbol, applying the same rules as the
/// reflection-based <c>ToonSerializer</c> (property selection and order, naming, constructor selection).
/// </summary>
internal static class TypeModelBuilder
{
    private const string AttributeNamespace = "ToonNet.Core.Serialization.Attributes.";

    private static readonly SymbolDisplayFormat TypeNameFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static readonly SymbolDisplayFormat TypeOfFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    public static TypeModel Build(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var type = (INamedTypeSymbol)context.TargetSymbol;
        var compilation = context.SemanticModel.Compilation;
        var location = (context.TargetNode as TypeDeclarationSyntax)?.Identifier.GetLocation();
        var diagnostics = new List<DiagnosticInfo>();

        for (var current = type; current != null; current = current.ContainingType)
        {
            if (!IsPartial(current))
            {
                diagnostics.Add(DiagnosticInfo.Create(DiagnosticHelper.InvalidClassStructure, location, current.Name));
                return Empty(type, diagnostics);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        var attribute = context.Attributes[0];
        var properties = CollectProperties(type);

        if (properties.Count == 0)
        {
            diagnostics.Add(DiagnosticInfo.Create(DiagnosticHelper.NoProperties, location, type.Name));
        }

        var canUseUnsafeAccessor = !IsGeneric(type) &&
                                   compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.UnsafeAccessorAttribute") != null;

        var constructor = SelectConstructor(type);

        if (constructor == null && !type.IsAbstract && !type.IsValueType)
        {
            diagnostics.Add(DiagnosticInfo.Create(DiagnosticHelper.NoPublicConstructor, location, type.Name));
        }

        var setsRequiredMembers = constructor != null && constructor.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute");

        var parameters = constructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
        var boundProperties = new HashSet<int>();
        var parameterModels = new List<ParameterModel>();
        var planBuilder = new ValuePlanBuilder(compilation);
        var allowFallback = GetBool(attribute, "AllowReflectionFallback", true);
        var fallbackDescriptor = allowFallback ? DiagnosticHelper.ReflectionFallback : DiagnosticHelper.ReflectionFallbackNotAllowed;

        foreach (var parameter in parameters)
        {
            var index = properties.FindIndex(p => string.Equals(p.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                boundProperties.Add(index);
            }

            // A parameter bound to a property with [ToonConverter] is read by the converter, like in ToonSerializer.
            // ... and a parameter of the same type as its property is reported with the property, not twice.
            var reasons = new List<string>();
            var reportedWithProperty = index >= 0 && (HasConverter(properties[index]) ||
                                                      SymbolEqualityComparer.Default.Equals(parameter.Type, properties[index].Type));
            var plan = planBuilder.Build(parameter.Type, reportedWithProperty ? [] : reasons);
            ReportFallback(diagnostics, fallbackDescriptor, reasons, $"constructor parameter '{parameter.Name}'", type, parameter.Locations, location);

            parameterModels.Add(new ParameterModel(parameter.Name, parameter.Type.ToDisplayString(TypeNameFormat), index, FormatDefaultValue(parameter), plan));
        }

        var propertyModels = properties.Select((property, index) =>
        {
            var reasons = new List<string>();
            var plan = planBuilder.Build(property.Type, HasConverter(property) ? [] : reasons);
            ReportFallback(diagnostics, fallbackDescriptor, reasons, $"property '{property.Name}'", type, property.Locations, location);

            return CreatePropertyModel(property, plan, type, compilation, canUseUnsafeAccessor, boundProperties.Contains(index), setsRequiredMembers);
        }).ToImmutableArray();

        var namingPolicy = attribute.NamedArguments.FirstOrDefault(a => a.Key == "NamingPolicy").Value.Value as int?;

        // A struct without a usable public constructor is still created with `new T()`, like Activator.CreateInstance.
        var constructorModel = constructor != null
            ? new ConstructorModel(new EquatableArray<ParameterModel>(parameterModels.ToImmutableArray()), setsRequiredMembers)
            : type.IsValueType ? new ConstructorModel(default, false) : null;

        return new TypeModel(
            Emit: true,
            Namespace: type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(),
            ContainingTypeDeclarations: ContainingDeclarations(type),
            Declaration: Declaration(type),
            FullyQualifiedName: type.ToDisplayString(TypeNameFormat),
            TypeOfName: type.ToDisplayString(TypeOfFormat),
            HintName: HintName(type),
            IsValueType: type.IsValueType,
            PublicMethods: GetBool(attribute, "GeneratePublicMethods", true),
            NullChecks: GetBool(attribute, "IncludeNullChecks", true),
            Documentation: GetBool(attribute, "IncludeDocumentation", true),
            CanUseUnsafeAccessor: canUseUnsafeAccessor,
            CheckRuntimeType: !type.IsValueType && !type.IsSealed,
            HasTypeConverter: type.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == AttributeNamespace + "ToonConverterAttribute"),
            FixedNamingPolicy: namingPolicy,
            Properties: new EquatableArray<PropertyModel>(propertyModels),
            Constructor: type.IsAbstract ? null : constructorModel,
            Diagnostics: new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutableArray()));
    }

    private static TypeModel Empty(INamedTypeSymbol type, List<DiagnosticInfo> diagnostics)
    {
        return new TypeModel(false, null, default, Declaration(type), type.ToDisplayString(TypeNameFormat), type.ToDisplayString(TypeOfFormat),
                             HintName(type), type.IsValueType, true, true, true, false, false, false, null, default, null,
                             new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutableArray()));
    }

    internal static bool IsPartial(INamedTypeSymbol type)
    {
        return type.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is TypeDeclarationSyntax declaration &&
                                                       declaration.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    private static bool IsGeneric(INamedTypeSymbol type)
    {
        for (var current = type; current != null; current = current.ContainingType)
        {
            if (current.TypeParameters.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Public instance properties with a public getter that are not indexers and not marked <c>[ToonIgnore]</c>,
    /// ordered by <c>[ToonPropertyOrder]</c> (default 0), then base-class properties before derived ones, then
    /// declaration order. Overridden and hidden base properties appear once.
    /// </summary>
    private static List<IPropertySymbol> CollectProperties(INamedTypeSymbol type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var collected = new List<(IPropertySymbol Property, int Order, int Position)>();

        for (var current = type; current != null; current = current.BaseType)
        {
            var depth = InheritanceDepth(current);
            var index = 0;

            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (property.IsStatic || property.IsIndexer || property.GetMethod?.DeclaredAccessibility != Accessibility.Public ||
                    property.Type.IsRefLikeType || property.Type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer ||
                    property.ReturnsByRef || property.ReturnsByRefReadonly || !seen.Add(property.Name))
                {
                    continue;
                }

                if (FindAttribute(property, "ToonIgnoreAttribute") != null)
                {
                    continue;
                }

                var order = FindAttribute(property, "ToonPropertyOrderAttribute") is { ConstructorArguments.Length: > 0 } orderAttribute &&
                            orderAttribute.ConstructorArguments[0].Value is int value
                    ? value
                    : 0;

                collected.Add((property, order, depth * 100_000 + index++));
            }
        }

        return collected.OrderBy(p => p.Order).ThenBy(p => p.Position).Select(p => p.Property).ToList();
    }

    private static int InheritanceDepth(INamedTypeSymbol type)
    {
        var depth = 0;

        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            depth++;
        }

        return depth;
    }

    /// <summary>Finds a ToonNet attribute on a property or on the property it overrides (reflection inherits them).</summary>
    private static AttributeData? FindAttribute(IPropertySymbol property, string name)
    {
        for (var current = property; current != null; current = current.OverriddenProperty)
        {
            var attribute = current.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == AttributeNamespace + name);

            if (attribute != null)
            {
                return attribute;
            }
        }

        return null;
    }

    /// <summary>
    /// A <c>[ToonConstructor]</c> constructor, otherwise the public parameterless one, otherwise the public constructor
    /// with the most parameters (e.g. the primary constructor of a positional record).
    /// </summary>
    internal static IMethodSymbol? SelectConstructor(INamedTypeSymbol type)
    {
        if (type.IsAbstract)
        {
            return null;
        }

        // Reflection does not see the implicit parameterless constructor of a struct.
        var constructors = type.InstanceConstructors
                               .Where(c => c.DeclaredAccessibility == Accessibility.Public &&
                                           !(type.IsValueType && c.IsImplicitlyDeclared && c.Parameters.Length == 0))
                               .ToList();

        return constructors.FirstOrDefault(c => c.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == AttributeNamespace + "ToonConstructorAttribute"))
               ?? constructors.FirstOrDefault(c => c.Parameters.Length == 0)
               ?? constructors.OrderByDescending(c => c.Parameters.Length).FirstOrDefault();
    }

    private static bool HasConverter(IPropertySymbol property) => FindAttribute(property, "ToonConverterAttribute") != null;

    /// <summary>Reports TOON006 (or TOON007 when fallback is not allowed) for a member whose value uses reflection.</summary>
    private static void ReportFallback(List<DiagnosticInfo> diagnostics, DiagnosticDescriptor descriptor, List<string> reasons, string member,
                                       INamedTypeSymbol type, ImmutableArray<Location> memberLocations, Location? typeLocation)
    {
        if (reasons.Count == 0)
        {
            return;
        }

        var location = memberLocations.FirstOrDefault(l => l.IsInSource) ?? typeLocation;
        diagnostics.Add(DiagnosticInfo.Create(descriptor, location, member, type.Name, string.Join("; ", reasons.Distinct())));
    }

    private static PropertyModel CreatePropertyModel(IPropertySymbol property, ValuePlan plan, INamedTypeSymbol type, Compilation compilation,
                                                     bool canUseUnsafeAccessor, bool boundToConstructor, bool setsRequiredMembers)
    {
        var propertyType = property.Type;

        var attributeName = FindAttribute(property, "ToonPropertyAttribute") is { ConstructorArguments.Length: > 0 } nameAttribute
            ? nameAttribute.ConstructorArguments[0].Value as string
            : null;

        var names = attributeName != null
            ? ImmutableArray.Create(attributeName, attributeName, attributeName, attributeName)
            : ImmutableArray.Create(property.Name, ToCamelCase(property.Name), ToSnakeCase(property.Name), property.Name.ToLowerInvariant());

        var converter = FindAttribute(property, "ToonConverterAttribute") is { ConstructorArguments.Length: > 0 } converterAttribute &&
                        converterAttribute.ConstructorArguments[0].Value is ITypeSymbol converterType
            ? converterType.ToDisplayString(TypeOfFormat)
            : null;

        var setter = FindSetter(property);
        var write = GetWriteKind(property, setter, type, compilation, canUseUnsafeAccessor, boundToConstructor, setsRequiredMembers);

        return new PropertyModel(
            Name: property.Name,
            TypeName: propertyType.ToDisplayString(TypeNameFormat),
            TypeOfName: propertyType.ToDisplayString(TypeOfFormat),
            Names: new EquatableArray<string>(names),
            Plan: plan,
            ConverterTypeName: converter,
            IsReadOnly: setter == null,
            Write: write,
            SetterOwner: write == WriteKind.Accessor ? setter!.ContainingType.ToDisplayString(TypeOfFormat) : null,
            SetterOwnerIsValueType: setter?.ContainingType.IsValueType ?? false);
    }

    /// <summary>The setter of the property, or of the property it overrides when the override only redefines the getter.</summary>
    private static IMethodSymbol? FindSetter(IPropertySymbol property)
    {
        for (var current = property; current != null; current = current.OverriddenProperty)
        {
            if (current.SetMethod != null)
            {
                return current.SetMethod;
            }
        }

        return null;
    }

    private static WriteKind GetWriteKind(IPropertySymbol property, IMethodSymbol? setter, INamedTypeSymbol type, Compilation compilation,
                                          bool canUseUnsafeAccessor, bool boundToConstructor, bool setsRequiredMembers)
    {
        if (setter == null)
        {
            return WriteKind.None;
        }

        var accessible = compilation.IsSymbolAccessibleWithin(setter, type);

        // The compiler insists on required members in the object initializer unless the constructor sets them.
        if (property.IsRequired && !setsRequiredMembers)
        {
            return WriteKind.Initializer;
        }

        if (boundToConstructor)
        {
            return WriteKind.None;
        }

        if (accessible && !setter.IsInitOnly)
        {
            return WriteKind.Assign;
        }

        if (canUseUnsafeAccessor && !setter.ContainingType.IsGenericType)
        {
            return WriteKind.Accessor;
        }

        // Generic types cannot use [UnsafeAccessor] on .NET 8; an init-only property is then always initialized.
        return accessible ? WriteKind.Initializer : WriteKind.None;
    }

    // Same algorithms as ToonSerializer.ToCamelCase / ToSnakeCase, so both produce identical keys.
    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
        {
            return name;
        }

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var result = new StringBuilder();
        result.Append(char.ToLowerInvariant(name[0]));

        for (var i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(name[i]));
            }
            else
            {
                result.Append(name[i]);
            }
        }

        return result.ToString();
    }

    /// <summary>The C# expression for a parameter's default value, used when the document has no value for it.</summary>
    private static string FormatDefaultValue(IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue || parameter.ExplicitDefaultValue is not { } value)
        {
            return "default!";
        }

        var type = parameter.Type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
            ? nullable.TypeArguments[0]
            : parameter.Type;
        var typeName = type.ToDisplayString(TypeOfFormat);

        return value switch
        {
            string s => SymbolDisplay.FormatLiteral(s, true),
            char c => SymbolDisplay.FormatLiteral(c, true),
            bool b => b ? "true" : "false",
            float f when float.IsNaN(f) => "float.NaN",
            float f when float.IsPositiveInfinity(f) => "float.PositiveInfinity",
            float f when float.IsNegativeInfinity(f) => "float.NegativeInfinity",
            float f => f.ToString("R", CultureInfo.InvariantCulture) + "F",
            double d when double.IsNaN(d) => "double.NaN",
            double d when double.IsPositiveInfinity(d) => "double.PositiveInfinity",
            double d when double.IsNegativeInfinity(d) => "double.NegativeInfinity",
            double d => d.ToString("R", CultureInfo.InvariantCulture) + "D",
            decimal m => m.ToString(CultureInfo.InvariantCulture) + "M",
            IFormattable number => $"({typeName})({number.ToString(null, CultureInfo.InvariantCulture)})",
            _ => "default!"
        };
    }

    private static bool GetBool(AttributeData attribute, string name, bool defaultValue)
    {
        return attribute.NamedArguments.FirstOrDefault(a => a.Key == name).Value.Value is bool value ? value : defaultValue;
    }

    private static string Declaration(INamedTypeSymbol type)
    {
        var keyword = type switch
        {
            { IsRecord: true, IsValueType: true } => "record struct",
            { IsRecord: true } => "record",
            { TypeKind: TypeKind.Struct } => "struct",
            { TypeKind: TypeKind.Interface } => "interface",
            _ => "class"
        };

        var typeParameters = type.TypeParameters.Length > 0 ? "<" + string.Join(", ", type.TypeParameters.Select(t => t.Name)) + ">" : "";

        return $"partial {keyword} {type.Name}{typeParameters}";
    }

    private static EquatableArray<string> ContainingDeclarations(INamedTypeSymbol type)
    {
        var declarations = new List<string>();

        for (var current = type.ContainingType; current != null; current = current.ContainingType)
        {
            declarations.Insert(0, Declaration(current));
        }

        return new EquatableArray<string>(declarations.ToImmutableArray());
    }

    private static string HintName(INamedTypeSymbol type)
    {
        var name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
        var builder = new StringBuilder(name.Length);

        foreach (var c in name)
        {
            builder.Append(char.IsLetterOrDigit(c) || c is '.' or '_' ? c : '_');
        }

        return builder.Append(".g.cs").ToString();
    }
}
