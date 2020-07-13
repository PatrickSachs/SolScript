using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public class Expression_Number : SolExpression {
        public SolNumber Value;

        public Expression_Number(SolAssembly assembly, SourceLocation location, double value) : base(assembly, location) {
            Value = new SolNumber(value);
        }

        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables) {
            return Value;
        }

        protected override string ToString_Impl() {
            return Value.ToString();
        }
    }
}