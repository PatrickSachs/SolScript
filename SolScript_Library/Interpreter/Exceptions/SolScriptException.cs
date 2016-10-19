using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SolScript.Interpreter.Exceptions
{
    [Serializable]
    public class SolScriptException : Exception {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public SolScriptException() {
        }

        public SolScriptException(string message) : base(message) {
        }

        public SolScriptException(string message, Exception inner) : base(message, inner) {
        }

        protected SolScriptException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {
        }
    }
}
