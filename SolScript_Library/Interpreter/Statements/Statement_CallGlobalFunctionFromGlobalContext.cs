using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements
{
    public class Statement_CallGlobalFunctionFromGlobalContext : SolStatement
    {
        public Statement_CallGlobalFunctionFromGlobalContext(SolAssembly assembly, SolSourceLocation location, string functionName, params SolExpression[] arguments) : base(assembly, location)
        {
            FunctionName = functionName;
            Arguments = arguments;
        }

        public readonly SolExpression[] Arguments;
        public readonly string FunctionName;

        #region Overrides

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables)
        {
            // todo: not happy with the class name
            context.CurrentLocation = Location;
            SolValue functionRaw;
            try {
                // todo: it feels wrong to directy access the local variables, but since this statement only calls from global context it should be fine
                // however id like this class to purly be a "call a global" statement, without the already in global requirement
                // (even then it may be okay since globals are a "flat" layer and no global could overwrite a local)
                functionRaw = Assembly.LocalVariables.Get(FunctionName);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, "Failed to get global function \"" + FunctionName + "\" - " + ex.Message);
            }
            SolFunction function = functionRaw as SolFunction;
            if (function == null) {
                throw new SolRuntimeException(context, "Tried to call a " + functionRaw.Type + " value.");
            }
            var callArgs = new SolValue[Arguments.Length];
            for (int i = 0; i < callArgs.Length; i++) {
                callArgs[i] = Arguments[i].Evaluate(context, parentVariables);
            }
            SolValue ret = function.Call(context, callArgs);
            return ret;
        }

        protected override string ToString_Impl()
        {
            return $"{FunctionName}({InternalHelper.JoinToString(",", Arguments)})";
        }

        #endregion
    }
}