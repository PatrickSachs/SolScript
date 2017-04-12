using System.Collections.Generic;
using JetBrains.Annotations;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     This list type extends the <see cref="System.Collections.Generic.List{T}" /> and implements
    ///     <see cref="IReadOnlyList{T}" />.
    /// </summary>
    /// <typeparam name="T">The list type.</typeparam>
    public class List<T> : System.Collections.Generic.List<T>, IReadOnlyList<T>
    {
        /// <inheritdoc />
        public List() {}

        /// <inheritdoc />
        public List(int capacity) : base(capacity) {}

        /// <inheritdoc />
        public List([NotNull] IEnumerable<T> collection) : base(collection) {}
    }
}