using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace ToonNet.Core.Models;

/// <summary>
///     The properties of a <see cref="ToonObject"/>: a dictionary that keeps keys in insertion order.
/// </summary>
/// <remarks>
///     Key order is significant in TOON, so unlike <see cref="Dictionary{TKey,TValue}"/> a key added after a removal is
///     always placed last. Replacing the value of an existing key keeps its position. Lookups are O(1) (small
///     collections use a linear search, which is faster for a few keys); removing a key is O(n).
/// </remarks>
public sealed class ToonPropertyCollection : IDictionary<string, ToonValue>, IReadOnlyDictionary<string, ToonValue>
{
    /// <summary>Collections with more properties than this keep a hash index of their keys.</summary>
    private const int IndexThreshold = 16;

    private KeyValuePair<string, ToonValue>[] _entries;
    private int _count;
    private int _version;

    /// <summary>
    ///     Key positions in <see cref="_entries"/>; null until a lookup in a collection of more than
    ///     <see cref="IndexThreshold"/> properties.
    /// </summary>
    private Dictionary<string, int>? _indexes;

    /// <summary>
    ///     Creates an empty collection.
    /// </summary>
    public ToonPropertyCollection()
    {
        _entries = [];
    }

    /// <summary>
    ///     Creates an empty collection with room for <paramref name="capacity"/> properties.
    /// </summary>
    /// <param name="capacity">The number of properties to reserve room for; 0 or less reserves none.</param>
    public ToonPropertyCollection(int capacity)
    {
        _entries = capacity > 0 ? new KeyValuePair<string, ToonValue>[capacity] : [];
    }

    /// <summary>
    ///     Creates a collection with the given properties, in their enumeration order.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when a key occurs more than once.</exception>
    public ToonPropertyCollection(IEnumerable<KeyValuePair<string, ToonValue>> properties)
        : this(properties is ICollection<KeyValuePair<string, ToonValue>> collection ? collection.Count : 0)
    {
        ArgumentNullException.ThrowIfNull(properties);

        foreach (var (key, value) in properties)
        {
            Add(key, value);
        }
    }

    /// <summary>
    ///     Gets the number of properties.
    /// </summary>
    public int Count => _count;

    /// <summary>
    ///     Gets the keys in order. The list is a snapshot.
    /// </summary>
    public IReadOnlyList<string> Keys => SnapshotKeys();

    /// <summary>
    ///     Gets the values in key order. The list is a snapshot.
    /// </summary>
    public IReadOnlyList<ToonValue> Values => SnapshotValues();

