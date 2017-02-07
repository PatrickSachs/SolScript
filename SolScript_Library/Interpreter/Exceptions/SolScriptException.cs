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
    public abstract class SolScriptException : Exception
    {
        protected SolScriptException() {}

        protected SolScriptException(string message) : base(message) {}

        protected SolScriptException(string message, Exception inner) : base(message, inner) {}

        protected SolScriptException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {}

        /// <summary>
        ///     This method writes the exception message of a SolException to a StringBuilder. Always make sure to use this method
        ///     instead of simply using the <see cref="Exception.Message" /> property as SolScript makes very heavy use of
        ///     Exception nesting.
        /// </summary>
        /// <param name="exception">The exception to write.</param>
        /// <param name="target">The builder to write the output to.</param>
        public static void UnwindExceptionStack(SolScriptException exception, StringBuilder target)
        {
            Exception ex = exception;
            while (ex != null) {
                if (!ex.GetType().IsSubclassOf(typeof(SolScriptException))) {
                    target.Append("[Native Exception: \"" + ex.GetType().Name + "\"]");
                }
                target.AppendLine(ex.Message);
                ex = ex.InnerException;
            }
        }
    }
}