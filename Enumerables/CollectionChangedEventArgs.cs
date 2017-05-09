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
        ///     The type of action performed.
        /// </summary>
        public enum ActionType
        {
            /// <summary>
            ///     No action was performed.
            /// </summary>
            None,

            /// <summary>
            ///     One or multiple elements have been added.
            /// </summary>
            Add,

            /// <summary>
            ///     One or multiple elements have been removed.
            /// </summary>
            Remove,

            /// <summary>
            ///     The collection has been reset/massively changed.
            /// </summary>
            Reset
        }

        /// <summary>
        ///     Creates new collection changed event arguments from the given parameters.
        /// </summary>
        /// <param name="action">The action that has been performed.</param>
        /// <param name="changedItems">The modified items.</param>
        /// <exception cref="ArgumentNullException"><paramref name="changedItems" /> is <see langword="null" /></exception>
        public CollectionChangedEventArgs(ActionType action, [NotNull] IList changedItems)
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
        public CollectionChangedEventArgs(ActionType action, params object[] changedItems)
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
        public ActionType Action { get; }

        /// <summary>
        ///     Obtains new collection changed event args for resetting a collection.
        /// </summary>
        /// <returns>The event args.</returns>
        public static CollectionChangedEventArgs Reset()
        {
            return new CollectionChangedEventArgs(ActionType.Reset, new PSList<object>());
        }

        /// <summary>
        ///     Obtains new collection changed event args for resetting a collection.
        /// </summary>
        /// <param name="items">The changed items.</param>
        /// <returns>The event args.</returns>
        public static CollectionChangedEventArgs Reset(IList items)
        {
            return new CollectionChangedEventArgs(ActionType.Reset, items);
        }

        /// <summary>
        ///     Obtains new collection changed event args for resetting a collection.
        /// </summary>
        /// <param name="items">The changed items.</param>
        /// <returns>The event args.</returns>
        public static CollectionChangedEventArgs Reset(params object[] items)
        {
            return new CollectionChangedEventArgs(ActionType.Reset, items);
        }

        /// <summary>
        ///     Obtains new collection changed event args for addings item to the collection.
        /// </summary>
        /// <param name="items">The added items.</param>
        /// <returns>The event args.</returns>
        public static CollectionChangedEventArgs Add(params object[] items)
        {
            return new CollectionChangedEventArgs(ActionType.Add, items);
        }

        /// <summary>
        ///     Obtains new collection changed event args for addings item to the collection.
        /// </summary>
        /// <param name="items">The added items.</param>
        /// <returns>The event args.</returns>
        public static CollectionChangedEventArgs Add(IList items)
        {
            return new CollectionChangedEventArgs(ActionType.Add, items);
        }

        /// <summary>
        ///     Obtains new collection changed event args for removing item from the collection.
        /// </summary>
        /// <param name="items">The removed items.</param>
        /// <returns>The event args.</returns>
        public static CollectionChangedEventArgs Remove(params object[] items)
        {
            return new CollectionChangedEventArgs(ActionType.Remove, items);
        }

        /// <summary>
        ///     Obtains new collection changed event args for removing item from the collection.
        /// </summary>
        /// <param name="items">The removed items.</param>
        /// <returns>The event args.</returns>
        public static CollectionChangedEventArgs Remove(IList items)
        {
            return new CollectionChangedEventArgs(ActionType.Remove, items);
        }
    }
}