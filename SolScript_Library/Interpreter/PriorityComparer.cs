using System.Collections.Generic;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     This <see cref="IComparer{T}" /> is used to compare two objects implementing the <see cref="IPriority" />
    ///     interface. Objects with a higher priority are considered to be greater than ones with a lower priority. Objects
    ///     with the same priority are considered to be equally great.
    /// </summary>
    public class PriorityComparer : IComparer<IPriority>
    {
        private PriorityComparer() {}

        /// <summary>
        ///     The singleton instance of this comparer. There is no need to instantiate a new instance since the class itself has
        ///     no persisting state.
        /// </summary>
        public static readonly PriorityComparer Instance = new PriorityComparer();

        #region IComparer<IPriority> Members

        /// <inheritdoc />
        public int Compare(IPriority priority1, IPriority priority2)
        {
            if (priority1.Priority > priority2.Priority) {
                return -1;
            }
            if (priority1.Priority < priority2.Priority) {
                return 1;
            }
            return 0;
        }

        #endregion
    }
}