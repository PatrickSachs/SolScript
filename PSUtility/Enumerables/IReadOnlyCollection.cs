/*#if !NETFX_45

// ReSharper disable once CheckNamespace

namespace System.Collections.Generic
{
    /// <summary>
    ///     A read only collection allows you to publically expose a collection without the risk of having the data
    ///     manipulated.
    /// </summary>
    /// <typeparam name="T">The list type.</typeparam>
    public interface IReadOnlyCollection<T> : IEnumerable<T>
    {
        /// <summary>
        ///     The amount of elements in this collection.
        /// </summary>
        int Count { get; }

        *
        /// <summary>
        ///     Checks if the <see cref="IReadOnlyCollection{T}" /> contains the given item.
        /// </summary>
        /// <param name="item">The item to find.</param>
        /// <returns>true if the item is contained in this collection, false if not.</returns>
        bool Contains(T item);

        /
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
        void CopyTo(Array array, int index);

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
        void CopyTo(Array<T> array, int index);*
    }
}

#endif*/