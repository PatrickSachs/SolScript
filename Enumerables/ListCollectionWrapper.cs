using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PSUtility.Properties;
using PSUtility.Strings;

namespace PSUtility.Enumerables
{
    public interface IListCollectionWrapper : ICollection, ICollection<object>
    {
        new int Count { get; }
    }

    public static class ListCollectionWrapper
    {
        public static IListCollectionWrapper FromIList(IList list)
        {
            return new ListImpl(list);
        }

        public static IListCollectionWrapper FromICollection<T>(ICollection<T> collection)
        {
            return new CollectionImpl<T>(collection);
        }

        /// <exception cref="ArgumentException"><paramref name="type"/> implements neither <see cref="IList"/> or <see cref="ICollection{T}"/>.</exception>
        public static IListCollectionWrapper FromType(Type type)
        {
            Type list = null;
            Type collection = null;
            foreach (Type @interface in type.GetInterfaces()) {
                if (@interface == typeof(IList)) {
                    list = @interface;
                    break;
                }
                if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(Collection<>)) {
                    collection = @interface;
                }
            }
            if (list != null) {
                return FromIList((IList) Activator.CreateInstance(type));
            }
            if (collection != null) {
                // Reflection fun!
                Type genericArg = collection.GetGenericArguments()[0];
                return (IListCollectionWrapper) typeof(IListCollectionWrapper)
                    .GetMethod(nameof(FromICollection), BindingFlags.Public | BindingFlags.Static)
                    .MakeGenericMethod(genericArg)
                    .Invoke(null, new[] {Activator.CreateInstance(type)});
            }
            throw new ArgumentException(Resources.Err_NotCollectionOrListType.F(type));
        }

        private class ListImpl : IListCollectionWrapper
        {
            private readonly IList m_List;

            public ListImpl(IList list)
            {
                m_List = list;
            }

            /// <inheritdoc />
            public IEnumerator GetEnumerator() => m_List.GetEnumerator();

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
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is smaller than 0.</exception>
            public void CopyTo(Array array, int index)
            {
                ArrayUtility.Copy(m_List, 0, array, index, m_List.Count);
            }

            /// <inheritdoc />
            public int Count => m_List.Count;

            /// <inheritdoc />
            public object SyncRoot => m_List.SyncRoot;

            /// <inheritdoc />
            public bool IsSynchronized => m_List.IsSynchronized;

            /// <inheritdoc />
            public bool IsReadOnly => m_List.IsReadOnly;

            /// <inheritdoc />
            public void Add(object item)
            {
                m_List.Add(item);
            }

            /// <inheritdoc />
            public void Clear()
            {
                m_List.Clear();
            }

            /// <inheritdoc />
            public bool Contains(object item)
            {
                return m_List.Contains(item);
            }

            /// <inheritdoc />
            public bool Remove(object item)
            {
                int prev = m_List.Count;
                m_List.Remove(item);
                return prev > m_List.Count;
            }

            /// <inheritdoc />
            IEnumerator<object> IEnumerable<object>.GetEnumerator()
            {
                yield return m_List.GetEnumerator();
            }

            /// <summary>
            ///     Copies the elements of this collection to an array.
            /// </summary>
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
            public void CopyTo(object[] array, int arrayIndex)
            {
                ArrayUtility.Copy(this, 0, array, arrayIndex, Count);
            }
        }

        private class CollectionImpl<T> : IListCollectionWrapper
        {
            private readonly ICollection<T> m_Collection;
            private readonly object m_FallbackSyncRoot;

            public CollectionImpl(ICollection<T> collection)
            {
                m_Collection = collection;
                m_FallbackSyncRoot = new object();
            }

            /// <inheritdoc />
            public IEnumerator GetEnumerator() => m_Collection.GetEnumerator();

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
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is smaller than 0.</exception>
            public void CopyTo(Array array, int index)
            {
                ArrayUtility.Copy(this, 0, array, index, m_Collection.Count);
            }

            /// <inheritdoc />
            public int Count => m_Collection.Count;

            /// <inheritdoc />
            public object SyncRoot {
                get {
                    ICollection iCollection = m_Collection as ICollection;
                    if (iCollection != null) {
                        return iCollection.SyncRoot;
                    }
                    return m_FallbackSyncRoot;
                }
            }

            /// <inheritdoc />
            public bool IsSynchronized {
                get {
                    ICollection iCollection = m_Collection as ICollection;
                    if (iCollection != null) {
                        return iCollection.IsSynchronized;
                    }
                    return false;
                }
            }

            /// <inheritdoc />
            public bool IsReadOnly => m_Collection.IsReadOnly;

            /// <inheritdoc />
            public void Add(object item)
            {
                m_Collection.Add((T) item);
            }

            /// <inheritdoc />
            public void Clear()
            {
                m_Collection.Clear();
            }

            /// <inheritdoc />
            public bool Contains(object item)
            {
                return m_Collection.Contains((T) item);
            }

            /// <inheritdoc />
            public bool Remove(object item)
            {
                return m_Collection.Remove((T) item);
            }

            /// <inheritdoc />
            IEnumerator<object> IEnumerable<object>.GetEnumerator()
            {
                yield return m_Collection.GetEnumerator();
            }

            /// <summary>
            ///     Copies the elements of this collection to an array.
            /// </summary>
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
            public void CopyTo(object[] array, int arrayIndex)
            {
                ArrayUtility.Copy(this, 0, array, arrayIndex, Count);
            }
        }
    }
}