using System;
using System.Collections;
using System.Collections.Generic;

namespace PSUtility.Enumerables
{
    public class ReadOnlyHashSet<T> : IEnumerable<T>
    {
        private readonly Func<HashSet<T>> m_Func;
        private readonly HashSet<T> m_Reference;

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="reference" /> is <see langword="null" /></exception>
        public ReadOnlyHashSet(HashSet<T> reference)
        {
            if (reference == null) {
                throw new ArgumentNullException(nameof(reference));
            }
            m_Reference = reference;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="func" /> is <see langword="null" /></exception>
        public ReadOnlyHashSet(Func<HashSet<T>> func)
        {
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }
            m_Func = func;
        }

        protected virtual HashSet<T> Set {
            get {
                if (m_Reference == null) {
                    return m_Reference;
                }
                return m_Func();
            }
        }

        /// <inheritdoc />
        public int Count => Set.Count;

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return Set.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return Set.IsSubsetOf(other);
        }

        /// <inheritdoc />
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return Set.IsSupersetOf(other);
        }

        /// <inheritdoc />
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return Set.IsProperSupersetOf(other);
        }

        /// <inheritdoc />
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return Set.IsProperSubsetOf(other);
        }

        /// <inheritdoc />
        public bool Overlaps(IEnumerable<T> other)
        {
            return Set.Overlaps(other);
        }

        /// <inheritdoc />
        public bool SetEquals(IEnumerable<T> other)
        {
            return Set.SetEquals(other);
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return Set.Contains(item);
        }
    }
}