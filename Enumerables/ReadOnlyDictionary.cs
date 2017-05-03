using System;
using System.Collections;
using System.Collections.Generic;

namespace PSUtility.Enumerables
{
    public class ReadOnlyDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Func<IDictionary<TKey, TValue>> m_Func;
        private readonly IDictionary<TKey, TValue> m_Reference;
        private ReadOnlyCollection<TKey> m_Keys;
        private ReadOnlyCollection<TValue> m_Values;

        public ReadOnlyDictionary(IDictionary<TKey, TValue> reference)
        {
            m_Reference = reference;
        }

        public ReadOnlyDictionary(Func<IDictionary<TKey, TValue>> func)
        {
            m_Func = func;
        }

        protected virtual IDictionary<TKey, TValue> Dictionary {
            get {
                if (m_Reference != null) {
                    return m_Reference;
                }
                return m_Func();
            }
        }

        /// <inheritdoc />
        public int Count => Dictionary.Count;

        /// <inheritdoc />
        public TValue this[TKey key] => Dictionary[key];

        /// <inheritdoc />
        public ReadOnlyCollection<TKey> Keys {
            get {
                if (m_Keys == null) {
                    m_Keys = m_Reference != null
                        ? new ReadOnlyCollection<TKey>(m_Reference.Keys)
                        : new ReadOnlyCollection<TKey>(() => Dictionary.Keys);
                }
                return m_Keys;
            }
        }

        /// <inheritdoc />
        public ReadOnlyCollection<TValue> Values {
            get {
                if (m_Values == null) {
                    m_Values = m_Reference != null
                        ? new ReadOnlyCollection<TValue>(m_Reference.Values)
                        : new ReadOnlyCollection<TValue>(() => Dictionary.Values);
                }
                return m_Values;
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }

        /// <inheritdoc />
        public bool ContainsKey(TKey key)
        {
            return Dictionary.ContainsKey(key);
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value)
        {
            return Dictionary.TryGetValue(key, out value);
        }
    }
}