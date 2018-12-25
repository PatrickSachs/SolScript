using JetBrains.Annotations;

namespace PSUtility.Metadata
{
    /// <summary>
    ///     This interface is implemented on types providing the storage of meta data.
    /// </summary>
    [PublicAPI]
    public interface IMetaDataProvider
    {
        /// <summary>
        ///     Tries to get the value assigned with the given meta key.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="key">The meta key.</param>
        /// <param name="value">The meta value.</param>
        /// <returns>true if the value could be obtained, false if not.</returns>
        bool TryGetMetaValue<T>(MetaKey<T> key, out T value);

        /// <summary>
        ///     Tries to set the value to the given meta key.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="key">The meta key.</param>
        /// <param name="value">The meta value.</param>
        /// <returns>true if the value could be set, false if not.</returns>
        bool TrySetMetaValue<T>(MetaKey<T> key, T value);

        /// <summary>
        ///     Checks if a value is assigned to the given meta key.
        /// </summary>
        /// <typeparam name="T">The calue type.</typeparam>
        /// <param name="key">The meta key.</param>
        /// <param name="ignoreType">If this is true the type(<typeparamref name="T" />) will be ignored.</param>
        /// <returns>true if a key exists, false if not.</returns>
        bool HasMetaValue<T>(MetaKey<T> key, bool ignoreType = false);
    }
}