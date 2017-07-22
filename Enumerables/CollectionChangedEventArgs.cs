using System;
using System.Collections;
using JetBrains.Annotations;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     These event args are used to signify a change in a collection.
    /// </summary>
    [PublicAPI]
    public class CollectionChangedEventArgs : EventArgs
    {

        /// <summary>
        ///     Creates new collection changed event arguments from the given parameters.
        /// </summary>
        /// <param name="action">The action that has been performed.</param>
        /// <param name="changedItems">The modified items.</param>
        /// <exception cref="ArgumentNullException"><paramref name="changedItems" /> is <see langword="null" /></exception>
        public CollectionChangedEventArgs(CollectionChangedType action, [NotNull] IList changedItems)
        {
            if (changedItems == null) {
                throw new ArgumentNullException(nameof(changedItems));
            }
            ChangedItems = changedItems;
            Action = action;
        }

        /// <summary>
        ///     Creates new collection changed event arguments from the given parameters.
        /// </summary>
        /// <param name="action">The action that has been performed.</param>
        /// <param name="changedItems">The modified items.</param>
        /// <exception cref="ArgumentNullException"><paramref name="changedItems" /> is <see langword="null" /></exception>
        public CollectionChangedEventArgs(CollectionChangedType action, params object[] changedItems)
        {
            if (changedItems == null) {
                throw new ArgumentNullException(nameof(changedItems));
            }
            ChangedItems = changedItems;
            Action = action;
        }

        /// <summary>
        ///     The modified items.
        /// </summary>
        public IList ChangedItems { get; }

        /// <summary>
        ///     The action that has been performed.
        /// </summary>
        public CollectionChangedType Action { get; }

        /// <summary>
        ///     Obtains new collection changed event args for resetting a collection.
        /// </summary>
        /// <returns>The event args.</returns>
        public static CollectionChangedEventArgs Reset()
        {
            return new CollectionChangedEventArgs(CollectionChangedType.Reset, new PSList<object>());
        }

        /// <summary>
        ///     Obtains new collection changed event args for resetting a collection.
        /// </summary>
        /// <param name="items">The changed items.</param>
        /// <returns>The event args.</returns>
        public static CollectionChangedEventArgs Reset(IList items)
        {
            return new CollectionChangedEventArgs(CollectionChangedType.Reset, items);
        }

        /// <summary>
        ///     Obtains new collection changed event args for resetting a collection.
        /// </summary>
        /// <param name="items">The changed items.</param>
        /// <returns>The event args.</returns>
        public static CollectionChangedEventArgs Reset(params object[] items)
        {
            return new CollectionChangedEventArgs(CollectionChangedType.Reset, items);
        }

        /// <summary>
        ///     Obtains new collection changed event args for addings item to the collection.
        /// </summary>
        /// <param name="items">The added items.</param>
        /// <returns>The event args.</returns>
        public static CollectionChangedEventArgs Add(params object[] items)
        {
            return new CollectionChangedEventArgs(CollectionChangedType.Add, items);
        }

        /// <summary>
        ///     Obtains new collection changed event args for addings item to the collection.
        /// </summary>
        /// <param name="items">The added items.</param>
        /// <returns>The event args.</returns>
        public static CollectionChangedEventArgs Add(IList items)
        {
            return new CollectionChangedEventArgs(CollectionChangedType.Add, items);
        }

        /// <summary>
        ///     Obtains new collection changed event args for removing item from the collection.
        /// </summary>
        /// <param name="items">The removed items.</param>
        /// <returns>The event args.</returns>
        public static CollectionChangedEventArgs Remove(params object[] items)
        {
            return new CollectionChangedEventArgs(CollectionChangedType.Remove, items);
        }

        /// <summary>
        ///     Obtains new collection changed event args for removing item from the collection.
        /// </summary>
        /// <param name="items">The removed items.</param>
        /// <returns>The event args.</returns>
        public static CollectionChangedEventArgs Remove(IList items)
        {
            return new CollectionChangedEventArgs(CollectionChangedType.Remove, items);
        }
    }
}