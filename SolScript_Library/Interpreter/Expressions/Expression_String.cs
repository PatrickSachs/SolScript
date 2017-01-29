using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public class Expression_String : SolExpression {
        public Expression_String(SolAssembly assembly, SolSourceLocation location, string value) : base(assembly, location) {
            Value = new SolString(value);
        }

        public readonly SolString Value;

        #region Overrides

        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables) {
            //context.EvaluationStack.Push(Value);
            return Value;
        }

        protected override string ToString_Impl() {
            return "\"" + Value.Value + "\"";
        }

        #endregion
    }
}