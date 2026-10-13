using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace ToonNet.Core.Models;

/// <summary>
///     The properties of a <see cref="ToonObject"/>: a dictionary that keeps keys in insertion order.
/// </summary>
/// <remarks>
///     Key order is significant in TOON, so unlike <see cref="Dictionary{TKey,TValue}"/> a key added after a removal is
///     always placed last. Replacing the value of an existing key keeps its position. Lookups are O(1); removing a key is
///     O(n).
/// </remarks>
public sealed class ToonPropertyCollection : IDictionary<string, ToonValue>, IReadOnlyDictionary<string, ToonValue>
{
    private readonly List<KeyValuePair<string, ToonValue>> _entries = [];
    private readonly Dictionary<string, int> _indexes = new(StringComparer.Ordinal);

    /// <summary>
    ///     Creates an empty collection.
    /// </summary>
    public ToonPropertyCollection() { }

    /// <summary>
    ///     Creates a collection with the given properties, in their enumeration order.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when a key occurs more than once.</exception>
    public ToonPropertyCollection(IEnumerable<KeyValuePair<string, ToonValue>> properties)
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
    public int Count => _entries.Count;

    /// <summary>
    ///     Gets the keys in order. The list is a snapshot.
    /// </summary>
    public IReadOnlyList<string> Keys => _entries.ConvertAll(e => e.Key);

    /// <summary>
    ///     Gets the values in key order. The list is a snapshot.
    /// </summary>
    public IReadOnlyList<ToonValue> Values => _entries.ConvertAll(e => e.Value);

    /// <summary>
    ///     Gets or sets the value of a key. Setting a new key appends it; setting an existing key keeps its position.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown by the getter when the key does not exist.</exception>
    public ToonValue this[string key]
    {
        get => _indexes.TryGetValue(key, out var index) ? _entries[index].Value : throw new KeyNotFoundException($"The key '{key}' was not found.");
        set
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);

            if (_indexes.TryGetValue(key, out var index))
            {
                _entries[index] = new KeyValuePair<string, ToonValue>(key, value);
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
            throw new ArgumentException($"An item with the same key has already been added. Key: {key}", nameof(key));
        }
    }

    /// <summary>
    ///     Adds a property at the end unless the key already exists.
    /// </summary>
    /// <returns><c>true</c> when the property was added.</returns>
    public bool TryAdd(string key, ToonValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        if (_indexes.ContainsKey(key))
        {
            return false;
        }

        Append(key, value);
        return true;
    }

    /// <summary>
    ///     Determines whether the key exists.
    /// </summary>
    public bool ContainsKey(string key) => _indexes.ContainsKey(key);

    /// <summary>
    ///     Gets the value of a key.
    /// </summary>
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out ToonValue value)
    {
        if (_indexes.TryGetValue(key, out var index))
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
        if (!_indexes.Remove(key, out var index))
        {
            value = null;
            return false;
        }

        value = _entries[index].Value;
        _entries.RemoveAt(index);

        for (var i = index; i < _entries.Count; i++)
        {
            _indexes[_entries[i].Key] = i;
        }

        return true;
    }

    /// <summary>
    ///     Removes all properties.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
        _indexes.Clear();
    }

    /// <summary>
    ///     Returns an enumerator over the properties in order.
    /// </summary>
    public List<KeyValuePair<string, ToonValue>>.Enumerator GetEnumerator() => _entries.GetEnumerator();

    private void Append(string key, ToonValue value)
    {
        _indexes.Add(key, _entries.Count);
        _entries.Add(new KeyValuePair<string, ToonValue>(key, value));
    }

    #region Explicit interface implementations

    ICollection<string> IDictionary<string, ToonValue>.Keys => _entries.ConvertAll(e => e.Key);

    ICollection<ToonValue> IDictionary<string, ToonValue>.Values => _entries.ConvertAll(e => e.Value);

    IEnumerable<string> IReadOnlyDictionary<string, ToonValue>.Keys => Keys;

    IEnumerable<ToonValue> IReadOnlyDictionary<string, ToonValue>.Values => Values;

    bool ICollection<KeyValuePair<string, ToonValue>>.IsReadOnly => false;

    void ICollection<KeyValuePair<string, ToonValue>>.Add(KeyValuePair<string, ToonValue> item) => Add(item.Key, item.Value);

    bool ICollection<KeyValuePair<string, ToonValue>>.Contains(KeyValuePair<string, ToonValue> item) =>
        TryGetValue(item.Key, out var value) && EqualityComparer<ToonValue>.Default.Equals(value, item.Value);

    void ICollection<KeyValuePair<string, ToonValue>>.CopyTo(KeyValuePair<string, ToonValue>[] array, int arrayIndex) => _entries.CopyTo(array, arrayIndex);

    bool ICollection<KeyValuePair<string, ToonValue>>.Remove(KeyValuePair<string, ToonValue> item) =>
        ((ICollection<KeyValuePair<string, ToonValue>>)this).Contains(item) && Remove(item.Key);

    IEnumerator<KeyValuePair<string, ToonValue>> IEnumerable<KeyValuePair<string, ToonValue>>.GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _entries.GetEnumerator();

    #endregion
}
