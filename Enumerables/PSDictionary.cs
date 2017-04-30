using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     This <see cref="PSDictionary{TKey,TValue}" /> extends
    ///     <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> and implements
    ///     <see cref="IReadOnlyDictionary{TKey,TValue}" />.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    [PublicAPI]
    public class PSDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IPSDictionary<TKey, TValue>
    {
        // The lazy dictionary keys.
        private Collection<TKey> m_Keys;
        // The lazy dictionary values.
        private Collection<TValue> m_Values;

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

        #region IReadOnlyDictionary<TKey,TValue> Members

        /// <inheritdoc />
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            TValue value;
            if (!TryGetValue(item.Key, out value)) {
                return false;
            }
            return Equals(value, item.Value);
        }

        /// <summary>
        ///     All keys of this dictionary.
        /// </summary>
        public new IReadOnlyCollection<TKey> Keys => m_Keys ?? (m_Keys = new Collection<TKey>(base.Keys));

        /// <summary>
        ///     All values of this dictionary.
        /// </summary>
        public new IReadOnlyCollection<TValue> Values => m_Values ?? (m_Values = new Collection<TValue>(base.Values));

        /// <inheritdoc />
        IEnumerable<TKey> IPSDictionary<TKey, TValue>.Keys => Keys;

        /// <inheritdoc />
        IEnumerable<TValue> IPSDictionary<TKey, TValue>.Values => Values;

        /// <inheritdoc />
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        /// <inheritdoc />
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

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

        #endregion
    }
}