using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public class Expression_String : SolExpression {
        public readonly SolString Value;

        public Expression_String(SourceLocation location, string value) : base(location) {
            Value = new SolString(value);
        }

        public override SolValue Evaluate(SolExecutionContext context) {
            //context.EvaluationStack.Push(Value);
            return Value;
        }

        protected override string ToString_Impl() {
            return $"Expression_String({Value})";
        }
    }
}