namespace ToonNet.SourceGenerators;

/// <summary>
///     Internal marker interface injected by the ToonSerializable source generator.
///     Indicates that a class has generated TOON serialization methods.
/// </summary>
/// <remarks>
///     This interface is for internal implementation details only and should not be used
///     directly by application code. It exists solely to allow runtime detection of
///     source-generated serialization capabilities.
/// </remarks>
internal interface IToonSerializable
{
    // This interface has no members.
    // Its presence indicates that the class has generated Serialize/Deserialize methods.
}