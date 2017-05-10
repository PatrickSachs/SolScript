using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     A MultiDictionary associates multiple values with a single key. Use a multi dicitonary in cases in which a
    ///     <see cref="IDictionary{TKey,TValue}" /> which an <see cref="IList{T}" /> as value would have been used. The
    ///     dictionary automatically creates the value lists when you try to access them. The only exception to this is the
    ///     <see cref="TryGetValue" /> method.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValues">The type of all values.</typeparam>
    [PublicAPI]
    public class MultiDictionary<TKey, TValues> : IDictionary<TKey, IList<TValues>>
    {
        private readonly PSDictionary<TKey, PSList<TValues>> m_Dictionary;
        
        // The lazy dictionary keys.
        private ReadOnlyImpl m_ReadOnly;

        private ReadOnlyCollection<ReadOnlyList<TValues>> m_Values;

        /// <inheritdoc />
        public MultiDictionary() : this(EqualityComparer<TKey>.Default) {}

        /// <inheritdoc />
        public MultiDictionary(IEqualityComparer<TKey> comparer)
        {
            m_Dictionary = new PSDictionary<TKey, PSList<TValues>>(comparer);
        }

        /// <summary>
        ///     All keys of this dictionary.
        /// </summary>
        public ReadOnlyCollection<TKey> Keys => m_Dictionary.Keys;

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, IList<TValues>>> GetEnumerator()
        {
            foreach (KeyValuePair<TKey, PSList<TValues>> pair in m_Dictionary) {
                yield return new KeyValuePair<TKey, IList<TValues>>(pair.Key, pair.Value);
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        void ICollection<KeyValuePair<TKey, IList<TValues>>>.Add(KeyValuePair<TKey, IList<TValues>> item)
        {
            GetList(item.Key).AddRange(item.Value);
        }

        /// <inheritdoc />
        public void Clear()
        {
            m_Dictionary.Clear();
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<TKey, IList<TValues>>>.Contains(KeyValuePair<TKey, IList<TValues>> item)
        {
            PSList<TValues> list = GetListNoAlloc(item.Key);
            if (list == null) {
                return false;
            }
            foreach (TValues value in item.Value) {
                if (!list.Contains(value)) {
                    return false;
                }
            }
            return true;
        }

        /// <inheritdoc />
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">The start index of the array.</param>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The type of the source <see cref="MultiDictionary{TKey, TValues}" /> cannot be cast
        ///     automatically to the type of the destination <paramref name="array" />.
        /// </exception>
        /// <exception cref="RankException">The source array is multidimensional.</exception>
        /// <exception cref="InvalidCastException">
        ///     At least one element in the source <see cref="Array" /> cannot be cast
        ///     to the type of destination <paramref name="array" />.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
        /// <exception cref="ArgumentException"><paramref name="array" /> is not long enough.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is smaller than 0.</exception>
        public void CopyTo(KeyValuePair<TKey, IList<TValues>>[] array, int arrayIndex)
        {
            ArrayUtility.Copy(this, 0, array, arrayIndex, Count);
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<TKey, IList<TValues>>>.Remove(KeyValuePair<TKey, IList<TValues>> item)
        {
            PSList<TValues> list = GetListNoAlloc(item.Key);
            if (list == null) {
                return false;
            }
            bool didRemoveAll = true;
            foreach (TValues value in item.Value) {
                if (!list.Remove(value)) {
                    didRemoveAll = false;
                }
            }
            return didRemoveAll;
        }

        /// <inheritdoc />
        public int Count => m_Dictionary.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public bool ContainsKey(TKey key)
        {
            return m_Dictionary.ContainsKey(key);
        }

        /// <inheritdoc />
        void IDictionary<TKey, IList<TValues>>.Add(TKey key, IList<TValues> value)
        {
            AddRange(key, value);
        }

        /// <inheritdoc />
        public bool Remove(TKey key)
        {
            return m_Dictionary.Remove(key);
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out IList<TValues> value)
        {
            PSList<TValues> list = GetListNoAlloc(key);
            if (list != null) {
                value = list;
                return true;
            }
            value = null;
            return false;
        }

        /// <inheritdoc />
        public IList<TValues> this[TKey key] {
            get { return GetList(key); }
            set { m_Dictionary[key] = new PSList<TValues>(value); }
        }

        /// <inheritdoc />
        ICollection<TKey> IDictionary<TKey, IList<TValues>>.Keys => ((IDictionary<TKey, PSList<TValues>>) m_Dictionary).Keys;

        /// <inheritdoc />
        ICollection<IList<TValues>> IDictionary<TKey, IList<TValues>>.Values {
            get { return (ICollection<IList<TValues>>) ((IDictionary<TKey, PSList<TValues>>) m_Dictionary).Values; }
        }

        public ReadOnlyCollection<ReadOnlyList<TValues>> Values {
            get {
                if (m_Values == null) {
                    m_Values = ReadOnlyCollection<ReadOnlyList<TValues>>.Wrap(
                        ((IDictionary<TKey, PSList<TValues>>)m_Dictionary).Values,
                        list => list.AsReadOnly());
                }
                return m_Values;
            }
        }

        public ReadOnlyDictionary<TKey, ReadOnlyList<TValues>> AsReadOnly()
        {
            if (m_ReadOnly == null) {
                m_ReadOnly = new ReadOnlyImpl(this);
            }
            return m_ReadOnly;
        }

        /// <summary>
        ///     Adds several new items to the dictionary.
        /// </summary>
        /// <param name="key">The item key.</param>
        /// <param name="values">The values to add.</param>
        public void AddRange(TKey key, IEnumerable<TValues> values)
        {
            GetList(key).AddRange(values);
        }

        /// <summary>
        ///     Adds a single new item to the dictionary.
        /// </summary>
        /// <param name="key">The item key.</param>
        /// <param name="value">The value.</param>
        public void Add(TKey key, TValues value)
        {
            GetList(key).Add(value);
        }

        /// <summary>
        ///     Gets how many items are in this dictionary in total. This requires iteration through all keys.
        /// </summary>
        /// <returns>The item count.</returns>
        public int GetFlatCount()
        {
            int count = 0;
            foreach (PSList<TValues> value in m_Dictionary.Values) {
                count += value.Count;
            }
            return count;
        }

        /// <summary>
        ///     Adds several new items to the dictionary.
        /// </summary>
        /// <param name="key">The item key.</param>
        /// <param name="value">The values to add.</param>
        public void AddRange(TKey key, params TValues[] value)
        {
            AddRange(key, (IEnumerable<TValues>) value);
        }

        private class ReadOnlyImpl : ReadOnlyDictionary<TKey, ReadOnlyList<TValues>>
        {
            private readonly MultiDictionary<TKey, TValues> m_Dictionary;

            /// <inheritdoc />
            /// <exception cref="ArgumentNullException"><paramref name="dictionary" /> is <see langword="null" /></exception>
            public ReadOnlyImpl(MultiDictionary<TKey, TValues> dictionary)
            {
                if (dictionary == null) {
                    throw new ArgumentNullException(nameof(dictionary));
                }
                m_Dictionary = dictionary;
            }

            /// <inheritdoc />
            public override int Count => m_Dictionary.Count;

            /// <inheritdoc />
            public override ReadOnlyList<TValues> this[TKey key] {
                get {
                    PSList<TValues> list = m_Dictionary.GetListNoAlloc(key);
                    if (list == null) {
                        return ReadOnlyList<TValues>.Empty();
                    }
                    return list.AsReadOnly();
                }
            }

            /// <inheritdoc />
            public override ReadOnlyCollection<TKey> Keys => m_Dictionary.Keys;

            /// <inheritdoc />
            public override ReadOnlyCollection<ReadOnlyList<TValues>> Values => m_Dictionary.Values;

            /// <inheritdoc />
            public override IEnumerator<KeyValuePair<TKey, ReadOnlyList<TValues>>> GetEnumerator()
            {
                foreach (var pair in m_Dictionary.m_Dictionary) {
                    yield return new KeyValuePair<TKey, ReadOnlyList<TValues>>(pair.Key, pair.Value.AsReadOnly());
                }
            }

            /// <inheritdoc />
            public override bool ContainsKey(TKey key)
            {
                return m_Dictionary.ContainsKey(key);
            }

            /// <inheritdoc />
            public override bool TryGetValue(TKey key, out ReadOnlyList<TValues> value)
            {
                var list = m_Dictionary.GetListNoAlloc(key);
                if (list != null) {
                    value = list.AsReadOnly();
                    return true;
                }
                value = null;
                return false;
            }
        }

        #region List Helper

        [CanBeNull]
        private PSList<TValues> GetListNoAlloc([NotNull] TKey key)
        {
            PSList<TValues> list;
            if (!m_Dictionary.TryGetValue(key, out list)) {
                return null;
            }
            return list;
        }

        [NotNull]
        private PSList<TValues> GetList([NotNull] TKey key)
        {
            PSList<TValues> list;
            if (!m_Dictionary.TryGetValue(key, out list)) {
                m_Dictionary[key] = list = new PSList<TValues>();
            }
            return list;
        }

        #endregion
    }
}