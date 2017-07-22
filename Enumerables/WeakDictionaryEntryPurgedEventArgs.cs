using System;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     Event args used after an entry in a weak dictionary has been purged.
    /// </summary>
    public class WeakDictionaryEntryPurgedEventArgs<T> : EventArgs
    {
        /// <inheritdoc />
        public WeakDictionaryEntryPurgedEventArgs(T purged, bool collected)
        {
            Purged = purged;
            Collected = collected;
        }

        /// <summary>
        ///     True idicates that the entry was purged because the value was collected, false indicates that another reason caused
        ///     the purge.
        /// </summary>
        public bool Collected { get; }

        /// <summary>
        ///     The purged value.
        /// </summary>
        public T Purged { get; }
    }
}