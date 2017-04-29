using System.Text;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The SolStackFrame struct represents a single function call on the stack trace. It is used to provide a quick
    ///     debugging helper is case the application crashes.
    /// </summary>
    public struct SolStackFrame
    {
        /// <summary>
        ///     The location in the code from where the function was called.
        /// </summary>
        public readonly SourceLocation Location;

        /// <summary>
        ///     The function that has been called during this frame.
        /// </summary>
        /// <remarks>
        ///     Warning: Storing the function as a reference could prevent the GC of e.g. a lambda function(and thus
        ///     possibly an entire class hierarchy!) if the stack trace is not popped correctly.
        /// </remarks>
        public readonly SolFunction Function;

        /// <summary>
        ///     Creates a new stack frame.
        /// </summary>
        /// <param name="location">The location of the stack frame in code.</param>
        /// <param name="function">The function the frame related to.</param>
        public SolStackFrame(SourceLocation location, SolFunction function)
        {
            Location = location;
            Function = function;
        }

        /// <summary>
        ///     Appends a nice format of the function name of this stack frame to a <see cref="StringBuilder" />.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public void AppendFunctionName(StringBuilder builder)
        {
            SolClassFunction classFunction = Function as SolClassFunction;
            DefinedSolFunction definedFunction = Function as DefinedSolFunction;
            if (classFunction != null) {
                bool _;
                SolClassDefinition onClass = classFunction.ClassInstance.InheritanceChain.Definition;
                SolClassDefinition definingClass = classFunction.Definition.DefinedIn ?? onClass;
                builder.Append(definingClass.Type);
                builder.Append(".");
            }
            if (definedFunction != null) {
                builder.Append(definedFunction.Definition.Name);
            } else {
                builder.Append("function#" + Function.Id);
            }
        }

        /// <inheritdoc />
        [Pure]
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Location);
            builder.Append(" : ");
            AppendFunctionName(builder);
            builder.Append("(");
            AppendFunctionParameters(builder);
            builder.Append(") [@ ");
            builder.Append(Function.Location);
            builder.Append("]");
            return builder.ToString();
        }

        /// <summary>
        ///     Appends a nice format of the function parameters of this stack frame to a <see cref="StringBuilder" />.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public void AppendFunctionParameters(StringBuilder builder)
        {
            bool first = true;
            foreach (SolParameter parameter in Function.ParameterInfo) {
                if (!first) {
                    builder.Append(", ");
                }
                builder.Append(parameter.Name);
                builder.Append(" : ");
                builder.Append(parameter.Type);
                first = false;
            }
        }

        /// <summary>
        ///     Checks if this stack frame is eual to another. Two stack frames are considered equal if their
        ///     <see cref="Location" /> equals and
        ///     their <see cref="Function" /> is reference equal.
        /// </summary>
        /// <param name="other">The other frame.</param>
        /// <returns>true if equal, false if not.</returns>
        [Pure]
        public bool Equals(SolStackFrame other)
        {
            return Location.Equals(other.Location) && Function == other.Function;
        }

        /// <summary>
        ///     Wraps <see cref="Equals(SolScript.Interpreter.SolStackFrame)" />.
        /// </summary>
        /// <param name="frame1">First frame.</param>
        /// <param name="frame2">Second frame.</param>
        /// <returns>true if equal, false if not.</returns>
        public static bool operator ==(SolStackFrame frame1, SolStackFrame frame2)
        {
            return frame1.Equals(frame2);
        }

        /// <summary>
        ///     Wraps <see cref="Equals(object)" />.
        /// </summary>
        /// <param name="frame1">First frame.</param>
        /// <param name="frame2">Second potential frame.</param>
        /// <returns>true if equal, false if not.</returns>
        public static bool operator ==(SolStackFrame frame1, object frame2)
        {
            return frame1.Equals(frame2);
        }

        /// <summary>
        ///     Wraps <see cref="Equals(object)" />.
        /// </summary>
        /// <param name="frame1">First frame.</param>
        /// <param name="frame2">Second potential frame.</param>
        /// <returns>true not if equal, false if.</returns>
        public static bool operator !=(SolStackFrame frame1, object frame2)
        {
            return frame1.Equals(frame2);
        }

        /// <summary>
        ///     Wraps <see cref="Equals(SolScript.Interpreter.SolStackFrame)" />.
        /// </summary>
        /// <param name="frame1">First frame.</param>
        /// <param name="frame2">Second frame.</param>
        /// <returns>true if not equal, false if.</returns>
        public static bool operator !=(SolStackFrame frame1, SolStackFrame frame2)
        {
            return !frame1.Equals(frame2);
        }

        /// <inheritdoc />
        /// <seealso cref="Equals(SolScript.Interpreter.SolStackFrame)" />
        [Pure]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            return obj is SolStackFrame && Equals((SolStackFrame) obj);
        }

        /// <inheritdoc />
        [Pure]
        public override int GetHashCode()
        {
            unchecked {
                return (Location.GetHashCode() * 397) ^ (Function != null ? Function.GetHashCode() : 0);
            }
        }
    }
}