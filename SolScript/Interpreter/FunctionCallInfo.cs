using JetBrains.Annotations;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter {
    public class FunctionCallInfo {
        public FunctionCallInfo(SolClass classInstance) {
            ClassInstance = classInstance;
        }
        
        [CanBeNull] public SolClass ClassInstance;
    }
}