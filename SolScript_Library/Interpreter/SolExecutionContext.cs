using System.Collections.Generic;
using System.Text;
using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter {
    /// <summary> The SolExcecutionContext holds various information about the current
    ///     script execution. A lot of methods require you to pass the execution
    ///     context, so it is generally a good idea to pass it around wherever you can. </summary>
    public class SolExecutionContext {
        public readonly SolAssembly Assembly;

        public SolExecutionContext(SolAssembly assembly) {
            Assembly = assembly;
            StackTrace = new Stack<StackFrame>();
            //Assembly = assembly;
        }

        /// <summary> The Assembly assigned to this SolExecutionContext. All
        ///     SolExecutionContext must have an assembly. </summary>
        //public readonly SolAssembly Assembly;

        public readonly Stack<StackFrame> StackTrace;

        /// <summary> The VariableContext of the active context. </summary>
        //public readonly VarContext VariableContext = new VarContext();

        /// <summary> The current source location we are in. Try updating this whenever
        ///     possible as it contains vital information for debugging in case an error
        ///     arises. </summary>
        public SourceLocation CurrentLocation;

        private SolExecutionContext m_ParentContext;

        public string GenerateStackTrace() {
            StringBuilder builder = new StringBuilder();
            foreach (StackFrame frame in StackTrace) {
                builder.AppendLine(
                    frame.Location + " : " +
                    frame.Name + "(" +
                    InternalHelper.JoinToString(",", frame.Function.Parameters) +
                    (frame.Function.ParameterAllowOptional ? "..." : "") + ")");
            }
            if (m_ParentContext != null) {
                builder.AppendLine(m_ParentContext.GenerateStackTrace());
            }
            return builder.ToString();
        }

        /*/// <summary> Creates a new SolExecutionContext with a blank VariableContext. </summary>
        /// <param name="assembly"> The assembly assigned to this SolExecutionContext </param>
        public static SolExecutionContext Rooted(SolAssembly assembly) {
            return new SolExecutionContext(assembly);
        }

        /// <summary> Creates a new SolExecutionContext nested inside another variable
        ///     context. </summary>
        /// <param name="context"> The parent context. </param>
        public static SolExecutionContext Nested(SolExecutionContext context) {
            SolExecutionContext ctx = new SolExecutionContext(context.Assembly);
            ctx.VariableContext.ParentContext = context.VariableContext;
            ctx.m_ParentContext = context;
            return ctx;
        }*/

        #region Nested type: StackFrame

        public struct StackFrame {
            public readonly SourceLocation Location;
            public readonly string Name;
            public readonly SolFunction Function;

            public StackFrame(SourceLocation location, string name, SolFunction function) {
                Location = location;
                Name = name;
                Function = function;
            }
        }

        #endregion
    }
}