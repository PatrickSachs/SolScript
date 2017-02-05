using System;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types
{
    public abstract class SolFunction : SolValue, ISourceLocateable
    {
        protected SolFunction()
        {
            Id = s_NextId++;
        }

        public const string TYPE = "function";

        // public IVariables ParentVariables;
        private static uint s_NextId;
        public readonly uint Id;

        public abstract SolAssembly Assembly { get; }
        public abstract SolParameterInfo ParameterInfo { get; }
        public abstract SolType ReturnType { get; }

        public override string Type => TYPE;

        #region ISourceLocateable Members

        public abstract SolSourceLocation Location { get; }

        #endregion

        #region Overrides

        public override bool IsEqual(SolExecutionContext context, SolValue other)
        {
            SolFunction otherFunc = other as SolFunction;
            return otherFunc != null && Id == otherFunc.Id;
        }

        #endregion

        /// <summary>
        ///     Calls the function.
        /// </summary>
        /// <param name="context">The exceution context to call the function in.</param>
        /// <param name="args">The arguments for the function. The arguments will be verified by the function.</param>
        /// <returns>The return value of the function.</returns>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        [NotNull]
        public SolValue Call([NotNull] SolExecutionContext context, [ItemNotNull] params SolValue[] args)
        {
            context.PushStackFrame(this);
            try {
                ParameterInfo.VerifyArguments(Assembly, args);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, ex.Message, ex);
            }
            SolValue returnValue = Call_Impl(context, args);
            context.PopStackFrame();
            if (!ReturnType.IsCompatible(Assembly, returnValue.Type)) {
                throw new SolRuntimeException(context, $"Expected a return value of type '\"{ReturnType}\", recceived a value of type \"{returnValue.Type}\".");
            }
            return returnValue;
        }

        /// <inheritdoc cref="Call"/>
        protected abstract SolValue Call_Impl([NotNull] SolExecutionContext context, [ItemNotNull] params SolValue[] args);

        /// <summary>
        ///     Insers the given arguments into the variables. The name of the variables is used according to the ones defined in
        ///     the <see cref="ParameterInfo" />.
        /// </summary>
        /// <param name="variables">The variables to insert the arguments into.</param>
        /// <param name="arguments">The arguments to insert.</param>
        /// <remarks>
        ///     No further type/length checks will be performed. It is your responsibility to make sure that the types are of the
        ///     correct
        ///     type. <br />This should typically be no big issue since the default arguments passed to the function have already
        ///     been
        ///     verified. <br />Should you override the arguments manually for some reason you may want to re-check the types to
        ///     avoid
        ///     crtical bugs in user code.
        /// </remarks>
        /// <exception cref="SolVariableException">An error occured during insertion.</exception>
        protected virtual void InsertParameters(IVariables variables, SolValue[] arguments)
        {
            for (int i = 0; i < ParameterInfo.Count; i++) {
                SolParameter parameter = ParameterInfo[i];
                variables.Declare(parameter.Name, parameter.Type);
                variables.Assign(parameter.Name, arguments.Length > i ? arguments[i] : SolNil.Instance);
            }
            // Additional arguments
            if (ParameterInfo.AllowOptional) {
                variables.Declare("args", new SolType("table", false));
                SolTable argsTable = new SolTable();
                for (int i = ParameterInfo.Count; i < arguments.Length; i++) {
                    argsTable.Append(arguments[i]);
                }
                variables.Assign("args", argsTable);
            }
        }
    }
}