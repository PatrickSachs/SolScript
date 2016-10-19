using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements
{
    public abstract class SolStatement : ICanTerminateParent {
        public readonly SourceLocation Location;

        public SolStatement(SourceLocation location) {
            Location = location;
        }

        public abstract SolValue Execute(SolExecutionContext context);

        public virtual bool DidTerminateParent { get; protected set; }

        public override string ToString() {
            return ToString_Impl();
        }

        protected abstract string ToString_Impl();
    }
}
