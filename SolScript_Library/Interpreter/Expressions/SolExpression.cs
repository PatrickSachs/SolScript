using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public abstract class SolExpression : ISourceLocateable {
        public SolExpression(SolAssembly assembly, SolSourceLocation location) {
            Assembly = assembly;
            Location = location;
        }

        public readonly SolAssembly Assembly;
        public SolSourceLocation Location { get; }

        public abstract SolValue Evaluate(SolExecutionContext context, IVariables parentVariables);

        public override string ToString() => ToString_Impl();

        protected abstract string ToString_Impl();
    }
}