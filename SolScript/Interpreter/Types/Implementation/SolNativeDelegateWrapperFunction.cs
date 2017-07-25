using System;
using Irony.Parsing;
using NodeParser;
using SolScript.Exceptions;
using SolScript.Utility;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     Wraps a native delegate in a SolScript function. The usage of this delegate is most likely the most intuitive one.
    ///     <br /><br />
    ///     How does this work?
    ///     <br />
    ///     When calling the function call arguments of the function are marshalled to the parameters required by the delegate.
    ///     Afterwards the return value is marshalled back to its respective SolScript representation.
    ///     <br />
    ///     This is actually the same process that is done internally when calling a native method from SolScript.
    ///     <br /><br />
    ///     If you wish to directly obtain the SolScript values take a look at the
    ///     <see cref="SolScriptDelegateWrapperFunction" /> class.
    /// </summary>
    public sealed class SolNativeDelegateWrapperFunction : SolFunction
    {
        /// <summary>
        ///     Creates the auto delegate wrapper around the givem delegate.
        /// </summary>
        /// <param name="assembly">The assembly the function belongs to. Required when looking up classes when marshalling.</param>
        /// <param name="del">Any delegate. The types are determines by the return/parameters types of this delegate.</param>
        /// <exception cref="SolMarshallingException">Failed to marshal a parameter type.</exception>
        public SolNativeDelegateWrapperFunction(SolAssembly assembly, Delegate del)
        {
            Assembly = assembly;
            m_Delegate = del;
            try {
                m_ParameterInfo = InternalHelper.GetParameterInfo(assembly, del.Method.GetParameters());
            } catch (MemberAccessException ex) {
                throw new SolMarshallingException("Could not access the inner method of the \"" + del.GetType() + "\" delegate.", ex);
            }
        }

        // The wrapper delegate.
        private readonly Delegate m_Delegate;
        // The parameters.
        private readonly SolParameterInfo.Native m_ParameterInfo;

        /// <inheritdoc />
        public override SolAssembly Assembly { get; }

        /// <inheritdoc />
        public override NodeLocation Location => SolSourceLocation.Native();

        /// <summary>
        ///     The type returned by this function.
        /// </summary>
        public Type NativeReturnType => m_Delegate.Method.ReturnType;

        /// <inheritdoc />
        /// <remarks>The function allows all types of parameters.</remarks>
        public override SolParameterInfo ParameterInfo => m_ParameterInfo;

        /// <inheritdoc />
        public override SolType ReturnType => SolType.AnyNil;

        /// <inheritdoc />
        /// <exception cref="SolMarshallingException"> The value cannot be converted. </exception>
        public override object ConvertTo(Type type)
        {
            // Directly return the delegate to avoid nesting delegates in delegates.
            if (type.IsInstanceOfType(m_Delegate)) {
                return m_Delegate;
            }
            return base.ConvertTo(type);
        }

        #region Overrides

        /// <inheritdoc />
        public override IClassLevelLink DefinedIn => null;

        /*/// <inheritdoc />
        protected override SolClass GetClassInstance(out bool isCurrent, out bool resetOnExit)
        {
            isCurrent = false;
            resetOnExit = false;
            return null;
        }*/

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A native exception occured while calling the auto delegate wrapper function.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            object[] marshalled;
            try {
                marshalled = m_ParameterInfo.Marshal(context, args);
            } catch (SolMarshallingException ex) {
                throw new SolRuntimeException(context, "Failed to marshal the arguments of an auto delegate function.", ex);
            }
            object returnValue;
            try {
                returnValue = m_Delegate.DynamicInvoke(marshalled);
            } catch (Exception ex) {
                throw new SolRuntimeException(context, "A native exception occured while calling the auto delegate wrapper function.", ex);
            }
            try {
                return SolMarshal.MarshalFromNative(Assembly, NativeReturnType, returnValue);
            } catch (SolMarshallingException ex) {
                throw new SolRuntimeException(context, "Failed to marshal the return value of an auto delgate back to SolScript.", ex);
            }
        }

        #endregion
    }
}