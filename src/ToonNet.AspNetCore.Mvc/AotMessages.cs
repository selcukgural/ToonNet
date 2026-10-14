namespace ToonNet.AspNetCore.Mvc;

/// <summary>The messages of the trimming and NativeAOT annotations.</summary>
internal static class AotMessages
{
    public const string Reflection =
        "This member converts .NET types with reflection, which trimming and NativeAOT cannot analyze. In NativeAOT apps, read bodies with ReadFromToonAsync(ToonTypeInfo) and write responses with ToonSerializer and a ToonTypeInfo ([ToonSerializable]).";

    public const string AnnotatedConstructor = "The constructors of the type carry the trimming and NativeAOT requirements.";
}
