namespace PSUtility.Enumerables
{
    /// <summary>
    ///     The type of action performed.
    /// </summary>
    public enum CollectionChangedType
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
        /// One or multiple elements have been modified.
        /// </summary>
        Modify,

        /// <summary>
        ///     The collection has been reset/massively changed.
        /// </summary>
        Reset
    }
}