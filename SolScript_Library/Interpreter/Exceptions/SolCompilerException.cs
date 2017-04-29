using System;
using System.Runtime.Serialization;
using Irony.Parsing;

namespace SolScript.Interpreter.Exceptions
{
    /// <summary>
    ///     The <see cref="SolCompilerException" /> is used to indicate that something went wrong while compiling SolScript.
    /// </summary>
    public class SolCompilerException : SolException
    {
        /// <summary>
        ///     Creates a new compiler exception.
        /// </summary>
        /// <param name="location">The code location this exception relates to.</param>
        /// <param name="message">The error message.</param>
        public SolCompilerException(SourceLocation location, string message) : base(location, message) {}

        /// <summary>
        ///     Deserializes a compiler exception.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The current streaming context.</param>
        /// <exception cref="SerializationException">
        ///     The class name is null or <see cref="P:System.Exception.HResult" /> is zero
        ///     (0).
        /// </exception>
        /// <exception cref="ArgumentNullException">The <paramref name="info" /> parameter is null. </exception>
        protected SolCompilerException(SerializationInfo info, StreamingContext context) : base(info, context) {}

        /// <summary>
        ///     Creates a new compiler exception.
        /// </summary>
        /// <param name="location">The code location this exception relates to.</param>
        /// <param name="message">The error message.</param>
        /// <param name="inner">The inner wrapped exception.</param>
        public SolCompilerException(SourceLocation location, string message, Exception inner) : base(location, message, inner) {}
    }
}