using System;
using System.Collections;
using System.Collections.Generic;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     The observable list allows you to track all modifications done to it using a simple event.
    /// </summary>
    /// <typeparam name="T">The list type.</typeparam>
    public class ObservableList<T> : IList<T>, IList, INotifyCollectionChanged
    {
        /// <summary>
        ///     The backing list.
        /// </summary>
        private readonly PSList<T> m_List;

        /// <summary>
        ///     Creates a new empty obserable list.
        /// </summary>
        public ObservableList()
        {
            m_List = new PSList<T>();
        }

        /// <summary>
        ///     Creates a new empty obserable list with the given capcity.
        /// </summary>
        /// <param name="capacity">The capacity</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///         <paramref name="capacity" /> is less than 0. </exception>
        public ObservableList(int capacity)
        {
            m_List = new PSList<T>(capacity);
        }

        /// <summary>
        ///     Creates a new obserable list fiiled with the given elements.
        /// </summary>
        /// <param name="collection">The elements</param>
        /// <exception cref="ArgumentNullException">
        ///         <paramref name="collection" /> is null.</exception>
        public ObservableList(IEnumerable<T> collection)
        {
            m_List = new PSList<T>(collection);
        }

        /// <inheritdoc />
        /// <param name="array">The array.</param>
        /// <param name="index">The start index of the array.</param>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The type of the source <see cref="ObservableList{T}" /> cannot be cast
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
        void ICollection.CopyTo(Array array, int index)
        {
            ArrayUtility.Copy(m_List, 0, array, index, m_List.Count);
        }

        /// <inheritdoc />
        public object SyncRoot => m_List.SyncRoot;

        /// <inheritdoc />
        public bool IsSynchronized => false;

        /// <inheritdoc />
        int IList.Add(object value)
        {
            int index = ((IList) m_List).Add(value);
            if (index != -1) {
                OnCollectionChanged(CollectionChangedEventArgs.Add(value));
            }
            return index;
        }

        /// <inheritdoc />
        bool IList.Contains(object value)
        {
            return ((IList) m_List).Contains(value);
        }

        /// <inheritdoc />
        int IList.IndexOf(object value)
        {
            return ((IList) m_List).IndexOf(value);
        }

        /// <inheritdoc />
        /// <exception cref="NullReferenceException">
        ///     <paramref name="value" /> is null reference in the <see cref="IList" />.
        /// </exception>
        void IList.Insert(int index, object value)
        {
            ((IList) m_List).Insert(index, value);
            OnCollectionChanged(CollectionChangedEventArgs.Add(value));
        }

        /// <inheritdoc />
        void IList.Remove(object value)
        {
            int oldCount = m_List.Count;
            ((IList) m_List).Remove(value);
            int newCount = m_List.Count;
            if (oldCount != newCount) {
                OnCollectionChanged(CollectionChangedEventArgs.Remove(value));
            }
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentOutOfRangeException" accessor="set"><paramref name="value" /> is out of range.</exception>
        object IList.this[int index] {
            get { return ((IList) m_List)[index]; }
            set {
                if (index < 0 || m_List.Count >= index) {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                T old = m_List[index];
                if (!ReferenceEquals(old, value)) {
                    ((IList) m_List)[index] = value;
                    OnCollectionChanged(new CollectionChangedEventArgs(CollectionChangedType.Modify, old, value));
                }
            }
        }

        /// <inheritdoc />
        public bool IsFixedSize => false;

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return m_List.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public void Add(T item)
        {
            m_List.Add(item);
            OnCollectionChanged(CollectionChangedEventArgs.Add(item));
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (m_List.Count > 0) {
                m_List.Clear();
                OnCollectionChanged(CollectionChangedEventArgs.Reset());
            }
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return m_List.Contains(item);
        }

        /// <inheritdoc />
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">The start index of the array.</param>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The type of the source <see cref="ObservableList{T}" /> cannot be cast
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
            ArrayUtility.Copy(m_List, 0, array, arrayIndex, m_List.Count);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            if (m_List.Remove(item)) {
                OnCollectionChanged(CollectionChangedEventArgs.Remove(item));
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public int Count => m_List.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public int IndexOf(T item)
        {
            return m_List.IndexOf(item);
        }

        /// <inheritdoc />
        public void Insert(int index, T item)
        {
            m_List.Insert(index, item);
            OnCollectionChanged(CollectionChangedEventArgs.Add(item));
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <param name="index"></param>
        ///     is out of range.
        /// </exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || m_List.Count >= index) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            T element = m_List[index];
            // PSList has a RemoveAt method, but this ensures that the removed element is the same as 
            // the one in the event. A bit slower, but hey.
            // If it's an issue for your project fork this git and adjust it :)
            if (m_List.Remove(element)) {
                OnCollectionChanged(CollectionChangedEventArgs.Remove(element));
            }
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <param name="index"></param>
        ///     is out of range.
        /// </exception>
        public T this[int index] {
            get { return m_List[index]; }
            set {
                if (index < 0 || m_List.Count >= index) {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                T old = m_List[index];
                if (!ReferenceEquals(old, value)) {
                    m_List[index] = value;
                    OnCollectionChanged(new CollectionChangedEventArgs(CollectionChangedType.Modify, old, value));
                }
            }
        }

        /// <inheritdoc />
        public event CollectionChangedEventHandler CollectionChanged;

        /// <summary>
        ///     Returns a read only list of this obserable list.
        /// </summary>
        /// <returns>The read only list.</returns>
        public ReadOnlyList<T> AsReadOnly()
        {
            return m_List.AsReadOnly();
        }

        protected virtual void OnCollectionChanged(CollectionChangedEventArgs eventargs)
        {
            CollectionChanged?.Invoke(this, eventargs);
        }
    }
}