using System.Runtime.CompilerServices;

namespace ToonNet.Core.Serialization;

/// <summary>
///     Equality comparers for the dictionaries and sets reading creates, for key types whose default hash codes a
///     document can make collide: a 64-bit or larger value hashes to the XOR of its halves (<c>(i &lt;&lt; 32) | i</c> is
///     0 for every <c>i</c>), which made a few megabytes of keys take seconds of CPU to add. These comparers hash all of
///     the value with <see cref="HashCode"/>, which is seeded randomly per process, and compare as the default comparer.
/// </summary>
internal static class ToonKeyComparer
{
    /// <summary>The comparer for <typeparamref name="T"/>, or null when its default comparer is fine.</summary>
    public static IEqualityComparer<T>? For<T>()
    {
        // typeof comparisons the JIT folds for each T: no boxing of a default value, no branches left
        object? comparer =
            typeof(T) == typeof(long) ? Int64
            : typeof(T) == typeof(ulong) ? UInt64
            : typeof(T) == typeof(double) ? Double
            : typeof(T) == typeof(Guid) ? GuidKeys
            : typeof(T) == typeof(DateTime) ? DateTimeKeys
            : typeof(T) == typeof(DateTimeOffset) ? DateTimeOffsetKeys
            : typeof(T) == typeof(TimeSpan) ? TimeSpanKeys
            : typeof(T) == typeof(Int128) ? Int128Keys
            : typeof(T) == typeof(UInt128) ? UInt128Keys
            : typeof(T) == typeof(nint) && IntPtr.Size == 8 ? NativeInt
            : typeof(T) == typeof(nuint) && IntPtr.Size == 8 ? NativeUInt
            : null;

        return (IEqualityComparer<T>?)comparer;
    }

    private static int Hash(ulong bits) => HashCode.Combine((uint)bits, (uint)(bits >> 32));

    private static readonly Comparer<long> Int64 = new(static v => Hash((ulong)v));
    private static readonly Comparer<ulong> UInt64 = new(static v => Hash(v));

    // Equal doubles hash alike: 0.0 and -0.0 are equal, and so are all NaNs (double.Equals)
    private static readonly Comparer<double> Double = new(static v => v == 0 ? 0 : double.IsNaN(v) ? int.MinValue : Hash(BitConverter.DoubleToUInt64Bits(v)));

    private static readonly Comparer<Guid> GuidKeys = new(static v =>
    {
        Span<ulong> halves = stackalloc ulong[2];
        v.TryWriteBytes(System.Runtime.InteropServices.MemoryMarshal.AsBytes(halves));
        return HashCode.Combine(Hash(halves[0]), Hash(halves[1]));
    });

    // DateTime equality compares ticks (not the kind), DateTimeOffset the UTC instant
    private static readonly Comparer<DateTime> DateTimeKeys = new(static v => Hash((ulong)v.Ticks));
    private static readonly Comparer<DateTimeOffset> DateTimeOffsetKeys = new(static v => Hash((ulong)v.UtcTicks));
    private static readonly Comparer<TimeSpan> TimeSpanKeys = new(static v => Hash((ulong)v.Ticks));
    private static readonly Comparer<Int128> Int128Keys = new(static v => HashCode.Combine(Hash((ulong)v), Hash((ulong)(v >> 64))));
    private static readonly Comparer<UInt128> UInt128Keys = new(static v => HashCode.Combine(Hash((ulong)v), Hash((ulong)(v >> 64))));
    private static readonly Comparer<nint> NativeInt = new(static v => Hash((ulong)v));
    private static readonly Comparer<nuint> NativeUInt = new(static v => Hash(v));

    private sealed class Comparer<T>(Func<T, int> hash) : IEqualityComparer<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(T? x, T? y) => EqualityComparer<T>.Default.Equals(x, y);

        public int GetHashCode(T obj) => hash(obj);
    }
}
