using System.Collections.Concurrent;
using System.Collections.Immutable;
using ToonNet.Core;
using ToonNet.Core.Serialization;

namespace ToonNet.Tests.Serialization;

/// <summary>Types whose reading or writing lost or wrapped data, and the shapes no other test covered.</summary>
public class TypeEdgeCaseTests
{
    [Flags]
    public enum ByteFlags : byte
    {
        None = 0,
        A = 1,
        B = 2
    }

    public enum Plain
    {
        Zero,
        One
    }

    [Flags]
    public enum LongFlags : long
    {
        High = 1L << 40
    }

    public sealed class EnumHolder
    {
        public ByteFlags B { get; set; }
        public Plain P { get; set; }
        public LongFlags L { get; set; }
    }

    [Theory]
    [InlineData("B: 300")]
    [InlineData("B: -1")]
    [InlineData("P: 4294967297")]
    [InlineData("P: 1.5")]
    public void NumericEnumValues_DoNotWrap(string toon)
    {
        var ex = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<EnumHolder>(toon));

        Assert.Contains("target type: " + (toon[0] == 'B' ? "ByteFlags" : "Plain"), ex.Message);
    }

    [Fact]
    public void NumericEnumValues_InRange_AndWideFlags()
    {
        var holder = ToonSerializer.Deserialize<EnumHolder>("B: 3\nP: 1\nL: 1099511627776")!;

        Assert.Equal(ByteFlags.A | ByteFlags.B, holder.B);
        Assert.Equal(Plain.One, holder.P);
        Assert.Equal(LongFlags.High, holder.L);
        Assert.Equal("B: \"A, B\"\nP: One\nL: High", ToonSerializer.Serialize(holder));
    }

    [Fact]
    public void MultidimensionalArrays_AreRejected()
    {
        var read = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<int[,]>("[4]: 1,2,3,4"));
        Assert.Contains("Multidimensional arrays", read.Message);

        var write = Assert.Throws<ToonEncodingException>(() => ToonSerializer.Serialize(new int[,] { { 1, 2 }, { 3, 4 } }));
        Assert.Contains("Multidimensional arrays", write.Message);

        Assert.Throws<ToonEncodingException>(() => ToonSerializer.Serialize(new { Grid = new int[2, 2] }));
    }

    [Fact]
    public void JaggedArrays_RoundTrip()
    {
        int[][] jagged = [[1, 2], [3], []];

        var text = ToonSerializer.Serialize(jagged);

        Assert.Equal("[3]:\n  - [2]: 1,2\n  - [1]: 3\n  - [0]:", text);
        Assert.Equal(jagged, ToonSerializer.Deserialize<int[][]>(text));
    }

    [Fact]
    public void QueuesAndStacks_RoundTripInOrder()
    {
        var queue = new Queue<int>([1, 2, 3]);
        var stack = new Stack<int>([1, 2, 3]); // 3 on top

        Assert.Equal(queue, ToonSerializer.Deserialize<Queue<int>>(ToonSerializer.Serialize(queue)));
        Assert.Equal("[3]: 3,2,1", ToonSerializer.Serialize(stack));
        Assert.Equal(stack, ToonSerializer.Deserialize<Stack<int>>(ToonSerializer.Serialize(stack)));
        Assert.Equal([1, 2, 3], ToonSerializer.Deserialize<ConcurrentQueue<int>>("[3]: 1,2,3")!);
        Assert.Equal(new ConcurrentStack<int>([1, 2, 3]), ToonSerializer.Deserialize<ConcurrentStack<int>>("[3]: 3,2,1")!);
    }

    [Fact]
    public void ImmutableInterfaces_AreCreated()
    {
        Assert.Equal([1, 2], ToonSerializer.Deserialize<IImmutableList<int>>("[2]: 1,2")!);
        Assert.Equal([1, 2], ToonSerializer.Deserialize<IImmutableSet<int>>("[3]: 1,2,1")!.Order());
        Assert.Equal([1, 2], ToonSerializer.Deserialize<IImmutableQueue<int>>("[2]: 1,2")!);

        var stack = ImmutableStack.Create(1, 2, 3);
        Assert.Equal(stack, ToonSerializer.Deserialize<IImmutableStack<int>>(ToonSerializer.Serialize(stack))!);
        Assert.Equal(stack, ToonSerializer.Deserialize<ImmutableStack<int>>(ToonSerializer.Serialize(stack))!);
    }

    [Fact]
    public void ValueTuples_AreNotSupported()
    {
        var ex = Assert.Throws<NotSupportedException>(() => ToonSerializer.Serialize((1, "a")));
        Assert.Contains("Value tuples", ex.Message);
        Assert.Throws<NotSupportedException>(() => ToonSerializer.Deserialize<(int, string)>("Item1: 1"));

        // Reference tuples have properties
        Assert.Equal("Item1: 1\nItem2: a", ToonSerializer.Serialize(Tuple.Create(1, "a")));
    }

    public sealed class ReadOnlyHolder
    {
        public int W { get; set; } = 1;
        public int R => 2;
    }

    [Fact]
    public void IncludeReadOnlyProperties_False_LeavesThemOut()
    {
        var options = new ToonSerializerOptions { IncludeReadOnlyProperties = false };

        Assert.Equal("W: 1\nR: 2", ToonSerializer.Serialize(new ReadOnlyHolder()));
        Assert.Equal("W: 1", ToonSerializer.Serialize(new ReadOnlyHolder(), options));
        Assert.Equal("[2]{W}:\n  1\n  1", ToonSerializer.Serialize(new List<ReadOnlyHolder> { new(), new() }, options));
        Assert.Equal("v:\n  W: 1", ToonSerializer.Serialize(new Dictionary<string, ReadOnlyHolder> { ["v"] = new() }, options));
    }
}
