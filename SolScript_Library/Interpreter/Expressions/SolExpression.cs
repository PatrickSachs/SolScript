using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public abstract class SolExpression {
        public SolExpression(SolAssembly assembly, SourceLocation location) {
            Assembly = assembly;
            Location = location;
        }

        public readonly SolAssembly Assembly;
        public readonly SourceLocation Location;

        public abstract SolValue Evaluate(SolExecutionContext context, IVariables parentVariables);

        public override string ToString() => ToString_Impl();

        protected abstract string ToString_Impl();
    }
}