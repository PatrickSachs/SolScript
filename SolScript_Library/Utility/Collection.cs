using System;
using System.Collections;
using System.Collections.Generic;

namespace SolScript.Utility
{
    /// <summary>
    ///     The collection implements <see cref="ICollection{T}" /> and <see cref="IReadOnlyCollection{T}" />. You can
    ///     wrap any standard collection inside of this collection or create a new one entirely.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Collection<T> : ICollection<T>, IReadOnlyCollection<T>
    {
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

        // The wrapped collection.
        private readonly ICollection<T> m_Collection;

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
        int IReadOnlyCollection<T>.Count => m_Collection.Count;

        /// <inheritdoc />
        bool IReadOnlyCollection<T>.Contains(T item)
        {
            return m_Collection.Contains(item);
        }

        #endregion

        /// <summary>
        ///     Gets the collection wrapped inside of this wrapper.
        /// </summary>
        /// <returns>The collection.</returns>
        public ICollection<T> GetCollection() => m_Collection;
    }
}