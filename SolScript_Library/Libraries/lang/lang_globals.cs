using System;
using System.Text;
using JetBrains.Annotations;
using SolScript.Interpreter;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;
using SolScript.Utility;

// ReSharper disable InconsistentNaming

namespace SolScript.Libraries.lang
{
    /// <summary>
    ///     All global functions and fields in the <see cref="lang" /> library.
    /// </summary>
    [PublicAPI]
    [SolGlobal(lang.NAME)]
    public static class lang_Globals
    {
        private const string DEFAULT_ERROR_MESSAGE = "An error occured.";
        private const string DEFAULT_ASSERTION_FAILED_MESSAGE = "Assertion failed!";

        /// <summary>
        ///     Raises an exceution error with the given message.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message">The error message. (Default: "An error occured.")</param>
        /// <exception cref="SolRuntimeException">
        ///     Throws a <see cref="SolRuntimeException" /> with the value of
        ///     <paramref name="message" /> as <see cref="Exception.Message" />.
        /// </exception>
        [SolGlobal(lang.NAME)]
        public static void error(SolExecutionContext context, [SolContract(SolString.TYPE, true)] [CanBeNull] SolString message)
        {
            string messageStr = message?.Value ?? DEFAULT_ERROR_MESSAGE;
            throw new SolRuntimeException(context, messageStr);
        }

        /// <summary>
        ///     Asserts that a certain expression is not false.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value">The value to assert.</param>
        /// <param name="message">The error message if the assertion fails. (Default: "Assertion failed!")</param>
        /// <remarks>
        ///     Be wary that this message asserts that an expression is NOT FALSE. <br />
        ///     A few examples:
        ///     <br />
        ///     <c>
        ///         assert(false) -> ERROR!
        ///     </c>
        ///     <br />
        ///     <c>
        ///         assert(nil) -> ERROR!
        ///     </c>
        ///     <br />
        ///     <c>
        ///         assert(true) -> OK!
        ///     </c>
        ///     <br />
        ///     <c>
        ///         assert(0) -> OK!
        ///     </c>
        ///     <br />
        ///     <c>
        ///         assert({}) -> OK!
        ///     </c>
        ///     <br />
        ///     <c>
        ///         assert("") -> OK!
        ///     </c>
        /// </remarks>
        /// <exception cref="SolRuntimeException">The assertion failed.</exception>
        [SolGlobal(lang.NAME)]
        public static void assert(SolExecutionContext context, SolValue value, [SolContract(SolString.TYPE, true)] [CanBeNull] SolString message)
        {
            if (value.IsFalse(context)) {
                throw new SolRuntimeException(context, message?.Value ?? DEFAULT_ASSERTION_FAILED_MESSAGE);
            }
        }

        /// <summary>
        ///     Prints values to the standard output.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="values">The values to print.</param>
        [SolGlobal(lang.NAME)]
        public static void print(SolExecutionContext context, params SolValue[] values)
        {
            // todo: unify std out with the one of IO - stdout and stdin properties in solassembly?
            StringBuilder builder = new StringBuilder();
            SolStackFrame frame;
            // We are peeking at a depth of one since zero would be this
            // method. And the user doesn't need to know that the print
            // method is the print method.
            if (context.PeekStackFrame(out frame, 1)) {
                frame.AppendFunctionName(builder);
                builder.Append(' ');
            }
            builder.Append("[");
            builder.Append(context.CurrentLocation);
            builder.Append("] : ");
            builder.Append(InternalHelper.JoinToString(",", values, value => value.ToString(context)));
            Console.WriteLine(builder.ToString());
        }

        /// <summary>
        ///     Gets the type of the given <paramref name="value" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The type.</returns>
        [SolGlobal(lang.NAME)]
        [SolContract(SolString.TYPE, false)]
        public static SolString type([SolContract(SolValue.ANY_TYPE, true)] SolValue value)
        {
            return SolString.ValueOf(value.Type);
        }

        /// <summary>
        ///     Converts a value to its string representation.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The string representation of the given <paramref name="value" />.</returns>
        [SolGlobal(lang.NAME)]
        [SolContract(SolString.TYPE, false)]
        public static SolString to_string(SolExecutionContext context, [SolContract(SolValue.ANY_TYPE, true)] SolValue value)
        {
            return value.ToString(context);
        }
    }
}