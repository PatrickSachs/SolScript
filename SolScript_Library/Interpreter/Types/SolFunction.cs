using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types.Implementation;

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

        public delegate object AutoDelegate(params object[] arguments);

        public delegate T AutoDelegate<out T>(params object[] arguments);

        /// <summary>
        ///     A delegate used to represent SolFunctions.
        /// </summary>
        /// <param name="arguments">The arguments to the function call.</param>
        /// <returns>The return value.</returns>
        public delegate SolValue Delegate(params SolValue[] arguments);

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

        // The Id the next function will recceive.
        private static uint s_NextId;

        private static readonly MethodInfo s_CreateAutoDelegateMethod =
            typeof(SolFunction).GetMethods(BindingFlags.Instance | BindingFlags.Public).First(m => m.Name == nameof(CreateAutoDelegate) && m.GetGenericArguments().Length == 1);

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
        public abstract SolSourceLocation Location { get; }

        #endregion

        #region Overrides

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
            if (type == typeof(Delegate)) {
                return CreateDelegate();
            }
            if (type == typeof(AutoDelegate)) {
                return CreateAutoDelegate(SolMarshal.GetNativeType(Assembly, ReturnType.Type));
            }
            if (type.IsGenericType) {
                Type openGenericType = type.GetGenericTypeDefinition();
                if (openGenericType == typeof(AutoDelegate<>)) {
                    return s_CreateAutoDelegateMethod.MakeGenericMethod(type.GetGenericArguments()).Invoke(this, new object[0]);
                }
            }
            return base.ConvertTo(type);
        }

        #endregion

        /// <summary>
        ///     A dummy function, doing nothing once called. Accepts any parameter, returns nothing(thus nil).
        /// </summary>
        /// <param name="assembly">The assembly this function belongs to.</param>
        /// <returns>The dummy function.</returns>
        public static SolFunction Dummy(SolAssembly assembly)
            => new SolScriptLamdaFunction(assembly, SolSourceLocation.Native(), SolParameterInfo.Any, SolType.AnyNil, new SolChunk(assembly, SolSourceLocation.Native(),  null), null);

        /// <summary>
        ///     Creates a delegate you can use to call the function.
        /// </summary>
        /// <returns>The delegate.</returns>
        /// <remarks>
        ///     Keep in mind that the preferred way of calling a function is using the <see cref="Call" /> method. If you are
        ///     unsure about the <see cref="SolExecutionContext" /> parameter just create a new one.
        /// </remarks>
        public virtual Delegate CreateDelegate()
        {
            return delegate(SolValue[] arguments) { return Call(new SolExecutionContext(Assembly, "Native delegate call"), arguments); };
        }

        /// <typeparam name="T">The desired return type of the function.</typeparam>
        /// <inheritdoc cref="CreateAutoDelegate" />
        public virtual AutoDelegate<T> CreateAutoDelegate<T>()
        {
            return delegate(object[] arguments) {
                SolValue[] solArguments = SolMarshal.MarshalFromNative(Assembly, arguments);
                SolValue returnValue = Call(new SolExecutionContext(Assembly, "Native auto delegate call"), solArguments);
                return (T) SolMarshal.MarshalFromSol(returnValue, typeof(T));
            };
        }

        /// <summary>
        ///     Ceates a delegate you can use to the function. The passed arguments will automatically be marshalled to the types
        ///     required by the function.
        /// </summary>
        /// <param name="returnType">The desired return type of the function.</param>
        /// <returns>The delegate.</returns>
        /// <remarks>
        ///     Keep in mind that the preferred way of calling a function is using the <see cref="Call" /> method. If you are
        ///     unsure about the <see cref="SolExecutionContext" /> parameter just create a new one.<br />Also make sure to check
        ///     for <see cref="SolMarshallingException" />s whenever calling this function. They are thrown whenevr your arguments
        ///     could not be converted into the ones required by this function.
        /// </remarks>
        public virtual AutoDelegate CreateAutoDelegate(Type returnType)
        {
            return delegate(object[] arguments) {
                SolValue[] solArguments = SolMarshal.MarshalFromNative(Assembly, arguments);
                SolValue returnValue = Call(new SolExecutionContext(Assembly, "Native auto delegate call"), solArguments);
                return SolMarshal.MarshalFromSol(returnValue, returnType);
            };
        }

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
                args = ParameterInfo.VerifyArguments(Assembly, args);
            } catch (SolVariableException ex) {
                throw SolRuntimeException.InvalidFunctionCallParameters(context, ex);
            }
            SolValue returnValue = Call_Impl(context, args);
            if (!ReturnType.IsCompatible(Assembly, returnValue.Type)) {
                throw new SolRuntimeException(context, $"Expected a return value of type \"{ReturnType}\", recceived a value of type \"{returnValue.Type}\".");
            }
            context.PopStackFrame();
            return returnValue;
        }

        /// <inheritdoc cref="Call" />
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