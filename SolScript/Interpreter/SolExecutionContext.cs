using System;
using System.Collections.Generic;
using System.Text;
using Irony.Parsing;
using JetBrains.Annotations;
using NodeParser;
using SolScript.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The <see cref="SolExecutionContext"/> holds various information about the current
    ///     script execution. A lot of methods require you to pass the execution
    ///     context, so it is generally a good idea to pass it around wherever you can.<br />
    ///     You may create a new execution context at any given time without any negative implications (if the assembly is set
    ///     to the correct one), besides Stack Trace loss unless the old context is set as <see cref="ParentContext" /> of the
    ///     new one. You may derive your own execution context class from this one.
    /// </summary>
    // todo: somehow tie the stack frames and class entry stack. Both have very similar uses and structure & are fired at function entry.
    public class SolExecutionContext
    {
        /*
        /// <summary>
        ///     The current class we are in, containg the class instance, and which class inheritance level of said instance we are
        ///     currently in. This is required to e.g. resolve which class inheritance level "base" exactly is.
        /// </summary>
        public class SolClassEntry : ICloneable
        {
            /// <summary>
            ///     The class instance.
            /// </summary>
            public SolClass Instance { get; private set; }

            /// <summary>
            ///     The inheritance level we are at.
            /// </summary>
            public SolClassDefinition Level { get; private set; }

            public SolClassEntry MoveTo(SolClassEntry entry)
            {
                
            }

            /// <summary>
            /// Moves the entry to the given class and the given inheritance level.
            /// </summary>
            /// <param name="theClass">The class instance.</param>
            /// <param name="inheritanceLevel">The inheritance level we are at.</param>
            /// <returns>A clone of the entry before it is moved to </returns>
            /// <exception cref="ArgumentNullException">An argument is <see langword="null"/></exception>
            public SolClassEntry MoveTo([NotNull] SolClass theClass, [NotNull] SolClassDefinition inheritanceLevel)
            {
                if (theClass == null) {
                    throw new ArgumentNullException(nameof(theClass));
                }
                if (inheritanceLevel == null) {
                    throw new ArgumentNullException(nameof(inheritanceLevel));
                }
                Instance = theClass;
                Level = inheritanceLevel;
            }

            /// <summary>
            /// Moves to a global context.
            /// </summary>
            public SolClassEntry MoveToGlobal()
            {
                Instance = null;
                Level = null;
            }

            /// <inheritdoc />
            object ICloneable.Clone()
            {
                return Clone();
            }

            /// <summary>
            /// Creates a clone of this class entry.
            /// </summary>
            /// <returns>The entry.</returns>
            public SolClassEntry Clone()
            {
                return new SolClassEntry() {
                    Instance = Instance,
                    Level = Level
                };
            }
        }
        */
        /// <summary>
        ///     Creates a new <see cref="SolExecutionContext"/>.
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
        ///     The assembly this execution context belongs to. Setting the assembly to the correct one is very important as the
        ///     Assembly field of the execution context may be the only way to get a handle to the type registry of an assembly.
        /// </summary>
        public readonly SolAssembly Assembly;

        /// <summary>
        /// The stack of classes we have entered.
        /// </summary>
        protected readonly Stack<SolClassEntry> ClassEntries = new Stack<SolClassEntry>();

        /// <summary>
        ///     The raw Stack Trace. Be very careful with this as corruption in this stack can lead to crashes in the
        ///     SolScript runtime as the stack trace is not constantly checked for integrity.
        /// </summary>
        protected readonly LinkedList<SolStackFrame> StackTrace = new LinkedList<SolStackFrame>();

        /// <summary>
        ///     The current source location we are in. Try updating this whenever
        ///     possible as it contains vital information for debugging in case an error
        ///     arises.
        /// </summary>
        public NodeLocation CurrentLocation { get; set; }

        /*/// <summary>
        ///     The class this context is currently in. This property is set automatically.<br /> Only change it if you know what
        ///     you are doing as changing this value can break the runtime.
        /// </summary>
        public SolClass CurrentClass { get; set; }*/

        /// <summary>
        ///     The name of this execution context. This name is purely for debugging or crash report purposes and thus should have
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
        ///     Gets the amount of stack frames currently on the stack trace.
        /// </summary>
        public int StackFrameCount => StackTrace.Count;

        public void PushClassEntry(SolClassEntry entry)
        {
            ClassEntries.Push(entry);
        }

        public SolClassEntry PopClassEntry()
        {
            return ClassEntries.Pop();
        }

        public bool PeekClassEntry(out SolClassEntry entry)
        {
            if (ClassEntries.Count == 0) {
                entry = default(SolClassEntry);
                return false;
            }
            entry = ClassEntries.Peek();
            return true;
        }

        /// <summary>
        ///     Creates a new stack frame calling the given function.
        /// </summary>
        /// <param name="function">The function that has been called.</param>
        /// <remarks>
        ///     The location used for the stack frame location is <see cref="CurrentLocation" />. Make sure to update this
        ///     value beforehand/only afterwards.
        /// </remarks>
        public void PushStackFrame(SolFunction function)
        {
            PushStackFrame(new SolStackFrame(CurrentLocation, function));
        }

        /// <summary>
        ///     Pushes a new already created stack frame onto the stack.
        /// </summary>
        /// <param name="frame">The stack frame.</param>
        public virtual void PushStackFrame(SolStackFrame frame)
        {
            StackTrace.AddLast(frame);
        }

        /// <summary>
        ///     Pops the top frame of the stack trace.
        /// </summary>
        /// <returns>The stack frame.</returns>
        /// <exception cref="InvalidOperationException">The stack trace is empty.</exception>
        public virtual SolStackFrame PopStackFrame()
        {
            SolStackFrame last = StackTrace.Last.Value;
            StackTrace.RemoveLast();
            return last;
        }

        /// <summary>
        ///     Tries to get the latest <see cref="SolStackFrame" /> on the stack trace.
        /// </summary>
        /// <param name="frame">The stack frame. Only valid if the method returned true.</param>
        /// <param name="depth">How deep to peek? Zero is the last element, one is the element before the last, and so on.</param>
        /// <returns>If the stack frame could be obtained. Can fail if the stack trace is empty.</returns>
        /// <exception cref="ArgumentException">Cannot peek by a negative amount(<paramref name="depth" /> is smaller than 0).</exception>
        public virtual bool PeekStackFrame(out SolStackFrame frame, int depth = 0)
        {
            if (depth < 0) {
                throw new ArgumentException("Cannot peek by a negative amount.");
            }
            if (StackTrace.Count == 0) {
                frame = default(SolStackFrame);
                return false;
            }
            LinkedListNode<SolStackFrame> node = StackTrace.Last;
            while (depth > 0) {
                node = node.Previous;
                if (node == null) {
                    frame = default(SolStackFrame);
                    return false;
                }
                depth--;
            }
            frame = node.Value;
            return true;
        }

        /// <summary>
        ///     Generates the stack trace builder and appends all necessary data.
        /// </summary>
        /// <returns>The builder.</returns>
        /// <remarks>
        ///     Keep in mind that some methods may append further data to the builder. The builder should thus end in a new
        ///     line.
        /// </remarks>
        protected virtual StringBuilder GenerateStackTrace_Impl()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Error in execution context \"" + Name + "\".");
            /*if (CurrentClass != null) {
                builder.AppendLine("(Currently in class: \"" + CurrentClass.Type + "\" - Instance-Id: " + CurrentClass.Id + ")");
            } else {
                builder.AppendLine();
            }*/
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

        /// <summary>
        ///     Generates the stack trace with information about each called function. Also (optionally) indicates that a given
        ///     exception has caused the generation of this stack trace.
        /// </summary>
        /// <returns>The stack trace as string.</returns>
        public virtual string GenerateStackTrace(/*Exception nativeException = null*/)
        {
            // <param name="nativeException">The exception.</param>
            StringBuilder builder = GenerateStackTrace_Impl();
            /*if (nativeException is SolException) {
                SolException.UnwindExceptionStack((SolException) nativeException, builder);
            } else if (nativeException != null) {
                builder.Append("Caused by a native exception: ");
                builder.Append(nativeException.GetType().Name);
                if (!string.IsNullOrEmpty(nativeException.Message)) {
                    builder.Append("(");
                    builder.Append(nativeException.Message);
                    builder.AppendLine(")");
                }
                builder.AppendLine(nativeException.StackTrace);
            }*/
            return builder.ToString();
        }
    }
}