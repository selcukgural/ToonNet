using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ToonNet.SourceGenerators.Model;

/// <summary>
/// Everything the emitter needs to know about a <c>[ToonSerializable]</c> type. Holds only strings and values (no
/// symbols or syntax), so it is cheap to compare between generator runs.
/// </summary>
/// <param name="Emit">False when an error diagnostic prevents generating code.</param>
internal sealed record TypeModel(
    bool Emit,
    string? Namespace,
    EquatableArray<string> ContainingTypeDeclarations,
    string Declaration,
    string FullyQualifiedName,
    string TypeOfName,
    string HintName,
    bool IsValueType,
    bool PublicMethods,
    bool NullChecks,
    bool Documentation,
    bool CanUseUnsafeAccessor,
    int? FixedNamingPolicy,
    EquatableArray<PropertyModel> Properties,
    ConstructorModel? Constructor,
    EquatableArray<DiagnosticInfo> Diagnostics);

/// <summary>A property in serialization order.</summary>
/// <param name="TypeName">Fully qualified type including nullable annotations, for declarations.</param>
/// <param name="TypeOfName">Fully qualified type without nullable reference annotations, for <c>typeof</c>.</param>
/// <param name="Names">Serialized names for the Default, CamelCase, SnakeCase and LowerCase policies.</param>
/// <param name="Write">How the generated <c>Deserialize</c> assigns the property.</param>
/// <param name="SetterOwner">For <see cref="WriteKind.Accessor"/>: the type that declares the setter.</param>
internal sealed record PropertyModel(
    string Name,
    string TypeName,
    string TypeOfName,
    EquatableArray<string> Names,
    PrimitiveKind Primitive,
    string? PrimitiveTypeName,
    bool CanBeNull,
    bool IsNullableValueType,
    string? ConverterTypeName,
    bool IsReadOnly,
    WriteKind Write,
    string? SetterOwner,
    bool SetterOwnerIsValueType);

/// <summary>The constructor used by the generated <c>Deserialize</c> method.</summary>
internal sealed record ConstructorModel(EquatableArray<ParameterModel> Parameters, bool SetsRequiredMembers);

/// <param name="PropertyIndex">The index of the property bound to this parameter (case-insensitive name match), or -1.</param>
/// <param name="DefaultValue">C# expression used when the document has no value for the parameter.</param>
internal sealed record ParameterModel(string Name, string TypeName, int PropertyIndex, string DefaultValue);

internal enum PrimitiveKind
{
    None,
    String,
    Boolean,
    Integer,
    UInt64,
    Single,
    Double,
    Decimal
}

internal enum WriteKind
{
    /// <summary>Not written (no setter, or a setter generated code cannot reach).</summary>
    None,

    /// <summary>Assigned after construction when the key is present.</summary>
    Assign,

    /// <summary>Assigned in the object initializer (required or init-only members that cannot be set later).</summary>
    Initializer,

    /// <summary>Set after construction through an <c>[UnsafeAccessor]</c> to the setter (init-only or inaccessible).</summary>
    Accessor
}

/// <summary>A diagnostic without a <see cref="Location"/> object, so it can live in an equatable model.</summary>
internal sealed record DiagnosticInfo(string Id, string? FilePath, TextSpan Span, LinePositionSpan LineSpan, EquatableArray<string> Arguments)
{
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location? location, params string[] arguments)
    {
        var lineSpan = location?.GetLineSpan();

        return new DiagnosticInfo(descriptor.Id, location?.SourceTree?.FilePath, location?.SourceSpan ?? default, lineSpan?.Span ?? default,
                                  new EquatableArray<string>(arguments.ToImmutableArray()));
    }

    public Location ToLocation() => FilePath is null ? Location.None : Location.Create(FilePath, Span, LineSpan);
}
