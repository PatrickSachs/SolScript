using JetBrains.Annotations;

namespace SolScript.Interpreter {
    public interface ICanTerminateParent {
        bool DidTerminateParent { get; }
        //[CanBeNull] SolValue TerminateParentValue { get; }
    }
}