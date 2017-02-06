using System.Text;
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
        public readonly SolSourceLocation Location;

        /// <summary>
        ///     The function that has been called during this frame.
        /// </summary>
        /// <remarks>
        ///     Warning: Storing the function as a reference could prevent the GC of e.g. a pratically lambda function(and thus
        ///     possibly an entire class hierarchy!) if the stack trace is not popped correctly.
        /// </remarks>
        public readonly SolFunction Function;

        internal SolStackFrame(SolSourceLocation location, SolFunction function)
        {
            Location = location;
            Function = function;
        }

        public void AppendFunctionName(StringBuilder builder)
        {
            SolClassFunction classFunction = Function as SolClassFunction;
            DefinedSolFunction definedFunction = Function as DefinedSolFunction;
            if (classFunction != null) {
                SolClassDefinition definingClass = classFunction.GetDefiningClass();
                builder.Append(definingClass.Type);
                builder.Append(".");
            }
            if (definedFunction != null) {
                builder.Append(definedFunction.Definition.Name);
            } else {
                builder.Append("function#" + Function.Id);
            }
        }

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

        [Pure]
        public bool Equals(SolStackFrame other)
        {
            return Location.Equals(other.Location) && Function == other.Function;
        }

        [Pure]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            return obj is SolStackFrame && Equals((SolStackFrame) obj);
        }

        [Pure]
        public override int GetHashCode()
        {
            unchecked {
                return (Location.GetHashCode() * 397) ^ (Function != null ? Function.GetHashCode() : 0);
            }
        }
    }
}