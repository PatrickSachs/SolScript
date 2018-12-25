using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     Wrapper implementation of <see cref="IReadOnlyList{T}" />. This class wraps around an already existing other
    ///     collection.
    /// </summary>
    /// <typeparam name="T">The list type.</typeparam>
    [PublicAPI]
    public abstract class ReadOnlyList<T> : IEnumerable<T>
    {
        private static readonly ReadOnlyList<T> s_Empty = new EmptyImpl();

        /// <summary>
        ///     Gets the number of elements contained in the <see cref="ReadOnlyList{T}" />.
        /// </summary>
        public abstract int Count { get; }

        // ReSharper disable once ExceptionNotThrown
        /// <summary>
        ///     Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is less than 0. -or- <paramref name="index" />
        ///     is equal to or greater than <see cref="Count" />.
        /// </exception>
        public abstract T this[int index] { get; }

        /// <summary>
        ///     The sync root for thread safe access.
        /// </summary>
        public abstract object SyncRoot { get; }

        /// <inheritdoc />
        public abstract IEnumerator<T> GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Wraps the given list in a read only list. A direct reference is stored, thus updating the read ony list
        ///     automatically.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns>The read only list</returns>
        /// <exception cref="ArgumentNullException"><paramref name="list" /> is <see langword="null" /></exception>
        public static ReadOnlyList<T> Wrap(IList<T> list)
        {
            return new ListImpl(list, list is ICollection);
        }

        /// <summary>
        ///     Wraps an array in a read only list.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns>The read only list.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
        public static ReadOnlyList<T> Wrap(T[] array)
        {
            return new ArrayImpl2(array);
        }

        /// <summary>
        ///     Wraps an array in a read only list.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns>The read only list.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
        public static ReadOnlyList<T> Wrap(Array<T> array)
        {
            return new ArrayImpl(array);
        }

        /// <summary>
        ///     Wraps a delegate in a read only list. The delegate is called during each list operation.
        /// </summary>
        /// <typeparam name="TList">The list type.</typeparam>
        /// <param name="func">The delegate.</param>
        /// <returns>The read only list</returns>
        /// <exception cref="ArgumentNullException"><paramref name="func" /> is null</exception>
        public static ReadOnlyList<T> FromDelegate<TList>(Func<TList> func) where TList : IList<T>
        {
            return new FuncImpl<TList>(func, typeof(ICollection).IsAssignableFrom(typeof(TList)));
        }

        /// <summary>
        ///     Gets an empty read only list.
        /// </summary>
        /// <returns>The read only list.</returns>
        public static ReadOnlyList<T> Empty() => s_Empty;

        /// <summary>
        ///     Gets the index of the given element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The index.</returns>
        public abstract int IndexOf(T element);

        /// <summary>
        ///     Copies the elements of this collection to an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="index">The start index of the array.</param>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The type of the source <see cref="ReadOnlyList{T}" /> cannot be cast
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
        public virtual void CopyTo(Array array, int index)
        {
            ArrayUtility.Copy(this, 0, array, index, Count);
        }

        /// <summary>
        ///     Copies the elements of this collection to an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="index">The start index of the array.</param>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The type of the source <see cref="ReadOnlyList{T}" /> is not assignable from <typeparamref name="T" />.
        /// </exception>
        /// <exception cref="RankException">The source array is multidimensional.</exception>
        /// <exception cref="InvalidCastException">
        ///     At least one element in the source <see cref="Array" /> cannot be cast
        ///     to <typeparamref name="T" />.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
        /// <exception cref="ArgumentException"><paramref name="array" /> is not long enough.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is smaller than 0.</exception>
        public void CopyTo(Array<T> array, int index)
        {
            CopyTo(array.m_Array, index);
        }

        private class ArrayImpl2 : ReadOnlyList<T>
        {
            private readonly object m_SyncRoot = new object();
            private T[] m_Array;

            /// <inheritdoc />
            /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
            public ArrayImpl2(T[] array)
            {
                if (array == null) {
                    throw new ArgumentNullException(nameof(array));
                }
                m_Array = array;
            }

            /// <inheritdoc />
            public override int Count => m_Array.Length;

            /// <inheritdoc />
            public override T this[int index] {
                get { return m_Array[index]; }
            }

            /// <inheritdoc />
            public override object SyncRoot => m_SyncRoot;

            /// <inheritdoc />
            public override IEnumerator<T> GetEnumerator()
            {
                return ((IEnumerable<T>) m_Array).GetEnumerator();
            }

            /// <inheritdoc />
            public override int IndexOf(T element)
            {
                bool isValueType = typeof(T).IsValueType;
                for (int i = 0; i < m_Array.Length; i++) {
                    T e = m_Array[i];
                    if (isValueType) {
                        if (Equals(e, element)) {
                            return i;
                        }
                    } else {
                        if (ReferenceEquals(e, element)) {
                            return i;
                        }
                    }
                }
                return -1;
            }
        }

        private class ArrayImpl : ReadOnlyList<T>
        {
            private readonly object m_SyncRoot = new object();
            private Array<T> m_Array;

            /// <inheritdoc />
            /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
            public ArrayImpl(Array<T> array)
            {
                if (array == null) {
                    throw new ArgumentNullException(nameof(array));
                }
                m_Array = array;
            }

            /// <inheritdoc />
            public override int Count => m_Array.Length;

            /// <inheritdoc />
            public override T this[int index] {
                get { return m_Array[index]; }
            }

            /// <inheritdoc />
            public override object SyncRoot => m_SyncRoot;

            /// <inheritdoc />
            public override IEnumerator<T> GetEnumerator()
            {
                return m_Array.GetEnumerator();
            }

            /// <inheritdoc />
            public override int IndexOf(T element)
            {
                bool isValueType = typeof(T).IsValueType;
                for (int i = 0; i < m_Array.Length; i++) {
                    T e = m_Array[i];
                    if (isValueType) {
                        if (Equals(e, element)) {
                            return i;
                        }
                    } else {
                        if (ReferenceEquals(e, element)) {
                            return i;
                        }
                    }
                }
                return -1;
            }
        }

        private class FuncImpl<TList> : ReadOnlyList<T> where TList : IList<T>
        {
            private readonly Func<TList> m_Func;
            private readonly bool m_ImplementsCollection;
            private readonly object m_SyncRoot = new object();

            /// <inheritdoc />
            /// <exception cref="ArgumentNullException"><paramref name="func" /> is null</exception>
            public FuncImpl(Func<TList> func, bool implementsCollection)
            {
                if (func == null) {
                    throw new ArgumentNullException(nameof(func));
                }
                m_Func = func;
                m_ImplementsCollection = implementsCollection;
            }

            private IList<T> List => m_Func();

            /// <inheritdoc />
            public override int Count => List.Count;

            /// <inheritdoc />
            public override T this[int index] => List[index];

            /// <inheritdoc />
            public override object SyncRoot {
                get {
                    if (m_ImplementsCollection) {
                        return ((ICollection) m_Func()).SyncRoot;
                    }
                    return m_SyncRoot;
                }
            }

            /// <inheritdoc />
            public override IEnumerator<T> GetEnumerator()
            {
                return List.GetEnumerator();
            }

            /// <inheritdoc />
            public override int IndexOf(T element)
            {
                return List.IndexOf(element);
            }
        }

        private class ListImpl : ReadOnlyList<T>
        {
            private readonly bool m_ImplementsCollection;

            private readonly IList<T> m_List;
            private readonly object m_SyncRoot = new object();

            /// <inheritdoc />
            /// <exception cref="ArgumentNullException"><paramref name="list" /> is <see langword="null" /></exception>
            public ListImpl(IList<T> list, bool implementsCollection)
            {
                if (list == null) {
                    throw new ArgumentNullException(nameof(list));
                }
                m_List = list;
                m_ImplementsCollection = implementsCollection;
            }

            /// <inheritdoc />
            public override int Count => m_List.Count;

            /// <inheritdoc />
            public override T this[int index] => m_List[index];

            /// <inheritdoc />
            public override object SyncRoot {
                get {
                    if (m_ImplementsCollection) {
                        return ((ICollection) m_List).SyncRoot;
                    }
                    return m_SyncRoot;
                }
            }

            /// <inheritdoc />
            public override IEnumerator<T> GetEnumerator()
            {
                return m_List.GetEnumerator();
            }

            /// <inheritdoc />
            public override int IndexOf(T element)
            {
                return m_List.IndexOf(element);
            }
        }

        private class EmptyImpl : ReadOnlyList<T>
        {
            private readonly object m_SyncRoot = new object();

            /// <inheritdoc />
            public override int Count => 0;

            /// <inheritdoc />
            /// <exception cref="ArgumentOutOfRangeException" accessor="get">Cannot index a read only list.</exception>
            public override T this[int index] {
                get { throw new ArgumentOutOfRangeException(nameof(index), "Cannot index a read only list."); }
            }

            /// <inheritdoc />
            public override object SyncRoot => m_SyncRoot;

            /// <inheritdoc />
            public override IEnumerator<T> GetEnumerator()
            {
                return Enumerable.Empty<T>().GetEnumerator();
            }

            /// <inheritdoc />
            public override int IndexOf(T element)
            {
                return -1;
            }
        }

        /*private readonly Func<IReadOnlyList<T>> m_Delegate1;
        private readonly Func<IList<T>> m_Delegate2;
        private readonly Mode m_Mode;
        private readonly IReadOnlyList<T> m_Value1;
        private readonly IList<T> m_Value2;

        protected ReadOnlyList(Func<IReadOnlyList<T>> del1, Func<IList<T>> del2, IReadOnlyList<T> val1, IList<T> val2, Mode mode)
        {
            //new ReadOnlyCollection<int>(null).
            m_Delegate1 = del1;
            m_Delegate2 = del2;
            m_Value1 = val1;
            m_Value2 = val2;
            m_Mode = mode;
        }

        protected bool _IsWrapperReadOnly {
            get {
                switch (m_Mode) {
                    case Mode.ReadOnlyListDelegate:
                    case Mode.ReadOnlyList:
                        return true;
                    case Mode.ListDelegate:
                    case Mode.List:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        protected IList<T> _List {
            get {
                switch (m_Mode) {
                    case Mode.ListDelegate:
                        return m_Delegate2();
                    case Mode.List:
                        return m_Value2;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        protected IReadOnlyList<T> _ReadOnlyList {
            get {
                switch (m_Mode) {
                    case Mode.ReadOnlyListDelegate:
                        return m_Delegate1();
                    case Mode.ReadOnlyList:
                        return m_Value1;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        protected IEnumerable<T> _Enumerable {
            get {
                switch (m_Mode) {
                    case Mode.ReadOnlyListDelegate:
                        return m_Delegate1();
                    case Mode.ListDelegate:
                        return m_Delegate2();
                    case Mode.ReadOnlyList:
                        return m_Value1;
                    case Mode.List:
                        return m_Value2;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
    
        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return _Enumerable.GetEnumerator();
        }

        /// <inheritdoc />
        public int Count {
            get {
                if (_IsWrapperReadOnly) {
                    return _ReadOnlyList.Count;
                }
                return _List.Count;
            }
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            if (_IsWrapperReadOnly) {
                return _ReadOnlyList.Contains(item);
            }
            return _List.Contains(item);
        }

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
            if (_IsWrapperReadOnly) {
                var rol = _ReadOnlyList;
                ArrayUtility.Copy(rol, 0, array, index, rol.Count);
            } else {
                IList<T> list = _List;
                ArrayUtility.Copy(list, 0, array, index, list.Count);
            }
        }

        /// <summary>
        ///     Copies the elements of this collection to an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="index">The start index of the array.</param>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The type of the source <see cref="IReadOnlyCollection{T}" /> is not assignable from <typeparamref name="T" />.
        /// </exception>
        /// <exception cref="RankException">The source array is multidimensional.</exception>
        /// <exception cref="InvalidCastException">
        ///     At least one element in the source <see cref="Array" /> cannot be cast
        ///     to <typeparamref name="T" />.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
        /// <exception cref="ArgumentException"><paramref name="array" /> is not long enough.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is smaller than 0.</exception>
        public void CopyTo(Array<T> array, int index)
        {
            if (_IsWrapperReadOnly)
            {
                var rol = _ReadOnlyList;
                ArrayUtility.Copy(rol, 0, array, index, rol.Count);
            } else {
                IList<T> list = _List;
                ArrayUtility.Copy(list, 0, array, index, list.Count);
            }
        }

        /// <inheritdoc />
        public T this[int index] {
            get {
                if (_IsWrapperReadOnly) {
                    return _ReadOnlyList[index];
                }
                return _List[index];
            }
        }

        /// <summary>
        ///     Creates a new <see cref="IReadOnlyList{T}" />. The list itself is provided by the result of the delegate.
        /// </summary>
        /// <param name="delegate">The delegate.</param>
        /// <returns>The readonly list.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="delegate" /> is <see langword="null" /></exception>
        public static IReadOnlyList<T> FromDelegate(Func<IReadOnlyList<T>> @delegate)
        {
            if (@delegate == null) {
                throw new ArgumentNullException(nameof(@delegate));
            }
            return new ReadOnlyList<T>(@delegate, null, null, null, Mode.ReadOnlyListDelegate);
        }

        /// <summary>
        ///     Creates a new <see cref="IReadOnlyList{T}" />. The list itself is provided by the result of the delegate.
        /// </summary>
        /// <param name="delegate">The delegate.</param>
        /// <returns>The readonly list.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="delegate" /> is <see langword="null" /></exception>
        public static IReadOnlyList<T> FromDelegate(Func<IList<T>> @delegate)
        {
            if (@delegate == null) {
                throw new ArgumentNullException(nameof(@delegate));
            }
            return new ReadOnlyList<T>(null, @delegate, null, null, Mode.ListDelegate);
        }

        /// <summary>
        ///     Creates a new <see cref="IReadOnlyList{T}" />. The given list is not copied. The reference to it is stored.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns>The readonly list.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="list" /> is <see langword="null" /></exception>
        public static IReadOnlyList<T> FromValue(IReadOnlyList<T> list)
        {
            if (list == null) {
                throw new ArgumentNullException(nameof(list));
            }
            return new ReadOnlyList<T>(null, null, list, null, Mode.ReadOnlyList);
        }

        /// <summary>
        ///     Creates a new <see cref="IReadOnlyList{T}" />. The given list is not copied. The reference to it is stored.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns>The readonly list.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="list" /> is <see langword="null" /></exception>
        public static IReadOnlyList<T> FromValue(IList<T> list)
        {
            if (list == null) {
                throw new ArgumentNullException(nameof(list));
            }
            return new ReadOnlyList<T>(null, null, null, list, Mode.List);
        }

        protected enum Mode
        {
            ReadOnlyListDelegate,
            ListDelegate,
            ReadOnlyList,
            List
        }*/
        /*public int IndexOf(T element)
        {
            if (m_Reference != null) {
                return m_Reference.IndexOf(element);
            }
            if (m_Array != null) {
                for (int i = 0; i < m_Array.Length; i++) {
                    if (Equals(m_Array[i], element)) {
                        return i;
                    }
                }
                return -1;
            }
            return m_Func().IndexOf(element);
        }*/
        /*       /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            if (m_Reference != null) {
                return m_Reference.GetEnumerator();
            }
            if (m_Array != null) {
                return m_Array.GetEnumerator();
            }
            return m_Func().GetEnumerator();
        }*/
        /*/// <inheritdoc />
        public int Count {
            get {
                if (m_Reference != null) {
                    return m_Reference.Count;
                }
                if (m_Array != null) {
                    return m_Array.Length;
                }
                return m_Func().Count;
            }
        }*/
        /*/// <inheritdoc />
        public T this[int index] {
            get {
                if (m_Reference != null) {
                    return m_Reference[index];
                }
                if (m_Array != null) {
                    return m_Array[index];
                }
                return m_Func()[index];
            }
        }*/
        /*/// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
        public ReadOnlyList(Array<T> array)
        {
            if (array == null) {
                throw new ArgumentNullException(nameof(array));
            }
            m_Array = array;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="reference" /> is <see langword="null" /></exception>
        public ReadOnlyList(IList<T> reference)
        {
            if (reference == null) {
                throw new ArgumentNullException(nameof(reference));
            }
            m_Reference = reference;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="func" /> is <see langword="null" /></exception>
        public ReadOnlyList(Func<IList<T>> func)
        {
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }
            m_Func = func;
        }*/

        /*private Array<T> m_Array;
        private Func<IList<T>> m_Func;
        private IList<T> m_Reference;*/
    }
}