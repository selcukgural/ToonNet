using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.SourceGenerators.Tests.Models.Other
{
    [ToonSerializable]
    public partial class Parcel
    {
        public double WeightKg { get; set; }
    }
}

[ToonSerializable]
public partial class GlobalNamespaceModel
{
    public string Name { get; set; } = "";
}
