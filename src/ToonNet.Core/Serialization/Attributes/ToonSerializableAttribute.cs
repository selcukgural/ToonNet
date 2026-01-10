namespace ToonNet.Core.Serialization.Attributes;

/// <summary>
///     Marks a class for automatic TOON serialization code generation via source generator.
///     The source generator will create Serialize and Deserialize methods at compile-time,
///     eliminating reflection overhead and enabling AOT-ready deployments.
/// </summary>
/// <remarks>
///     <para>
///         The attributed class MUST be declared as <c>partial</c> to allow the source generator
///         to inject generated code.
///     </para>
///     <para>
///         Usage example:
///         <code>
/// [ToonSerializable]
/// public partial class User
/// {
///     public string Name { get; set; }
///     public int Age { get; set; }
/// }
/// 
/// // Generated methods become available:
/// var user = new User { Name = "Alice", Age = 30 };
/// var doc = User.Serialize(user);
/// var deserialized = User.Deserialize(doc);
/// </code>
///     </para>
///     <para>
///         Generated methods are static and follow this pattern:
///         <code>
/// public static ToonDocument Serialize(T value, ToonSerializerOptions? options = null)
/// public static T Deserialize(ToonDocument doc, ToonSerializerOptions? options = null)
/// </code>
///     </para>
///     <para>
///         Performance benefits:
///         - 3-5x faster than reflection-based serialization
///         - Zero allocation in hot paths
///         - Full compile-time type safety
///         - Native AOT compatible
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ToonSerializableAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets whether to generate public static Serialize/Deserialize methods.
    ///     If false, methods will be internal (useful for source generation testing).
    /// </summary>
    /// <remarks>
    ///     Default: <c>true</c> (public methods)
    /// </remarks>
    public bool GeneratePublicMethods { get; init; } = true;

    /// <summary>
    ///     Gets or sets the property naming policy for generated serialization code.
    ///     This policy is applied to all properties, but can be overridden per-property
    ///     using the [ToonProperty(name)] attribute.
    /// </summary>
    /// <remarks>
    ///     Default: <see cref="PropertyNamingPolicy.Default" /> (property names as-is)
    ///     Examples:
    ///     <code>
    /// [ToonSerializable(NamingPolicy = PropertyNamingPolicy.CamelCase)]
    /// public partial class User
    /// {
    ///     public string FirstName { get; set; }  // Serializes as "firstName"
    /// }
    /// </code>
    /// </remarks>
    public PropertyNamingPolicy NamingPolicy { get; init; } = PropertyNamingPolicy.Default;

    /// <summary>
    ///     Gets or sets whether to include null-check guards in generated code.
    ///     When enabled, the generator includes ArgumentNullException checks for non-nullable properties.
    /// </summary>
    /// <remarks>
    ///     Default: <c>true</c> (include null checks)
    ///     Set to <c>false</c> for performance-critical scenarios where null checking
    ///     is handled elsewhere, or for maximum code size reduction.
    /// </remarks>
    public bool IncludeNullChecks { get; init; } = true;

    /// <summary>
    ///     Gets or sets whether to generate methods with extensive XML documentation.
    ///     When enabled, generated methods include summary, parameter, and return tags.
    /// </summary>
    /// <remarks>
    ///     Default: <c>true</c> (include documentation)
    ///     Set to <c>false</c> to reduce generated code size if documentation is not needed.
    /// </remarks>
    public bool IncludeDocumentation { get; init; } = true;
}