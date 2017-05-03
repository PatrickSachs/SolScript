using System.Collections.Generic;
using PSUtility.Enumerables;

namespace SolScript.Utility
{
    /// <summary>
    ///     Useful in number of places that return an empty list to avoid unnecessary memory allocation.
    /// </summary>
    /// <typeparam name="T">The list type.</typeparam>
    internal static class EmptyReadOnlyList<T>
    {
        /// <summary>
        ///     The list instance.
        /// </summary>
        public static readonly ReadOnlyList<T> Value = new ReadOnlyList<T>(ArrayUtility.Empty<T>());
    }
}