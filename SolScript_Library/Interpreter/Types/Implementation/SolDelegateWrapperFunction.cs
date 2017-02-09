using System;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation
{
    public sealed class SolDelegateWrapperFunction : SolFunction
    {
        public SolDelegateWrapperFunction(SolAssembly assembly, Delegate fDelegate)
        {
            m_Delegate = fDelegate;
            Assembly = assembly;
        }

        private readonly Delegate m_Delegate;

        /// <inheritdoc />
        public override SolAssembly Assembly { get; }

        /// <inheritdoc />
        public override SolParameterInfo ParameterInfo => SolAutoDelegateWrapperFunction.AnyParameters;

        /// <inheritdoc />
        public override SolType ReturnType => SolType.AnyNil;

        /// <inheritdoc />
        public override SolSourceLocation Location => SolSourceLocation.Native();

        #region Overrides

        /// <inheritdoc />
        public override Delegate CreateDelegate()
        {
            return m_Delegate;
        }

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">An exception occured while calling a delegate wrapper function.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            try {
                return m_Delegate.Invoke(args);
            } catch (Exception ex) {
                throw new SolRuntimeException(context, "An exception occured while calling a delegate wrapper function.", ex);
            }
        }

        #endregion
    }
}