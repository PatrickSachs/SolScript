using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using PSUtility.Properties;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     A Dicionary where both values are key and value, allowing you to access data in a bi-directionary matter.
    /// </summary>
    /// <typeparam name="TValue1">The first type.</typeparam>
    /// <typeparam name="TValue2">The second type.</typeparam>
    public class BiDictionary<TValue1, TValue2> : IDictionary<TValue1, TValue2>
    {
        private readonly PSDictionary<TValue1, TValue2> m_Map1;
        private readonly PSDictionary<TValue2, TValue1> m_Map2;

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException" accessor="set">
        ///     <paramref name="value" /> is <see langword="null" /> -or-
        ///     <paramref name="key" /> is <see langword="null" />
        /// </exception>
        public TValue1 this[TValue2 key] {
            get { return m_Map2[key]; }
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value));
                }
                if (key == null) {
                    throw new ArgumentNullException(nameof(key));
                }
                m_Map2[key] = value;
                m_Map1[value] = key;
            }
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException" accessor="set">
        ///     <paramref name="value" /> is <see langword="null" /> -or-
        ///     <paramref name="key" /> is <see langword="null" />
        /// </exception>
        public TValue2 this[TValue1 key] {
            get { return m_Map1[key]; }
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value));
                }
                if (key == null) {
                    throw new ArgumentNullException(nameof(key));
                }
                m_Map1[key] = value;
                m_Map2[value] = key;
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TValue1, TValue2>> GetEnumerator()
        {
            foreach (KeyValuePair<TValue1, TValue2> pair in m_Map1) {
                yield return pair;
            }
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<TValue1, TValue2> item)
        {
            TValue2 value2;
            if (!m_Map1.TryGetValue(item.Key, out value2)) {
                return false;
            }
            return m_Map2.Comparer.Equals(item.Value, value2);
        }

        /// <inheritdoc />
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">The start index of the array.</param>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The type of the source <see cref="BiDictionary{TValue1, TValue2}" /> cannot be cast
        ///     automatically to the type of the destination <paramref name="array" />.
        /// </exception>
        /// <exception cref="RankException">The source array is multidimensional.</exception>
        /// <exception cref="InvalidCastException">
        ///     At least one element in the source <see cref="Array" /> cannot be cast
        ///     to the type of destination <paramref name="array" />.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
        /// <exception cref="ArgumentException"><paramref name="array" /> is not long enough.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is smaller than 0.</exception>
        public void CopyTo(KeyValuePair<TValue1, TValue2>[] array, int arrayIndex)
        {
            ArrayUtility.Copy(m_Map1, 0, array, arrayIndex, Count);
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="item.Key" /> is <see langword="null" /> -or-
        ///     <paramref name="item.Value" /> is <see langword="null" />
        /// </exception>
        public bool Remove(KeyValuePair<TValue1, TValue2> item)
        {
            if (item.Key == null) {
                throw new ArgumentNullException("item.Key");
            }
            if (item.Value == null) {
                throw new ArgumentNullException("item.Value");
            }
            TValue2 value2;
            if (!m_Map1.TryGetValue(item.Key, out value2)) {
                return false;
            }
            if (!m_Map2.Comparer.Equals(item.Value, value2)) {
                return false;
            }
            bool r1 = m_Map1.Remove(item.Key);
            bool r2 = m_Map2.Remove(item.Value);
            if (!r1 || !r2) {
                // ReSharper disable once ExceptionNotDocumented
                throw new InvalidOperationException("Internal state corruption.");
            }
            return true;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" /></exception>
        public bool Remove([NotNull] TValue1 key)
        {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            TValue2 value2;
            if (!m_Map1.TryGetValue(key, out value2)) {
                return false;
            }
            bool r1 = m_Map1.Remove(key);
            bool r2 = m_Map2.Remove(value2);
            if (!r1 || !r2) {
                // ReSharper disable once ExceptionNotDocumented
                throw new InvalidOperationException("Internal state corruption.");
            }
            return true;
        }

        /// <inheritdoc />
        public bool TryGetValue(TValue1 key, out TValue2 value)
        {
            return m_Map1.TryGetValue(key, out value);
        }

        /// <summary>
        ///     Creates a "normal" read-only dictionary using the first value as key.
        /// </summary>
        /// <returns>The dictionary.</returns>
        public ReadOnlyDictionary<TValue1, TValue2> Value1AsReadOnly()
        {
            return m_Map1.AsReadOnly();
        }

        /// <summary>
        ///     Creates a "normal" read-only dictionary using the second value as key.
        /// </summary>
        /// <returns>The dictionary.</returns>
        public ReadOnlyDictionary<TValue2, TValue1> Value2AsReadOnly()
        {
            return m_Map2.AsReadOnly();
        }

        /// <inheritdoc />
        public bool Add(KeyValuePair<TValue1, TValue2> item)
        {
            return Add(item.Key, item.Value);
        }

        /// <summary>
        ///     Adds new items to the dictionary.
        /// </summary>
        /// <param name="item1">The first item.</param>
        /// <param name="item2">The second item.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="item1" /> is <see langword="null" /> -or-
        ///     <paramref name="item2" /> is <see langword="null" />
        /// </exception>
        public bool Add(TValue1 item1, TValue2 item2)
        {
            if (item1 == null) {
                throw new ArgumentNullException(nameof(item1));
            }
            if (item2 == null) {
                throw new ArgumentNullException(nameof(item2));
            }
            if (m_Map1.ContainsKey(item1)) {
                return false;
            }
            if (m_Map2.ContainsKey(item2)) {
                return false;
            }
            m_Map1.Add(item1, item2);
            m_Map2.Add(item2, item1);
            return true;
        }

        /// <inheritdoc />
        public bool Add(KeyValuePair<TValue2, TValue1> item)
        {
            return Add(item.Value, item.Key);
        }

        /// <inheritdoc />
        public bool Add(TValue2 item2, TValue1 item1)
        {
            return Add(item1, item2);
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<TValue2, TValue1> item)
        {
            TValue1 value1;
            if (!m_Map2.TryGetValue(item.Key, out value1)) {
                return false;
            }
            return m_Map1.Comparer.Equals(item.Value, value1);
        }

        /// <inheritdoc />
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">The start index of the array.</param>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The type of the source <see cref="BiDictionary{TValue1, TValue2}" /> cannot be cast
        ///     automatically to the type of the destination <paramref name="array" />.
        /// </exception>
        /// <exception cref="RankException">The source array is multidimensional.</exception>
        /// <exception cref="InvalidCastException">
        ///     At least one element in the source <see cref="Array" /> cannot be cast
        ///     to the type of destination <paramref name="array" />.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
        /// <exception cref="ArgumentException"><paramref name="array" /> is not long enough.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is smaller than 0.</exception>
        public void CopyTo(KeyValuePair<TValue2, TValue1>[] array, int arrayIndex)
        {
            ArrayUtility.Copy(m_Map1, 0, array, arrayIndex, Count);
        }

        /// <inheritdoc />
        public bool Contains(TValue1 value1)
        {
            return m_Map1.ContainsKey(value1);
        }

        /// <inheritdoc />
        public bool Contains(TValue2 value2)
        {
            return m_Map2.ContainsKey(value2);
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" /></exception>
        public bool Remove([NotNull] TValue2 key)
        {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            TValue1 value1;
            if (!m_Map2.TryGetValue(key, out value1)) {
                return false;
            }
            bool r1 = m_Map1.Remove(value1);
            bool r2 = m_Map2.Remove(key);
            if (!r1 || !r2) {
                // ReSharper disable once ExceptionNotDocumented
                throw new InvalidOperationException("Internal state corruption.");
            }
            return true;
        }

        /// <inheritdoc />
        public bool TryGetValue(TValue2 key, out TValue1 value)
        {
            return m_Map2.TryGetValue(key, out value);
        }

        #region Constructors

        /// <summary>
        ///     Creates a new BiDictionary.
        /// </summary>
        public BiDictionary()
        {
            m_Map1 = new PSDictionary<TValue1, TValue2>();
            m_Map2 = new PSDictionary<TValue2, TValue1>();
        }

        /// <summary>
        ///     Creates a new BiDictionary.
        /// </summary>
        /// <param name="comparer1">The comparer used to compare values of the first type.</param>
        /// <param name="comparer2">The comparer used to compare values of the second type.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="comparer1" /> is <see langword="null" /> -or-
        ///     <paramref name="comparer2" /> is <see langword="null" />
        /// </exception>
        public BiDictionary([NotNull] IEqualityComparer<TValue1> comparer1, [NotNull] IEqualityComparer<TValue2> comparer2)
        {
            if (comparer1 == null) {
                throw new ArgumentNullException(nameof(comparer1));
            }
            if (comparer2 == null) {
                throw new ArgumentNullException(nameof(comparer2));
            }
            m_Map1 = new PSDictionary<TValue1, TValue2>(comparer1);
            m_Map2 = new PSDictionary<TValue2, TValue1>(comparer2);
        }

        /// <summary>Creates a new Bi-Dictionary from the given source map.</summary>
        /// <param name="map">The source map.</param>
        /// <exception cref="ArgumentException">Invalid source map.</exception>
        public BiDictionary(IDictionary<TValue1, TValue2> map) : this(EqualityComparer<TValue1>.Default, EqualityComparer<TValue2>.Default)
        {
            try {
                m_Map1 = new PSDictionary<TValue1, TValue2>(map);
                m_Map2 = new PSDictionary<TValue2, TValue1>();
                foreach (KeyValuePair<TValue1, TValue2> pair in map) {
                    m_Map2.Add(pair.Value, pair.Key);
                }
            } catch (ArgumentException ex) {
                m_Map1.Clear();
                m_Map2.Clear();
                throw new ArgumentException(Resources.Err_InvalidBiDictionarySource, nameof(map), ex);
            }
        }

        /// <summary>Creates a new Bi-Dictionary from the given source map.</summary>
        /// <param name="map">The source map.</param>
        /// <param name="comparer1">The comparer used to compare values of the first type.</param>
        /// <param name="comparer2">The comparer used to compare values of the second type.</param>
        /// <exception cref="ArgumentException">Invalid source map.</exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="comparer1" /> is <see langword="null" /> -or-
        ///     <paramref name="comparer2" /> is <see langword="null" />
        /// </exception>
        public BiDictionary(IDictionary<TValue1, TValue2> map, [NotNull] IEqualityComparer<TValue1> comparer1, [NotNull] IEqualityComparer<TValue2> comparer2)
        {
            if (comparer1 == null) {
                throw new ArgumentNullException(nameof(comparer1));
            }
            if (comparer2 == null) {
                throw new ArgumentNullException(nameof(comparer2));
            }
            try {
                m_Map1 = new PSDictionary<TValue1, TValue2>(map, comparer1);
                m_Map2 = new PSDictionary<TValue2, TValue1>(comparer2);
                foreach (KeyValuePair<TValue1, TValue2> pair in map) {
                    m_Map2.Add(pair.Value, pair.Key);
                }
            } catch (ArgumentException ex) {
                m_Map1.Clear();
                m_Map2.Clear();
                throw new ArgumentException(Resources.Err_InvalidBiDictionarySource, nameof(map), ex);
            }
        }

        /// <summary>Creates a new Bi-Dictionary from the given source map.</summary>
        /// <param name="map">The source map.</param>
        /// <exception cref="ArgumentException">Invalid source map.</exception>
        public BiDictionary(IDictionary<TValue2, TValue1> map)
        {
            try {
                m_Map1 = new PSDictionary<TValue1, TValue2>();
                m_Map2 = new PSDictionary<TValue2, TValue1>(map);
                foreach (KeyValuePair<TValue2, TValue1> pair in map) {
                    m_Map1.Add(pair.Value, pair.Key);
                }
            } catch (ArgumentException ex) {
                m_Map1.Clear();
                m_Map2.Clear();
                throw new ArgumentException(Resources.Err_InvalidBiDictionarySource, nameof(map), ex);
            }
        }

        #endregion

        #region Value Type Independant Operations

        /// <inheritdoc />
        public void Clear()
        {
            m_Map1.Clear();
            m_Map2.Clear();
        }

        /// <inheritdoc />
        public int Count => m_Map1.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public ReadOnlyCollection<TValue1> Value1 => m_Map1.Keys;

        /// <inheritdoc />
        public ReadOnlyCollection<TValue2> Value2 => m_Map2.Keys;

        #endregion

        #region IDictionary Hidden Methods

        /*
         These methods have been hidden since they are required by the IDictionary interface but do not play well
         with the naming/functionality of this class.
        */

        /// <inheritdoc />
        void ICollection<KeyValuePair<TValue1, TValue2>>.Add(KeyValuePair<TValue1, TValue2> item)
        {
            Add(item);
        }

        /// <inheritdoc />
        bool IDictionary<TValue1, TValue2>.ContainsKey(TValue1 key)
        {
            return Contains(key);
        }

        /// <inheritdoc />
        void IDictionary<TValue1, TValue2>.Add(TValue1 key, TValue2 value)
        {
            Add(key, value);
        }

        // The ICollection implementation is hidden on PSDictionaries, so we need to cast them to IDictionary
        // before we can get them.
        /// <inheritdoc />
        ICollection<TValue1> IDictionary<TValue1, TValue2>.Keys => ((IDictionary<TValue1, TValue2>) m_Map1).Keys;

        /// <inheritdoc />
        ICollection<TValue2> IDictionary<TValue1, TValue2>.Values => ((IDictionary<TValue2, TValue1>) m_Map2).Keys;

        #endregion
    }
}