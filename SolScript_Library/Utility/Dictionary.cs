using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace SolScript.Utility
{
    /// <summary>
    ///     This <see cref="Dictionary{TKey,TValue}" /> extends
    ///     <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> and implements
    ///     <see cref="IReadOnlyDictionary{TKey,TValue}" />.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    public class Dictionary<TKey, TValue> : System.Collections.Generic.Dictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        /// <inheritdoc />
        public Dictionary() {}

        /// <inheritdoc />
        public Dictionary(int capacity) : base(capacity) {}

        /// <inheritdoc />
        public Dictionary(IEqualityComparer<TKey> comparer) : base(comparer) {}

        /// <inheritdoc />
        public Dictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) {}

        /// <inheritdoc />
        public Dictionary([NotNull] IDictionary<TKey, TValue> dictionary) : base(dictionary) {}

        /// <inheritdoc />
        public Dictionary([NotNull] IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer) {}

        /// <inheritdoc />
        protected Dictionary(SerializationInfo info, StreamingContext context) : base(info, context) {}

        // The lazy dictionary keys.
        private Collection<TKey> l_keys;
        // The lazy dictionary values.
        private Collection<TValue> l_values;

        #region IReadOnlyDictionary<TKey,TValue> Members

        /// <inheritdoc />
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            TValue value;
            if (!TryGetValue(item.Key, out value)) {
                return false;
            }
            return Equals(value, item.Value);
        }

        /// <inheritdoc />
        public new IReadOnlyCollection<TKey> Keys => l_keys ?? (l_keys = new Collection<TKey>(base.Keys));

        /// <inheritdoc />
        public new IReadOnlyCollection<TValue> Values => l_values ?? (l_values = new Collection<TValue>(base.Values));

        #endregion
    }
}