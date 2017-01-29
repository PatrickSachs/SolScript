using System;
using System.Runtime.Serialization;

namespace SolScript.Interpreter.Exceptions
{
    /// <summary>
    ///     This is the base exception for all exceptions in SolScript. The type is abstract, thus relying on custom more
    ///     specific exception implementations.
    /// </summary>
    [Serializable]
    public abstract class SolScriptException : Exception
    {
        protected SolScriptException() {}

        protected SolScriptException(string message) : base(message) {}

        protected SolScriptException(string message, Exception inner) : base(message, inner) {}

        protected SolScriptException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {}
    }
}