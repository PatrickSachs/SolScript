using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

//SolScript.Interpreter.Exceptions.SolScriptInterpreterException
namespace SolScript.Interpreter.Exceptions {
    [Serializable]
    public class SolScriptInterpreterException : SolScriptException {
        private SolScriptInterpreterException() {
        }

        private SolScriptInterpreterException(string message) : base(message) {
        }

        public SolScriptInterpreterException(SolExecutionContext context, string message)
            : base(
                context?.CurrentLocation + " : " + message + "\nStack Trace:\n-------------- -\n" +
                context?.GenerateStackTrace()) {
        }

        private SolScriptInterpreterException(string message, Exception inner) : base(message, inner) {
        }

        protected SolScriptInterpreterException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {
        }

        public static SolScriptInterpreterException Raw([CanBeNull] SolExecutionContext context, string message) {
            return
                new SolScriptInterpreterException(context, message);
        }

        public static SolScriptInterpreterException IndexOutOfRange(SolExecutionContext context, string gotIndex,
            string minIndex, string maxIndex, string message) {
            return
                new SolScriptInterpreterException(
                    $"{context.CurrentLocation} : Index out of range: {message}\nIndex: \"{gotIndex}\", Index Range: \"{minIndex}\"-\"{maxIndex}\"\nStack Trace:\n---------------\n{context.GenerateStackTrace()}");
        }

        public static SolScriptInterpreterException IllegalAccessName(SolExecutionContext context, string name,
            string message) {
            return
                new SolScriptInterpreterException(
                    $"{context.CurrentLocation} : Illegal access: {message}. Accessor name: {name}\nStack Trace:\n---------------\n{context.GenerateStackTrace()}");
        }

        public static SolScriptInterpreterException IllegalAccessType(SolExecutionContext context, string type,
            string message) {
            return
                new SolScriptInterpreterException(
                    $"{context.CurrentLocation} : Illegal access: {message}. Accessor type: {type}\nStack Trace:\n---------------\n{context.GenerateStackTrace()}");
        }

        public static SolScriptInterpreterException InvalidTypes(SolExecutionContext context, string expected,
            string got, string message) {
            return
                new SolScriptInterpreterException(
                    $"{context.CurrentLocation} : Invalid Types: {message} - Expected \"{expected}\", got \"{got}\"\nStack Trace:\n---------------\n{context.GenerateStackTrace()}");
        }

        public static SolScriptInterpreterException InvalidTypes(SolExecutionContext context, string[] expected,
            string got, string message)
        {
            return
                new SolScriptInterpreterException(
                    $"{context.CurrentLocation} : Invalid Types: {message} - Expected \"{string.Join("\"/\"", expected)}\", got \"{got}\"\nStack Trace:\n---------------\n{context.GenerateStackTrace()}");
        }
        public static SolScriptInterpreterException InvalidTypes(SolExecutionContext context, string[] expected,
           string[] got, string message)
        {
            return
                new SolScriptInterpreterException(
                    $"{context.CurrentLocation} : Invalid Types: {message} - Expected \"{string.Join("\"/\"", expected)}\", got \"{string.Join("\"/\"", got)}\"\nStack Trace:\n---------------\n{context.GenerateStackTrace()}");
        }
    }
}