using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements
{
    public abstract class SolStatement : ISourceLocateable {
        public readonly SolAssembly Assembly;
        public SolSourceLocation Location { get; }

        public SolStatement(SolAssembly assembly, SolSourceLocation location) {
            Assembly = assembly;
            Location = location;
        }

        public abstract SolValue Execute(SolExecutionContext context, IVariables parentVariables);

        public virtual Terminators Terminators { get; protected set; }

        //public virtual bool DidTerminateParent { get; protected set; }
        //public virtual bool DidTerminateIterator { get; protected set; }

        public override string ToString() {
            return ToString_Impl();
        }

        protected abstract string ToString_Impl();
    }
}
