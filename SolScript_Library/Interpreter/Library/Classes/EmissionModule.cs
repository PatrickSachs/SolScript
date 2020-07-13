using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Library.Classes {
    /*[SolLibraryClass(SolLibrary.STD_NAME, SolTypeMode.Singleton)]
    [SolLibraryName("Emit")]
    public sealed class EmissionModule {
        [UsedImplicitly]
        public void insert_function(SolExecutionContext context, [CanBeNull] SolClass solClass, [CanBeNull] string functionName, [CanBeNull] SolFunction function, bool local) {
            if (function == null || solClass == null || functionName == null) {
                throw SolScriptInterpreterException.InvalidTypes(
                    context,
                    new[] {"class!", "string!", "function!"},
                    new[] {solClass?.Type ?? "nil", functionName != null ? "string" : "nil", function != null ? function.Type : "nil"},
                    "Invalid type parameters for Emit.insert_function.");
            }
            solClass.Context.VariableContext.DeclareVariable(solClass.Context, functionName, function, new SolType("function", false), local);
        }
    }*/
}