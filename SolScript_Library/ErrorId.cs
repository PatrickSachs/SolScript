namespace SolScript
{
    /// <summary>
    ///     All SolScript error Ids.
    /// </summary>
    public enum ErrorId
    {
        /// <summary>
        ///     Default value. No id.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Could not reslove error id.
        /// </summary>
        InternalFailedToResolve = 1,

        /// <summary>
        ///     This error marks that something has been done to prevent corruption of the assembly.
        /// </summary>
        InternalSecurityMeasure = 2,
    }
}