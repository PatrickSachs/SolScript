using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     A collection providing an
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [PublicAPI]
    public class ObservableCollection<T> : ICollection<T>, INotifyCollectionChanged
    {
        private readonly ICollection<T> m_BackingCollection;
        private ReadOnlyCollection<T> m_ReadOnly;

        private ObservableCollection(ICollection<T> wrap)
        {
            m_BackingCollection = wrap;
        }

        /// <summary>
        ///     Creates a new empty oberable collection.
        /// </summary>
        public ObservableCollection()
        {
            m_BackingCollection = new Collection<T>();
        }

        /// <summary>
        ///     Creates a new obserable collection containing all elements in the given enumerable.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        public ObservableCollection(IEnumerable<T> enumerable)
        {
            m_BackingCollection = new Collection<T>(enumerable);
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => m_BackingCollection.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public void Add(T item)
        {
            m_BackingCollection.Add(item);
            OnCollectionChanged(CollectionChangedEventArgs.Add(item));
        }

        /// <inheritdoc />
        public void Clear()
        {
            m_BackingCollection.Clear();
            OnCollectionChanged(CollectionChangedEventArgs.Reset());
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
                OnCollectionChanged(CollectionChangedEventArgs.Remove(item));
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public int Count => m_BackingCollection.Count;

        /// <inheritdoc />
        public bool IsReadOnly => m_BackingCollection.IsReadOnly;

        /// <summary>
        ///     Wraps an already existing collection into an observable collection. Any modifcation done to this collection outside
        ///     of the obserable collection will not raise events.
        /// </summary>
        /// <param name="wrap">The collection to wrap.</param>
        /// <returns>The obserable collection.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="wrap" /> is <see langword="null" /></exception>
        public static ObservableCollection<T> Wrap([NotNull] ICollection<T> wrap)
        {
            if (wrap == null) {
                throw new ArgumentNullException(nameof(wrap));
            }
            return new ObservableCollection<T>(wrap);
        }

        /// <summary>
        ///     Returns a read only collection representing this read only collection.
        /// </summary>
        /// <returns>The read onyl collection.</returns>
        public ReadOnlyCollection<T> AsReadOnly()
        {
            if (m_ReadOnly == null) {
                m_ReadOnly = ReadOnlyCollection<T>.Wrap(this);
            }
            return m_ReadOnly;
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

        /// <summary>
        ///     This event is invoked whenever the collection changes.
        /// </summary>
        public event CollectionChangedEventHandler CollectionChanged;

        protected void OnCollectionChanged(CollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
    }
}