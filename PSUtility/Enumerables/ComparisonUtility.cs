using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     Utility methods related to comparers and comparisons.
    /// </summary>
    [PublicAPI]
    public static class ComparisonUtility
    {
        /// <summary>
        ///     Creates a comparer for the given comparison delegate.
        /// </summary>
        /// <typeparam name="T">The comparison type.</typeparam>
        /// <param name="comparison">The comparison delegate.</param>
        /// <returns>The comparer.</returns>
        public static IComparer<T> ToComparer<T>(Comparison<T> comparison)
        {
            return new ComparisonComparer<T>(comparison);
        }

        private class ComparisonComparer<T> : IComparer<T>
        {
            private readonly Comparison<T> m_Comparison;

            public ComparisonComparer(Comparison<T> comparison)
            {
                m_Comparison = comparison;
            }

            /// <inheritdoc />
            public int Compare(T x, T y)
            {
                return m_Comparison(x, y);
            }
        }
    }
}