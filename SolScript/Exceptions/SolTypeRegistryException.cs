using System;
using System.Runtime.Serialization;
using Irony.Parsing;

namespace SolScript.Exceptions
{
    /// <summary>
    ///     This exception is used to indicate an error inside the type registry.
    /// </summary>
    [Serializable]
    public class SolTypeRegistryException : SolException
    {
        /// <inheritdoc />
        public SolTypeRegistryException(SourceLocation location, string message) : base(location, message) {}

        /// <inheritdoc />
        /// <exception cref="SerializationException">
        ///     The class name is null or <see cref="P:System.Exception.HResult" /> is zero
        ///     (0).
        /// </exception>
        /// <exception cref="ArgumentNullException">The <paramref name="info" /> parameter is null. </exception>
        /// <exception cref="InvalidCastException">A value cannot be converted to a required type. </exception>
        protected SolTypeRegistryException(SerializationInfo info, StreamingContext context) : base(info, context) {}

        /// <inheritdoc />
        public SolTypeRegistryException(SourceLocation location, string message, Exception inner) : base(location, message, inner) {}
    }
}