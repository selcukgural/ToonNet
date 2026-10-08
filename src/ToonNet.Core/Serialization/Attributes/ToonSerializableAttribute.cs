namespace ToonNet.Core.Serialization.Attributes;

/// <summary>
///     Marks a class, struct or record for TOON serialization code generation by the
///     <c>ToonNet.SourceGenerators</c> package, which adds static <c>Serialize</c> and <c>Deserialize</c> methods.
/// </summary>
/// <remarks>
///     <para>
///         The attributed type (and any type it is nested in) must be declared <c>partial</c>.
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
///         The generated code follows the same rules as <see cref="ToonSerializer"/> (property selection and order,
///         constructor selection, <c>[ToonProperty]</c>, <c>[ToonIgnore]</c>, <c>[ToonConverter]</c>, null handling) and
///         produces the same output. Strings, booleans and numbers are converted inline; other property types
///         (collections, enums, dates, nested objects) are delegated to <see cref="ToonSerializer.SerializeToValue{T}"/>
///         and <see cref="ToonSerializer.DeserializeFromValue{T}"/>, which use reflection.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
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
    ///     Gets or sets a fixed property naming policy for generated serialization code.
    ///     When set, it replaces <see cref="ToonSerializerOptions.PropertyNamingPolicy"/>; when not set, the generated code
    ///     uses the naming policy of the options passed at runtime. <c>[ToonProperty(name)]</c> overrides both.
    /// </summary>
    /// <remarks>
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
    ///     Gets or sets whether the generated methods throw <see cref="ArgumentNullException"/> for a null argument.
    /// </summary>
    /// <remarks>
    ///     Default: <c>true</c>. Has no effect on the <c>Serialize</c> method of structs.
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