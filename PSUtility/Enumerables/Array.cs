using System;
using System.Collections;
using System.Collections.Generic;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     Wraps an <see cref="Array" /> in an <see cref="IReadOnlyList{T}" />
    /// </summary>
    /// <typeparam name="T">The array type.</typeparam>
    public class Array<T> : IEnumerable<T>, ICloneable //IReadOnlyList<T>,
    {
        // The array.
        internal T[] m_Array;

        private ReadOnlyList<T> m_ReadOnly;

        /// <summary>
        ///     Creates a new array wrapper for the given array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <remarks>This internal array is a direct reference to the given array. No cloning is performed.</remarks>
        /// <exception cref="ArgumentNullException">The <paramref name="array" /> is null.</exception>
        public Array(params T[] array)
        {
            if (array == null) {
                throw new ArgumentNullException(nameof(array));
            }
            m_Array = array;
        }

        /// <summary>
        ///     Creates a new array of the given length.
        /// </summary>
        /// <param name="length">The array length.</param>
        public Array(int length)
        {
            m_Array = new T[length];
        }

        /// <summary>
        ///     The backing array.
        /// </summary>
        /// <exception cref="ArgumentNullException" accessor="set">
        ///     Cannot set the backing array of an Array to null.
        ///     <paramref name="value" />
        /// </exception>
        public T[] Value {
            get { return m_Array; }
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value));
                }
                m_Array = value;
            }
        }

        /// <summary>
        ///     The lenght of this array.
        /// </summary>
        public int Length => m_Array.Length;

        /// <summary>
        ///     Creates a read only representation of this array.
        /// </summary>
        /// <returns>The read only list.</returns>
        public ReadOnlyList<T> AsReadOnly()
        {
            if (m_ReadOnly == null) {
                m_ReadOnly = ReadOnlyList<T>.Wrap(this);
            }
            return m_ReadOnly;
        }

        #region Nested type: Enumerator

        // Enumerates through the array.
        private class Enumerator : IEnumerator<T>
        {
            private bool m_Disposed;
            private int m_Index = -1;

            private Array<T> m_Wrapper;

            public Enumerator(Array<T> wrapper)
            {
                m_Wrapper = wrapper;
            }

            private void AssertDispose(string message = null)
            {
                if (m_Disposed) {
                    throw new ObjectDisposedException(message ?? "The array enumerator is disposed.");
                }
            }

            private void AssertRange(int index)
            {
                if (index < 0 || index >= m_Wrapper.m_Array.Length) {
                    throw new InvalidOperationException("The index " + index + " is out of range. Min: 0; Max: " + (m_Wrapper.m_Array.Length - 1));
                }
            }

            #region IEnumerator<T> Members

            private void Dispose(bool disposing)
            {
                if (!m_Disposed) {
                    m_Wrapper = null;
                    m_Index = -1;
                    m_Disposed = true;
                }
            }

            /// <inheritdoc />
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <inheritdoc />
            public bool MoveNext()
            {
                AssertDispose();
                m_Index++;
                return m_Index < m_Wrapper.m_Array.Length;
            }

            /// <inheritdoc />
            public void Reset()
            {
                AssertDispose();
                m_Index = -1;
            }

            /// <inheritdoc />
            public T Current {
                get {
                    AssertDispose();
                    AssertRange(m_Index);
                    return m_Wrapper.m_Array[m_Index];
                }
            }

            /// <inheritdoc />
            object IEnumerator.Current => Current;

            #endregion
        }

        #endregion

        #region IReadOnlyList<T> Members

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            foreach (T val in m_Array) {
                if (Equals(val, item)) {
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Index is out of range. <paramref name="index" />
        /// </exception>
        public T this[int index] {
            get {
                if (index < 0 || index >= m_Array.Length) {
                    throw new ArgumentOutOfRangeException(nameof(index), "Cannot access index " + index + " in an array of length " + m_Array.Length + ".");
                }
                return m_Array[index];
            }
            set {
                if (index < 0 || index >= m_Array.Length) {
                    throw new ArgumentOutOfRangeException(nameof(index), "Cannot access index " + index + " in an array of length " + m_Array.Length + ".");
                }
                m_Array[index] = value;
            }
        }

        /// <inheritdoc />
        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <inheritdoc />
        public Array<T> Clone()
        {
            var newarray = new T[m_Array.Length];
            Array.Copy(m_Array, newarray, newarray.Length);
            return new Array<T>(newarray);
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
        ///     At least one element in the source <see cref="T:System.Array" /> cannot be cast
        ///     to the type of destination <paramref name="array" />.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
        /// <exception cref="ArgumentException"><paramref name="array" /> is not long enough.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is smaller than 0.</exception>
        public void CopyTo(Array array, int index)
        {
            ArrayUtility.Copy(this, 0, array, index, Length);
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
        ///     At least one element in the source <see cref="T:System.Array" /> cannot be cast
        ///     to <typeparamref name="T" />.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
        /// <exception cref="ArgumentException"><paramref name="array" /> is not long enough.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is smaller than 0.</exception>
        public void CopyTo(Array<T> array, int index)
        {
            ArrayUtility.Copy(this, 0, array, index, Length);
        }

        #endregion
    }
}