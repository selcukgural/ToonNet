using Microsoft.CodeAnalysis;

namespace ToonNet.SourceGenerators.Utilities;

/// <summary>
/// Helper class for creating diagnostic messages during code generation.
/// </summary>
internal static class DiagnosticHelper
{
    private const string Category = "ToonSerializableGenerator";

    /// <summary>
    /// Error: General code generation failure.
    /// </summary>
    public static readonly DiagnosticDescriptor GenerationError = new(
        id: "TOON001",
        title: "TOON serialization code generation error",
        messageFormat: "Failed to generate TOON serialization code for type '{0}': {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Error: Type is not a partial class.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidClassStructure = new(
        id: "TOON002",
        title: "Invalid class structure for TOON serialization",
        messageFormat: "Type '{0}' must be declared as 'partial' to use [ToonSerializable]",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Warning: No serializable properties found.
    /// </summary>
    public static readonly DiagnosticDescriptor NoProperties = new(
        id: "TOON003",
        title: "No serializable properties found",
        messageFormat: "Type '{0}' has no public properties eligible for serialization",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Error: Type argument is not supported for code generation.
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedType = new(
        id: "TOON004",
        title: "Unsupported type for code generation",
        messageFormat: "Type '{0}' is not supported for automatic serialization code generation",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
