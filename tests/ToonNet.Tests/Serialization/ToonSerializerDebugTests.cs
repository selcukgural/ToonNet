using ToonNet.Core.Serialization;
using Xunit.Abstractions;

namespace ToonNet.Tests.Serialization;

public class ToonSerializerDebugTests
{
    private readonly ITestOutputHelper _output;

    public ToonSerializerDebugTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Debug_NestedSerialization()
    {
        var person = new DebugPerson
        {
            Name = "Test",
            Age = 30,
            Address = new DebugAddress
            {
                City = "Boston"
            }
        };

        var toon = ToonSerializer.Serialize(person);
        _output.WriteLine("=== SERIALIZED ===");
        _output.WriteLine(toon);
        _output.WriteLine("==================");

        var deserialized = ToonSerializer.Deserialize<DebugPerson>(toon);

        _output.WriteLine("=== DESERIALIZED ===");
        _output.WriteLine($"Name: {deserialized?.Name}");
        _output.WriteLine($"Age: {deserialized?.Age}");
        _output.WriteLine($"Address: {deserialized?.Address?.City}");
        _output.WriteLine("====================");

        Assert.NotNull(deserialized);
        Assert.Equal("Test", deserialized.Name);
        Assert.NotNull(deserialized.Address);
        Assert.Equal("Boston", deserialized.Address.City);
    }
}

public class DebugAddress
{
    public string City { get; set; } = "";
}

public class DebugPerson
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public DebugAddress? Address { get; set; }
}