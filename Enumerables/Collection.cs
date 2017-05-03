using System;
using System.Collections;
using System.Collections.Generic;
using PSUtility.Properties;
using PSUtility.Strings;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     The collection implements <see cref="ICollection{T}" /> and <see cref="IReadOnlyCollection{T}" />. You can
    ///     wrap any standard collection inside of this collection or create a new one entirely.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Collection<T> : ICollection<T>//, IReadOnlyCollection<T>
    {
        // The wrapped collection.
        private readonly ICollection<T> m_Collection;

        private ReadOnlyCollection<T> m_ReadOnly;

        public ReadOnlyCollection<T> AsReadOnly()
        {
            if (m_ReadOnly == null) {
                m_ReadOnly = new ReadOnlyCollection<T>(this);
            }
            return m_ReadOnly;
        }

        /// <summary>
        ///     Wraps an already existing <see cref="ICollection{T}" /> inside a <see cref="Collection{T}" />.
        /// </summary>
        /// <param name="collection">The collection to wrap.</param>
        public Collection(ICollection<T> collection)
        {
            m_Collection = collection;
        }

        /// <summary>
        ///     Creates a new <see cref="Collection{T}" />.
        /// </summary>
        public Collection() : this(new System.Collections.Generic.List<T>()) {}

        /// <summary>
        ///     Gets the collection wrapped inside of this wrapper.
        /// </summary>
        /// <returns>The collection.</returns>
        public ICollection<T> GetCollection() => m_Collection;

        #region ICollection<T> Members

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return m_Collection.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public void Add(T item)
        {
            m_Collection.Add(item);
        }

        /// <inheritdoc />
        public void Clear()
        {
            m_Collection.Clear();
        }


        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            m_Collection.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            return m_Collection.Remove(item);
        }

        /// <inheritdoc />
        int ICollection<T>.Count => m_Collection.Count;

        /// <inheritdoc />
        public bool IsReadOnly => m_Collection.IsReadOnly;

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return m_Collection.Contains(item);
        }

        #endregion

        #region IReadOnlyCollection<T> Members

        /// <inheritdoc />
        public int Count => m_Collection.Count;

        /// <summary>
        ///     Copies the elements of this collection to an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="index">The start index of the array.</param>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The type of the source <see cref="IReadOnlyCollection{T}" /> cannot be cast
        ///     automatically to the type of the destination <paramref name="array" />.
        /// </exception>
        /// <exception cref="RankException">The source array is multidimensional.</exception>
        /// <exception cref="InvalidCastException">
        ///     At least one element in the source <see cref="Array" /> cannot be cast
        ///     to the type of destination <paramref name="array" />.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
        /// <exception cref="ArgumentException"><paramref name="array" /> is not long enough.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is smaller than 0.</exception>
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
        ///     The type of the source <see cref="IReadOnlyCollection{T}" /> is not assignable from <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="RankException">The source array is multidimensional.</exception>
        /// <exception cref="InvalidCastException">
        ///     At least one element in the source <see cref="Array" /> cannot be cast
        ///     to <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
        /// <exception cref="ArgumentException"><paramref name="array" /> is not long enough.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is smaller than 0.</exception>
        public void CopyTo(Array<T> array, int index)
        {
            ArrayUtility.Copy(this, 0, array, index, Count);
        }

        #endregion
    }
}