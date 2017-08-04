// ---------------------------------------------------------------------
// SolScript - A simple but powerful scripting language.
// Official repository: https://bitbucket.org/PatrickSachs/solscript/
// ---------------------------------------------------------------------
// Copyright 2017 Patrick Sachs
// Permission is hereby granted, free of charge, to any person obtaining 
// a copy of this software and associated documentation files (the 
// "Software"), to deal in the Software without restriction, including 
// without limitation the rights to use, copy, modify, merge, publish, 
// distribute, sublicense, and/or sell copies of the Software, and to 
// permit persons to whom the Software is furnished to do so, subject to 
// the following conditions:
// 
// The above copyright notice and this permission notice shall be 
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN 
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.
// ---------------------------------------------------------------------
// ReSharper disable ArgumentsStyleStringLiteral

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using JetBrains.Annotations;
using NodeParser;
using PSUtility.Enumerables;
using SolScript.Compiler.Native;
using SolScript.Interpreter;

namespace SolScript.Exceptions
{
    /// <summary>
    ///     This is the base exception for all exceptions in SolScript. The type is abstract, thus relying on custom more
    ///     specific exception implementations.
    /// </summary>
    [Serializable]
    public abstract class SolException : Exception, ISourceLocateable
    {
        /// <summary>
        ///     Creates an empty exception.
        /// </summary>
        protected SolException(NodeLocation location) : this(location, UNSPECIFIED_ERROR) {}

        /// <summary>
        ///     Creates an exception with the given message.
        /// </summary>
        /// <param name="location">The location in code this exception relates to.</param>
        /// <param name="message">The error message.</param>
        protected SolException(NodeLocation location, string message) : base(location + " : " + message)
        {
            Location = location;
            RawMessage = message;
        }

        /// <summary>
        ///     Creates an exception with the given message and a wrapped inner exception.
        /// </summary>
        /// <param name="location">The location in code this exception relates to.</param>
        /// <param name="message">The error message.</param>
        /// <param name="inner">The inner exception.</param>
        protected SolException(NodeLocation location, string message, Exception inner) : base(location + " : " + message, inner)
        {
            Location = location;
            RawMessage = message;
        }

        /// <summary>
        ///     Deserializes an exception.
        /// </summary>
        /// <param name="info">The serialized exception.</param>
        /// <param name="context">The current streaming context.</param>
        /// <exception cref="SerializationException">
        ///     The class name is null or <see cref="P:System.Exception.HResult" /> is zero
        ///     (0).
        /// </exception>
        /// <exception cref="ArgumentNullException">The <paramref name="info" /> parameter is null. </exception>
        /// <exception cref="InvalidCastException">A value cannot be converted to a required type. </exception>
        protected SolException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
            int position = info.GetInt32(SER_POSITION);
            int line = info.GetInt32(SER_LINE);
            int column = info.GetInt32(SER_COLUMN);
            string file = info.GetString(SER_FILE);
            RawMessage = info.GetString(SER_RAW);
            Location = new NodeLocation(line, column, position, file);
        }

        /// <summary>
        ///     The name of the <see cref="Exception.Data" /> key containing the Stack Trace in some of the exceptions thrown by
        ///     the <see cref="NativeCompiler.MethodBody{T}" /> and <see cref="NativeCompiler.MethodBodyNoConvert" />.
        /// </summary>
        public const string EXCEPTION_STACK_TRACE = "SolStackTrace";

        /// <summary>
        ///     The ID of the context the given stack trace are related to. Context IDs are an internal SolScript feature and can
        ///     be ignored by you. Simple set this value to 0 when manually injecitng stack traces.
        /// </summary>
        /// <remarks>
        ///     For the curious ones: When resolving the exception stack we we go through the exception hierarchy and only add
        ///     a stack trace to list list of stack traces that will be printed if its context ID was different to the one before
        ///     it. This saves us from printing the same stack trace(each time with one method less) many times in deeply nested
        ///     function calls.<br />
        ///     It also makes debugging easier where watches are impractical.
        /// </remarks>
        public const string EXCEPTION_SOURCE_ID = "SolSourceContextId";

        private const string UNSPECIFIED_ERROR = "An unspecified error occured.";
        private const string SER_RAW = "SolException.RawMessage";
        private const string SER_FILE = "SolInterpreterException.Location.File";
        private const string SER_COLUMN = "SolException.Location.Column";
        private const string SER_LINE = "SolException.Location.Line";
        private const string SER_POSITION = "SolException.Location.Position";

        /// <summary>
        ///     The message without any location details.
        /// </summary>
        public string RawMessage { get; }

        #region ISourceLocateable Members

        /// <inheritdoc />
        public NodeLocation Location { get; }

