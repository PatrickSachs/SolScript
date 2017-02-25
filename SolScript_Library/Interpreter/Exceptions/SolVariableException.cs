using System;
using System.Runtime.Serialization;

namespace SolScript.Interpreter.Exceptions
{
    /// <summary>
    ///     The SolVariableException is used whenever an error while resolving a variable occured.
    /// </summary>
    [Serializable]
    public class SolVariableException : SolException
    {
        public SolVariableException() {}

        public SolVariableException(string message) : base(message) {}

        public SolVariableException(string message, Exception inner) : base(message, inner) {}

        protected SolVariableException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {}
    }
}