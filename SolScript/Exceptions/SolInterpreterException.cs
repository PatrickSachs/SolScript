using System;
using System.Runtime.Serialization;
using Irony.Parsing;
using NodeParser;

namespace SolScript.Exceptions
{
    /// <summary>
    ///     This exception signals an exception during the interpretation phase of a script.
    /// </summary>
    [Serializable]
    public class SolInterpreterException : SolException
    {
        /// <inheritdoc />
        protected SolInterpreterException(NodeLocation location) : base(location) {}

        /// <inheritdoc />
        public SolInterpreterException(NodeLocation location, string message) : base(location, message) {}

        /// <inheritdoc />
        public SolInterpreterException(NodeLocation location, string message, Exception inner) : base(location, message, inner) {}

        /// <inheritdoc />
        /// <exception cref="SerializationException">
        ///     The class name is null or <see cref="P:System.Exception.HResult" /> is zero
        ///     (0).
        /// </exception>
        /// <exception cref="InvalidCastException">A value cannot be converted to a required type. </exception>
        protected SolInterpreterException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}