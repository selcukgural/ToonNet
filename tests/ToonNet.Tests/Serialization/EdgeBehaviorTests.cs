using System.Text;
using ToonNet.Core;
using ToonNet.Core.Llm;
using ToonNet.Core.Models;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.Tests.Serialization;

/// <summary>Behaviors at the edges of the serializer that the final review of 4.0 found.</summary>
public class EdgeBehaviorTests
{
    [Fact]
    public void DateAndTimeKeys_AreWrittenLikeTheirValues_AndReadBack()
    {
        var times = new Dictionary<TimeOnly, int> { [new TimeOnly(1, 2, 3)] = 1, [new TimeOnly(1, 2, 59)] = 2 };
        var dates = new Dictionary<DateOnly, int> { [new DateOnly(2024, 3, 4)] = 1 };
        var spans = new Dictionary<TimeSpan, int> { [TimeSpan.FromMinutes(90)] = 1 };
        var flags = new Dictionary<bool, int> { [true] = 1, [false] = 0 };

        Assert.Equal("\"01:02:03\": 1\n\"01:02:59\": 2", ToonSerializer.Serialize(times));
        Assert.Equal(times, ToonSerializer.Deserialize<Dictionary<TimeOnly, int>>(ToonSerializer.Serialize(times)));
        Assert.Equal("\"2024-03-04\": 1", ToonSerializer.Serialize(dates));
        Assert.Equal(dates, ToonSerializer.Deserialize<Dictionary<DateOnly, int>>(ToonSerializer.Serialize(dates)));
        Assert.Equal(spans, ToonSerializer.Deserialize<Dictionary<TimeSpan, int>>(ToonSerializer.Serialize(spans)));
        Assert.Equal("true: 1\nfalse: 0", ToonSerializer.Serialize(flags));
        Assert.Equal(flags, ToonSerializer.Deserialize<Dictionary<bool, int>>(ToonSerializer.Serialize(flags)));
    }

    private sealed class SpanHolder
    {
        public int A { get; set; } = 1;
        public ReadOnlySpan<byte> Bytes => "x"u8;
    }

    private sealed class MemoryHolder
    {
        public Memory<int> Mem { get; set; } = new[] { 1, 2 };
    }

    [Fact]
    public void RefStructProperties_AreLeftOut_AndMemoryIsAClearError()
    {
        Assert.Equal("A: 1", ToonSerializer.Serialize(new SpanHolder()));
        Assert.Throws<NotSupportedException>(() => ToonSerializer.Serialize(new MemoryHolder()));
    }

    [Fact]
    public async Task CanceledFileWriters_LeaveTheFile()
    {
        var path = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(path, "old");
            using var canceled = new CancellationTokenSource();
            await canceled.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                ToonSerializer.SerializeCollectionToFileAsync(new[] { 1, 2 }, path, cancellationToken: canceled.Token).AsTask());

            Assert.Equal("old", await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class Cyclic
    {
        public Cyclic? Next { get; set; }
    }

    [Fact]
    public void LongPaths_AreShortenedInMessages()
    {
        var key = new string('k', 60);
        var toon = new StringBuilder();

        for (var i = 0; i < 5; i++)
        {
            toon.Append(' ', 2 * i).Append(key).Append(i).Append(":\n");
        }

        toon.Append(' ', 10).Append("x: abc");
        var ex = Assert.Throws<ToonSerializationException>(() =>
            ToonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, int>>>>>>>(toon.ToString()));

        // The path has 5 keys of 61 characters: its start and end are shown, all of it is in the property
        Assert.True(ex.Message.Length < 330, $"{ex.Message.Length} characters");
        Assert.Contains("....", ex.Message);
        Assert.EndsWith(".x", ex.Path);
        Assert.True(ex.Path!.Length > 300);
    }

    [Flags]
    public enum Access
    {
        None = 0,
        Read = 1,
        Write = 2
    }

    public sealed class Customer
    {
        public string Name { get; set; } = "";
        public string Country { get; set; } = "";
    }

    public sealed class Order
    {
        public int Id { get; set; }
        public Customer Customer { get; set; } = new();
        public string? Note { get; set; }
        public Access Access { get; set; }

        [ToonConverter(typeof(MoneyConverter))]
        public decimal Total { get; set; }
    }

    public sealed class MoneyConverter : ToonConverter<decimal>
    {
        public override ToonValue? Write(decimal value, ToonSerializerOptions options) => new ToonString(value + " EUR");

        public override decimal Read(ToonValue value, ToonSerializerOptions options) => decimal.Parse(((ToonString)value).Value.Split(' ')[0]);
    }

    public sealed class Server
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
    }

    public sealed class Catalog
    {
        public List<Order> Orders { get; set; } = [];
        public Dictionary<string, Server> Servers { get; set; } = [];
    }

    [Fact]
    public void DescribeShape_FollowsTheFormsTheSerializerWrites()
    {
        Assert.Equal("""
                     Orders[N]{Id,Customer{Name,Country},Note,Access,Total}:
                       <integer>,<string>,<string>,<string|null>,<flags: None|Read|Write>,<any>
                     Servers[N:]{Host,Port}:
                       <key>: <string>,<integer>
                     """, ToonPrompt.DescribeShape<Catalog>());

        // With '|' as the delimiter, the choices of a placeholder are separated by '/'
        var pipe = new ToonSerializerOptions { ToonOptions = new ToonOptions { Delimiter = '|' } };
        Assert.Equal("""
                     Orders[N|]{Id|Customer{Name|Country}|Note|Access|Total}:
                       <integer>|<string>|<string>|<string/null>|<flags: None/Read/Write>|<any>
                     Servers[N:|]{Host|Port}:
                       <key>: <string>|<integer>
                     """, ToonPrompt.DescribeShape<Catalog>(pipe));

        // The serializer writes those forms
        var catalog = new Catalog
        {
            Orders = [new() { Id = 1, Customer = new() { Name = "a", Country = "b" }, Total = 2 }],
            Servers = new() { ["x"] = new() { Host = "h", Port = 1 }, ["y"] = new() { Host = "i", Port = 2 } }
        };
        var text = ToonSerializer.Serialize(catalog);
        Assert.StartsWith("Orders[1]{Id,Customer{Name,Country},Note,Access,Total}:", text);
        Assert.Contains("Servers[2:]{Host,Port}:", text);
    }
}
