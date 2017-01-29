using System.Text;
using JetBrains.Annotations;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter {
    public class SolChunk : ITerminateable {
        public SolChunk(SolAssembly assembly) {
            Assembly = assembly;
            Id = ++s_LastId;
        }

        private static int s_LastId = -1;
        public readonly SolAssembly Assembly;
        public readonly int Id;

        [CanBeNull] public TerminatingSolExpression ReturnExpression;

        public SolStatement[] Statements;

        #region ITerminateable Members

        public Terminators Terminators { get; private set; }

        #endregion

        #region Overrides

        public override int GetHashCode() {
            return 30 + Id;
        }

        public override string ToString() {
            return ToString(0);
        }

        #endregion

        public string ToString(int indent) {
            string indentStr = new string(' ', indent);
            StringBuilder builder = new StringBuilder();
            foreach (SolStatement statement in Statements) {
                builder.AppendLine(indentStr + statement);
            }
            if (ReturnExpression != null) {
                builder.Append(indentStr + "return ");
                builder.AppendLine(ReturnExpression.ToString());
            }
            return builder.ToString();
        }

        public SolValue ExecuteInTarget(SolExecutionContext context, IVariables variables) {
            Terminators = Terminators.None;
            foreach (SolStatement statement in Statements) {
                SolValue value = statement.Execute(context, variables);
                Terminators = statement.Terminators;
                // If either return, break, or continue occured we break out of the current chunk.
                if (Terminators != Terminators.None) {
                    return value;
                }
            }
            if (ReturnExpression != null) {
                SolValue value = ReturnExpression.Evaluate(context, variables);
                Terminators = ReturnExpression.Terminators;
                return value;
            }
            return SolNil.Instance;
        }

        public SolValue ExecuteInNew(SolExecutionContext context) {
            Terminators = Terminators.None;
            ChunkVariables variables = new ChunkVariables(Assembly);
            foreach (SolStatement statement in Statements) {
                statement.Execute(context, variables);
                Terminators = statement.Terminators;
                // If either return, break, or continue occured we break out of the current chunk.
                if (Terminators != Terminators.None) {
                    return SolNil.Instance;
                }
            }
            if (ReturnExpression != null) {
                SolValue value = ReturnExpression.Evaluate(context, variables);
                Terminators = ReturnExpression.Terminators;
                return value;
            }
            return SolNil.Instance;
        }
    }
}