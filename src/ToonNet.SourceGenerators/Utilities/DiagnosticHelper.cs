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
    /// Error: The class structure is invalid for TOON serialization.
    /// This diagnostic is reported when a class decorated with the [ToonSerializable] attribute is not declared as 'partial.'
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidClassStructure = new("TOON002", "Invalid class structure for TOON serialization",
                                                                            "Type '{0}' must be declared as 'partial' to use [ToonSerializable]",
                                                                            Category, DiagnosticSeverity.Error, true);

    /// <summary>
    /// Warning: No serializable properties found.
    /// </summary>
    public static readonly DiagnosticDescriptor NoProperties = new("TOON003", "No serializable properties found",
                                                                   "Type '{0}' has no public properties eligible for serialization", Category,
                                                                   DiagnosticSeverity.Warning, true);

    /// <summary>
    /// Error: Type argument is not supported for code generation.
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedType = new("TOON004", "Unsupported type for code generation",
                                                                      "Type '{0}' is not supported for automatic serialization code generation",
                                                                      Category, DiagnosticSeverity.Error, true);
}