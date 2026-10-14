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
///         produces the same output. Primitives, enums, dates, other <c>[ToonSerializable]</c> types, arrays, lists, sets
///         and dictionaries are converted by generated code. Other property types (for example <c>object</c>, interfaces or
///         classes without the attribute) are handed to the reflection-based serializer and reported as warning
///         <c>TOON006</c>. When the options contain converters, the generated methods defer to
///         <see cref="ToonSerializer"/> entirely.
///     </para>
///     <para>
///         The generated methods are also registered with <see cref="ToonSerializer"/>, which then uses them for values
///         of the type (non-generic types that are not nested in a <c>private</c> or <c>protected</c> type).
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
    ///     Gets or sets a fixed property naming policy for the type.
    ///     When set, it replaces <see cref="ToonSerializerOptions.PropertyNamingPolicy"/> for the type's properties, in the
    ///     generated code and in <see cref="ToonSerializer"/> (also when it falls back to reflection, for example because
    ///     the options contain converters); derived types without the attribute inherit it. When not set, the naming
    ///     policy of the options passed at runtime is used. <c>[ToonProperty(name)]</c> overrides both.
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

    /// <summary>
    ///     Gets or sets whether property values the generator cannot convert itself may be handed to the reflection-based
    ///     <see cref="ToonSerializer"/>.
    /// </summary>
    /// <remarks>
    ///     Default: <c>true</c>, and each such property is reported as warning <c>TOON006</c>. Set to <c>false</c> to turn
    ///     these into error <c>TOON007</c>, so the type's properties are converted without reflection when the options
    ///     contain no converters. Some values still reach the reflection-based serializer at run time: an object written
    ///     as its runtime type (<see cref="ToonSerializerOptions.SerializeRuntimeTypes"/>), a collection of a type the
    ///     generator does not know (a property declared as <c>IList&lt;T&gt;</c> holding a custom list), and a document
    ///     value of the wrong kind (a string where an object is expected), which it reports as an error. Under trimming or
    ///     native AOT these fail instead.
    /// </remarks>
    public bool AllowReflectionFallback { get; init; } = true;
}