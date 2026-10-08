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
    /// Looks up a descriptor by id (used when replaying diagnostics stored in the generator model).
    /// </summary>
    public static DiagnosticDescriptor Get(string id) => id switch
    {
        "TOON002" => InvalidClassStructure,
        "TOON003" => NoProperties,
        "TOON005" => NoPublicConstructor,
        _         => GenerationError
    };
}
