using ToonNet.Comparison;
using ToonNet.Core.Serialization;

namespace ToonNet.Compatibility.Tests;

/// <summary>
///     ToonNet against the official .NET implementation of TOON (Toon.Format): both conform to spec v4.4, write the
///     same text for the same data, and read each other's text. The differences that remain are listed here, so a
///     change on either side shows up.
/// </summary>
public class OfficialLibraryParityTests
{
    [Fact]
    public void BothLibraries_PassEverySpecFixture()
    {
        var root = Parity.FindFixtures();

        var (toonNetPassed, total) = Parity.RunToonNetFixtures(root);
        var (toonFormatPassed, _) = Parity.RunToonFormatFixtures(root);

        Assert.Equal(564, total);
        Assert.Equal(total, toonNetPassed);
        Assert.Equal(total, toonFormatPassed);
    }

    [Fact]
    public void RandomDocuments_AreEncodedIdentically_AndReadByTheOtherLibrary()
    {
        var (documents, encodeDifferences, decodeDifferences) = Parity.Fuzz(5_000);

        Assert.Equal(5_000, documents);
        Assert.Equal(0, encodeDifferences);
        Assert.Equal(0, decodeDifferences);
    }

    [Fact]
    public void TypedData_IsSerializedIdentically_AndReadBackToTheSameValues()
    {
        // Tabular, list, nested field group and keyed tabular forms; CheckTyped throws on any difference
        Parity.CheckTyped("Employees", Datasets.Employees(50));
        Parity.CheckTyped("Orders", Datasets.Orders(50));
        Parity.CheckTyped("Products", Datasets.Products(50));
        Parity.CheckTyped("Servers", Datasets.Servers(50));
        Parity.CheckTyped("Server", Datasets.Servers(1)["node-0"]);
    }

    [Fact]
    public void Enums_AreWrittenByName_WhereTheOfficialLibraryWritesNumbers()
    {
        // Host type mapping is implementation-defined (spec §3); System.Text.Json's default maps enums to numbers
        Assert.Equal("Day: Friday", ToonSerializer.Serialize(new { Day = DayOfWeek.Friday }));
        Assert.Equal("Day: 5", Toon.Format.ToonEncoder.Encode(new { Day = DayOfWeek.Friday }));
    }

    [Theory]
    [InlineData("12345678901234567.89", "12345678901234568")]
    [InlineData("123456789012345678901234567", "1.2345678901234568e+26")]
    public void Decimals_KeepEveryDigit_WhereTheOfficialLibraryRoundsThemToDoubles(string value, string official)
    {
        // Spec §2 requires decode(encode(x)) == x; Toon.Format 1.0.0 converts decimals to doubles first
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        var ours = ToonSerializer.Serialize(new { Amount = amount });

        Assert.Equal(amount, ToonSerializer.Deserialize<Dictionary<string, decimal>>(ours)!["Amount"]);
        Assert.Equal("Amount: " + official, Toon.Format.ToonEncoder.Encode(new { Amount = amount }));
    }

    [Fact]
    public void UInt64MaxValue_RoundTrips_WhereTheOfficialLibraryRoundsIt()
    {
        Assert.Equal("Max: 18446744073709551615", ToonSerializer.Serialize(new { Max = ulong.MaxValue }));
        Assert.Equal("Max: 18446744073709552000", Toon.Format.ToonEncoder.Encode(new { Max = ulong.MaxValue }));
    }
}
