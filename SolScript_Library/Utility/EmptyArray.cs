namespace SolScript.Utility
{
    /// <summary>
    ///     Useful in number of places that return an empty byte array to avoid unnecessary memory allocation.
    /// </summary>
    /// <typeparam name="T">The array type.</typeparam>
    internal static class EmptyArray<T>
    {
        /// <summary>
        ///     The array instance.
        /// </summary>
        public static readonly T[] Value = new T[0];
    }
}