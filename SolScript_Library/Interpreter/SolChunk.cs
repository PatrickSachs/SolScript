using System.Text;
using JetBrains.Annotations;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter {
    public class SolChunk {
        public SolChunk(SolAssembly assembly) {
            Assembly = assembly;
            Id = ++s_LastId;
        }

        private static int s_LastId = -1;
        public readonly SolAssembly Assembly;
        public readonly int Id;

        [CanBeNull] public TerminatingSolExpression ReturnExpression;

        public SolStatement[] Statements;

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

        public SolValue Execute(SolExecutionContext context, IVariables variables, out Terminators terminators) {
            foreach (SolStatement statement in Statements) {
                SolValue value = statement.Execute(context, variables, out terminators);
                // If either return, break, or continue occured we break out of the current chunk.
                if (terminators != Terminators.None) {
                    return value;
                }
            }
            if (ReturnExpression != null) {
                SolValue value = ReturnExpression.Evaluate(context, variables, out terminators);
                return value;
            }
            terminators = Terminators.None;
            return SolNil.Instance;
        }
    }
}