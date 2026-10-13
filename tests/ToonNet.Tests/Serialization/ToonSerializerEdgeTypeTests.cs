using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Numerics;
using ToonNet.Core;
using ToonNet.Core.Serialization;
using ToonNet.Core.Serialization.Attributes;

namespace ToonNet.Tests.Serialization;

/// <summary>
///     Dictionary and collection types without a plain constructor, numbers at the edge of their range and
///     non-public constructors.
/// </summary>
public class ToonSerializerEdgeTypeTests
{
    private const string Map = "a: 1\nb: x";

    [Fact]
    public void Hashtable_RoundTrips()
    {
        var table = new Hashtable { ["a"] = 1L, ["b"] = "x" };

        var copy = ToonSerializer.Deserialize<Hashtable>(ToonSerializer.Serialize(table))!;

        Assert.Equal(2, copy.Count);
        Assert.Equal(1L, copy["a"]);
        Assert.Equal("x", copy["b"]);
    }

    [Fact]
    public void NonGenericDictionaryInterface_ReadsUntypedValues()
    {
        var dictionary = ToonSerializer.Deserialize<IDictionary>(Map)!;

        Assert.Equal(1L, dictionary["a"]);
        Assert.Equal("x", dictionary["b"]);
    }

    [Fact]
    public void ArrayList_AndNonGenericInterfaces_ReadItems()
    {
        const string toon = "[3]: 1,x,true";

        Assert.Equal(new object[] { 1L, "x", true }, ToonSerializer.Deserialize<ArrayList>(toon)!.ToArray());
        Assert.Equal(new object[] { 1L, "x", true }, ToonSerializer.Deserialize<IList>(toon)!.Cast<object>().ToArray());
        Assert.Equal(new object[] { 1L, "x", true }, ToonSerializer.Deserialize<IEnumerable>(toon)!.Cast<object>().ToArray());
    }

    [Fact]
    public void ReadOnlyAndImmutableDictionaries_AreCreated()
    {
        const string toon = "a: 1\nb: 2";

        Assert.Equal(2, ToonSerializer.Deserialize<ReadOnlyDictionary<string, int>>(toon)!["b"]);
        Assert.Equal(2, ToonSerializer.Deserialize<ImmutableDictionary<string, int>>(toon)!["b"]);
        Assert.Equal(2, ToonSerializer.Deserialize<ImmutableSortedDictionary<string, int>>(toon)!["b"]);
        Assert.Equal(2, ToonSerializer.Deserialize<SortedDictionary<string, int>>(toon)!["b"]);
        Assert.Equal(2, ToonSerializer.Deserialize<ConcurrentDictionary<string, int>>(toon)!["b"]);
        Assert.Equal(2, ToonSerializer.Deserialize<IReadOnlyDictionary<string, int>>(toon)!["b"]);
    }

    [Fact]
    public void CustomGenericDictionary_WithoutNonGenericInterface_IsFilled()
    {
        var copy = ToonSerializer.Deserialize<OnlyGenericDictionary>("a: 1\nb: 2")!;

        Assert.Equal(2, copy["b"]);
    }

    [Theory]
    [InlineData(typeof(float), "1e39")]
    [InlineData(typeof(Half), "70000")]
    public void FloatingTargets_RejectValuesOutsideTheirRange(Type type, string number)
    {
        var ex = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize($"V: {number}", typeof(Holder<>).MakeGenericType(type)));

        Assert.Contains("outside the range", ex.Message);
    }

    [Fact]
    public void WideIntegerTargets_ReadNumbersBeyondTheDecimalRangeExactly()
    {
        const string big = "123456789012345678901234567890123";

        Assert.Equal(BigInteger.Parse(big), ToonSerializer.Deserialize<Holder<BigInteger>>($"V: {big}")!.V);
        Assert.Equal(BigInteger.Pow(10, 40), ToonSerializer.Deserialize<Holder<BigInteger>>("V: 1e40")!.V);
        Assert.Equal(Int128.Parse(big), ToonSerializer.Deserialize<Holder<Int128>>($"V: {big}")!.V);
        Assert.Equal(UInt128.MaxValue, ToonSerializer.Deserialize<Holder<UInt128>>($"V: {UInt128.MaxValue}")!.V);
        Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Holder<Int128>>($"V: {UInt128.MaxValue}"));
        Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<Holder<BigInteger>>("V: 1.00000000000000000000000000000000001e30"));
    }

    [Fact]
    public void NonPublicToonConstructor_IsUsed()
    {
        var copy = ToonSerializer.Deserialize<PrivateConstructor>("Name: Ada")!;

        Assert.Equal("Ada", copy.Name);
        Assert.True(copy.FromConstructor);
    }

    public sealed class Holder<T>
    {
        public T V { get; set; } = default!;
    }

    public sealed class OnlyGenericDictionary : IDictionary<string, int>
    {
        private readonly Dictionary<string, int> _inner = new();
        public int this[string key] { get => _inner[key]; set => _inner[key] = value; }
        public ICollection<string> Keys => _inner.Keys;
        public ICollection<int> Values => _inner.Values;
        public int Count => _inner.Count;
        public bool IsReadOnly => false;
        public void Add(string key, int value) => _inner.Add(key, value);
        public void Add(KeyValuePair<string, int> item) => _inner.Add(item.Key, item.Value);
        public void Clear() => _inner.Clear();
        public bool Contains(KeyValuePair<string, int> item) => _inner.Contains(item);
        public bool ContainsKey(string key) => _inner.ContainsKey(key);
        public void CopyTo(KeyValuePair<string, int>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, int>>)_inner).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => _inner.GetEnumerator();
        public bool Remove(string key) => _inner.Remove(key);
        public bool Remove(KeyValuePair<string, int> item) => _inner.Remove(item.Key);
        public bool TryGetValue(string key, out int value) => _inner.TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class PrivateConstructor
    {
        public PrivateConstructor(int unused) => Name = "";

        [ToonConstructor]
        private PrivateConstructor(string name)
        {
            Name = name;
            FromConstructor = true;
        }

        public string Name { get; }

        [ToonIgnore]
        public bool FromConstructor { get; }
    }
}
