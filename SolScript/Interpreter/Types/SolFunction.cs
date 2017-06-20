using System;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Exceptions;
using SolScript.Interpreter.Types.Marshal;

namespace SolScript.Interpreter.Types
{
    /// <summary>
    ///     This is the base class for all function in SolScript. Only use more derived type if you know exactly what you are
    ///     doing.<br />
    ///     The <see cref="SolFunction" /> class itself allows you to easily call a function with some arguments and obtain its
    ///     return value.
    /// </summary>
    public abstract class SolFunction : SolValue, ISourceLocateable
    {
        #region Delegates

        /// <summary>
        ///     A delegate used to represent SolFunctions.
        /// </summary>
        /// <param name="arguments">The arguments to the function call.</param>
        /// <returns>The return value.</returns>
        [NotNull] public delegate SolValue DirectDelegate([ItemNotNull] params SolValue[] arguments);

        #endregion

        // SolFunction may not be extended by other assemblies.
        internal SolFunction()
        {
            Id = s_NextId++;
        }

        /// <summary>
        ///     A <see cref="SolFunction" /> is always of type "function".
        /// </summary>
        public const string TYPE = "function";

        // The Id the next function will receive.
        private static uint s_NextId;

        /// <summary>
        ///     The unique Id of this function.
        /// </summary>
        public readonly uint Id;
        
        /// <summary>
        ///     The <see cref="SolAssembly" /> this function belongs to.
        /// </summary>
        public abstract SolAssembly Assembly { get; }

        /// <summary>
        ///     The parameters of this function.
        /// </summary>
        public abstract SolParameterInfo ParameterInfo { get; }

        /// <summary>
        ///     The return type of this function.
        /// </summary>
        public abstract SolType ReturnType { get; }

        /// <inheritdoc cref="TYPE" />
        public override string Type => TYPE;

        #region ISourceLocateable Members

        /// <inheritdoc />
        public abstract SourceLocation Location { get; }

        #endregion

        #region Overrides

        /// <inheritdoc />
        public override bool IsReferenceEqual(SolExecutionContext context, SolValue other)
        {
            return Id == (other as SolFunction)?.Id;
        }

        /// <inheritdoc />
        /// <remarks>One function equals another if they share the same <see cref="Id" />.</remarks>
        public override bool IsEqual(SolExecutionContext context, SolValue other)
        {
            SolFunction otherFunc = other as SolFunction;
            return otherFunc != null && Id == otherFunc.Id;
        }

        /// <inheritdoc />
        protected override string ToString_Impl(SolExecutionContext context)
        {
            return "function#" + Id;
        }

        /// <inheritdoc />
        /// <exception cref="SolMarshallingException"> The value cannot be converted. </exception>
        public override object ConvertTo(Type type)
        {
            // We have a special delegate for direct calls which saves us from having 
            // to create costly reflections stuff to build and marshal the delegate.
            if (type == typeof(DirectDelegate)) {
                return (DirectDelegate) (arguments => Call(new SolExecutionContext(Assembly, "Direct native delegate call"), arguments));
            }
            if (typeof(Delegate).IsAssignableFrom(type)) {
                return NativeDelegateMarshaller.CreateDelegate(type, this);
            }
            return base.ConvertTo(type);
        }

        #endregion

        /// <summary>
        ///     Gets the class instance of this function.
        /// </summary>
        /// <param name="isCurrent">
        ///     Should the <see cref="SolExecutionContext.CurrentClass" /> of the active context be set to this
        ///     class?
        /// </param>
        /// <param name="resetOnExit">
        ///     Should the
        ///     <see cref="SolExecutionContext.CurrentClass" /> of the execution context be reset to its previous value once
        ///     exiting this function?<br />
        ///     This can have an impact even if <paramref name="isCurrent" /> is false, in case e.g. a nested function changes the
        ///     <see cref="SolExecutionContext.CurrentClass" />.
        /// </param>
        /// <returns>The class instance. Null if none.</returns>
        [CanBeNull]
        protected abstract SolClass GetClassInstance(out bool isCurrent, out bool resetOnExit);

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
            SolClass oldClass = context.CurrentClass;
            bool isCurrent, resetOnExit;
            SolClass newClass = GetClassInstance(out isCurrent, out resetOnExit);
            if (isCurrent) {
                context.CurrentClass = newClass;
            }
            context.PushStackFrame(this);
            try {
                args = ParameterInfo.VerifyArguments(Assembly, args);
            } catch (SolVariableException ex) {
                throw SolRuntimeException.InvalidFunctionCallParameters(context, ex);
            }
            SolValue returnValue = Call_Impl(context, args);
            if (!ReturnType.IsCompatible(Assembly, returnValue.Type)) {
                throw new SolRuntimeException(context, $"Expected a return value of type \"{ReturnType}\", recceived a value of type \"{returnValue.Type}\".");
            }
            context.PopStackFrame();
            if (resetOnExit) {
                context.CurrentClass = oldClass;
            }
            return returnValue;
        }

        /// <inheritdoc cref="Call" />
        protected abstract SolValue Call_Impl([NotNull] SolExecutionContext context, [ItemNotNull] params SolValue[] args);

        /// <summary>
        ///     Inserts the given arguments into the variables. The name of the variables is used according to the ones defined in
        ///     the <see cref="ParameterInfo" />.
        /// </summary>
        /// <param name="variables">The variables to insert the arguments into.</param>
        /// <param name="arguments">The arguments to insert.</param>
        /// <remarks>
        ///     No further type/length checks will be performed. It is your responsibility to make sure that the types are of the
        ///     correct type. <br />This should typically be no big issue since the default arguments passed to the function have
        ///     already
        ///     been verified. <br />Should you override the arguments manually for some reason you may want to re-check the types
        ///     to
        ///     avoid critical bugs in user code.
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