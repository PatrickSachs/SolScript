using System;
using System.Runtime.Serialization;

namespace SolScript.Interpreter.Exceptions
{
    /// <summary>
    ///     The <see cref="SolMarshallingException" /> indicates an error while concerting values or types.
    /// </summary>
    /// <remarks>Marshalling exceptions are always at native code location.</remarks>
    [Serializable]
    public class SolMarshallingException : SolException
    {
        /// <summary>
        ///     Creates a new marshalling exception with the given message.
        /// </summary>
        /// <param name="message">The message.</param>
        public SolMarshallingException(string message) : base(SolSourceLocation.Native(), message) {}

        /// <summary>
        ///     Creates a new marshalling exception with the given message and a wrapped exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The wrapped exception.</param>
        public SolMarshallingException(string message, Exception inner) : base(SolSourceLocation.Native(), message, inner) {}

        /// <summary>
        ///     Creates a marshalling exception indicating that a SolScript type could not be converted to a native type.
        /// </summary>
        /// <param name="from">The SolScript type.</param>
        /// <param name="to">The native type.</param>
        /// <param name="message">A optional additional message.</param>
        public SolMarshallingException(string from, Type to, string message = "Cannot convert types!")
            : base(SolSourceLocation.Native(), from + "->" + to.Name + ": " + message) {}

        /// <summary>
        ///     Creates a marshalling exception indicating that a SolScript type could not be converted to a native type. Also
        ///     wraps an exception.
        /// </summary>
        /// <param name="from">The SolScript type.</param>
        /// <param name="to">The native type.</param>
        /// <param name="message">A optional additional message.</param>
        /// <param name="inner">The wrapped exception.</param>
        public SolMarshallingException(string from, Type to, string message, Exception inner)
            : base(SolSourceLocation.Native(), from + "->" + to.Name + ": " + message, inner) {}


        /// <summary>
        ///     Creates a marshalling exception indicating that a native type could not be converted to a SolScript type.
        /// </summary>
        /// <param name="to">The SolScript type.</param>
        /// <param name="from">The native type.</param>
        /// <param name="message">A optional additional message.</param>
        public SolMarshallingException(Type from, string to, string message = "Cannot convert types!")
            : base(SolSourceLocation.Native(), from.Name + "->" + to + ": " + message) {}

        /// <summary>
        ///     Creates a marshalling exception indicating that a native type could not be converted to a SolScript type. Also
        ///     wraps an exception.
        /// </summary>
        /// <param name="to">The SolScript type.</param>
        /// <param name="from">The native type.</param>
        /// <param name="message">A optional additional message.</param>
        /// <param name="inner">The wrapped exception.</param>
        public SolMarshallingException(Type from, string to, string message, Exception inner)
            : base(SolSourceLocation.Native(), from.Name + "->" + to + ": " + message, inner) {}

        /// <summary>
        ///     Creates a marshalling exception indicating that no SolScript type could be found to represent a native type.
        /// </summary>
        /// <param name="type">The native type.</param>
        /// <param name="message">A optional additional message.</param>
        public SolMarshallingException(Type type, string message = "Cannot find matching SolScript type!")
            : base(SolSourceLocation.Native(), type.Name + ": " + message) {}

        /// <summary>
        ///     Creates a marshalling exception indicating that no native type could be found to represent a SolScript type.
        /// </summary>
        /// <param name="type">The SolScript type.</param>
        /// <param name="message">A optional additional message.</param>
        public SolMarshallingException(string type, string message = "Cannot find matching native type!")
            : base(SolSourceLocation.Native(), type + ": " + message) {}

        /// <inheritdoc />
        /// <exception cref="SerializationException">
        ///     The class name is null or <see cref="P:System.Exception.HResult" /> is zero
        ///     (0).
        /// </exception>
        /// <exception cref="InvalidCastException">A value cannot be converted to a required type. </exception>
        protected SolMarshallingException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {}
    }
}