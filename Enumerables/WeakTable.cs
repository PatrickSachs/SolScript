using System;
using System.Collections.Generic;
using PSUtility.Math;

namespace PSUtility.Enumerables
{
    public class WeakTable<TKey, TValue> where TKey : class
    {
        private readonly IEqualityComparer<TKey> m_Comparer;
        private int[] m_Buckets;
        private int m_Count;
        private int m_FreeCount;
        private int m_FreeList;
        private int[] m_HashCodes;
        private WeakReference[] m_Keys;
        private int[] m_Next;
        private TValue[] m_Values;
        private ulong m_Version;

        public WeakTable(IEqualityComparer<TKey> comparer)
        {
            m_Comparer = comparer;
        }

        private void Resize(int newSize)
        {
            var bucketsCopy = new int[newSize];
            for (int i = 0; i < bucketsCopy.Length; i++) {
                bucketsCopy[i] = -1;
            }

            var keysCopy = new WeakReference[newSize];
            var valuesCopy = new TValue[newSize];
            var hashCodesCopy = new int[newSize];
            var nextCopy = new int[newSize];

            Array.Copy(m_Values, 0, valuesCopy, 0, m_Count);
            Array.Copy(m_Keys, 0, keysCopy, 0, m_Count);
            Array.Copy(m_HashCodes, 0, hashCodesCopy, 0, m_Count);
            Array.Copy(m_Next, 0, nextCopy, 0, m_Count);

            for (int i = 0; i < m_Count; i++) {
                int index = hashCodesCopy[i] % newSize;
                nextCopy[i] = bucketsCopy[index];
                bucketsCopy[index] = i;
            }

            m_Buckets = bucketsCopy;
            m_Keys = keysCopy;
            m_Values = valuesCopy;
            m_HashCodes = hashCodesCopy;
            m_Next = nextCopy;
        }

        private void Resize()
        {
            Resize(PrimeHelper.ExpandPrime(m_Count));
        }

        public bool TryInsert(TKey key, TValue value, bool overwrite = true)
        {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            if (m_Buckets == null) {
                Initialize(0);
            }
            int hash = m_Comparer.GetHashCode(key) & int.MaxValue;
            int index = hash % m_Buckets.Length;
            int last = -1;
            for (int i1 = m_Buckets[index]; i1 >= 0; i1 = m_Next[i1]) {
                if (m_HashCodes[i1] == hash) {
                    WeakReference wRef = m_Keys[i1];
                    if (!wRef.IsAlive) {
                        if (last < 0) {
                            m_Buckets[index] = m_Next[i1];
                        } else {
                            m_Next[last] = m_Next[i1];
                        }
                        m_HashCodes[i1] = -1;
                        m_Next[i1] = m_FreeList;
                        m_Keys[i1] = null;
                        m_Values[i1] = default(TValue);
                        m_FreeList = i1;
                        m_FreeCount++;
                        m_Version++;
                    } else if (m_Comparer.Equals((TKey) wRef.Target, key)) {
                        // Colliding
                        if (!overwrite) {
                            return false;
                        }
                        m_Values[i1] = value;
                        m_Version++;
                        return true;
                    }
                }
                last = i1;
            }
            int i2;
            if (m_FreeCount > 0) {
                i2 = m_FreeList;
                m_FreeList = m_Next[i2];
                m_FreeCount--;
            } else {
                if (m_Count == m_Keys.Length) {
                    Resize();
                    index = hash % m_Buckets.Length;
                }
                i2 = m_Count;
                m_Count++;
            }
            m_HashCodes[i2] = hash;
            m_Next[i2] = m_Buckets[index];
            m_Keys[i2] = new WeakReference(key, false);
            m_Values[i2] = value;
            m_Buckets[index] = i2;
            m_Version++;
            return true;
        }

        /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" /></exception>
        public bool TryGet(TKey key, out TValue value)
        {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            int index = FindIndex(key);
            if (index >= 0) {
                value = m_Values[index];
                return true;
            }
            value = default(TValue);
            return false;
        }

        private int FindIndex(TKey key)
        {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            if (m_Buckets != null) {
                int hash = m_Comparer.GetHashCode(key) & int.MaxValue;
                int index = hash % m_Buckets.Length;
                int last = -1;
                for (int i = m_Buckets[hash % m_Buckets.Length]; i >= 0; i = m_Next[i]) {
                    if (m_HashCodes[i] == hash) {
                        WeakReference wRef = m_Keys[i];
                        if (!wRef.IsAlive) {
                            if (last < 0) {
                                m_Buckets[index] = m_Next[i];
                            } else {
                                m_Next[last] = m_Next[i];
                            }
                            m_HashCodes[i] = -1;
                            m_Next[i] = m_FreeList;
                            m_Keys[i] = null;
                            m_Values[i] = default(TValue);
                            m_FreeList = i;
                            m_FreeCount++;
                            m_Version++;
                        } else if (m_Comparer.Equals((TKey) wRef.Target, key)) {
                            return i;
                        }
                    }
                    last = i;
                }
            }
            return -1;
        }

        public bool TryRemove(TKey key)
        {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            int hash = m_Comparer.GetHashCode(key) & int.MaxValue;
            int index = hash % m_Buckets.Length;
            int last = -1;
            for (int i = m_Buckets[index]; i >= 0; i = m_Next[i]) {
                if (m_HashCodes[i] == hash) {
                    WeakReference wRef = m_Keys[i];
                    bool doDelete = false;
                    bool doReturn = false;
                    if (!wRef.IsAlive) {
                        doDelete = true;
                    } else if (m_Comparer.Equals((TKey) wRef.Target, key)) {
                        doDelete = true;
                        doReturn = true;
                    }
                    if (doDelete) {
                        if (last < 0) {
                            m_Buckets[index] = m_Next[i];
                        } else {
                            m_Next[last] = m_Next[i];
                        }
                        m_HashCodes[i] = -1;
                        m_Next[i] = m_FreeList;
                        m_Keys[i] = null;
                        m_Values[i] = default(TValue);
                        m_FreeList = i;
                        m_FreeCount++;
                        m_Version++;
                    }
                    if (doReturn) {
                        return true;
                    }
                }
                last = i;
            }
            return false;
        }

        private void Initialize(int capacity)
        {
            int prime = PrimeHelper.GetPrime(capacity);
            m_Buckets = new int[prime];
            for (int i = 0; i < m_Buckets.Length; i++) {
                m_Buckets[i] = -1;
            }
            m_Keys = new WeakReference[prime];
            m_Values = new TValue[prime];
            m_HashCodes = new int[prime];
            m_Next = new int[prime];
            m_FreeList = -1;
        }
    }
}