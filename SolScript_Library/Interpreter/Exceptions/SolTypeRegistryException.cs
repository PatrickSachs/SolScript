using System;
using System.Runtime.Serialization;

namespace SolScript.Interpreter.Exceptions
{
    /// <summary>
    ///     This exception is used to indicate an error inside the type registry.
    /// </summary>
    [Serializable]
    public class SolTypeRegistryException : SolScriptException
    {
        protected SolTypeRegistryException() {}

        public SolTypeRegistryException(string message) : base(message) {}

        protected SolTypeRegistryException(SerializationInfo info, StreamingContext context) : base(info, context) {}

        public SolTypeRegistryException(string message, Exception inner) : base(message, inner) {}
    }
}