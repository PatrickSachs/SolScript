using Irony.Parsing;

namespace SolScript.Interpreter.Expressions {
    public abstract class TerminatingSolExpression : SolExpression, ITerminateable {
        public TerminatingSolExpression(SolAssembly assembly, SourceLocation location) : base(assembly, location) {
        }

        public abstract Terminators Terminators { get; }
    }
}