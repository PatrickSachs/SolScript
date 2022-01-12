using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     A <see cref="ReadOnlyDictionary{TKey,TValue}" /> allows access to a dictionary without given the user an
    ///     opportunity to manipulate the data.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <remarks>This class is abstract. Feel free to implement your own subclasses for customized or enhanced behaviour.</remarks>
    [PublicAPI]
    public abstract class ReadOnlyDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        /// <inheritdoc />
        public abstract int Count { get; }

        /// <inheritdoc />
        public abstract TValue this[TKey key] { get; }

        /// <inheritdoc />
        public abstract ReadOnlyCollection<TKey> Keys { get; }

        /// <inheritdoc />
        public abstract ReadOnlyCollection<TValue> Values { get; }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public abstract IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator();

        /// <summary>
        ///     Wraps an already exisiting dictionary in a read only dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns>The read only dictionary.</returns>
        public static ReadOnlyDictionary<TKey, TValue> Wrap(IDictionary<TKey, TValue> dictionary)
        {
            return new DirectImpl(dictionary);
        }

        /// <summary>
        ///     Creates a new read only dictionary that obtains its dictionary through the use of a delegate. This can be handy if
        ///     the actual dictionary reference changes or must be obtained dynmically.
        /// </summary>
        /// <param name="func">The delegate.</param>
        /// <returns>The read only dictionary.</returns>
        public static ReadOnlyDictionary<TKey, TValue> FromDelegate(Func<IDictionary<TKey, TValue>> func)
        {
            return new FuncImpl(func);
        }

        /// <inheritdoc />
        public abstract bool ContainsKey(TKey key);

        /// <inheritdoc />
        public abstract bool TryGetValue(TKey key, out TValue value);

        private class FuncImpl : ReadOnlyDictionary<TKey, TValue>
        {
            private readonly Func<IDictionary<TKey, TValue>> m_Func;
            private ReadOnlyCollection<TKey> m_Keys;
            private ReadOnlyCollection<TValue> m_Values;

            public FuncImpl(Func<IDictionary<TKey, TValue>> func)
            {
                m_Func = func;
            }

            /// <inheritdoc />
            public override int Count => m_Func().Count;

            /// <inheritdoc />
            public override TValue this[TKey key] {
                get { return m_Func()[key]; }
            }

            /// <inheritdoc />
            public override ReadOnlyCollection<TKey> Keys {
                get {
                    if (m_Keys == null) {
                        m_Keys = ReadOnlyCollection<TKey>.FromDelegate(() => m_Func().Keys);
                    }
                    return m_Keys;
                }
            }

            /// <inheritdoc />
            public override ReadOnlyCollection<TValue> Values {
                get {
                    if (m_Values == null) {
                        m_Values = ReadOnlyCollection<TValue>.FromDelegate(() => m_Func().Values);
                    }
                    return m_Values;
                }
            }

            /// <inheritdoc />
            public override IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                return m_Func().GetEnumerator();
            }

            /// <inheritdoc />
            public override bool ContainsKey(TKey key)
            {
                return m_Func().ContainsKey(key);
            }

            /// <inheritdoc />
            public override bool TryGetValue(TKey key, out TValue value)
            {
                return m_Func().TryGetValue(key, out value);
            }
        }

        private class DirectImpl : ReadOnlyDictionary<TKey, TValue>
        {
            private readonly IDictionary<TKey, TValue> m_Reference;
            private ReadOnlyCollection<TKey> m_Keys;
            private ReadOnlyCollection<TValue> m_Values;

            public DirectImpl(IDictionary<TKey, TValue> reference)
            {
                m_Reference = reference;
            }

            /// <inheritdoc />
            public override int Count => m_Reference.Count;

            /// <inheritdoc />
            public override TValue this[TKey key] {
                get { return m_Reference[key]; }
            }

            /// <inheritdoc />
            public override ReadOnlyCollection<TKey> Keys {
                get {
                    // Using delegate since the reference value collection might change (?)
                    if (m_Keys == null) {
                        m_Keys = ReadOnlyCollection<TKey>.FromDelegate(() => m_Reference.Keys);
                    }
                    return m_Keys;
                }
            }

            /// <inheritdoc />
            public override ReadOnlyCollection<TValue> Values {
                get {
                    if (m_Values == null) {
                        m_Values = ReadOnlyCollection<TValue>.FromDelegate(() => m_Reference.Values);
                    }
                    return m_Values;
                }
            }

            /// <inheritdoc />
            public override IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                return m_Reference.GetEnumerator();
            }

            /// <inheritdoc />
            public override bool ContainsKey(TKey key)
            {
                return m_Reference.ContainsKey(key);
            }

            /// <inheritdoc />
            public override bool TryGetValue(TKey key, out TValue value)
            {
                return m_Reference.TryGetValue(key, out value);
            }
        }
    }
}