using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace PSUtility.Enumerables
{
    public class ObservableCollection<T> : ICollection<T>//, IReadOnlyObservableCollection<T>
    {
        private readonly ICollection<T> m_BackingCollection;

        public ObservableCollection()
        {
            m_BackingCollection = new Collection<T>();
        }

        public ObservableCollection(IEnumerable<T> enumerable)
        {
            m_BackingCollection = new PSList<T>(enumerable);
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => m_BackingCollection.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public void Add(T item)
        {
            m_BackingCollection.Add(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        /// <inheritdoc />
        public void Clear()
        {
            m_BackingCollection.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <inheritdoc />
        public bool Contains(T item) => m_BackingCollection.Contains(item);

        /// <inheritdoc />
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">The start index of the array.</param>
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
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is smaller than 0.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            ArrayUtility.Copy(m_BackingCollection, 0, array, arrayIndex, m_BackingCollection.Count);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            if (m_BackingCollection.Remove(item)) {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public int Count => m_BackingCollection.Count;

        /// <inheritdoc />
        public bool IsReadOnly => m_BackingCollection.IsReadOnly;

        /*/// <inheritdoc />
        int IReadOnlyCollection<T>.Count => Count;

        /// <inheritdoc />
        bool IReadOnlyCollection<T>.Contains(T item) => Contains(item);*/

        /// <inheritdoc />
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
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is smaller than 0.</exception>
        public void CopyTo(Array array, int index)
        {
            ArrayUtility.Copy(m_BackingCollection, 0, array, index, m_BackingCollection.Count);
        }

        /// <inheritdoc />
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
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is smaller than 0.</exception>
        public void CopyTo(Array<T> array, int index)
        {
            ArrayUtility.Copy(m_BackingCollection, 0, array, index, m_BackingCollection.Count);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
    }
}