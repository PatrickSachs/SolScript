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
    public abstract class SolException : Exception, ISourceLocateable
    {
        /// <summary>
        ///     Creates an empty exception.
        /// </summary>
        protected SolException(SolSourceLocation location) : this(location, UNSPECIFIED_ERROR) {}

        /// <summary>
        ///     Creates an exception with the given message.
        /// </summary>
        /// <param name="location">The location in code this exception relates to.</param>
        /// <param name="message">The error message.</param>
        protected SolException(SolSourceLocation location, string message) : base(location + " : " + message)
        {
            Location = location;
        }

        /// <summary>
        ///     Creates an exception with the given message and a wrapped inner exception.
        /// </summary>
        /// <param name="location">The location in code this exception relates to.</param>
        /// <param name="message">The error message.</param>
        /// <param name="inner">The inner exception.</param>
        protected SolException(SolSourceLocation location, string message, Exception inner) : base(location + " : " + message, inner)
        {
            Location = location;
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
            Location = new SolSourceLocation(file, position, line, column);
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
        public SolSourceLocation Location { get; }

        #endregion

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SerializationException">A value has already been associated with a name. </exception>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(SER_POSITION, Location.Position);
            info.AddValue(SER_FILE, Location.File);
            info.AddValue(SER_LINE, Location.Line);
            info.AddValue(SER_COLUMN, Location.Column);
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