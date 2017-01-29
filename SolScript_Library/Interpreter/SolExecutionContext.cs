using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The SolExcecutionContext holds various information about the current
    ///     script execution. A lot of methods require you to pass the execution
    ///     context, so it is generally a good idea to pass it around wherever you can.<br />
    ///     You may create a new execution context at any given time without any negative implications (if the assembly is set
    ///     to the correct one), besides Stack Trace loss unless the old context is set as <see cref="ParentContext" /> of the
    ///     new one. You may derive your own execution context class from this one.
    /// </summary>
    public class SolExecutionContext
    {
        /// <summary>
        ///     Creates a new exceution context.
        /// </summary>
        /// <param name="assembly">The assembly the context belongs to. <seealso cref="Assembly" /></param>
        /// <param name="name">The name of this context. <seealso cref="Name" /></param>
        public SolExecutionContext(SolAssembly assembly, string name)
        {
            Name = name;
            Assembly = assembly;
            CurrentLocation = SolSourceLocation.Native();
        }

        /// <summary>
        ///     The assembly this exceution context belongs to. Setting the assembly to the correct one is very important as the
        ///     Assembly field of the exceution context may be the only way to get a handle to the type registry of an assembly.
        /// </summary>
        public readonly SolAssembly Assembly;

        /// <summary>
        ///     The raw Stack Trace stack. Be very careful with this as corruption in this stack can lead to crashes in the
        ///     SolScript runtime as the stack trace is not constantly checked for integrity.
        /// </summary>
        protected readonly Stack<SolStackFrame> StackTrace = new Stack<SolStackFrame>();

        /// <summary>
        ///     The current source location we are in. Try updating this whenever
        ///     possible as it contains vital information for debugging in case an error
        ///     arises.
        /// </summary>
        public SolSourceLocation CurrentLocation { get; set; }

        /// <summary>
        ///     The name of this excution context. This name is purely for debugging or crash report purposes and thus should have
        ///     a descriptive name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     An optional parent context of this execution context. A parent context does not have any further effect than having
        ///     its Stack Trace included in the Stack Trace of this context.
        /// </summary>
        [CanBeNull]
        public SolExecutionContext ParentContext { get; set; }

        /// <summary>
        ///     Creates a new stack frame calling the given function..
        /// </summary>
        /// <param name="function">The function that has been called.</param>
        /// <remarks>
        ///     The location used for the stack frame location is <see cref="CurrentLocation" />. Make sure to update this
        ///     value beforehand/only afterwards.
        /// </remarks>
        public virtual void PushStackFrame(SolFunction function)
        {
            StackTrace.Push(new SolStackFrame(CurrentLocation, function));
        }

        /// <summary>
        ///     Pops the top frame of the stack trace.
        /// </summary>
        /// <returns>The stack frame.</returns>
        /// <exception cref="InvalidOperationException">The stack trace is empty.</exception>
        public virtual SolStackFrame PopStackFrame()
        {
            return StackTrace.Pop();
        }

        /// <summary>
        ///     Gets the amount of stack frames currently on the stack trace.
        /// </summary>
        /// <returns>An integer representing the amount of stack frames.</returns>
        public int GetStackFrameCount()
        {
            return StackTrace.Count;
        }

        protected StringBuilder GenerateStackTraceImpl()
        {
            StringBuilder builder = new StringBuilder();
            foreach (SolStackFrame frame in StackTrace) {
                builder.Append("  ");
                builder.AppendLine(frame.ToString());
            }
            if (ParentContext != null) {
                builder.Append("Transitioned from ");
                builder.Append(ParentContext.Name);
                builder.AppendLine(":");
                builder.AppendLine(ParentContext.GenerateStackTrace());
            }
            return builder;
        }

        public virtual string GenerateStackTrace()
        {
            return GenerateStackTraceImpl().ToString();
        }

        public virtual string GenerateStackTrace(Exception nativeException)
        {
            StringBuilder builder = GenerateStackTraceImpl();
            builder.Append("Caused by a native exception: ");
            builder.Append(nativeException.GetType().Name);
            if (!string.IsNullOrEmpty(nativeException.Message)) {
                builder.Append("(");
                builder.Append(nativeException.Message);
                builder.AppendLine(")");
            }
            builder.AppendLine(nativeException.StackTrace);
            return builder.ToString();
        }
    }
}