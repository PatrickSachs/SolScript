using System;
using System.Collections;
using System.Collections.Generic;

namespace SolScript.Utility
{
    /// <summary>
    ///     Wraps an <see cref="Array" /> in a <see cref="IReadOnlyList{T}" />
    /// </summary>
    /// <typeparam name="T">The array type.</typeparam>
    public class Array<T> : IReadOnlyList<T>, ICloneable
    {
        /// <summary>
        ///     Creates a new array wrapper for the given array.
        /// </summary>
        /// <param name="array">The array.</param>
        public Array(params T[] array)
        {
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

        // The array.
        private T[] m_Array;

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
                    throw new ArgumentNullException(nameof(value), "Cannot set the backing array of an Array to null.");
                }
                m_Array = value;
            }
        }

        /// <inheritdoc cref="Count" />
        public int Length => m_Array.Length;

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
        public int Count => m_Array.Length;

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
            T[] newarray = new T[m_Array.Length];
            Array.Copy(m_Array, newarray, newarray.Length);
            return new Array<T>(newarray);
        }

        #endregion

        #region Nested type: Enumerator

        // Enumerates through the array.
        private class Enumerator : IEnumerator<T>
        {
            public Enumerator(Array<T> wrapper)
            {
                m_Wrapper = wrapper;
            }

            private readonly Array<T> m_Wrapper;
            private bool m_Disposed;
            private int m_Index = -1;

            #region IEnumerator<T> Members

            /// <inheritdoc />
            public void Dispose()
            {
                AssertDispose("Cannot dispose the enumerator twice.");
                m_Disposed = true;
            }

            /// <inheritdoc />
            public bool MoveNext()
            {
                m_Index++;
                return m_Index < m_Wrapper.m_Array.Length;
            }

            /// <inheritdoc />
            public void Reset()
            {
                AssertDispose();
                m_Index = 0;
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

            private void AssertDispose(string message = null)
            {
                if (m_Disposed) {
                    throw new ObjectDisposedException(message ?? "The array enumerator is disposed.");
                }
            }

            private void AssertRange(int index)
            {
                if (index < 0 || index >= m_Wrapper.m_Array.Length) {
                    throw new IndexOutOfRangeException("The index " + index + " is out of range. Min: 0; Max: " + (m_Wrapper.m_Array.Length - 1));
                }
            }
        }

        #endregion
    }
}