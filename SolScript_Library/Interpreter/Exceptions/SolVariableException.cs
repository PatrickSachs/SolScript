using System;
using System.Runtime.Serialization;

namespace SolScript.Interpreter.Exceptions {
    [Serializable]
    public class SolVariableException : Exception {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public SolVariableException() {
        }

        public SolVariableException(string message) : base(message) {
        }

        public SolVariableException(string message, Exception inner) : base(message, inner) {
        }

        protected SolVariableException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {
        }
    }
}