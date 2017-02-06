using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;
using SolScript.Interpreter.Types.Interfaces;

namespace SolScript.Interpreter.Statements
{
    public class Statement_CallInstanceFunction : SolStatement
    {
        public Statement_CallInstanceFunction(SolAssembly assembly, SolSourceLocation location, string statementDefinedInClass, SolExpression functionGetter, params SolExpression[] args) : base(assembly, location)
        {
            FunctionGetter = functionGetter;
            Arguments = args;
            StatementDefinedInClassName = statementDefinedInClass;
        }

        public readonly SolExpression[] Arguments;
        public readonly SolExpression FunctionGetter;

        // WARNING: This could have been defined __ anywhere__ not even necessarily within the class inheritance tree!
        /// <summary>
        ///     This defines where the STATEMENT CALLING the function has been defined, and NOT where the FUNCTION ITSELF has been
        ///     defined.
        /// </summary>
        /// <remarks>
        ///     If you want to get a handle on the class that defined a function, cast the function itself to
        ///     <see cref="SolClassFunction" /> and obtain the class definition using
        ///     <see cref="SolClassFunction.GetDefiningClass" />.
        /// </remarks>
        [CanBeNull] public readonly string StatementDefinedInClassName;
        
        #region Overrides

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            context.CurrentLocation = Location;
            SolValue functionRaw = FunctionGetter.Evaluate(context, parentVariables);
            SolFunction function = functionRaw as SolFunction;
            if (function == null)
            {
                throw new SolRuntimeException(context, $"Tried to call a \"{functionRaw.Type}\" value.");
            }
            SolValue[] callArgs = GetArguments(context, parentVariables);
            terminators = Terminators.None;
            return function.Call(context, callArgs);
        }

        protected override string ToString_Impl()
        {
            return $"{FunctionGetter}({InternalHelper.JoinToString(",", Arguments)})";
        }

        #endregion

        private SolValue[] GetArguments(SolExecutionContext context, IVariables parentVariables)
        {
            var callArgs = new SolValue[Arguments.Length];
            for (int i = 0; i < callArgs.Length; i++) {
                callArgs[i] = Arguments[i].Evaluate(context, parentVariables);
            }
            return callArgs;
        }
    }
}