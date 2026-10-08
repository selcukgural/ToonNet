using Microsoft.CodeAnalysis;

namespace ToonNet.SourceGenerators.Model;

/// <summary>
/// Decides how generated code converts a declared type, mirroring the order of checks in <c>ToonSerializer</c>:
/// primitives, <c>ToonValue</c>, dictionaries, collections, then objects. Anything that cannot be converted without
/// knowing the runtime type, or without reflection metadata, becomes <see cref="ValueKind.Reflection"/>.
/// </summary>
internal sealed class ValuePlanBuilder(Compilation compilation)
{
    private static readonly SymbolDisplayFormat TypeNameFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private readonly INamedTypeSymbol?[] _listTypes =
    [
        compilation.GetTypeByMetadataName("System.Collections.Generic.List`1"),
        compilation.GetTypeByMetadataName("System.Collections.Generic.IList`1"),
        compilation.GetTypeByMetadataName("System.Collections.Generic.ICollection`1"),
        compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1"),
        compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyList`1"),
        compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyCollection`1")
    ];

    private readonly INamedTypeSymbol?[] _setTypes =
    [
        compilation.GetTypeByMetadataName("System.Collections.Generic.HashSet`1"),
        compilation.GetTypeByMetadataName("System.Collections.Generic.ISet`1"),
        compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlySet`1")
    ];

    private readonly INamedTypeSymbol?[] _dictionaryTypes =
    [
        compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2"),
        compilation.GetTypeByMetadataName("System.Collections.Generic.IDictionary`2"),
        compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyDictionary`2")
    ];

    private readonly INamedTypeSymbol? _keyValuePair = compilation.GetTypeByMetadataName("System.Collections.Generic.KeyValuePair`2");
    private readonly INamedTypeSymbol? _toonValue = compilation.GetTypeByMetadataName("ToonNet.Core.Models.ToonValue");

    // The non-special types with a fixed TOON form in ToonSerializer (TrySerializePrimitive / TryDeserializePrimitive)
    private readonly INamedTypeSymbol?[] _primitiveTypes =
    [
        compilation.GetTypeByMetadataName("System.DateTimeOffset"),
        compilation.GetTypeByMetadataName("System.DateOnly"),
        compilation.GetTypeByMetadataName("System.TimeOnly"),
        compilation.GetTypeByMetadataName("System.TimeSpan"),
        compilation.GetTypeByMetadataName("System.Guid"),
        compilation.GetTypeByMetadataName("System.Uri"),
        compilation.GetTypeByMetadataName("System.Half"),
        compilation.GetTypeByMetadataName("System.Int128"),
        compilation.GetTypeByMetadataName("System.UInt128"),
        compilation.GetTypeByMetadataName("System.Numerics.BigInteger")
    ];

    /// <summary>Builds the plan for <paramref name="type"/>; each reflection fallback adds a reason to <paramref name="reasons"/>.</summary>
    public ValuePlan Build(ITypeSymbol type, List<string> reasons)
    {
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            var inner = Build(nullable.TypeArguments[0], reasons);
            return inner with { TypeName = Name(type), CanBeNull = true, IsNullableValueType = true };
        }

        var inline = InlineKind(type);

        if (inline != PrimitiveKind.None)
        {
            return Plan(ValueKind.Inline, type, inline: inline);
        }

        if (type.SpecialType is SpecialType.System_Char or SpecialType.System_DateTime || type.TypeKind == TypeKind.Enum ||
            Matches(type, _primitiveTypes))
        {
            return Plan(ValueKind.Primitive, type);
        }

        if (_toonValue != null && InheritsFrom(type, _toonValue))
        {
            return Plan(ValueKind.Raw, type);
        }

        if (type is IArrayTypeSymbol { IsSZArray: true } array)
        {
            return Sequence(ValueKind.Array, type, array.ElementType, reasons);
        }

        if (type is INamedTypeSymbol { IsGenericType: true } generic)
        {
            if (Matches(generic, _dictionaryTypes))
            {
                return Dictionary(generic, reasons);
            }

            if (Matches(generic, _listTypes))
            {
                return Sequence(ValueKind.List, type, generic.TypeArguments[0], reasons);
            }

            if (Matches(generic, _setTypes))
            {
                return Sequence(ValueKind.Set, type, generic.TypeArguments[0], reasons);
            }
        }

        return GeneratedOrReflection(type, reasons);
    }

    private ValuePlan Sequence(ValueKind kind, ITypeSymbol type, ITypeSymbol elementType, List<string> reasons)
    {
        // A sequence of key/value pairs declared as IEnumerable<KeyValuePair<,>> may be a dictionary at runtime, which
        // ToonSerializer writes as an object.
        if (_keyValuePair != null && SymbolEqualityComparer.Default.Equals(elementType.OriginalDefinition, _keyValuePair))
        {
            return Reflection(type, reasons, "a sequence of KeyValuePair may be a dictionary at runtime");
        }

        return Plan(kind, type, element: Build(elementType, reasons));
    }

