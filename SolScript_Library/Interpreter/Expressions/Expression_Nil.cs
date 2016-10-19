using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public class Expression_Nil : SolExpression {
        public override SolValue Evaluate(SolExecutionContext context) {
            return SolNil.Instance;
        }

        protected override string ToString_Impl() {
            return "nil";
        }

        public Expression_Nil(SourceLocation location) : base(location) {
        }
    }
}