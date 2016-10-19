using System;
using System.Runtime.Serialization;

namespace SolScript.Interpreter.Exceptions {
    [Serializable]
    public class SolScriptMarshallingException : Exception {
        public SolScriptMarshallingException() {
        }

        public SolScriptMarshallingException(string message) : base(message) {
        }

        public SolScriptMarshallingException(string message, Exception inner) : base(message, inner) {
        }

        public SolScriptMarshallingException(string from, Type to, string message = "Cannot convert types!")
            : base(from + "->" + to.Name + ": " + message) {
        }

        public SolScriptMarshallingException(Type from, string to, string message = "Cannot convert types!")
            : base(from.Name + "->" + to + ": " + message) {
        }

        public SolScriptMarshallingException(Type type, string message = "Cannot find fitting SolType!")
            : base(type.Name + ": " + message) {
        }

        protected SolScriptMarshallingException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {
        }
    }
}