using System;
using System.Runtime.Serialization;
using System.Text;

namespace SolScript.Interpreter.Exceptions
{
    /// <summary>
    ///     This is the base exception for all exceptions in SolScript. The type is abstract, thus relying on custom more
    ///     specific exception implementations.
    /// </summary>
    [Serializable]
    public abstract class SolException : Exception
    {
        /// <summary>
        ///     Creates an empty exception.
        /// </summary>
        protected SolException() {}

        /// <summary>
        ///     Creates an exception with the given message.
        /// </summary>
        /// <param name="message">The error message.</param>
        protected SolException(string message) : base(message) {}

        /// <summary>
        ///     Creates an exception with the given message and a wrapped inner exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="inner">The inner exception.</param>
        protected SolException(string message, Exception inner) : base(message, inner) {}

        /// <summary>
        ///     Deserializes an exception.
        /// </summary>
        /// <param name="info">The serialized exception.</param>
        /// <param name="context">The current streaming context.</param>
        /// <exception cref="SerializationException">The class name is null or <see cref="P:System.Exception.HResult" /> is zero (0). </exception>
        /// <exception cref="ArgumentNullException">The <paramref name="info" /> parameter is null. </exception>
        protected SolException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {}

        /// <summary>
        ///     This method writes the exception message of a SolException to a StringBuilder. Always make sure to use this method
        ///     instead of simply using the <see cref="Exception.Message" /> property as SolScript makes very heavy use of
        ///     Exception nesting.
        /// </summary>
        /// <param name="exception">The exception to write.</param>
        /// <param name="target">The builder to write the output to.</param>
        public static void UnwindExceptionStack(SolException exception, StringBuilder target)
        {
            Exception ex = exception;
            while (ex != null) {
                if (!ex.GetType().IsSubclassOf(typeof(SolException))) {
                    target.Append("[Native Exception: \"" + ex.GetType().Name + "\"]");
                }
                target.AppendLine(ex.Message);
                ex = ex.InnerException;
            }
        }
    }
}