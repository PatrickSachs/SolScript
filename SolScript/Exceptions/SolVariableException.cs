using System;
using System.Runtime.Serialization;
using Irony.Parsing;
using NodeParser;

namespace SolScript.Exceptions
{
    /// <summary>
    ///     The SolVariableException is used whenever an error while resolving a variable occured.
    /// </summary>
    [Serializable]
    public class SolVariableException : SolException
    {
        /// <inheritdoc />
        public SolVariableException(NodeLocation location, string message) : base(location, message) {}

        /// <inheritdoc />
        public SolVariableException(NodeLocation location, string message, Exception inner) : base(location, message, inner) {}

        /// <inheritdoc />
        /// <exception cref="SerializationException">
        ///     The class name is null or <see cref="P:System.Exception.HResult" /> is zero
        ///     (0).
        /// </exception>
        /// <exception cref="InvalidCastException">A value cannot be converted to a required type. </exception>
        protected SolVariableException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {}
    }
}