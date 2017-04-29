using System;
using System.Collections;
using System.Collections.Generic;

namespace PSUtility.Enumerables
{
    public interface ISet<T> : ISet, IReadOnlySet<T>, ICollection<T>
    {
        /// <summary>
        ///     Adds multiple items to this set.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>The amount of items added.</returns>
        int AddRange(IEnumerable<T> items);

        /// <summary>
        ///     Removes all items matching the given predicate from the set.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <returns>The amount of removed elements.</returns>
        int RemoveWhere(Predicate<T> predicate);

        void ExceptWith(IEnumerable<T> enumerable);
        void IntersectWith(IEnumerable<T> enumerable);
        void SymmetricExceptWith(IEnumerable<T> enumerable);
        void UnionWith(IEnumerable<T> enumerable);
        new bool Contains(T element);
    }

    public interface ISet : IReadOnlySet, ICollection
    {
        /// <summary>
        ///     Adds an item to this set.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>true if the item has been added, false if not.</returns>
        /// <exception cref="InvalidCastException">
        ///     The item cannot be cast to the set type. This exception should only be expected
        ///     in generic collections.
        /// </exception>
        bool Add(object item);

        /// <summary>
        ///     Adds multiple items to this set.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>The amount of items added.</returns>
        /// <exception cref="InvalidCastException">
        ///     One item item cannot be cast to the set type. This exception should only be expected
        ///     in generic collections.
        /// </exception>
        int AddRange(IEnumerable items);

        /// <summary>
        ///     Removes an item from this set.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>true if the item has been removed, false if not.</returns>
        /// <exception cref="InvalidCastException">
        ///     The item cannot be cast to the set type. This exception should only be expected
        ///     in generic collections.
        /// </exception>
        bool Remove(object item);

        /// <summary>
        ///     Removes all items matching the given predicate from the set.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <returns>The amount of removed elements.</returns>
        int RemoveWhere(Predicate<object> predicate);

        /// <summary>
        ///     Clears all elements from this set.
        /// </summary>
        void Clear();

        void ExceptWith(IEnumerable enumerable);
        void IntersectWith(IEnumerable enumerable);
        void SymmetricExceptWith(IEnumerable enumerable);
        void UnionWith(IEnumerable enumerable);
        new bool Contains(object element);
    }
}