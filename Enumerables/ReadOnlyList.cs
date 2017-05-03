using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     Wrapper implementation of <see cref="IReadOnlyList{T}" />. This class wraps around an already existing other
    ///     collection.
    /// </summary>
    /// <typeparam name="T">The list type.</typeparam>
    [PublicAPI]
    public class ReadOnlyList<T> : IEnumerable<T>
    {
        private Array<T> m_Array;
        private Func<IList<T>> m_Func;
        private IList<T> m_Reference;

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
        public ReadOnlyList(Array<T> array)
        {
            if (array == null) {
                throw new ArgumentNullException(nameof(array));
            }
            m_Array = array;
        }

        /*protected virtual IList<T> List {
            get {
                if (m_Reference != null) {
                    return m_Reference;
                }
                return m_Func();
            }
        }*/

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
        }

        /// <inheritdoc />
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
        }

        /// <inheritdoc />
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
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            if (m_Reference != null) {
                return m_Reference.GetEnumerator();
            }
            if (m_Array != null) {
                return m_Array.GetEnumerator();
            }
            return m_Func().GetEnumerator();
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
    }
}