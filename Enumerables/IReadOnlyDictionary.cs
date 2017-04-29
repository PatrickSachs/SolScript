
#if !NETFX_45

// ReSharper disable once CheckNamespace

namespace System.Collections.Generic
{
    /// <summary>
    ///     A 1:1 representation of the <see cref="IReadOnlyDictionary{TKey,TValue}" /> from .NET 4.5
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    public interface IReadOnlyDictionary<TKey, TValue> : IReadOnlyCollection<KeyValuePair<TKey, TValue>>
    {
        /// <summary>
        ///     Indexes the <see cref="IReadOnlyDictionary{TKey,TValue}" /> with the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value associated with this key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key" /> is null</exception>
        /// <exception cref="KeyNotFoundException">The property is retrieved and key is not found.</exception>
        TValue this[TKey key] { get; }

        /// <summary>
        ///     Gets an enumerable collection that contains the keys in the read-only dictionary.
        /// </summary>
        IEnumerable<TKey> Keys { get; }

        /// <summary>
        ///     Gets an enumerable collection that contains the values in the read-only dictionary.
        /// </summary>
        IEnumerable<TValue> Values { get; }

        /// <summary>
        ///     Determines whether the read-only dictionary contains an element that has the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>true if the key is contained, false if not.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key" /> is null</exception>
        bool ContainsKey(TKey key);

        /// <summary>
        ///     Gets the value that is associated with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value. Only valid if the method returned true.</param>
        /// <returns>true if the value was retrieved, false if not.</returns>
        bool TryGetValue(TKey key, out TValue value);
    }
}

#endif