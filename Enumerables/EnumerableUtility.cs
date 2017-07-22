using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     Extension and utility methods for the <see cref="IEnumerable{T}" /> interface.
    /// </summary>
    [PublicAPI]
    public static class EnumerableUtility
    {
        private const string NULL = "null";
        private const string DEFAULT_JOINER = ", ";

        /// <summary>
        ///     Locks onto the sync root and calls a function.
        /// </summary>
        /// <typeparam name="T">The list type.</typeparam>
        /// <param name="list">The list.</param>
        /// <param name="func">The function to invoke once the list has been locked.</param>
        /// <returns>The enumerable.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="list" /> is <see langword="null" /></exception>
        public static PSList<T> Lock<T>([NotNull] this PSList<T> list, Action<PSList<T>> func)
        {
            if (list == null) {
                throw new ArgumentNullException(nameof(list));
            }
            lock (list.SyncRoot) {
                func(list);
            }
            return list;
        }

        /// <summary>
        ///     Locks onto the sync root and calls a function.
        /// </summary>
        /// <typeparam name="T">The list type.</typeparam>
        /// <param name="list">The list.</param>
        /// <returns>The enumerable.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="list" /> is <see langword="null" /></exception>
        public static ReadOnlyList<T> Lock<T>([NotNull] this ReadOnlyList<T> list, Action<ReadOnlyList<T>> func)
        {
            if (list == null) {
                throw new ArgumentNullException(nameof(list));
            }
            lock (list.SyncRoot) {
                func(list);
            }
            return list;
        }

        /// <summary>
        ///     Locks onto the sync root and calls a function.
        /// </summary>
        /// <typeparam name="T">The list type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="list">The list.</param>
        /// <param name="func">The function to invoke once the list has been locked.</param>
        /// <returns>The enumerable.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="list" /> is <see langword="null" /></exception>
        public static TResult Lock<T, TResult>([NotNull] this PSList<T> list, Func<PSList<T>, TResult> func)
        {
            if (list == null) {
                throw new ArgumentNullException(nameof(list));
            }
            lock (list.SyncRoot) {
                return func(list);
            }
        }

        /// <summary>
        ///     Locks onto the sync root and calls a function.
        /// </summary>
        /// <typeparam name="T">The list type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="list">The list.</param>
        /// <returns>The enumerable.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="list" /> is <see langword="null" /></exception>
        public static TResult Lock<T, TResult>([NotNull] this ReadOnlyList<T> list, Func<ReadOnlyList<T>, TResult> func)
        {
            if (list == null) {
                throw new ArgumentNullException(nameof(list));
            }
            lock (list.SyncRoot) {
                return func(list);
            }
        }

        /// <summary>
        ///     Locks onto the sync root and iterates the collection. Only useful for deferred execution.
        /// </summary>
        /// <typeparam name="T">The list type.</typeparam>
        /// <param name="list">The list.</param>
        /// <returns>The enumerable.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="list" /> is <see langword="null" /></exception>
        public static IEnumerable<T> Lock<T>([NotNull] this PSList<T> list)
        {
            if (list == null) {
                throw new ArgumentNullException(nameof(list));
            }
            lock (list.SyncRoot) {
                foreach (T element in list) {
                    yield return element;
                }
            }
        }

        /// <summary>
        ///     Locks onto the sync root and iterates the collection. Only useful for deferred execution.
        /// </summary>
        /// <typeparam name="T">The list type.</typeparam>
        /// <param name="list">The list.</param>
        /// <returns>The enumerable.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="list" /> is <see langword="null" /></exception>
        public static IEnumerable<T> Lock<T>([NotNull] this ReadOnlyList<T> list)
        {
            if (list == null) {
                throw new ArgumentNullException(nameof(list));
            }
            lock (list.SyncRoot) {
                foreach (T element in list) {
                    yield return element;
                }
            }
        }

        /// <summary>
        ///     Either indexes the list at the given index or returns the defualt value if it is not possible.
        /// </summary>
        /// <typeparam name="T">List type.</typeparam>
        /// <param name="list">The list.</param>
        /// <param name="index">The index.</param>
        /// <param name="defValue">The default value.</param>
        /// <returns>The value.</returns>
        public static T IndexOrDefault<T>(this IList<T> list, int index, T defValue = default(T))
        {
            if (index < 0 || index >= list.Count) {
                return defValue;
            }
            return list[index];
        }


        /// <summary>
        ///     Either indexes the list at the given index or returns the defualt value if it is not possible.
        /// </summary>
        /// <typeparam name="T">List type.</typeparam>
        /// <param name="list">The list.</param>
        /// <param name="index">The index.</param>
        /// <param name="defValue">The default value.</param>
        /// <returns>The value.</returns>
        public static T IndexOrDefault<T>(this ReadOnlyList<T> list, int index, T defValue = default(T))
        {
            if (index < 0 || list.Count >= index) {
                return defValue;
            }
            return list[index];
        }

        /// <summary>
        ///     Concatenates two sequences.
        /// </summary>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="second">The sequence to concatenate to the first sequence. </param>
        /// <typeparam name="TSource">TSource:  The type of the elements of the input sequences. </typeparam>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains the concatenated elements of the two input sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is null.</exception>
        public static IEnumerable<TSource> Concat<TSource>(
            this IEnumerable<TSource> first, 
            params TSource[] second)
        {
            return first.Concat((IEnumerable<TSource>)second);
        }

        /// <summary>
        ///     Concatenates two sequences.
        /// </summary>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="concat">How should the operation be performed?</param>
        /// <param name="second">The sequence to concatenate to the first sequence. </param>
        /// <typeparam name="TSource">TSource:  The type of the elements of the input sequences. </typeparam>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains the concatenated elements of the two input sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is null.</exception>
        public static IEnumerable<TSource> Concat<TSource>(
            this IEnumerable<TSource> first,
            EnumerableConcat concat,
            params TSource[] second)
        {
            return Concat(first, concat, (IEnumerable<TSource>) second);
        }

        /// <summary>
        ///     Concatenates two sequences.
        /// </summary>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="concat">How should the operation be performed?</param>
        /// <param name="second">The sequence to concatenate to the first sequence. </param>
        /// <typeparam name="TSource">TSource:  The type of the elements of the input sequences. </typeparam>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains the concatenated elements of the two input sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is null.</exception>
        public static IEnumerable<TSource> Concat<TSource>(
            this IEnumerable<TSource> first,
            EnumerableConcat concat,
            IEnumerable<TSource> second)
        {
            if (concat == EnumerableConcat.Append)
            {
                return first.Concat(second);
            }
            else
            {
                return second.Concat(first);
            }
        }

        /// <summary>
        ///     Converts an enumerable of objects to a string using the <see cref="object.ToString()" /> method.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="separator">The separator between values.</param>
        /// <param name="enumerable">The enumerable to convert.</param>
        /// <returns>The joined string.</returns>
        /// <seealso cref="JoinToString{T}(IEnumerable{T}, string, Joiner{T})" />
        [DebuggerStepThrough]
        public static string JoinToString<T>(this IEnumerable<T> enumerable, string separator = DEFAULT_JOINER)
        {
            StringBuilder builder = new StringBuilder();
            foreach (T element in enumerable) {
                if (builder.Length != 0) {
                    builder.Append(separator);
                }
                builder.Append(element != null ? element.ToString() : NULL);
            }
            return builder.ToString();
        }

        [NotNull] public delegate string Joiner<in T>(T value);

        /// <summary>
        ///     Converts an enumerable of objects to a string using a conversion delegate.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="separator">The separator between values.</param>
        /// <param name="enumerable">The enumerable to convert.</param>
        /// <param name="obtainer">The delegate used to convert.</param>
        /// <returns>The joined string.</returns>
        /// <seealso cref="JoinToString{T}(IEnumerable{T}, string)" />
        [DebuggerStepThrough]
        public static string JoinToString<T>(this IEnumerable<T> enumerable, string separator, Joiner<T> obtainer)
        {
            StringBuilder builder = new StringBuilder();
            foreach (T element in enumerable) {
                if (builder.Length != 0) {
                    builder.Append(separator);
                }
                builder.Append(obtainer(element));
            }
            return builder.ToString();
        }

        /// <summary>
        ///     Converts an enumerable of objects to a string using a conversion delegate.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="enumerable">The enumerable to convert.</param>
        /// <param name="obtainer">The delegate used to convert.</param>
        /// <returns>The joined string.</returns>
        /// <seealso cref="JoinToString{T}(IEnumerable{T}, string)" />
        [DebuggerStepThrough]
        public static string JoinToString<T>(this IEnumerable<T> enumerable, Joiner<T> obtainer)
        {
            return JoinToString(enumerable, DEFAULT_JOINER, obtainer);
        }

        /// <summary>
        ///     Removes all elements matching the predicate from this collection.
        /// </summary>
        /// <typeparam name="T">The collection type.</typeparam>
        /// <param name="collection">The collection to remove from.</param>
        /// <param name="predicate">The removal predicate.</param>
        /// <returns>The amount of elements removed.</returns>
        public static int RemoveWhere<T>(this ICollection<T> collection, Func<T, bool> predicate)
        {
            if (collection.Count == 0) {
                return 0;
            }
            List<T> remove = collection.Where(predicate).ToList();
            int removed = 0;
            foreach (T toRemove in remove) {
                if (collection.Remove(toRemove)) {
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>
        ///     Finds the first index in the enumerable matching the given predicate.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="predicate">The predicate to use for matches.</param>
        /// <returns>The index.</returns>
        /// <remarks>Keep in mind that this method only makes sense on enumerables with consistent indexing.</remarks>
        public static int FindIndex<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            int i = 0;
            foreach (T value in enumerable) {
                if (predicate(value)) {
                    return i;
                }
                i++;
            }
            return -1;
        }
    }
}