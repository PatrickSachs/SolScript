using System;
using System.Collections;
using System.Collections.Generic;

namespace PSUtility.Enumerables
{
    public class PSHashSet : HashSet<object>, IReadOnlySet<object>
    {
        /// <inheritdoc />
        void IReadOnlySet<object>.CopyTo(Array array, int index)
        {
            CopyTo(array, index);
        }

        /// <summary>
        ///     Copies the elements of this collection to an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="index">The start index of the array.</param>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The type of the source <see cref="PSHashSet" /> cannot be cast
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
        public void CopyTo(Array<object> array, int index)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool IsProperSubsetOf(IEnumerable enumerable)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool IsProperSupersetOf(IEnumerable enumerable)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool IsSubsetOf(IEnumerable enumerable)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool IsSupersetOf(IEnumerable enumerable)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool Overlaps(IEnumerable enumerable)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool SetEquals(IEnumerable enumerable)
        {
            throw new NotImplementedException();
        }

        /*/// <inheritdoc />
        void IReadOnlyCollection<object>.CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }*/
    }
}