using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public abstract class SolExpression {
        public SolExpression(SourceLocation location) {
            Location = location;
        }

        public readonly SourceLocation Location;

        public abstract SolValue Evaluate(SolExecutionContext context);

        public override string ToString() => ToString_Impl();

        protected abstract string ToString_Impl();
    }
}