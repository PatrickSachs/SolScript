using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Irony.Parsing;

namespace SolScript.Interpreter.Exceptions
{
    [Serializable]
    public class SolScriptInterpreterException : SolScriptException {
        public SolScriptInterpreterException() {
        }

        public SolScriptInterpreterException(string message) : base(message) {
        }

        public SolScriptInterpreterException(string message, Exception inner) : base(message, inner) {
        }

        protected SolScriptInterpreterException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {
        }

        public static SolScriptInterpreterException InvalidTypes(SourceLocation location, string expected, string got, string message)
        {
            return new SolScriptInterpreterException(location + " : Invalid Types: " + message + " - Expected \"" + expected + "\", got \"" +
                                              got + "\"");
        }

        public static SolScriptInterpreterException InvalidTypes(SourceLocation location, string[] expected, string got, string message)
        {
            return new SolScriptInterpreterException(location + " : Invalid Types: " + message + " - Expected \"" + string.Join("\"/\"", expected) + "\", got \"" +
                                              got + "\"");
        }
    }
}
