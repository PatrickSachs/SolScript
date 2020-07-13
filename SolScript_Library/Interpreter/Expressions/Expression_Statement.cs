using Irony.Parsing;
using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public class Expression_Statement : SolExpression {
        public Expression_Statement(SolAssembly assembly, SourceLocation location) : base(assembly, location) {
        }

        public SolStatement Statement;

        #region Overrides

        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables) {
            context.CurrentLocation = Location;
            return Statement.Execute(context, parentVariables);
        }

        protected override string ToString_Impl() {
            return Statement.ToString();
        }

        #endregion
    }
}