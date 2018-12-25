using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace PSUtility.Enumerables
{
    internal sealed class DictionaryDebugView<K, V>
    {
        private readonly IDictionary<K, V> dict;

        public DictionaryDebugView(IDictionary<K, V> dictionary)
        {
            dict = dictionary;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<K, V>[] Items {
            get {
                var items = new KeyValuePair<K, V>[dict.Count];
                dict.CopyTo(items, 0);
                return items;
            }
        }
    }

    /// <summary>
    ///     This <see cref="PSDictionary{TKey,TValue}" /> extends
    ///     <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" />.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    [PublicAPI, DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
    public class PSDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private static readonly Func<TKey, TValue> s_DefaultFactory = key => Activator.CreateInstance<TValue>();

        [NotNull]
        public Func<TKey, TValue> Factory { get; set; } = s_DefaultFactory;

        // The lazy dictionary keys.
        private ReadOnlyCollection<TKey> m_Keys;
        // The read-only representation.
        private ReadOnlyDictionary<TKey, TValue> m_ReadOnly;
        // The lazy dictionary values.
        private ReadOnlyCollection<TValue> m_Values;

        /// <inheritdoc />
        public PSDictionary() {}

        /// <inheritdoc />
        public PSDictionary(int capacity) : base(capacity) {}

        /// <inheritdoc />
        public PSDictionary(IEqualityComparer<TKey> comparer) : base(comparer) {}

        /// <inheritdoc />
        public PSDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) {}

        /// <inheritdoc />
        public PSDictionary([NotNull] IDictionary<TKey, TValue> dictionary) : base(dictionary) {}

        /// <inheritdoc />
        public PSDictionary([NotNull] IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer) {}

        /// <inheritdoc />
        protected PSDictionary(SerializationInfo info, StreamingContext context) : base(info, context) {}

        /// <summary>
        ///     All keys in this dictionary. The collection updated automatically.
        /// </summary>
        public new ReadOnlyCollection<TKey> Keys => m_Keys ?? (m_Keys = ReadOnlyCollection<TKey>.Wrap(base.Keys));

        /// <summary>
        ///     All values in this dictionary. The collection updated automatically.
        /// </summary>
        public new ReadOnlyCollection<TValue> Values => m_Values ?? (m_Values = ReadOnlyCollection<TValue>.Wrap(base.Values));

        /// <summary>
        ///     The sync root for threaded access.
        /// </summary>
        public object SyncRoot => ((ICollection)this).SyncRoot;

        /// <summary>
        ///     Gets a read only version of this dictionary. The instance is cahed. This means that only one read only instance per
        ///     dictionary will be created this way. If you need a different instance you need to wrap it manually.
        /// </summary>
        /// <returns>The read only dictionary.</returns>
        public ReadOnlyDictionary<TKey, TValue> AsReadOnly() => m_ReadOnly ?? (m_ReadOnly = ReadOnlyDictionary<TKey, TValue>.Wrap(this));

        /// <summary>
        /// Gets(if already exists) the value of the given key or creates a new one and registers it in this directory(uses <see cref="Factory"/>).
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value.</returns>
        public TValue GetOrCreate(TKey key)
        {
            TValue value;
            if (!TryGetValue(key, out value)) {
                value = Factory(key);
                this[key] = value;
            }
            return value;
        }

        /// <summary>
        ///     Adds a key value pair of an item to this dictionary.
        /// </summary>
        /// <param name="pair">The pair to add.</param>
        /// <exception cref="ArgumentException">
        ///     An element with the same key already exists in the
        ///     <see cref="PSDictionary{TKey,TValue}" />.
        /// </exception>
        public void Add(KeyValuePair<TKey, TValue> pair)
        {
            Add(pair.Key, pair.Value);
        }
        
        /// <summary>
        ///     Adds several new items to the dictionary.
        /// </summary>
        /// <param name="pairs">The items to add.</param>
        /// <exception cref="ArgumentException">
        ///     An element with the same key already exists in the
        ///     <see cref="PSDictionary{TKey,TValue}" />.
        /// </exception>
        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            foreach (KeyValuePair<TKey, TValue> pair in pairs) {
                Add(pair);
            }
        }

        private class __DEBUG
        {
            private readonly PSDictionary<TKey, TValue> m_Dictionary;

            public __DEBUG(PSDictionary<TKey, TValue> dictionary)
            {
                m_Dictionary = dictionary;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public KeyValuePair<TKey, TValue>[] Values {
                get {
                    var array = new KeyValuePair<TKey, TValue>[m_Dictionary.Count];
                    int i = 0;
                    foreach (KeyValuePair<TKey, TValue> pair in m_Dictionary) {
                        if (i == array.Length) {
                            break;
                        }
                        array[i] = pair;
                        i++;
                    }
                    return array;
                }
            }
        }

        /*/// <inheritdoc />
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            TValue value;
            if (!TryGetValue(item.Key, out value)) {
                return false;
            }
            return Equals(value, item.Value);
        }*/

        /// <inheritdoc />
        /// <exception cref="ArrayTypeMismatchException">
        ///     The type of the source <see cref="T:System.Array" /> cannot be cast
        ///     automatically to the type of the destination <paramref name="array" />.
        /// </exception>
        /// <exception cref="RankException">The source array is multidimensional.</exception>
        /// <exception cref="InvalidCastException">
        ///     At least one element in the source <see cref="T:System.Array" /> cannot be cast
        ///     to the type of destination <paramref name="array" />.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="array" /> is not long enough. -or- <paramref name="index" /> is
        ///     smaller than 0.
        /// </exception>
        public void CopyTo(Array array, int index)
        {
            ArrayUtility.Copy(this, 0, array, index, Count);
        }

        /// <summary>
        ///     Copies the elements of this collection to an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="index">The start index of the array.</param>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The type of the source <see cref="T:System.Array" /> is not assignable from
        ///     <see cref="KeyValuePair{TKey, TValue}" />.
        /// </exception>
        /// <exception cref="RankException">The source array is multidimensional.</exception>
        /// <exception cref="InvalidCastException">
        ///     At least one element in the source <see cref="T:System.Array" /> cannot be cast
        ///     to <see cref="KeyValuePair{TKey, TValue}" />.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
        /// <exception cref="ArgumentException"><paramref name="array" /> is not long enough.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is smaller than 0.</exception>
        public void CopyTo(Array<KeyValuePair<TKey, TValue>> array, int index)
        {
            ArrayUtility.Copy(this, 0, array, index, Count);
        }
    }
}