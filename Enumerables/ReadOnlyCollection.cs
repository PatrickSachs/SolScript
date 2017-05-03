using System;
using System.Collections;
using System.Collections.Generic;

namespace PSUtility.Enumerables
{
    public class ReadOnlyCollection<T> : IEnumerable<T>
    {
        private readonly ICollection<T> m_Collection;
        private readonly Func<ICollection<T>> m_Func;

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" /></exception>
        public ReadOnlyCollection(ICollection<T> collection)
        {
            if (collection == null) {
                throw new ArgumentNullException(nameof(collection));
            }
            m_Collection = collection;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="func" /> is <see langword="null" /></exception>
        public ReadOnlyCollection(Func<ICollection<T>> func)
        {
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }
            m_Func = func;
        }

        protected virtual ICollection<T> Collection {
            get {
                if (m_Collection != null) {
                    return m_Collection;
                }
                return m_Func();
            }
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return Collection.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public int Count => Collection.Count;
    }
}