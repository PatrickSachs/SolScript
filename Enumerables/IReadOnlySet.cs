// ReSharper disable once CheckNamespace

namespace System.Collections.Generic
{
    public interface IReadOnlySet<T> : IReadOnlySet, IReadOnlyCollection<T>
    {
        new int Count { get; }

        /// <summary>
        ///     Copies the elements of this collection to an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="index">The start index of the array.</param>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The type of the source <see cref="IReadOnlySet{T}" /> cannot be cast
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
        new void CopyTo(Array array, int index);

        bool IsProperSubsetOf(IEnumerable<T> enumerable);
        bool IsProperSupersetOf(IEnumerable<T> enumerable);
        bool IsSubsetOf(IEnumerable<T> enumerable);
        bool IsSupersetOf(IEnumerable<T> enumerable);
        bool Overlaps(IEnumerable<T> enumerable);
        bool SetEquals(IEnumerable<T> enumerable);
    }

    public interface IReadOnlySet : IEnumerable
    {
        int Count { get; }
        bool Contains(object item);
        void CopyTo(Array array, int index);
        bool IsProperSubsetOf(IEnumerable enumerable);
        bool IsProperSupersetOf(IEnumerable enumerable);
        bool IsSubsetOf(IEnumerable enumerable);
        bool IsSupersetOf(IEnumerable enumerable);
        bool Overlaps(IEnumerable enumerable);
        bool SetEquals(IEnumerable enumerable);
    }
}