using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     This list type extends the <see cref="System.Collections.Generic.List{T}" /> and provides several additional
    ///     extensions and helpers making your life easier.
    /// </summary>
    /// <typeparam name="T">The list type.</typeparam>
    public class PSList<T> : List<T>
    {
        private ReadOnlyList<T> m_ReadOnly;

        /// <inheritdoc />
        public PSList() {}

        /// <inheritdoc />
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="capacity" /> is less than 0.
        /// </exception>
        public PSList(int capacity) : base(capacity) {}

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="collection" /> is null.
        /// </exception>
        public PSList([NotNull] IEnumerable<T> collection) : base(collection) {}

        /// <summary>
        ///     The sync root for thread safe access.
        /// </summary>
        public object SyncRoot => ((ICollection) this).SyncRoot;

        /// <summary>
        ///     Gets a read only list representing this list. The list is updated automatically. Only one rad only list per list
        ///     exists. If you need a new reference your need to wrap the list yourself.
        /// </summary>
        /// <returns>The read only list.</returns>
        public new ReadOnlyList<T> AsReadOnly()
        {
            if (m_ReadOnly == null) {
                m_ReadOnly = ReadOnlyList<T>.Wrap(this);
            }
            return m_ReadOnly;
        }

        /// <summary>
        ///     Copies the elements of this collection to an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="index">The start index of the array.</param>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The type of the source <see cref="PSList{T}" /> cannot be cast
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

        /// <summary>
        ///     Copies the elements of this collection to an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="index">The start index of the array.</param>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The type of the source <see cref="PSList{T}" /> is not assignable from <typeparamref name="T" />.
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
            ArrayUtility.Copy(this, 0, array, index, Count);
        }
    }
}