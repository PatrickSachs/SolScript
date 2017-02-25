using System;
using System.Runtime.Serialization;

namespace SolScript.Interpreter.Exceptions
{
    /// <summary>
    ///     Use this exception if you wish to throw an exception in a native library method/property but cannot get a handle on
    ///     to a <see cref="SolExecutionContext" />. This exception will be catched by the runtime and then converted into a
    ///     normal <see cref="SolRuntimeException" />.
    /// </summary>
    [Serializable]
    public class SolRuntimeNativeException : SolException
    {
        // todo: fully implement catching this one.
        protected SolRuntimeNativeException() {}

        /// <summary>
        ///     Creates a new <see cref="SolRuntimeNativeException" /> with an exception <paramref name="message" />.
        /// </summary>
        /// <param name="message">The message.</param>
        public SolRuntimeNativeException(string message) : base(message) {}

        protected SolRuntimeNativeException(SerializationInfo info, StreamingContext context) : base(info, context) {}

        /// <summary>
        ///     Creates a new <see cref="SolRuntimeNativeException" /> with an exception <paramref name="message" /> and an
        ///     <paramref name="inner" /> exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The wrapped exception.</param>
        public SolRuntimeNativeException(string message, Exception inner) : base(message, inner) {}
    }
}