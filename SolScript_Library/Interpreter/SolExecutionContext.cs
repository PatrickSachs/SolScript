using Irony.Parsing;

namespace SolScript.Interpreter {
    public class SolExecutionContext {
        public SolExecutionContext(SolAssembly assembly) {
            Assembly = assembly;
        }

        public readonly VarContext VariableContext = new VarContext();
        public readonly Interpreter.SolAssembly Assembly;
        public SourceLocation CurrentLocation;

        /*public static SolExecutionContext CreateFrom([NotNull] SolExecutionContext parent) {
            SolExecutionContext context = new SolExecutionContext(); // {TypeRegistry = parent.TypeRegistry};
            context.VariableContext.ParentContext = parent.VariableContext;
            context.Assembly = parent.Assembly;
            return context;
        }*/

        /*public static SolExecutionContext CreateRooted([NotNull] Interpreter.SolAssembly script) {
            var ctx = new SolExecutionContext {
                Assembly = script.NotNull()
            };
            //ctx.VariableContext.AdditionalContext = new VarContext[1] {script.RootContext.VariableContext};
            return ctx;
        }*/
    }
}