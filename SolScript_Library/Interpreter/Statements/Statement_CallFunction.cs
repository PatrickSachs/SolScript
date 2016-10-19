using System.Collections.Generic;
using Irony.Parsing;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_CallFunction : SolStatement {
        public SolExpression FunctionGetter;
        public SolExpression[] Arguments;

        public override SolValue Execute(SolExecutionContext context)
        {
            context.CurrentLocation = Location;
            SolValue value = FunctionGetter.Evaluate(context);
            SolFunction function = value as SolFunction;
            if (function == null) {
                throw new SolScriptInterpreterException(Location + " : Tried to call a '" + value.Type +
                                                        "' value. Only functions can be called.");
            }
            var callArgs = new SolValue[Arguments.Length];
            for (int i = 0; i < callArgs.Length; i++) {
                callArgs[i] = Arguments[i].Evaluate(context);
            }
            SolValue ret = function.Call(callArgs, context);
            return ret;
        }

        protected override string ToString_Impl() {
            return $"Statement_CallFunction(FunctionGetter={FunctionGetter}, Arguments=[{string.Join(",", (IEnumerable<SolExpression>)Arguments)}])";
        }

        public Statement_CallFunction(SourceLocation location) : base(location) {
        }
    }
}