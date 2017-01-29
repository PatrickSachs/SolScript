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
            //ClassGetter = classGetter;
            //FunctionName = functionName;
            FunctionGetter = functionGetter;
            Arguments = args;
            StatementDefinedInClassName = statementDefinedInClass;
        }

        public readonly SolExpression[] Arguments;
        public readonly SolExpression FunctionGetter;
        //public readonly SolString FunctionName;

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

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables)
        {
            context.CurrentLocation = Location;
            SolValue functionRaw = FunctionGetter.Evaluate(context, parentVariables);
            SolFunction function = functionRaw as SolFunction;
            if (function == null)
            {
                throw new SolRuntimeException(context, $"Tried to call a \"{functionRaw.Type}\" value.");
            }
            SolValue[] callArgs = GetArguments(context, parentVariables);
            return function.Call(context, callArgs);

            /*SolValue classInstRaw = ClassGetter.Evaluate(context, parentVariables);
            SolClass classInst = classInstRaw as SolClass;
            // Check 1: Are we calling a class instance function?
            if (classInst != null) {
                // If the statement was written anywhere inside the calling function we can access the 
                // locals of the respective inheritance level.
                // If not(=the statement was written in another class/global context) we will simply
                // grant access to the globals.
                SolValue functionRaw;
                SolClass.Inheritance classInheritance = classInst.FindInheritance(StatementDefinedInClassName);
                try {
                    if (classInheritance != null) {
                        functionRaw = classInheritance.Variables.Get(FunctionName.Value);
                    } else {
                        functionRaw = classInst.GlobalVariables.Get(FunctionName.Value);
                    }
                } catch (SolVariableException ex) {
                    throw new SolRuntimeException(context, $"Failed to get \"{classInst.Type}\" instance function \"{FunctionName}\" - {ex.Message}");
                }
                SolFunction function = functionRaw as SolFunction;
                if (function == null) {
                    throw new SolRuntimeException(context, $"Tried to call a \"{functionRaw.Type}\" value.");
                }
                SolValue[] callArgs = GetArguments(context, parentVariables);
                SolValue ret = function.Call(context, callArgs);
                return ret;
            }
            // Check 2: Are we calling a global function on any other indexable?
            IValueIndexable indexable = classInstRaw as IValueIndexable;
            if (indexable != null) {
                SolValue functionRaw;
                try {
                    functionRaw = indexable[FunctionName];
                } catch (SolVariableException ex) {
                    throw new SolRuntimeException(context, $"Failed to get function \"{FunctionName}\"  on an indexable value of type \"{classInstRaw.Type}\" - {ex.Message}");
                }
                SolFunction function = functionRaw as SolFunction;
                if (function == null) {
                    throw new SolRuntimeException(context, $"Tried to call a \"{functionRaw.Type}\" value.");
                }
                SolValue[] callArgs = GetArguments(context, parentVariables);
                function.Call(context, callArgs);
            }

            throw new SolRuntimeException(context, "Class instance functions cannot be called on " + classInstRaw.Type + " values.");*/
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