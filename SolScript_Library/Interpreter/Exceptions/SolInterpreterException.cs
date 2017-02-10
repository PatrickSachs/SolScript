using System;
using System.Runtime.Serialization;
using Irony.Parsing;

namespace SolScript.Interpreter.Exceptions
{
    /// <summary>
    ///     This exception signals an exception during the interpretation phase of a script.
    /// </summary>
    [Serializable]
    public class SolInterpreterException : SolScriptException
    {
        protected SolInterpreterException()
        {
            Location = SolSourceLocation.Empty();
            RawMessage = string.Empty;
        }

        public SolInterpreterException(SolSourceLocation location, string message) : base(location + " : " + message)
        {
            Location = location;
            RawMessage = message;
        }

        public SolInterpreterException(SolSourceLocation location, string message, Exception inner) : base(location + " : " + message, inner)
        {
            Location = location;
            RawMessage = message;
        }

        protected SolInterpreterException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            int position = info.GetInt32(SER_POSITION);
            int line = info.GetInt32(SER_LINE);
            int column = info.GetInt32(SER_COLUMN);
            string file = info.GetString(SER_FILE);
            RawMessage = info.GetString(SER_RAW);
            Location = new SolSourceLocation(file, position, line, column);
        }

        private const string SER_RAW = "SolInterpreterException_RawMessage";
        private const string SER_FILE = "SolInterpreterException_Location_FILE";
        private const string SER_COLUMN = "SolInterpreterException_Location_Column";
        private const string SER_LINE = "SolInterpreterException_Location_Line";
        private const string SER_POSITION = "SolInterpreterException_Location_Position";

        /// <summary>
        ///     The source location the exception occured in.
        /// </summary>
        public SolSourceLocation Location { get; }

        /// <summary>
        ///     The raw exception message without the source location.
        /// </summary>
        public string RawMessage { get; }

        #region Overrides

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
    }
}