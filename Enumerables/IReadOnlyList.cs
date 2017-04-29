#if !NETFX_45

// ReSharper disable once CheckNamespace
namespace System.Collections.Generic
{
    /// <summary>
    ///     A read only lists allows you to publically expose a list without the risk of having the data manipulated.
    /// </summary>
    /// <typeparam name="T">The list type.</typeparam>
    public interface IReadOnlyList<T> : IReadOnlyCollection<T>
    {
        /// <summary>
        ///     Indexes the list by the given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The value associated with this index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="index" /> is out of range. </exception>
        T this[int index] { get; }
    }
}

#endif