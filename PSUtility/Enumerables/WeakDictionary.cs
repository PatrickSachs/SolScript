using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace PSUtility.Enumerables
{
    public sealed class WeakDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : class
    {
        private readonly IEqualityComparer<TKey> m_Comparer;
        private readonly IEqualityComparer<TValue> m_ValueComparer;
        private readonly SynchronizationContext m_Context;
        private readonly PSDictionary<WeakReference, TValue> m_Dictionary = new PSDictionary<WeakReference, TValue>();
        private readonly Thread m_PurgeThread;
        private int m_AutoPurge;

        /// <inheritdoc />
        public WeakDictionary(int autoPurge, IEqualityComparer<TKey> comparer, IEqualityComparer<TValue> valueComparer)
        {
            m_AutoPurge = autoPurge;
            m_Comparer = comparer;
            m_ValueComparer = valueComparer;
            m_Context = SynchronizationContext.Current;
            if (m_AutoPurge > 0) {
                m_PurgeThread = new Thread(PurgeThread);
                m_PurgeThread.Name = "WeakDictionary<" + typeof(TKey).Name + ", " + typeof(TValue).Name + ">";
                m_PurgeThread.Start();
            } else {
                GC.SuppressFinalize(this);
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
            return Query((key, value) => QueryResult.Take).GetEnumerator();
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Query((key1, value1) => {
                if (m_Comparer.Equals(key1, item.Key))
                {
                    throw new ArgumentException("The already exists already.");
                }
                return QueryResult.None;
            }, () => {
                m_Dictionary.Add(new WeakReference(item.Key), item.Value);
            });
        }

        /// <inheritdoc />
        public void Clear()
        {
            PSDictionary<WeakReference, TValue> clone;
            lock (m_Dictionary.SyncRoot) {
                clone = new PSDictionary<WeakReference, TValue>(m_Dictionary);
                m_Dictionary.Clear();
            }
            foreach (KeyValuePair<WeakReference, TValue> pair in clone) {
                EntryPurged?.Invoke(this, new WeakDictionaryEntryPurgedEventArgs<TValue>(pair.Value, !pair.Key.IsAlive));
            }
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return Query((key1, value1) => {
                if (m_Comparer.Equals(key1, item.Key) && m_ValueComparer.Equals(value1, item.Value))
                {
                    return QueryResult.Take | QueryResult.End;
                }
                return QueryResult.None;
            }).Count != 0;
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Query((key1, value1) => {
                if (m_Comparer.Equals(key1, item.Key) && m_ValueComparer.Equals(value1, item.Value))
                {
                    return QueryResult.Take | QueryResult.Remove | QueryResult.End;
                }
                return QueryResult.None;
            }).Count != 0;
        }

        /// <inheritdoc />
        public int Count => m_Dictionary.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public bool ContainsKey(TKey key)
        {
            return Query((key1, value1) => {
                if (m_Comparer.Equals(key1, key))
                {
                    return QueryResult.Take | QueryResult.End;
                }
                return QueryResult.None;
            }).Count != 0;
        }

        /// <inheritdoc />
        public void Add(TKey key, TValue value)
        {
            lock (m_Dictionary.SyncRoot) {
                m_Dictionary.Add(new WeakReference(key), value);
            }
        }

        /// <inheritdoc />
        public bool Remove(TKey key)
        {
            return Query((key1, value1) => {
                if (m_Comparer.Equals(key1, key)) {
                    return QueryResult.Take | QueryResult.Remove | QueryResult.End;
                }
                return QueryResult.None;
            }).Count != 0;
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value)
        {
            using (var i = Query((key1, value1) => {
                if (m_Comparer.Equals(key1, key)) {
                    return QueryResult.Take | QueryResult.End;
                }
                return QueryResult.None;
            }).GetEnumerator()) {
                if (i.MoveNext()) {
                    value = i.Current.Value;
                    return true;
                }
                value = default(TValue);
                return false;
            }
        }

        /// <inheritdoc />
        public TValue this[TKey key] {
            get {
                return Query((key1, value1) => {
                    if (m_Comparer.Equals(key1, key)) {
                        return QueryResult.Take | QueryResult.End;
                    }
                    return QueryResult.None;
                }).Select(s => s.Value).FirstOrDefault();
            }
            set {
                Query((key1, value1) => {
                    if (m_Comparer.Equals(key1, key)) {
                        return QueryResult.Remove | QueryResult.End;
                    }
                    return QueryResult.None;
                }, () => { m_Dictionary.Add(new WeakReference(key), value); });
            }
        }

        /// <inheritdoc />
        public ICollection<TKey> Keys { get; }

        /// <inheritdoc />
        public ICollection<TValue> Values { get; }

        /// <inheritdoc />
        ~WeakDictionary()
        {
            m_AutoPurge = -1;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "WeakDictionary<" + typeof(TKey).Name + ", " + typeof(TValue).Name + ">";
        }

        public event EventHandler<WeakDictionary<TKey, TValue>, WeakDictionaryEntryPurgedEventArgs<TValue>> EntryPurged;

        private PSList<KeyValuePair<TKey, TValue>> Query(Func<TKey, TValue, QueryResult> func, Action syncAction = null)
        {
            Trace.WriteLine("Query " + ToString());
            PSHashSet<KeyValuePair<WeakReference, TValue>> purged = null;
            var results = new PSList<KeyValuePair<TKey, TValue>>();
            lock (m_Dictionary.SyncRoot) {
                foreach (KeyValuePair<WeakReference, TValue> pair in m_Dictionary) {
                    if (!pair.Key.IsAlive)
                    {
                        Trace.WriteLine("[PURGE] Somethign is dead " + pair.Value);
                        if (purged == null) {
                            purged = new PSHashSet<KeyValuePair<WeakReference, TValue>>();
                        }
                        purged.Add(pair);
                    } else {
                        TKey key = (TKey) pair.Key.Target;
                        QueryResult result = func(key, pair.Value);
                        if ((result & QueryResult.Take) == QueryResult.Take) {
                            results.Add(new KeyValuePair<TKey, TValue>(key, pair.Value));
                        }
                        if ((result & QueryResult.Remove) == QueryResult.Remove) {
                            if (purged == null) {
                                purged = new PSHashSet<KeyValuePair<WeakReference, TValue>>();
                            }
                            Trace.WriteLine("[PURGE] Requesting manual purge of " + pair.Key.Target + "/"+pair.Value + "\n" + new StackTrace().ToString());
                            purged.Add(pair);
                        }
                        if ((result & QueryResult.End) == QueryResult.End) {
                            break;
                        }
                    }
                }
                if (purged != null) {
                    foreach (KeyValuePair<WeakReference, TValue> reference in purged) {
                        m_Dictionary.Remove(reference.Key);
                    }
                }
                syncAction?.Invoke();
            }
            if (purged != null) {
                foreach (KeyValuePair<WeakReference, TValue> purgedValue in purged) {
                    EntryPurged?.Invoke(this, new WeakDictionaryEntryPurgedEventArgs<TValue>(purgedValue.Value, !purgedValue.Key.IsAlive));
                }
            }
            return results;
        }

        private void PurgeThread()
        {
            var dead = new PSHashSet<WeakReference>();
            Trace.WriteLine("[AUTO PURGER] Now runnng purger " + Thread.CurrentThread.Name);
            while (m_AutoPurge > 0) {
                //Trace.WriteLine("   ... Starting Wait " + Thread.CurrentThread.Name);
                Thread.Sleep(m_AutoPurge);
                //Trace.WriteLine("   ... Finished Wait " + Thread.CurrentThread.Name);
                PSHashSet<TValue> purged = null;
                lock (m_Dictionary.SyncRoot) {
                    foreach (KeyValuePair<WeakReference, TValue> pair in m_Dictionary) {
                        if (!pair.Key.IsAlive) {
                            if (purged == null) {
                                purged = new PSHashSet<TValue>();
                            }
                            Trace.WriteLine("   ... Purging: " + pair.Value + " in " + Thread.CurrentThread.Name);
                            dead.Add(pair.Key);
                            purged.Add(pair.Value);
                        }
                    }
                    foreach (WeakReference reference in dead) {
                        m_Dictionary.Remove(reference);
                    }
                }
                dead.Clear();
                if (purged != null) {
                    m_Context.Post(state => {
                        foreach (TValue value in purged) {
                            EntryPurged?.Invoke(this, new WeakDictionaryEntryPurgedEventArgs<TValue>(value, true));
                        }
                    }, null);
                }
                //Trace.WriteLine("   ... Next Round " + Thread.CurrentThread.Name);
            }
        }

        [Flags]
        private enum QueryResult
        {
            None = 0,
            Take = 1,
            End = 2,
            Remove = 4
        }
    }
}