using System;
using Irony.Parsing;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_CallFunction : SolStatement {
        public Statement_CallFunction(SolAssembly assembly, SourceLocation location, SolExpression classGetter, 
            SolString functionName, params SolExpression[] args) : base(assembly, location) {
            ClassGetter = classGetter;
            FunctionName = functionName;
            Arguments = args;
        }

        public SolExpression[] Arguments;
        public SolExpression ClassGetter;
        public SolString FunctionName;

        #region Overrides

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables) {
            context.CurrentLocation = Location;
            SolValue classInstRaw = ClassGetter.Evaluate(context, parentVariables);
            SolClass classInst = classInstRaw as SolClass;
            if (classInst == null) {
                throw SolScriptInterpreterException.InvalidTypes(context, "class", classInstRaw.Type, "Functions can only be called on class instances!");
            }
            SolValue functionRaw = classInst[FunctionName];
            SolFunction function = functionRaw as SolFunction;
            if (function == null) {
                throw SolScriptInterpreterException.InvalidTypes(context, "function", functionRaw.Type,
                    "Only functions can be called.");
            }
            var callArgs = new SolValue[Arguments.Length];
            for (int i = 0; i < callArgs.Length; i++) {
                callArgs[i] = Arguments[i].Evaluate(context, parentVariables);
            }
            //SolDebug.WriteLine("calling func with " + string.Join("|", (object[]) callArgs));
            context.StackTrace.Push(new SolExecutionContext.StackFrame(Location, classInstRaw.Type + "." + FunctionName.Value + "(" + Arguments.Length + " args)", function));
            SolValue ret = function.Call(context, classInst, callArgs);
#if DEBUG
            SolExecutionContext.StackFrame frame = context.StackTrace.Pop();
            if (frame.Function != function) {
                throw new InvalidOperationException("Fatal Error: Stack Trace Corruption!");
            }
#else
            context.StackTrace.Pop();
#endif
            return ret;
        }

        protected override string ToString_Impl() {
            return $"{ClassGetter}.{FunctionName}({InternalHelper.JoinToString(",", Arguments)})";
        }

        #endregion
    }
}