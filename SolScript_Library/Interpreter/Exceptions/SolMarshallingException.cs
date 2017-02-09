using System;
using System.Runtime.Serialization;

namespace SolScript.Interpreter.Exceptions {
    [Serializable]
    public class SolMarshallingException : SolScriptException {
        public SolMarshallingException() {
        }

        public SolMarshallingException(string message) : base(message) {
        }

        public SolMarshallingException(string message, Exception inner) : base(message, inner) {
        }

        public SolMarshallingException(string from, Type to, string message = "Cannot convert types!")
            : base(from + "->" + to.Name + ": " + message)
        {
        }
        public SolMarshallingException(string from, Type to, string message, Exception inner)
            : base(from + "->" + to.Name + ": " + message, inner)
        {
        }

        public SolMarshallingException(Type from, string to, string message = "Cannot convert types!")
            : base(from.Name + "->" + to + ": " + message) {
        }

        public SolMarshallingException(Type type, string message = "Cannot find matching SolType!")
            : base(type.Name + ": " + message) {
        }

        protected SolMarshallingException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {
        }
    }
}