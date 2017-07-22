﻿using System;
using System.Runtime.Serialization;
using System.Text;
using NodeParser;
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
        ///     This method writes the exception message of a SolException to a StringBuilder. Always make sure to use this method
        ///     instead of simply using the <see cref="Exception.Message" /> property as SolScript makes very heavy use of
        ///     Exception nesting.
        /// </summary>
        /// <param name="exception">The exception to write.</param>
        /// <param name="target">The builder to write the output to.</param>
        /// <param name="includeStackTrace">Should the stack trace be included?</param>
        public static void UnwindExceptionStack(Exception exception, StringBuilder target, bool includeStackTrace = true)
        {
            SolRuntimeException topRuntimeEx = null;
            Exception topNativeEx = null;
            Exception ex = exception;
            while (ex != null) {
                if (topRuntimeEx == null && ex is SolRuntimeException) {
                    topRuntimeEx = (SolRuntimeException) ex;
                } else if (topNativeEx == null && !(ex is SolException)) {
                    topNativeEx = ex;
                }
                target.AppendLine(ex.Message);
                ex = ex.InnerException;
            }
            string sep = new string('-', 25);
            if (topRuntimeEx != null) {
                target.AppendLine(sep);
                target.AppendLine("Stack Trace:");
                target.AppendLine(topRuntimeEx.SolStackTrace);
                target.AppendLine(sep);
            }
            if (topNativeEx != null) {
                if (topRuntimeEx == null) {
                    target.AppendLine(sep);
                }
                target.AppendLine("Caused by native exception " + topNativeEx.GetType().Name + ":");
                target.AppendLine(topNativeEx.StackTrace);
                target.AppendFormat(sep);
            }
        }
    }
}