using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PSUtility.Properties;
using PSUtility.Strings;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     Several utility methods related to arrays.
    /// </summary>
    public static class ArrayUtility
    {
        /// <summary>
        ///     Gets an empty array of the given type. The array instance is cached.
        /// </summary>
        /// <typeparam name="T">The array type.</typeparam>
        /// <returns>The array.</returns>
        public static T[] Empty<T>()
        {
            return EmptyArray<T>.Value;
        }

        /// <summary>
        ///     Gets the element type of the given array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns>The element type.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" /></exception>
        public static Type GetElementType(this Array array)
        {
            if (array == null) {
                throw new ArgumentNullException(nameof(array));
            }
            return array.GetType().GetElementType();
        }

        /// <summary>
        ///     Copies <paramref name="count" /> elements from <paramref name="source" /> to the <paramref name="target" />
        ///     array.
        /// </summary>
        /// <param name="source">The source to copy from.</param>
        /// <param name="sourceOffset">How many elements should be skipped before copying?</param>
        /// <param name="target">The target to copy to.</param>
        /// <param name="offset">Which index is the first index elements should be copied to?</param>
        /// <param name="count">How many elements should be copied?</param>
        /// <remarks>
        ///     This method will try really hard to find an efficient way to copy the elements. It will check if
        ///     <paramref name="source" /> is a (ReadOnly)List or Array(T) before iterating the enumerable itself.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="sourceOffset" /> is smaller than 0. -or-
        ///     <paramref name="offset" /> is smaller than 0. -or- <paramref name="count" /> is smaller than 0.
        /// </exception>
        /// <exception cref="RankException"><paramref name="target" /> is multidimensional.</exception>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The element type of <paramref name="target" /> is not assignable from
        ///     <typeparamref name="T" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="target" /> is not long enough to store all elements. -or-
        ///     <paramref name="source" /> does not provide enough elements.
        /// </exception>
        /// <exception cref="InvalidCastException">
        ///     At least one element in <paramref name="source" /> cannot be cast to
        ///     <typeparamref name="T" />.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />. -or-
        ///     <paramref name="target" /> is <see langword="null" />.
        /// </exception>
        public static void Copy<T>(IEnumerable<T> source, int sourceOffset, Array target, int offset, int count)
        {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }
            if (sourceOffset < 0) {
                throw new ArgumentOutOfRangeException(Resources.Err_SmallerThanZero.FormatWith(nameof(sourceOffset), sourceOffset));
            }
            if (offset < 0) {
                throw new ArgumentOutOfRangeException(Resources.Err_SmallerThanZero.FormatWith(nameof(offset), offset));
            }
            if (count < 0) {
                throw new ArgumentOutOfRangeException(Resources.Err_SmallerThanZero.FormatWith(nameof(count), count));
            }
            if (target.Rank != 1) {
                throw new RankException(Resources.Err_InvalidArrayRank.FormatWith(target.Rank, 1));
            }
            if (!typeof(T).IsAssignableFrom(target.GetElementType())) {
                throw new ArrayTypeMismatchException(Resources.Err_InvalidArrayType.FormatWith(target.GetElementType(), typeof(T)));
            }
            int endSourceIndex = sourceOffset + count;
            int endTargetIndex = offset + count;
            if (target.Length < endTargetIndex) {
                throw new ArgumentException(Resources.Err_ArrayTooSmall.FormatWith(target.Length, endTargetIndex), nameof(target));
            }
            // Arrays can be copied using native methods.
            {
                var sourceArray = source as T[];
                if (sourceArray == null) {
                    var temp = source as Array<T>;
                    if (temp != null) {
                        sourceArray = temp.m_Array;
                    }
                }
                if (sourceArray != null) {
                    if (sourceArray.Length < endSourceIndex) {
                        throw new ArgumentException(Resources.Err_ArrayTooSmall.FormatWith(sourceArray.Length, endSourceIndex), nameof(source));
                    }
                    Array.Copy(sourceArray, sourceOffset, target, offset, count);
                    return;
                }
            }
            // Lists provide better offset handling.
            /*{
                var sourceReadOnlyList = source as IReadOnlyList<T>;
                if (sourceReadOnlyList != null) {
                    if (sourceReadOnlyList.Count < endSourceIndex) {
                        throw new ArgumentException(Resources.Err_ArrayTooSmall.FormatWith(sourceReadOnlyList.Count, endSourceIndex), nameof(source));
                    }
                    for (int i = 0; i < count; i++) {
                        target.SetValue(sourceReadOnlyList[sourceOffset + i], offset + i);
                    }
                    return;
                }
            }*/
            {
                var sourceList = source as IList<T>;
                if (sourceList != null) {
                    if (sourceList.Count < endSourceIndex) {
                        throw new ArgumentException(Resources.Err_ArrayTooSmall.FormatWith(sourceList.Count, endSourceIndex), nameof(source));
                    }
                    for (int i = 0; i < count; i++) {
                        target.SetValue(sourceList[sourceOffset + i], offset + i);
                    }
                    return;
                }
            }
            // If all else fails copy manually. Offset is a bit wacky on enumerables, but we will try regardless. Results
            // may not be deterministic.
            {
                T[] sourceArray = source.ToArray();
                if (sourceArray.Length != endSourceIndex) {
                    throw new ArgumentException(Resources.Err_ArrayTooSmall.FormatWith(sourceArray.Length, endSourceIndex));
                }
                Array.Copy(sourceArray, sourceOffset, target, offset, count);
            }
        }

        /// <summary>
        ///     Copies <paramref name="count" /> elements from <paramref name="source" /> to the <paramref name="target" />
        ///     array.
        /// </summary>
        /// <param name="source">The source to copy from.</param>
        /// <param name="sourceOffset">How many elements should be skipped before copying?</param>
        /// <param name="target">The target to copy to.</param>
        /// <param name="offset">Which index is the first index elements should be copied to?</param>
        /// <param name="count">How many elements should be copied?</param>
        /// <remarks>
        ///     This method will try really hard to find an efficient way to copy the elements. It will check if
        ///     <paramref name="source" /> is a (ReadOnly)List or Array(T) before iterating the enumerable itself.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="sourceOffset" /> is smaller than 0. -or-
        ///     <paramref name="offset" /> is smaller than 0. -or- <paramref name="count" /> is smaller than 0.
        /// </exception>
        /// <exception cref="RankException"><paramref name="target" /> is multidimensional.</exception>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The element type of <paramref name="target" /> is not assignable from
        ///     <typeparamref name="T" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="target" /> is not long enough to store all elements. -or-
        ///     <paramref name="source" /> does not provide enough elements.
        /// </exception>
        /// <exception cref="InvalidCastException">
        ///     At least one element in <paramref name="source" /> cannot be cast to
        ///     <typeparamref name="T" />.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />. -or-
        ///     <paramref name="target" /> is <see langword="null" />.
        /// </exception>
        public static void Copy(IEnumerable source, int sourceOffset, Array target, int offset, int count)
        {
            Copy(source.Cast<object>(), sourceOffset, target, offset, count);
        }

        /// <summary>
        ///     Copies <paramref name="count" /> elements from <paramref name="source" /> to the <paramref name="target" />
        ///     array.
        /// </summary>
        /// <param name="source">The source to copy from.</param>
        /// <param name="sourceOffset">How many elements should be skipped before copying?</param>
        /// <param name="target">The target to copy to.</param>
        /// <param name="offset">Which index is the first index elements should be copied to?</param>
        /// <param name="count">How many elements should be copied?</param>
        /// <remarks>
        ///     This method will try really hard to find an efficient way to copy the elements. It will check if
        ///     <paramref name="source" /> is a (ReadOnly)List or Array(T) before iterating the enumerable itself.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="sourceOffset" /> is smaller than 0. -or-
        ///     <paramref name="offset" /> is smaller than 0. -or- <paramref name="count" /> is smaller than 0.
        /// </exception>
        /// <exception cref="RankException"><paramref name="target" /> is multidimensional.</exception>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The element type of <paramref name="target" /> is not assignable from
        ///     <typeparamref name="T" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="target" /> is not long enough to store all elements. -or-
        ///     <paramref name="source" /> does not provide enough elements.
        /// </exception>
        /// <exception cref="InvalidCastException">
        ///     At least one element in <paramref name="source" /> cannot be cast to
        ///     <typeparamref name="T" />.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />. -or-
        ///     <paramref name="target" /> is <see langword="null" />.
        /// </exception>
        public static void Copy<T>(IEnumerable<T> source, int sourceOffset, Array<T> target, int offset, int count)
        {
            Copy(source, sourceOffset, target.m_Array, offset, count);
        }

        /// <summary>
        ///     Copies <paramref name="count" /> elements from <paramref name="source" /> to the <paramref name="target" />
        ///     array.
        /// </summary>
        /// <param name="source">The source to copy from.</param>
        /// <param name="sourceOffset">How many elements should be skipped before copying?</param>
        /// <param name="target">The target to copy to.</param>
        /// <param name="offset">Which index is the first index elements should be copied to?</param>
        /// <param name="count">How many elements should be copied?</param>
        /// <remarks>
        ///     This method will try really hard to find an efficient way to copy the elements. It will check if
        ///     <paramref name="source" /> is a (ReadOnly)List or Array(T) before iterating the enumerable itself.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="sourceOffset" /> is smaller than 0. -or-
        ///     <paramref name="offset" /> is smaller than 0. -or- <paramref name="count" /> is smaller than 0.
        /// </exception>
        /// <exception cref="RankException"><paramref name="target" /> is multidimensional.</exception>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The element type of <paramref name="target" /> is not assignable from
        ///     <typeparamref name="T" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="target" /> is not long enough to store all elements. -or-
        ///     <paramref name="source" /> does not provide enough elements.
        /// </exception>
        /// <exception cref="InvalidCastException">
        ///     At least one element in <paramref name="source" /> cannot be cast to
        ///     <typeparamref name="T" />.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />. -or-
        ///     <paramref name="target" /> is <see langword="null" />.
        /// </exception>
        public static void Copy<T>(IEnumerable source, int sourceOffset, Array<T> target, int offset, int count)
        {
            Copy(source.Cast<object>(), sourceOffset, target.m_Array, offset, count);
        }

        /// <summary>
        ///     Copies <paramref name="count" /> elements from <paramref name="source" /> to the <paramref name="target" />
        ///     array-
        /// </summary>
        /// <param name="source">The source to copy from.</param>
        /// <param name="sourceOffset">How many elements should be skipped before copying?</param>
        /// <param name="target">The target to copy to.</param>
        /// <param name="offset">Which index is the first index elements should be copied to?</param>
        /// <param name="count">How many elements should be copied?</param>
        /// <remarks>
        ///     This method will try really hard to find an efficient way to copy the elements. It will check if
        ///     <paramref name="source" /> is a (ReadOnly)List or Array(T) before iterating the enumerable itself.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="sourceOffset" /> is smaller than 0. -or-
        ///     <paramref name="offset" /> is smaller than 0. -or- <paramref name="count" /> is smaller than 0.
        /// </exception>
        /// <exception cref="RankException"><paramref name="target" /> is multidimensional.</exception>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The element type of <paramref name="target" /> is not assignable from
        ///     <typeparamref name="T" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="target" /> is not long enough to store all elements. -or-
        ///     <paramref name="source" /> does not provide enough elements.
        /// </exception>
        /// <exception cref="InvalidCastException">
        ///     At least one element in <paramref name="source" /> cannot be cast to
        ///     <typeparamref name="T" />.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />. -or-
        ///     <paramref name="target" /> is <see langword="null" />.
        /// </exception>
        public static void Copy<T>(Array source, int sourceOffset, Array<T> target, int offset, int count)
        {
            Array.Copy(source, sourceOffset, target.m_Array, offset, count);
        }


        /// <summary>Alias for <see cref="Array.Copy(Array, int, Array, int, int)" />.</summary>
        /// <param name="source">The source to copy from.</param>
        /// <param name="sourceOffset">How many elements should be skipped before copying?</param>
        /// <param name="target">The target to copy to.</param>
        /// <param name="offset">Which index is the first index elements should be copied to?</param>
        /// <param name="count">How many elements should be copied?</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="sourceOffset" /> is smaller than 0. -or-
        ///     <paramref name="offset" /> is smaller than 0. -or- <paramref name="count" /> is smaller than 0.
        /// </exception>
        /// <exception cref="RankException"><paramref name="target" /> and <paramref name="source" /> have different ranks.</exception>
        /// <exception cref="ArrayTypeMismatchException">
        ///     The element type of <paramref name="target" /> is not assignable from
        ///     <typeparamref name="T" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="target" /> is not long enough to store all elements. -or-
        ///     <paramref name="source" /> does not provide enough elements.
        /// </exception>
        /// <exception cref="InvalidCastException">
        ///     At least one element in <paramref name="source" /> cannot be cast to
        ///     <typeparamref name="T" />.
        /// </exception>
        public static void Copy<T>(Array source, int sourceOffset, Array target, int offset, int count)
        {
            Array.Copy(source, sourceOffset, target, offset, count);
        }
    }
}