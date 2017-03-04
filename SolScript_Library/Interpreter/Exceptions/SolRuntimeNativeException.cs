using System;
using System.Reflection;
using System.Runtime.Serialization;
using SolScript.Utility;

namespace SolScript.Interpreter.Exceptions
{
    /// <summary>
    ///     Use this exception if you wish to throw an exception in a native library method/property but cannot get a handle on
    ///     to a <see cref="SolExecutionContext" />. This exception will be catched by the runtime and then converted into a
    ///     normal <see cref="SolRuntimeException" />.
    /// </summary>
    /// <remarks>
    ///     This exception will be catched and rethrown as <see cref="SolRuntimeException" /> by
    ///     <see cref="InternalHelper.SandboxInvokeMethod(SolExecutionContext,MethodBase,object,object[])" />.
    /// </remarks>
    [Serializable]
    public class SolRuntimeNativeException : SolException
    {
        /// <summary>
        ///     Creates a new <see cref="SolRuntimeNativeException" />.
        /// </summary>
        protected SolRuntimeNativeException() {}

        /// <summary>
        ///     Creates a new <see cref="SolRuntimeNativeException" /> with an exception <paramref name="message" />.
        /// </summary>
        /// <param name="message">The message.</param>
        public SolRuntimeNativeException(string message) : base(message) {}

        /// <summary>
        ///     Deserializes a <see cref="SolRuntimeNativeException" />.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        /// <exception cref="SerializationException">
        ///     The class name is null or <see cref="P:System.Exception.HResult" /> is zero
        ///     (0).
        /// </exception>
        /// <exception cref="ArgumentNullException">The <paramref name="info" /> parameter is null. </exception>
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