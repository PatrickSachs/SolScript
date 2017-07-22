using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     A dictionary in which the values are weak references. Written by DLP for SWIG.
    /// </summary>
    /// <remarks>
    ///     Null values are not allowed in this dictionary.
    ///     <br />
    ///     When a value is garbage-collected, the dictionary acts as though the key is
    ///     not present.
    ///     <br />
    ///     This class "cleans up" periodically by removing entries with garbage-collected
    ///     values. Cleanups only occur occasionally, and only when the dictionary is accessed;
    ///     Accessing it (for read or write) more often results in more frequent cleanups.
    ///     <br />
    ///     Watch out! The following interface members are not implemented:
    ///     IDictionary.Values, ICollection.Contains, ICollection.CopyTo, ICollection.Remove.
    ///     Also, the dictionary is NOT MULTITHREAD-SAFE.
    ///     <br />
    ///     Taken from https://gist.github.com/qwertie/3867055
    /// </remarks>
    public class WeakValueTable<TKey, TValue> : IDictionary<TKey, TValue>
        where TValue : class
    {
        private const int MIN_REHASH_INTERVAL = 500;
        private readonly PSDictionary<TKey, WeakReference<TValue>> m_Dictionary = new PSDictionary<TKey, WeakReference<TValue>>();
        private int m_CleanGeneration;
        private int m_Version, m_CleanVersion;

        private class WeakReference<T> : WeakReference
        {
            public WeakReference(T target) : base(target) {}
            public WeakReference(T target, bool trackResurrection) : base(target, trackResurrection) {}
            protected WeakReference(SerializationInfo info, StreamingContext context) : base(info, context) {}

            public new T Target {
                get { return (T) base.Target; }
                set { base.Target = value; }
            }
        }

        #region IDictionary<TKey,TValue> Members

        /// <inheritdoc />
        public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>) m_Dictionary).Keys;

        /// <inheritdoc />
        public ICollection<TValue> Values { // TODO. Maybe. Eventually.
            get { throw new NotImplementedException(); }
        }

        /// <inheritdoc />
        public bool ContainsKey(TKey key)
        {
            AutoCleanup(1);

            WeakReference<TValue> value;
            if (!m_Dictionary.TryGetValue(key, out value)) {
                return false;
            }
            return value.IsAlive;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">An element with the same key already exists in this WeakValueTable</exception>
        public void Add(TKey key, TValue value)
        {
            AutoCleanup(2);

            WeakReference<TValue> wr;
            if (m_Dictionary.TryGetValue(key, out wr)) {
                if (wr.IsAlive) {
                    throw new ArgumentException("An element with the same key already exists in this WeakValueTable", nameof(key));
                }
                wr.Target = value;
            } else {
                m_Dictionary.Add(key, new WeakReference<TValue>(value));
            }
        }

        /// <inheritdoc />
        public bool Remove(TKey key)
        {
            AutoCleanup(1);

            WeakReference<TValue> wr;
            if (!m_Dictionary.TryGetValue(key, out wr)) {
                return false;
            }
            m_Dictionary.Remove(key);
            return wr.IsAlive;
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value)
        {
            AutoCleanup(1);

            WeakReference<TValue> wr;
            if (m_Dictionary.TryGetValue(key, out wr)) {
                value = wr.Target;
            } else {
                value = null;
            }
            return value != null;
        }

        /// <inheritdoc />
        public TValue this[TKey key] {
            get { return m_Dictionary[key].Target; }
            set { m_Dictionary[key] = new WeakReference<TValue>(value); }
        }

        private void AutoCleanup(int incVersion)
        {
            m_Version += incVersion;

            // Cleanup the table every so often--less often for larger tables.
            long delta = m_Version - m_CleanVersion;
            if (delta > MIN_REHASH_INTERVAL + m_Dictionary.Count) {
                // A cleanup will be fruitless unless a GC has happened in the meantime.
                // WeakReferences can become zero only during the GC.
                int curGeneration = GC.CollectionCount(0);
                if (m_CleanGeneration != curGeneration) {
                    m_CleanGeneration = curGeneration;
                    Cleanup();
                    m_CleanVersion = m_Version;
                } else {
                    m_CleanVersion += MIN_REHASH_INTERVAL; // Wait a little while longer
                }
            }
        }

        private void Cleanup()
        {
            // Remove all pairs whose value is nullified.
            // Due to the fact that you can't change a Dictionary while enumerating 
            // it, we need an intermediate collection (the list of things to delete):
            var deadKeys = new List<TKey>();

            foreach (KeyValuePair<TKey, WeakReference<TValue>> kvp in m_Dictionary) {
                if (!kvp.Value.IsAlive) {
                    deadKeys.Add(kvp.Key);
                }
            }

            foreach (TKey key in deadKeys) {
                bool success = m_Dictionary.Remove(key);
                Debug.Assert(success);
            }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            m_Dictionary.Clear();
            m_Version = m_CleanVersion = 0;
            m_CleanGeneration = 0;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public int Count {
            // THIS VALUE MAY BE WRONG (i.e. it may be higher than the number of 
            // items you get from the iterator).
            get { return m_Dictionary.Count; }
        }

        public bool IsReadOnly {
            get { return false; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            int nullCount = 0;

            foreach (KeyValuePair<TKey, WeakReference<TValue>> kvp in m_Dictionary) {
                TValue target = kvp.Value.Target;
                if (target == null) {
                    nullCount++;
                } else {
                    yield return new KeyValuePair<TKey, TValue>(kvp.Key, target);
                }
            }

            if (nullCount > m_Dictionary.Count / 4) {
                Cleanup();
            }
        }

        #endregion
    }
}