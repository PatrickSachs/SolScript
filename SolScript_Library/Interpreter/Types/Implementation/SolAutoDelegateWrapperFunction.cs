using System;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     This function wraps an <see cref="SolFunction.AutoDelegate" />. It is only created when marshalling an
    ///     <see cref="SolFunction.AutoDelegate" /> back to SolScript.
    /// </summary>
    /// <remarks>
    ///     This is the most expensive type of function in SolScript as it marshals every value at least once even if the
    ///     function calls from SolScript to SolScript.
    /// </remarks>
    public sealed class SolAutoDelegateWrapperFunction : SolFunction
    {
        public SolAutoDelegateWrapperFunction(SolAssembly assembly, AutoDelegate autoDelegate)
        {
            Assembly = assembly;
            m_AutoDelegate = autoDelegate;
        }
        
        internal static readonly SolParameterInfo AnyParameters = new SolParameterInfo(new SolParameter[0], true);
        private readonly AutoDelegate m_AutoDelegate;

        /// <inheritdoc />
        public override SolAssembly Assembly { get; }

        /// <inheritdoc />
        /// <remarks>The function allows all types of parameters.</remarks>
        public override SolParameterInfo ParameterInfo => AnyParameters;

        /// <inheritdoc />
        public override SolType ReturnType => SolType.AnyNil;

        /// <inheritdoc />
        public override SolSourceLocation Location => SolSourceLocation.Native();

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A native exception occured while calling the auto delegate wrapper function.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            object returnValue;
            try {
                returnValue = m_AutoDelegate.Invoke(args);
            } catch (Exception ex) {
                throw new SolRuntimeException(context, "A native exception occured while calling the auto delegate wrapper function.", ex);
            }
            try {
                return SolMarshal.MarshalFromNative(Assembly, returnValue);
            } catch (SolMarshallingException ex) {
                throw new SolRuntimeException(context, "Failed to marshal the return value of an auto delgate back to SolScript.", ex);
            }
        }

        #endregion
    }
}