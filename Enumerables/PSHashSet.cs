using JetBrains.Annotations;

namespace PSUtility.Enumerables
{
    using scg = System.Collections.Generic;

    [PublicAPI]
    public class PSHashSet<T> : scg.HashSet<T> //, ISet<T>
    {
        private readonly object m_SyncRoot = new object();

        private ReadOnlyHashSet<T> m_ReadOnly;
        public PSHashSet() {}
        public PSHashSet(scg.IEnumerable<T> enumerable) : base(enumerable) {}

        public object SyncRoot => m_SyncRoot;

        public ReadOnlyHashSet<T> AsReadOnly()
        {
            if (m_ReadOnly == null) {
                m_ReadOnly = new ReadOnlyHashSet<T>(this);
            }
            return m_ReadOnly;
        }

        /*/// <summary>
        ///     Adds an item to this <see cref="PSHashSet{T}" />.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>true if the item has been added, false if not.</returns>
        /// <exception cref="InvalidCastException">
        ///     The item cannot be cast to <typeparamref name="T" />.
        /// </exception>
        bool ISet.Add(object item)
        {
            return Add((T) item);
        }

        /// <summary>
        ///     Removes an item from this <see cref="PSHashSet{T}" />.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>true if the item has been removed, false if not.</returns>
        /// <exception cref="InvalidCastException">
        ///     The item cannot be cast to <typeparamref name="T" />.
        /// </exception>
        bool ISet.Remove(object item)
        {
            return Remove((T) item);
        }

        /// <inheritdoc />
        int ISet.RemoveWhere(Predicate<object> predicate)
        {
            return RemoveWhere(obj => predicate(obj));
        }

        /// <inheritdoc />
        void ISet.ExceptWith(IEnumerable enumerable)
        {
            if (enumerable == null) {
                throw new ArgumentNullException(nameof(enumerable));
            }
            if (ReferenceEquals(enumerable, this)) {
                Clear();
            } else {
                foreach (object o in enumerable) {
                    Remove((T) o);
                }
            }
        }

        /// <inheritdoc />
        void ISet.IntersectWith(IEnumerable enumerable)
        {
            if (enumerable == null) {
                throw new ArgumentNullException(nameof(enumerable));
            }
            if (Count == 0) {
                return;
            }
            IntersectWith(enumerable.Cast<T>());
        }

        /// <inheritdoc />
        void ISet.SymmetricExceptWith(IEnumerable enumerable)
        {
            if (enumerable == null) {
                throw new ArgumentNullException(nameof(enumerable));
            }
            SymmetricExceptWith(enumerable.Cast<T>());
        }

        /// <inheritdoc />
        void ISet.UnionWith(IEnumerable enumerable)
        {
            if (enumerable == null) {
                throw new ArgumentNullException(nameof(enumerable));
            }
            foreach (object o in enumerable) {
                Add((T) o);
            }
        }

        /// <inheritdoc />
        bool scg.IReadOnlySet.Contains(object item)
        {
            return Contains((T) item);
        }

        /// <inheritdoc />
        void scg.IReadOnlySet.CopyTo(Array array, int index)
        {
            CopyTo(array, index);
        }

        /// <inheritdoc />
        bool scg.IReadOnlySet.IsProperSubsetOf(IEnumerable enumerable)
        {
            if (enumerable == null) {
                throw new ArgumentNullException(nameof(enumerable));
            }
            return IsProperSubsetOf(enumerable.Cast<T>());
        }

        /// <inheritdoc />
        bool scg.IReadOnlySet.IsProperSupersetOf(IEnumerable enumerable)
        {
            if (enumerable == null) {
                throw new ArgumentNullException(nameof(enumerable));
            }
            return IsProperSupersetOf(enumerable.Cast<T>());
        }

        /// <inheritdoc />
        bool scg.IReadOnlySet.IsSubsetOf(IEnumerable enumerable)
        {
            if (enumerable == null) {
                throw new ArgumentNullException(nameof(enumerable));
            }
            return IsSubsetOf(enumerable.Cast<T>());
        }

        /// <inheritdoc />
        bool scg.IReadOnlySet.IsSupersetOf(IEnumerable enumerable)
        {
            if (enumerable == null) {
                throw new ArgumentNullException(nameof(enumerable));
            }
            return IsSupersetOf(enumerable.Cast<T>());
        }

        /// <inheritdoc />
        bool scg.IReadOnlySet.Overlaps(IEnumerable enumerable)
        {
            if (enumerable == null) {
                throw new ArgumentNullException(nameof(enumerable));
            }
            if (Count == 0) {
                return false;
            }
            foreach (object o in enumerable) {
                if (Contains((T) o)) {
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc />
        bool scg.IReadOnlySet.SetEquals(IEnumerable enumerable)
        {
            if (enumerable == null) {
                throw new ArgumentNullException(nameof(enumerable));
            }
            return SetEquals(enumerable.Cast<T>());
        }

        *
        /// <inheritdoc />
        void scg.IReadOnlyCollection<T>.CopyTo(Array<T> array, int index) {}*

        /// <inheritdoc />
        public object SyncRoot => m_SyncRoot;

        /// <inheritdoc />
        public bool IsSynchronized => false;

        /// <inheritdoc />
        int ISet.AddRange(IEnumerable items)
        {
            int added = 0;
            foreach (object item in items) {
                if (Add((T) item)) {
                    added++;
                }
            }
            return added;
        }

        /// <inheritdoc />
        public int AddRange(scg.IEnumerable<T> items)
        {
            int added = 0;
            foreach (T item in items) {
                if (Add(item)) {
                    added++;
                }
            }
            return added;
        }

        *private static scg.HashSet<T> ToGenHashSet(IEnumerable enumerable)
        {
            scg.HashSet<T> set;
            var hashSet = enumerable as HashSet<T>;
            if (hashSet != null) {
                set = (scg.HashSet<T>) enumerable;
            } else {
                set = new scg.HashSet<T>();
                foreach (object o in enumerable) {
                    set.Add((T) o);
                }
            }
            return set;
        }*

        /// <summary>
        ///     Copies the elements of this collection to an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="index">The start index of the array.</param>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The type of the source <see cref="PSHashSet{T}" /> cannot be cast
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
        public void CopyTo(Array array, int index)
        {
            ArrayUtility.Copy(this, 0, array, index, Count);
        }

        /// <inheritdoc />
        bool ISet.Contains(object element)
        {
            return Contains((T) element);
        }*/
    }
}