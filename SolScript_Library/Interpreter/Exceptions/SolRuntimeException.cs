using System;
using System.Runtime.Serialization;

namespace SolScript.Interpreter.Exceptions
{
    /// <summary>
    ///     The SolRuntimeException is used to represent an exception that occured during the actual run of an assembly.
    /// </summary>
    public class SolRuntimeException : SolScriptException
    {
        protected SolRuntimeException() {}

        public SolRuntimeException(SolExecutionContext context, string message) : base($"{context.CurrentLocation} : {message}\nStack Trace:\n{context.GenerateStackTrace()}") {}

        protected SolRuntimeException(SerializationInfo info, StreamingContext context) : base(info, context) {}

        public SolRuntimeException(SolExecutionContext context, string message, Exception inner) : base(
            $"{context.CurrentLocation} : {message}\nStack Trace:\n{context.GenerateStackTrace(inner)}", inner) {}
    }
}