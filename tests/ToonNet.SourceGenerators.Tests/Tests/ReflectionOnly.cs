using ToonNet.Core.Serialization;

namespace ToonNet.SourceGenerators.Tests.Tests;

/// <summary>
/// ToonSerializer uses generated code for [ToonSerializable] types; parity tests need the reflection-based path.
/// </summary>
internal static class ReflectionOnly
{
    public static ToonSerializerOptions Options(ToonSerializerOptions? options = null)
    {
        options ??= new ToonSerializerOptions();

        var copy = new ToonSerializerOptions
        {
            ToonOptions = options.ToonOptions,
            IgnoreNullValues = options.IgnoreNullValues,
            PropertyNamingPolicy = options.PropertyNamingPolicy,
            AllowExtendedLimits = options.AllowExtendedLimits,
            MaxDepth = options.MaxDepth,
            IncludeReadOnlyProperties = options.IncludeReadOnlyProperties,
            PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive,
            UnmappedMemberHandling = options.UnmappedMemberHandling,
            RespectRequiredMembers = options.RespectRequiredMembers,
            UseGeneratedCode = false
        };

        foreach (var converter in options.Converters)
        {
            copy.AddConverter(converter);
        }

        return copy;
    }
}
