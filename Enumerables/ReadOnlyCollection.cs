using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     A <see cref="ReadOnlyCollection{T}" /> allows access to a collection without given the user an
    ///     opportunity to manipulate the data.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    /// <remarks>This class is abstract. Feel free to implement your own subclasses for customized or enhanced behaviour.</remarks>
    [PublicAPI]
    public abstract class ReadOnlyCollection<T> : IEnumerable<T>
    {
        /// <summary>
        ///     The amount of items in this collection.
        /// </summary>
        public abstract int Count { get; }

        /// <inheritdoc />
        public abstract IEnumerator<T> GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region TransformerDirectImpl

        private class TransformerDirectImpl<TRaw> : ReadOnlyCollection<T>
        {
            private readonly ICollection<TRaw> m_Collection;
            private readonly Func<TRaw, T> m_Transformer;

            /// <inheritdoc />
            /// <exception cref="ArgumentNullException">
            ///     <paramref name="collection" /> is <see langword="null" /> -or-
            ///     <paramref name="transformer" /> is <see langword="null" />
            /// </exception>
            public TransformerDirectImpl(ICollection<TRaw> collection, Func<TRaw, T> transformer)
            {
                if (collection == null) {
                    throw new ArgumentNullException(nameof(collection));
                }
                if (transformer == null) {
                    throw new ArgumentNullException(nameof(transformer));
                }
                m_Collection = collection;
                m_Transformer = transformer;
            }

            /// <inheritdoc />
            public override int Count => m_Collection.Count;

            /// <inheritdoc />
            public override IEnumerator<T> GetEnumerator()
            {
                foreach (TRaw raw in m_Collection) {
                    yield return m_Transformer(raw);
                }
            }
        }

        #endregion

        #region DirectImpl

        private class DirectImpl : ReadOnlyCollection<T>
        {
            private readonly ICollection<T> m_Collection;

            /// <inheritdoc />
            /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" /></exception>
            public DirectImpl(ICollection<T> collection)
            {
                if (collection == null) {
                    throw new ArgumentNullException(nameof(collection));
                }
                m_Collection = collection;
            }

            /// <inheritdoc />
            public override int Count => m_Collection.Count;

            /// <inheritdoc />
            public override IEnumerator<T> GetEnumerator()
            {
                return m_Collection.GetEnumerator();
            }
        }

        #endregion

        #region FuncImpl

        private class FuncImpl : ReadOnlyCollection<T>
        {
            private readonly Func<ICollection<T>> m_Func;


            /// <inheritdoc />
            /// <exception cref="ArgumentNullException"><paramref name="func" /> is <see langword="null" /></exception>
            public FuncImpl(Func<ICollection<T>> func)
            {
                if (func == null) {
                    throw new ArgumentNullException(nameof(func));
                }
                m_Func = func;
            }

            /// <inheritdoc />
            public override int Count => m_Func().Count;

            /// <inheritdoc />
            public override IEnumerator<T> GetEnumerator()
            {
                return m_Func().GetEnumerator();
            }
        }

        #endregion

        #region Creators

        /// <summary>
        ///     Wraps an already exisiting collection in a read only collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns>The read only collection.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" /></exception>
        public static ReadOnlyCollection<T> Wrap(ICollection<T> collection)
        {
            return new DirectImpl(collection);
        }

        /// <summary>
        ///     Wraps an already exisiting collection in a read only collection. This special implementation allows you to map a
        ///     value from the source collection to a different value using a transformer.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="transformer">The delegate that transforms a value from the source collection to another value.</param>
        /// <returns>The read only collection.</returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="collection" /> is <see langword="null" /> -or-
        ///     <paramref name="transformer" /> is <see langword="null" />
        /// </exception>
        public static ReadOnlyCollection<T> Wrap<TRaw>(ICollection<TRaw> collection, Func<TRaw, T> transformer)
        {
            return new TransformerDirectImpl<TRaw>(collection, transformer);
        }

        /// <summary>
        ///     Creates a new read only collection that obtains its collection through the use of a delegate. This can be handy if
        ///     the actual collection reference changes or must be obtained dynmically.
        /// </summary>
        /// <param name="func">The delegate.</param>
        /// <returns>The read only collection.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="func" /> is <see langword="null" /></exception>
        public static ReadOnlyCollection<T> FromDelegate(Func<ICollection<T>> func)
        {
            return new FuncImpl(func);
        }

        #endregion
    }
}