    /// <summary>
    ///     Gets or sets the value of a key. Setting a new key appends it; setting an existing key keeps its position.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown by the getter when the key does not exist.</exception>
    public ToonValue this[string key]
    {
        get => IndexOf(key) is var index and >= 0 ? _entries[index].Value : throw new KeyNotFoundException($"The key {ToonErrorText.Quote(key)} was not found.");
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (IndexOf(key) is var index and >= 0)
            {
                _entries[index] = new KeyValuePair<string, ToonValue>(key, value);
                _version++;
            }
            else
            {
                Append(key, value);
            }
        }
    }

    /// <summary>
    ///     Adds a property at the end.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the key already exists.</exception>
    public void Add(string key, ToonValue value)
    {
        if (!TryAdd(key, value))
        {
            throw new ArgumentException($"An item with the same key has already been added. Key: {ToonErrorText.Quote(key)}", nameof(key));
        }
    }

    /// <summary>
    ///     Adds a property at the end unless the key already exists.
    /// </summary>
    /// <returns><c>true</c> when the property was added.</returns>
    public bool TryAdd(string key, ToonValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (IndexOf(key) >= 0)
        {
            return false;
        }

        Append(key, value);
        return true;
    }

    /// <summary>
    ///     Determines whether the key exists.
    /// </summary>
    public bool ContainsKey(string key) => IndexOf(key) >= 0;

    /// <summary>
    ///     Gets the value of a key.
    /// </summary>
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out ToonValue value)
    {
        if (IndexOf(key) is var index and >= 0)
        {
            value = _entries[index].Value;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    ///     Removes a property; the following properties keep their order.
    /// </summary>
    /// <returns><c>true</c> when the key existed.</returns>
    public bool Remove(string key) => Remove(key, out _);

    /// <summary>
    ///     Removes a property and returns its value.
    /// </summary>
    public bool Remove(string key, [MaybeNullWhen(false)] out ToonValue value)
    {
        var index = IndexOf(key);

        if (index < 0)
        {
            value = null;
            return false;
        }

        value = _entries[index].Value;
        _count--;
        Array.Copy(_entries, index + 1, _entries, index, _count - index);
        _entries[_count] = default;
        _version++;

        if (_indexes != null)
        {
            _indexes.Remove(key);

            for (var i = index; i < _count; i++)
            {
                _indexes[_entries[i].Key] = i;
            }
        }

        return true;
    }

    /// <summary>
    ///     Removes the properties with the given keys at once, in time linear in the number of properties (removing them
    ///     one by one shifts the following properties each time); the others keep their order.
    /// </summary>
    /// <returns>The number of properties removed.</returns>
    internal int RemoveKeys(HashSet<string> keys)
    {
        var write = 0;

        for (var read = 0; read < _count; read++)
        {
            if (!keys.Contains(_entries[read].Key))
            {
                _entries[write++] = _entries[read];
            }
        }

        var removed = _count - write;

        if (removed == 0)
        {
            return 0;
        }

        Array.Clear(_entries, write, removed);
        _count = write;
        _version++;

        if (_indexes != null)
        {
            _indexes.Clear();

            for (var i = 0; i < _count; i++)
            {
                _indexes[_entries[i].Key] = i;
            }
        }

        return removed;
    }

    /// <summary>
    ///     Removes all properties.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_entries, 0, _count);
        _count = 0;
        _indexes = null;
        _version++;
    }

    /// <summary>
    ///     Returns an enumerator over the properties in order.
    /// </summary>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>
    ///     The property at <paramref name="index"/>, in insertion order (no bounds check beyond the array's).
    /// </summary>
    internal ref readonly KeyValuePair<string, ToonValue> EntryAt(int index) => ref _entries[index];

    /// <returns>The position of <paramref name="key"/>, or -1.</returns>
    private int IndexOf(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_indexes == null && _count > IndexThreshold)
        {
            // Lookups may run on several threads at once: the index is published only when complete
            var indexes = new Dictionary<string, int>(_count, StringComparer.Ordinal);

            for (var i = 0; i < _count; i++)
            {
                indexes.Add(_entries[i].Key, i);
            }

            _indexes = indexes;
        }

        if (_indexes != null)
        {
            return _indexes.TryGetValue(key, out var index) ? index : -1;
        }

        var entries = _entries;

        for (var i = 0; i < _count; i++)
        {
            if (string.Equals(entries[i].Key, key))
            {
                return i;
            }
        }

        return -1;
    }

    private void Append(string key, ToonValue value)
    {
        if (_count == _entries.Length)
        {
            Array.Resize(ref _entries, _entries.Length == 0 ? 4 : _entries.Length * 2);
        }

        _entries[_count] = new KeyValuePair<string, ToonValue>(key, value);
        _count++;
        _version++;

        _indexes?.Add(key, _count - 1);
    }

    /// <summary>
    ///     Adds a property at the end without looking for its key, which the caller knows is not there (the fields of a
    ///     parsed row). The index is built by the first lookup, so rows that are only enumerated never get one.
    /// </summary>
    internal void AppendUnique(string key, ToonValue value) => Append(key, value);

    private List<string> SnapshotKeys()
    {
        var keys = new List<string>(_count);

        for (var i = 0; i < _count; i++)
        {
            keys.Add(_entries[i].Key);
        }

        return keys;
    }

    private List<ToonValue> SnapshotValues()
    {
        var values = new List<ToonValue>(_count);

        for (var i = 0; i < _count; i++)
        {
            values.Add(_entries[i].Value);
        }

        return values;
    }

    /// <summary>
    ///     Enumerates the properties in order.
    /// </summary>
    /// <remarks>
    ///     Like <see cref="List{T}.Enumerator"/>, it throws when the collection is modified during enumeration.
    /// </remarks>
    public struct Enumerator : IEnumerator<KeyValuePair<string, ToonValue>>
    {
        private readonly ToonPropertyCollection _collection;
        private readonly int _version;
        private int _index;

        internal Enumerator(ToonPropertyCollection collection)
        {
            _collection = collection;
            _version = collection._version;
            _index = -1;
            Current = default;
        }

        /// <summary>
        ///     Gets the property at the current position.
        /// </summary>
        public KeyValuePair<string, ToonValue> Current { get; private set; }

        readonly object IEnumerator.Current => Current;

        /// <summary>
        ///     Advances to the next property.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the collection was modified.</exception>
        public bool MoveNext()
        {
            if (_version != _collection._version)
            {
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            }

            if (++_index < _collection._count)
            {
                Current = _collection._entries[_index];
                return true;
            }

            _index = _collection._count;
            Current = default;
            return false;
        }

        /// <summary>
        ///     Moves before the first property.
        /// </summary>
        public void Reset()
        {
            if (_version != _collection._version)
            {
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            }

            _index = -1;
            Current = default;
        }

        /// <summary>
        ///     Does nothing.
        /// </summary>
        public readonly void Dispose() { }
    }

    #region Explicit interface implementations

    ICollection<string> IDictionary<string, ToonValue>.Keys => SnapshotKeys();

    ICollection<ToonValue> IDictionary<string, ToonValue>.Values => SnapshotValues();

    IEnumerable<string> IReadOnlyDictionary<string, ToonValue>.Keys => Keys;

    IEnumerable<ToonValue> IReadOnlyDictionary<string, ToonValue>.Values => Values;

    bool ICollection<KeyValuePair<string, ToonValue>>.IsReadOnly => false;

    void ICollection<KeyValuePair<string, ToonValue>>.Add(KeyValuePair<string, ToonValue> item) => Add(item.Key, item.Value);

    bool ICollection<KeyValuePair<string, ToonValue>>.Contains(KeyValuePair<string, ToonValue> item) =>
        TryGetValue(item.Key, out var value) && EqualityComparer<ToonValue>.Default.Equals(value, item.Value);

    void ICollection<KeyValuePair<string, ToonValue>>.CopyTo(KeyValuePair<string, ToonValue>[] array, int arrayIndex) =>
        Array.Copy(_entries, 0, array, arrayIndex, _count);

    bool ICollection<KeyValuePair<string, ToonValue>>.Remove(KeyValuePair<string, ToonValue> item) =>
        ((ICollection<KeyValuePair<string, ToonValue>>)this).Contains(item) && Remove(item.Key);

    IEnumerator<KeyValuePair<string, ToonValue>> IEnumerable<KeyValuePair<string, ToonValue>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion
}
