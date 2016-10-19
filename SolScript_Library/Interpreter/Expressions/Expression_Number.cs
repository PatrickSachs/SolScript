using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public class Expression_Number : SolExpression {
        public SolNumber Value;

        public Expression_Number(SourceLocation location, double value) : base(location) {
            Value = new SolNumber(value);
        }

        public override SolValue Evaluate(SolExecutionContext context) {
            return Value;
        }

        protected override string ToString_Impl() {
            return $"Expression_Number({Value})";
        }
    }
}