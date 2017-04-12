using System.Collections.Generic;

namespace PSUtility.Enumerables
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

        /// <summary>
        ///     Checks if the <see cref="IReadOnlyCollection{T}" /> contains the given item.
        /// </summary>
        /// <param name="item">The item to find.</param>
        /// <returns>true if the item is contained in this collection, false if not.</returns>
        bool Contains(T item);
    }
}