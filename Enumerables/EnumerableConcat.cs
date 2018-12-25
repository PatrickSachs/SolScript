namespace PSUtility.Enumerables
{
    /// <summary>
    ///     Describes how two enumerables should be concatenated.
    /// </summary>
    public enum EnumerableConcat
    {
        /// <summary>
        ///     The second enumerable should be appended.
        /// </summary>
        Append,

        /// <summary>
        ///     The second enumerable should be preprended.
        /// </summary>
        Prepend
    }
}