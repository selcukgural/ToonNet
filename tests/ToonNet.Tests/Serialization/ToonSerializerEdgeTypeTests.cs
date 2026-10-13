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
    [Fact]
    public void Dates_AreWrittenWithoutTrailingFractionZeros_AndRoundTrip()
    {
        // ISO 8601 as System.Text.Json (and the official TOON library) writes it: the fraction only as long as needed
        var values = new
        {
            Whole = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            Millis = new DateTime(2026, 1, 2, 3, 4, 5, 120, DateTimeKind.Utc),
            Ticks = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified).AddTicks(1),
            Offset = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(3)),
            Dates = new[] { new DateTime(2026, 1, 2, 3, 4, 5, 500, DateTimeKind.Utc) }
        };

        var toon = ToonSerializer.Serialize(values);

        Assert.Equal("Whole: \"2026-01-02T03:04:05Z\"\nMillis: \"2026-01-02T03:04:05.12Z\"\nTicks: \"2026-01-02T03:04:05.0000001\"\n" +
                     "Offset: \"2026-01-02T03:04:05+03:00\"\nDates[1]: \"2026-01-02T03:04:05.5Z\"", toon);
        Assert.Equal(toon, new ToonNet.Core.Encoding.ToonEncoder().Encode(new ToonNet.Core.Models.ToonDocument(ToonSerializer.SerializeToValue(values))));

        var ticks = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1230);
        Assert.Equal(ticks, ToonSerializer.Deserialize<DateTime>(ToonSerializer.Serialize(ticks)));
        Assert.Equal(DateTimeKind.Utc, ToonSerializer.Deserialize<DateTime>(ToonSerializer.Serialize(ticks)).Kind);
    }

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
    public void WideIntegers_BeyondTheDecimalRange_RoundTripAsQuotedText()
    {
        Assert.Equal(Int128.MaxValue, ToonSerializer.Deserialize<Holder<Int128>>(ToonSerializer.Serialize(new Holder<Int128> { V = Int128.MaxValue }))!.V);
        Assert.Equal(Int128.MinValue, ToonSerializer.Deserialize<Holder<Int128>>(ToonSerializer.Serialize(new Holder<Int128> { V = Int128.MinValue }))!.V);
        Assert.Equal(UInt128.MaxValue, ToonSerializer.Deserialize<Holder<UInt128>>(ToonSerializer.Serialize(new Holder<UInt128> { V = UInt128.MaxValue }))!.V);
        Assert.Equal(BigInteger.Pow(7, 80), ToonSerializer.Deserialize<Holder<BigInteger>>(ToonSerializer.Serialize(new Holder<BigInteger> { V = BigInteger.Pow(7, 80) }))!.V);
        Assert.Contains("\"", ToonSerializer.Serialize(new Holder<Int128> { V = Int128.MaxValue }));
    }

    [Fact]
    public void NonPublicToonConstructor_IsUsed()
    {
        var copy = ToonSerializer.Deserialize<PrivateConstructor>("Name: Ada")!;

        Assert.Equal("Ada", copy.Name);
        Assert.True(copy.FromConstructor);
    }

    [Fact]
    public void CollectionTargets_AreCreatedFromTheReadItems()
    {
        const string toon = "[3]: 3,1,2";

        Assert.Equal(new[] { 3, 1, 2 }, ToonSerializer.Deserialize<int[]>(toon));
        Assert.Equal(new[] { 3, 1, 2 }, ToonSerializer.Deserialize<LinkedList<int>>(toon));
        Assert.Equal(new[] { 3, 1, 2 }, ToonSerializer.Deserialize<Collection<int>>(toon));
        Assert.Equal(new[] { 3, 1, 2 }, ToonSerializer.Deserialize<ImmutableList<int>>(toon));
        Assert.Equal(new[] { 1, 2, 3 }, ToonSerializer.Deserialize<SortedSet<int>>(toon));
        Assert.Equal(new[] { 3, 1, 2 }, ToonSerializer.Deserialize<IReadOnlySet<int>>(toon)!.ToArray());
        Assert.Equal(new[] { 3, 1, 2 }, ToonSerializer.Deserialize<IEnumerable<int>>(toon));
    }

    [Fact]
    public void CollectionTarget_ThatCannotBeCreated_Throws()
    {
        var ex = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<ReadOnlyCollection<int>>("[1]: 1"));

        Assert.Contains("Cannot create a collection of type ReadOnlyCollection", ex.Message);
    }

    [Theory]
    [InlineData(typeof(byte), "255", (byte)255)]
    [InlineData(typeof(sbyte), "-128", (sbyte)-128)]
    [InlineData(typeof(short), "-32768", short.MinValue)]
    [InlineData(typeof(ushort), "65535", ushort.MaxValue)]
    [InlineData(typeof(int), "-2147483648", int.MinValue)]
    [InlineData(typeof(uint), "4294967295", uint.MaxValue)]
    [InlineData(typeof(long), "9223372036854775807", long.MaxValue)]
    [InlineData(typeof(ulong), "18446744073709551615", ulong.MaxValue)]
    public void IntegerTargets_ReadTheirWholeRange(Type type, string number, object expected)
    {
        var value = ToonSerializer.Deserialize(number, type);

        Assert.IsType(type, value);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(typeof(byte), "256")]
    [InlineData(typeof(sbyte), "-129")]
    [InlineData(typeof(short), "32768")]
    [InlineData(typeof(ushort), "-1")]
    [InlineData(typeof(int), "2147483648")]
    [InlineData(typeof(uint), "-1")]
    [InlineData(typeof(long), "9223372036854775808")]
    [InlineData(typeof(ulong), "-1")]
    public void IntegerTargets_RejectValuesOutsideTheirRange(Type type, string number)
    {
        var ex = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize(number, type));

        Assert.Contains($"outside the range of {type.Name}", ex.Message);
    }

    [Fact]
    public void ThrowingConstructor_IsReportedWithTheTargetType()
    {
        var parameterless = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<ThrowingConstructor>("Name: x"));
        var parameterized = Assert.Throws<ToonSerializationException>(() => ToonSerializer.Deserialize<ThrowingRecord>("Name: x"));

        Assert.IsType<InvalidOperationException>(parameterless.InnerException);
        Assert.Contains("The constructor of ThrowingConstructor threw: boom", parameterless.Message);
        Assert.IsType<ArgumentException>(parameterized.InnerException);
        Assert.Contains("The constructor of ThrowingRecord threw", parameterized.Message);
    }

    [Fact]
    public void ConstructorParameters_WithoutEntries_UseTheirDefaults()
    {
        var copy = ToonSerializer.Deserialize<WithDefaults>("Name: x")!;

        Assert.Equal(("x", 7, 0, default(Guid)), (copy.Name, copy.Count, copy.Other, copy.Id));
    }

    [Fact]
    public void ConverterReturningNull_ForValueTypeParameter_PassesTheDefault()
    {
        var options = new ToonSerializerOptions();
        options.AddConverter(new NullIntConverter());

        Assert.Equal(0, ToonSerializer.Deserialize<WithDefaults>("Name: x\nCount: 5", options)!.Count);
    }

    public sealed class ThrowingConstructor
    {
        public ThrowingConstructor() => throw new InvalidOperationException("boom");

        public string Name { get; set; } = "";
    }

    public sealed record ThrowingRecord
    {
        public ThrowingRecord(string name) => Name = name.Length > 0 ? throw new ArgumentException("bad name", nameof(name)) : name;

        public string Name { get; }
    }

    public sealed record WithDefaults(string Name, int Count = 7, int Other = default, Guid Id = default);

    /// <summary>Reads every <see cref="int"/> as null, which a value type parameter receives as 0.</summary>
    private sealed class NullIntConverter : IToonConverter
    {
        public bool CanConvert(Type type) => type == typeof(int);

        public ToonNet.Core.Models.ToonValue Write(object? value, ToonSerializerOptions options) => (int)value!;

        public object? Read(ToonNet.Core.Models.ToonValue value, Type targetType, ToonSerializerOptions options) => null;
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
