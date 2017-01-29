using System.Text;
using Irony.Parsing;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public class Expression_TableConstructor : SolExpression {
        public Expression_TableConstructor(SolAssembly assembly, SolSourceLocation location) : base(assembly, location) {
        }

        public SolExpression[] Keys;
        public SolExpression[] Values;

        #region Overrides

        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables) {
            context.CurrentLocation = Location;
            SolTable table = new SolTable();
            for (int i = 0; i < Keys.Length; i++) {
                SolValue key = Keys[i].Evaluate(context, parentVariables);
                SolValue value = Values[i].Evaluate(context, parentVariables);
                try {
                    table[key] = value;
                } catch (SolVariableException ex) {
                    throw new SolRuntimeException(context, $"An error occured while creating the table at key \"{key}\".", ex);
                }
            }
            table.SetN(Keys.Length);
            return table;
        }

        protected override string ToString_Impl() {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            for (int i = 0; i < Keys.Length; i++) {
                SolExpression key = Keys[i];
                SolExpression value = Values[i];
                builder.AppendLine("  [" + key + "] = " + value);
            }
            builder.AppendLine("}");
            return builder.ToString();
        }

        #endregion
    }
}