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

        // Shapes generated code cannot have: instances (static classes), boxing and generic arguments (ref structs), or
        // a name the generated file can refer to (file-local types)
        var unsupported = type.IsStatic ? "a static class has no instances to serialize"
            : type.IsRefLikeType ? "a ref struct cannot be boxed or used as a type argument"
            : type.IsFileLocal ? "a file-local type cannot be named from the generated file"
            : null;

        if (unsupported != null)
        {
            diagnostics.Add(DiagnosticInfo.Create(DiagnosticHelper.UnsupportedTypeKind, location, type.Name, unsupported));
            return Empty(type, diagnostics);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var attribute = context.Attributes[0];
        var properties = CollectProperties(type);

        if (properties.Count == 0)
        {
            diagnostics.Add(DiagnosticInfo.Create(DiagnosticHelper.NoProperties, location, type.Name));
        }

        foreach (var property in properties)
        {
            if (FindAttribute(property, "ToonPropertyAttribute") is { ConstructorArguments.Length: > 0 } nameAttribute &&
                nameAttribute.ConstructorArguments[0].Value is string name && Shared.ToonKeys.HasUnpairedSurrogate(name))
            {
                diagnostics.Add(DiagnosticInfo.Create(DiagnosticHelper.InvalidPropertyName, location, property.Name));
                return Empty(type, diagnostics);
            }
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
            var plan = planBuilder.Build(Typed(parameter.Type, compilation), reportedWithProperty ? [] : reasons);
            ReportFallback(diagnostics, fallbackDescriptor, reasons, $"constructor parameter '{parameter.Name}'", type, parameter.Locations, location);

            parameterModels.Add(new ParameterModel(parameter.Name, Typed(parameter.Type, compilation).ToDisplayString(TypeNameFormat), index,
                                                   FormatDefaultValue(parameter), plan,
                                                   PassIn: parameter.RefKind is RefKind.In or RefKind.RefReadOnlyParameter));
        }

        var propertyModels = properties.Select((property, index) =>
        {
            var reasons = new List<string>();
            var plan = planBuilder.Build(Typed(property.Type, compilation), HasConverter(property) ? [] : reasons);
            ReportFallback(diagnostics, fallbackDescriptor, reasons, $"property '{property.Name}'", type, property.Locations, location);

            var model = CreatePropertyModel(property, plan, type, compilation, canUseUnsafeAccessor, boundProperties.Contains(index), setsRequiredMembers);

            // A property set in the object initializer gets a value even without a key, which would replace what the
            // object had (an initializer, a value set by the constructor): such an object is left to the
            // reflection-based reader, which leaves the property as it was
            return model.Write == WriteKind.Initializer && allowFallback ? model with { KeepsInitializer = true } : model;
        }).ToImmutableArray();

        var unsetRequiredHasInitializer = false;
        var unsetRequired = setsRequiredMembers ? [] : UnsetRequiredMembers(type, properties, out unsetRequiredHasInitializer);

        var hasTypeConverter = type.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == AttributeNamespace + "ToonConverterAttribute");
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
            HasTypeConverter: hasTypeConverter,
            RegisterWithSerializer: !hasTypeConverter && CanRegister(type, compilation),
            TypeInfoProperty: !hasTypeConverter && CanRegister(type, compilation) ? GetTypeInfoProperty(type, compilation)
                : type.GetMembers("ToonTypeInfo").IsEmpty && GetTypeInfoProperty(type, compilation) == TypeInfoProperty.EmitNew ? TypeInfoProperty.Hide
                : TypeInfoProperty.None,
            EmitSerializeMethod: !MethodNameTaken(type, "Serialize", type, compilation, diagnostics, location),
            EmitDeserializeMethod: !MethodNameTaken(type, "Deserialize", compilation.GetTypeByMetadataName("ToonNet.Core.Models.ToonDocument"), compilation,
                                                    diagnostics, location),
            FixedNamingPolicy: namingPolicy,
            Properties: new EquatableArray<PropertyModel>(propertyModels),
            Constructor: type.IsAbstract ? null : constructorModel,
            Diagnostics: new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutableArray()),
            UnsetRequired: new EquatableArray<string>(unsetRequired.ToImmutableArray()),
            UnsetRequiredHasInitializer: unsetRequiredHasInitializer && allowFallback);
    }

    /// <summary>
    ///     The <c>required</c> members generated code does not read (fields, ignored properties, properties without a
    ///     public getter), which the object initializer must still set: to their default, as reflection leaves them.
    /// </summary>
    private static List<string> UnsetRequiredMembers(INamedTypeSymbol type, List<IPropertySymbol> properties, out bool hasInitializer)
    {
        var read = new HashSet<string>(properties.Select(p => p.Name), StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var unset = new List<string>();
        hasInitializer = false;

        for (var current = type; current != null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                var required = member switch
                {
                    IPropertySymbol { IsRequired: true } property => !read.Contains(property.Name),
                    IFieldSymbol { IsRequired: true } => true,
                    _ => false
                };

                if (!required || !seen.Add(member.Name))
                {
                    continue;
                }

                unset.Add(EscapeKeyword(member.Name));
                hasInitializer |= member.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is PropertyDeclarationSyntax { Initializer: not null } or
                                                                                VariableDeclaratorSyntax { Initializer: not null });
            }
        }

        return unset;
    }

    private static TypeModel Empty(INamedTypeSymbol type, List<DiagnosticInfo> diagnostics)
    {
        return new TypeModel(false, null, default, Declaration(type), type.ToDisplayString(TypeNameFormat), type.ToDisplayString(TypeOfFormat),
                             HintName(type), type.IsValueType, true, true, true, false, false, false, false, TypeInfoProperty.None, false, false, null,
                             default, null,
                             new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutableArray()));
    }

    internal static bool IsPartial(INamedTypeSymbol type)
    {
        return type.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is TypeDeclarationSyntax declaration &&
                                                       declaration.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    /// <summary>
    /// Whether a module initializer can register the generated methods with <c>ToonSerializer</c>: the type must not
    /// be generic, must be reachable from the module (no private or protected type in the chain), and the referenced
    /// ToonNet.Core must have the registration API.
    /// </summary>
    private static bool CanRegister(INamedTypeSymbol type, Compilation compilation)
    {
        if (IsGeneric(type) || compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ModuleInitializerAttribute") == null)
        {
            return false;
        }

        for (var current = type; current != null; current = current.ContainingType)
        {
            if (current.IsFileLocal || current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal))
            {
                return false;
            }
        }

        return compilation.GetTypeByMetadataName("ToonNet.Core.Serialization.ToonSourceGenerationHelpers")?.GetMembers("Register").Length > 0;
    }

    /// <summary>
    /// Whether the static <c>ToonTypeInfo</c> property can be generated: ToonNet.Core must have the type, and no member of
    /// the type or a base type may take the name, except the property generated for a <c>[ToonSerializable]</c> base
    /// type, which the property hides.
    /// </summary>
    private static TypeInfoProperty GetTypeInfoProperty(INamedTypeSymbol type, Compilation compilation)
    {
        const string name = "ToonTypeInfo";

        if (compilation.GetTypeByMetadataName("ToonNet.Core.Serialization.ToonTypeInfo`1") == null || !type.GetMembers(name).IsEmpty)
        {
            return TypeInfoProperty.None;
        }

        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            var serializable = current.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == AttributeNamespace + "ToonSerializableAttribute");

            // A base type in this compilation gets the property from this generator run, so it is not among its members yet
            if (serializable && current.Locations.Any(l => l.IsInSource) && CanRegister(current, compilation))
            {
                return TypeInfoProperty.EmitNew;
            }

            if (!current.GetMembers(name).IsEmpty)
            {
                return serializable ? TypeInfoProperty.EmitNew : TypeInfoProperty.None;
            }
        }

        return TypeInfoProperty.Emit;
    }

    /// <summary>
    /// Whether a member takes the name of a generated static method, which would not compile: one that is not a method
    /// (a property, field, event or nested type), or a method with the generated method's parameters
    /// (<paramref name="firstParameter"/>, <c>ToonSerializerOptions</c>); reports TOON009 then.
    /// </summary>
    private static bool MethodNameTaken(INamedTypeSymbol type, string name, ITypeSymbol? firstParameter, Compilation compilation,
                                        List<DiagnosticInfo> diagnostics, Location? location)
    {
        var options = compilation.GetTypeByMetadataName("ToonNet.Core.Serialization.ToonSerializerOptions");
        var sameSignature = type.GetMembers(name).OfType<IMethodSymbol>().Any(method =>
            method.Parameters.Length == 2 && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, firstParameter) &&
            SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated), options));

        if (!sameSignature && type.GetMembers(name).All(member => member is IMethodSymbol) && type.GetTypeMembers(name).IsEmpty)
        {
            return false;
        }

        diagnostics.Add(DiagnosticInfo.Create(DiagnosticHelper.GeneratedMethodNameTaken, location, type.Name, name));
        return true;
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
                // A name C# cannot spell (from another language's assembly) is left out: it would go into generated code
                if (!property.CanBeReferencedByName || !SyntaxFacts.IsValidIdentifier(property.Name) ||
                    property.IsStatic || property.IsIndexer || property.GetMethod?.DeclaredAccessibility != Accessibility.Public ||
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

        // Reflection does not see the implicit parameterless constructor of a struct; a constructor with a ref struct
        // parameter (Span<T>) cannot be called with values read from a document
        var all = type.InstanceConstructors.Where(c => !(type.IsValueType && c.IsImplicitlyDeclared && c.Parameters.Length == 0) &&
                                                       !c.Parameters.Any(p => p.Type.IsRefLikeType || p.RefKind is RefKind.Ref or RefKind.Out ||
                                                                             p.Type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer)).ToList();
        var constructors = all.Where(c => c.DeclaredAccessibility == Accessibility.Public).ToList();

        // [ToonConstructor] may mark a constructor of any accessibility (generated code is a member of the type)
        return all.FirstOrDefault(c => c.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == AttributeNamespace + "ToonConstructorAttribute"))
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
        var propertyType = Typed(property.Type, compilation);

        var attributeName = FindAttribute(property, "ToonPropertyAttribute") is { ConstructorArguments.Length: > 0 } nameAttribute
            ? nameAttribute.ConstructorArguments[0].Value as string
            : null;

        var names = attributeName != null
            ? ImmutableArray.Create(attributeName, attributeName, attributeName, attributeName)
            : ImmutableArray.Create(property.Name, Shared.ToonNaming.ToCamelCase(property.Name), Shared.ToonNaming.ToSnakeCase(property.Name), property.Name.ToLowerInvariant());

        var converter = FindAttribute(property, "ToonConverterAttribute") is { ConstructorArguments.Length: > 0 } converterAttribute &&
                        converterAttribute.ConstructorArguments[0].Value is ITypeSymbol converterType
            ? converterType.ToDisplayString(TypeOfFormat)
            : null;

        // As reflection reads: public setters (set, init), and non-public ones of properties marked [ToonInclude]
        var setter = FindSetter(property);

        if (setter != null && setter.DeclaredAccessibility != Accessibility.Public && !IsIncluded(property))
        {
            setter = null;
        }

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
            SetterOwnerIsValueType: setter?.ContainingType.IsValueType ?? false,
            IsRequired: property.IsRequired);
    }

    /// <summary><c>dynamic</c> as <c>object</c>, which it is at run time (<c>typeof(dynamic)</c> does not compile).</summary>
    private static ITypeSymbol Typed(ITypeSymbol type, Compilation compilation) =>
        type.TypeKind == TypeKind.Dynamic ? compilation.ObjectType.WithNullableAnnotation(type.NullableAnnotation) : type;

    /// <summary>Whether the property, or one it overrides, is marked <c>[ToonInclude]</c>.</summary>
    private static bool IsIncluded(IPropertySymbol property)
    {
        for (var current = property; current != null; current = current.OverriddenProperty)
        {
            if (current.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "ToonNet.Core.Serialization.Attributes.ToonIncludeAttribute"))
            {
                return true;
            }
        }

        return false;
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
            DateTime dateTime => $"new global::System.DateTime({dateTime.Ticks.ToString(CultureInfo.InvariantCulture)}L)", // [DateTimeConstant]
            IFormattable number when number.GetType().IsPrimitive => $"({typeName})({number.ToString(null, CultureInfo.InvariantCulture)})",
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

        var typeParameters = type.TypeParameters.Length > 0 ? "<" + string.Join(", ", type.TypeParameters.Select(t => EscapeKeyword(t.Name))) + ">" : "";

        return $"partial {keyword} {EscapeKeyword(type.Name)}{typeParameters}";
    }

    /// <summary>A name as C# writes it: a keyword with '@' (<c>@class</c>, <c>@in</c>).</summary>
    private static string EscapeKeyword(string name) =>
        SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None || SyntaxFacts.GetContextualKeywordKind(name) is SyntaxKind.RecordKeyword
            ? "@" + name
            : name;

    private static EquatableArray<string> ContainingDeclarations(INamedTypeSymbol type)
    {
        var declarations = new List<string>();

        for (var current = type.ContainingType; current != null; current = current.ContainingType)
        {
            declarations.Insert(0, Declaration(current));
        }

        return new EquatableArray<string>(declarations.ToImmutableArray());
    }

    /// <summary>
    ///     The file name of a type's generated code: its readable name, and a hash of its exact name, because hint names
    ///     must be unique ignoring case and the readable part maps several characters to '_' (<c>Url</c> and <c>URL</c>,
    ///     <c>G&lt;T&gt;</c> and <c>G_T_</c> would collide, and the compiler would drop the code of every type).
    /// </summary>
    private static string HintName(INamedTypeSymbol type)
    {
        var name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
        var builder = new StringBuilder(name.Length + 16);

        foreach (var c in name)
        {
            builder.Append(char.IsLetterOrDigit(c) || c is '.' or '_' ? c : '_');
        }

        // FNV-1a over the metadata name (with arity and containing types): stable across runs and machines
        var exact = type.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString() + "." + MetadataName(type) : MetadataName(type);
        var hash = 2166136261u;

        foreach (var c in exact)
        {
            hash = (hash ^ c) * 16777619u;
        }

        return builder.Append('.').Append(hash.ToString("x8", CultureInfo.InvariantCulture)).Append(".g.cs").ToString();

        static string MetadataName(INamedTypeSymbol t) => t.ContainingType is { } outer ? MetadataName(outer) + "+" + t.MetadataName : t.MetadataName;
    }
}