        #endregion

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SerializationException">A value has already been associated with a name. </exception>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(SER_POSITION, Location.FileIndex);
            info.AddValue(SER_FILE, Location.File);
            info.AddValue(SER_LINE, Location.LineIndex);
            info.AddValue(SER_COLUMN, Location.ColumnIndex);
            info.AddValue(SER_RAW, RawMessage);
        }

        #endregion

        /// <summary>
        ///     Injects a SolScript stack trace into another exception. This stack trace will then be printed once unwinding the
        ///     stack trace.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="stackTrace">The stack trace.</param>
        /// <param name="id">The context id.</param>
        /// <seealso cref="EXCEPTION_SOURCE_ID" />
        /// <exception cref="ArgumentNullException">An argument is <see langword="null" /></exception>
        public static void InjectSolStackTrace([NotNull] Exception exception, [NotNull] string stackTrace, uint id = 0)
        {
            if (exception == null) {
                throw new ArgumentNullException(nameof(exception));
            }
            if (stackTrace == null) {
                throw new ArgumentNullException(nameof(stackTrace));
            }
            exception.Data.Add(EXCEPTION_STACK_TRACE, stackTrace);
            exception.Data.Add(EXCEPTION_SOURCE_ID, id);
        }

        /// <summary>
        ///     Extracts an inject stack trace from an exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="stackTrace">The extraced stack trace.</param>
        /// <param name="id">The extraced context id.</param>
        /// <returns>true if a stack trace was injected, otherwise false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception" /> is <see langword="null" /></exception>
        public static bool ExtractSolStackTrace([NotNull] Exception exception, out string stackTrace, out uint id)
        {
            if (exception == null) {
                throw new ArgumentNullException(nameof(exception));
            }
            SolRuntimeException runtimeException = exception as SolRuntimeException;
            if (runtimeException != null) {
                stackTrace = runtimeException.SolStackTrace;
                id = runtimeException.SourceContext;
                return true;
            }
            if (!exception.Data.Contains(EXCEPTION_STACK_TRACE)) {
                stackTrace = null;
                id = 0;
                return false;
            }
            stackTrace = (string) exception.Data[EXCEPTION_STACK_TRACE];
            if (!exception.Data.Contains(EXCEPTION_SOURCE_ID)) {
                id = 0;
                return true;
            }
            id = (uint) exception.Data[EXCEPTION_SOURCE_ID];
            return true;
        }

        /// <summary>
        ///     This method writes the exception message of a SolException to a StringBuilder. Always make sure to use this method
        ///     instead of simply using the <see cref="Exception.Message" /> property as SolScript makes very heavy use of
        ///     Exception nesting.
        /// </summary>
        /// <param name="exception">The exception to write.</param>
        /// <param name="target">The builder to write the output to.</param>
        public static void UnwindExceptionStack(Exception exception, StringBuilder target)
        {
            var stackTraces = new PSList<string>();
            uint lastCtx = 0;
            Exception topNativeEx = null;
            Exception ex = exception;
            while (ex != null) {
                /*SolRuntimeException currentRunEx = ex as SolRuntimeException;
                if (currentRunEx != null && (lastCtx == 0 || lastCtx != currentRunEx.SourceContext)) {
                    stackTraces.Add(currentRunEx.SolStackTrace);
                    lastCtx = currentRunEx.SourceContext;
                } else {
                    string trace;
                    uint id;
                    if (ExtractSolStackTrace(ex, out trace, out id) && (id == 0 || lastCtx == 0 || id != lastCtx)) {
                        stackTraces.Add(trace);
                        lastCtx = id;
                    }
                    if (topNativeEx == null && !(ex is SolException)) {
                        topNativeEx = ex;
                    }
                }*/
                string trace;
                uint id;
                if (ExtractSolStackTrace(ex, out trace, out id) && (id == 0 || lastCtx == 0 || id != lastCtx)) {
                    stackTraces.Add(trace);
                    lastCtx = id;
                }
                if (topNativeEx == null && !(ex is SolException)) {
                    topNativeEx = ex;
                }
                target.AppendLine(ex.Message);
                ex = ex.InnerException;
            }
            string sep = new string('-', 25);
            if (stackTraces.Count != 0) {
                target.AppendLine(sep);
                int i = 0;
                foreach (string stackTrace in ((IEnumerable<string>) stackTraces).Reverse()) {
                    target.AppendLine(i == 0 ? "Stack Trace:" : "Transitioned from:");
                    target.AppendLine(stackTrace);
                    target.AppendLine(sep);
                    i++;
                }
            }
            if (topNativeEx != null) {
                if (stackTraces.Count == 0) {
                    target.AppendLine(sep);
                }
                target.AppendLine("Caused by native exception " + topNativeEx.GetType().Name + ":");
                target.AppendLine(topNativeEx.StackTrace);
                target.AppendFormat(sep);
            }
        }
    }
}