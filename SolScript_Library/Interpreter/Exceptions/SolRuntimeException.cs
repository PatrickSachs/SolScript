using System;
using System.Runtime.Serialization;

namespace SolScript.Interpreter.Exceptions
{
    /// <summary>
    ///     The SolRuntimeException is used to represent an exception that occured during the actual run of an assembly.
    /// </summary>
    [Serializable]
    public class SolRuntimeException : SolException
    {
        /// <summary>
        ///     Creates a new runtime exception.
        /// </summary>
        /// <param name="context">The context. - Required for generating the stack trace.</param>
        /// <param name="message">The exception message.</param>
        public SolRuntimeException(SolExecutionContext context, string message) : base(context.CurrentLocation, $"{message}\nStack Trace:\n{context.GenerateStackTrace()}")
        {
            RawNoStackTrace = message;
        }

        /// <inheritdoc />
        /// <exception cref="SerializationException">
        ///     The class name is null or <see cref="P:System.Exception.HResult" /> is zero
        ///     (0).
        /// </exception>
        /// <exception cref="InvalidCastException">A value cannot be converted to a required type. </exception>
        protected SolRuntimeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            RawNoStackTrace = info.GetString(NO_STACK);
        }

        /// <summary>
        ///     Creates a new runtime exception and wraps an exception.
        /// </summary>
        /// <param name="context">The context. - Required for generating the stack trace.</param>
        /// <param name="message">The exception message.</param>
        /// <param name="inner">The wrapped exception.</param>
        public SolRuntimeException(SolExecutionContext context, string message, Exception inner) : base(
            context.CurrentLocation, $"{message}\nStack Trace:\n{context.GenerateStackTrace(inner)}", inner) {}

        private const string NO_STACK = "SolRuntimeException.RawNoStackTrace";

        /// <summary>
        ///     The exception message without stack trace or file location.
        /// </summary>
        public string RawNoStackTrace { get; }

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SerializationException">A value has already been associated with a name. </exception>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(NO_STACK, RawNoStackTrace);
        }

        #endregion

        internal static SolRuntimeException InvalidFunctionCallParameters(SolExecutionContext context, Exception inner)
        {
            // todo: streamline exception creation.
            return new SolRuntimeException(context, "Invalid function call parameters.", inner);
        }
    }
} 