    private ValuePlan Dictionary(INamedTypeSymbol type, List<string> reasons)
    {
        var keyType = type.TypeArguments[0];
        var keyPlan = Build(keyType, []);

        if (keyPlan.Kind is not (ValueKind.Inline or ValueKind.Primitive) || keyPlan.CanBeNull && keyType.SpecialType != SpecialType.System_String)
        {
            return Reflection(type, reasons, $"dictionary keys of type '{Display(keyType)}' are not supported");
        }

        return Plan(ValueKind.Dictionary, type, element: Build(type.TypeArguments[1], reasons), key: keyPlan);
    }

    private ValuePlan GeneratedOrReflection(ITypeSymbol type, List<string> reasons)
    {
        if (type.SpecialType == SpecialType.System_Object || type.TypeKind is TypeKind.Interface or TypeKind.TypeParameter or TypeKind.Dynamic ||
            type.IsAbstract)
        {
            return Reflection(type, reasons, $"the runtime type of '{Display(type)}' is not known at compile time");
        }

        if (type is not INamedTypeSymbol named || !HasAttribute(named, "ToonSerializableAttribute"))
        {
            return Reflection(type, reasons, $"'{Display(type)}' is not marked [ToonSerializable]");
        }

        if (HasAttribute(named, "ToonConverterAttribute"))
        {
            return Reflection(type, reasons, $"'{Display(type)}' has a type-level [ToonConverter]");
        }

        bool canSerialize, canDeserialize;

        if (named.DeclaringSyntaxReferences.Length > 0)
        {
            // Declared in this compilation: its methods are generated in this run.
            canSerialize = AllPartial(named);
            canDeserialize = canSerialize && (named.IsValueType || TypeModelBuilder.SelectConstructor(named) != null);
        }
        else
        {
            canSerialize = HasAccessibleMember(named, "__ToonSerializeValue");
            canDeserialize = HasAccessibleMember(named, "__ToonDeserializeValue");
        }

        if (!canSerialize || !canDeserialize)
        {
            return Reflection(type, reasons, canSerialize
                                  ? $"'{Display(type)}' has no public constructor"
                                  : $"'{Display(type)}' has no generated methods (not partial, or built with an older generator)");
        }

        return Plan(ValueKind.Generated, type, checkRuntimeType: !type.IsValueType && !type.IsSealed);
    }

    private ValuePlan Reflection(ITypeSymbol type, List<string> reasons, string reason)
    {
        reasons.Add(reason);
        return Plan(ValueKind.Reflection, type);
    }

    private static ValuePlan Plan(ValueKind kind, ITypeSymbol type, PrimitiveKind inline = PrimitiveKind.None, bool checkRuntimeType = false,
                                  ValuePlan? element = null, ValuePlan? key = null)
    {
        return new ValuePlan(kind, Name(type), Name(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)), !type.IsValueType, false, inline,
                             checkRuntimeType, element, key);
    }

    private static string Name(ITypeSymbol type) => type.ToDisplayString(TypeNameFormat);

    private static string Display(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);

    private static bool Matches(ITypeSymbol type, INamedTypeSymbol?[] candidates)
    {
        return candidates.Any(c => c != null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, c));
    }

    private static bool InheritsFrom(ITypeSymbol type, INamedTypeSymbol baseType)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAttribute(INamedTypeSymbol type, string name)
    {
        return type.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "ToonNet.Core.Serialization.Attributes." + name);
    }

    private static bool AllPartial(INamedTypeSymbol type)
    {
        for (var current = type; current != null; current = current.ContainingType)
        {
            if (!TypeModelBuilder.IsPartial(current))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasAccessibleMember(INamedTypeSymbol type, string name)
    {
        return type.GetMembers(name).Any(m => m.IsStatic && m.DeclaredAccessibility == Accessibility.Public);
    }

    private static PrimitiveKind InlineKind(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_String => PrimitiveKind.String,
            SpecialType.System_Boolean => PrimitiveKind.Boolean,
            SpecialType.System_SByte or SpecialType.System_Byte or SpecialType.System_Int16 or SpecialType.System_UInt16 or
                SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 => PrimitiveKind.Integer,
            SpecialType.System_UInt64 => PrimitiveKind.UInt64,
            SpecialType.System_Single => PrimitiveKind.Single,
            SpecialType.System_Double => PrimitiveKind.Double,
            SpecialType.System_Decimal => PrimitiveKind.Decimal,
            _ => PrimitiveKind.None
        };
    }
}
