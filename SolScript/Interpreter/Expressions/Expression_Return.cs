using Irony.Parsing;
using SolScript.Compiler;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public class Expression_Return : TerminatingSolExpression {
        public Expression_Return(SolExpression returnExpression) {
            ReturnExpression = returnExpression;
        }

        public readonly SolExpression ReturnExpression;
        
        #region Overrides

        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables, out Terminators terminators) {
            terminators = Terminators.Return;
            return ReturnExpression.Evaluate(context, parentVariables);
        }

        protected override string ToString_Impl() {
            return "return " + ReturnExpression;
        }

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            return ReturnExpression.Validate(context);
        }

        /// <inheritdoc />
        public override bool IsConstant => ReturnExpression.IsConstant;

        #endregion
    }
}