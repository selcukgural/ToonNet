using Microsoft.CodeAnalysis;

namespace ToonNet.SourceGenerators.Utilities;

/// <summary>
/// Provides diagnostic descriptors for reporting issues and warnings during
/// the Toon serialization code generation process.
/// </summary>
internal static class DiagnosticHelper
{
    /// <summary>
    /// The category identifier for diagnostics specific to TOON serialization code generation.
    /// </summary>
    private const string Category = "ToonSerializableGenerator";

    /// <summary>
    /// Error: General failure encountered during the generation of TOON serialization code.
    /// </summary>
    public static readonly DiagnosticDescriptor GenerationError = new("TOON001", "TOON serialization code generation error",
                                                                      "Failed to generate TOON serialization code for type '{0}': {1}", Category,
                                                                      DiagnosticSeverity.Error, true);

    /// <summary>
    /// Error: a type decorated with [ToonSerializable], or a type it is nested in, is not declared 'partial'.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidClassStructure = new("TOON002", "Invalid type structure for TOON serialization",
                                                                            "Type '{0}' must be declared as 'partial' to use [ToonSerializable]",
                                                                            Category, DiagnosticSeverity.Error, true);

    /// <summary>
    /// Warning: No serializable properties found.
    /// </summary>
    public static readonly DiagnosticDescriptor NoProperties = new("TOON003", "No serializable properties found",
                                                                   "Type '{0}' has no public properties eligible for serialization", Category,
                                                                   DiagnosticSeverity.Warning, true);

    /// <summary>
    /// Warning: the type has no public constructor, so no Deserialize method is generated.
    /// </summary>
    public static readonly DiagnosticDescriptor NoPublicConstructor = new("TOON005", "No public constructor for deserialization",
                                                                          "Type '{0}' has no public constructor; no Deserialize method is generated",
                                                                          Category, DiagnosticSeverity.Warning, true);

    /// <summary>
    /// Warning: a property or constructor parameter is converted by the reflection-based serializer.
    /// </summary>
    public static readonly DiagnosticDescriptor ReflectionFallback = new("TOON006", "Value is serialized with reflection",
                                                                         "The {0} of '{1}' is serialized with reflection: {2}", Category,
                                                                         DiagnosticSeverity.Warning, true,
                                                                         "Generated code hands this value to ToonSerializer, which uses reflection and is not trim or AOT safe. Mark the type [ToonSerializable], use a supported collection type, or add a [ToonConverter].");

    /// <summary>
    /// Error: like TOON006, for a type declared with <c>[ToonSerializable(AllowReflectionFallback = false)]</c>.
    /// </summary>
    public static readonly DiagnosticDescriptor ReflectionFallbackNotAllowed = new("TOON007", "Reflection fallback is not allowed",
                                                                                   "The {0} of '{1}' would be serialized with reflection ({2}), but AllowReflectionFallback is false",
                                                                                   Category, DiagnosticSeverity.Error, true);

    /// <summary>
    /// Error: a <c>[ToonProperty]</c> name contains an unpaired surrogate, which TOON cannot represent (spec §3).
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidPropertyName = new("TOON008", "Invalid serialized property name",
                                                                          "The [ToonProperty] name of '{0}' contains an unpaired surrogate, which TOON cannot represent",
                                                                          Category, DiagnosticSeverity.Error, true);

    /// <summary>
    /// Warning: a member of the type is named like a generated static method (<c>Serialize</c>, <c>Deserialize</c>),
    /// so that method is not generated.
    /// </summary>
    public static readonly DiagnosticDescriptor GeneratedMethodNameTaken = new("TOON009", "Generated method name taken",
                                                                               "'{0}' has a member named '{1}', so the generated static {1} method is left out; use ToonSerializer with {0}.ToonTypeInfo instead",
                                                                               Category, DiagnosticSeverity.Warning, true);

    /// <summary>
    /// Error: a static class, a ref struct or a file-local type cannot have generated serialization code.
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedTypeKind = new("TOON010", "Type cannot be serialized",
                                                                          "Type '{0}' cannot use [ToonSerializable]: {1}", Category,
                                                                          DiagnosticSeverity.Error, true);

    /// <summary>
    /// Error: the project references a ToonNet.Core the generated code does not fit (older, or another major version).
    /// </summary>
    public static readonly DiagnosticDescriptor CoreVersionMismatch = new("TOON011", "ToonNet.Core does not match the source generator",
                                                                          "ToonNet.SourceGenerators {0} needs ToonNet.Core {0} or a later {2}.x version, but the project references ToonNet.Core {1}; reference the same version of both packages",
                                                                          Category, DiagnosticSeverity.Error, true);

    /// <summary>
    /// Warning: a constructor parameter matches no public property, and the constructor is not marked
    /// <c>[ToonConstructor]</c>: reading the type throws, as with System.Text.Json.
    /// </summary>
    public static readonly DiagnosticDescriptor UnboundConstructorParameter = new("TOON012", "Constructor parameter matches no property",
                                                                                  "The constructor parameter '{0}' of '{1}' matches no public property, so reading '{1}' throws; add a property with its name, or mark the constructor [ToonConstructor] to read the parameter from the key '{0}'",
                                                                                  Category, DiagnosticSeverity.Warning, true);

    /// <summary>
    /// Warning: a type the generated code would name (a property's type, a base type, a converter) has a name that is not
    /// a C# identifier, which only another compiler or a hand-written assembly gives a type; nothing is generated, and the
    /// type is serialized with reflection.
    /// </summary>
    public static readonly DiagnosticDescriptor NameNotEmittable = new("TOON013", "Type name cannot be used in generated code",
                                                                       "No code is generated for '{0}': the name '{1}' that it uses is not a C# identifier; '{0}' is serialized with reflection",
                                                                       Category, DiagnosticSeverity.Warning, true);

    /// <summary>
    /// Looks up a descriptor by id (used when replaying diagnostics stored in the generator model).
    /// </summary>

    public static DiagnosticDescriptor Get(string id) => id switch
    {
        "TOON002" => InvalidClassStructure,
        "TOON003" => NoProperties,
        "TOON005" => NoPublicConstructor,
        "TOON006" => ReflectionFallback,
        "TOON007" => ReflectionFallbackNotAllowed,
        "TOON008" => InvalidPropertyName,
        "TOON009" => GeneratedMethodNameTaken,
        "TOON010" => UnsupportedTypeKind,
        "TOON012" => UnboundConstructorParameter,
        "TOON013" => NameNotEmittable,
        _         => GenerationError
    };
}
