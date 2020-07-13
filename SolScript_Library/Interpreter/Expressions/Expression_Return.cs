using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public class Expression_Return : TerminatingSolExpression {
        public Expression_Return(SolAssembly assembly, SourceLocation location, SolExpression returnExpression) : base(assembly, location) {
            ReturnExpression = returnExpression;
        }

        public readonly SolExpression ReturnExpression;

        public override Terminators Terminators => Terminators.Return;

        #region Overrides

        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables) {
            return ReturnExpression.Evaluate(context, parentVariables);
        }

        protected override string ToString_Impl() {
            return "return " + ReturnExpression;
        }

        #endregion
    }
}