using System;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Exceptions;

namespace SolScript.Interpreter.Types.Implementation
{
    public sealed class SolDelegateWrapperFunction : SolFunction
    {
        public SolDelegateWrapperFunction(SolAssembly assembly, Delegate fDelegate, [CanBeNull] SolParameterInfo parameters = null)
        {
            m_Delegate = fDelegate;
            Assembly = assembly;
            ParameterInfo = parameters ?? SolParameterInfo.Any;
        }

        private readonly Delegate m_Delegate;

        /// <inheritdoc />
        public override SolAssembly Assembly { get; }

        /// <inheritdoc />
        public override SolParameterInfo ParameterInfo { get; }

        /// <inheritdoc />
        public override SolType ReturnType => SolType.AnyNil;

        /// <inheritdoc />
        public override SourceLocation Location => SolSourceLocation.Native();

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

        /// <inheritdoc />
        protected override SolClass GetClassInstance(out bool isCurrent, out bool resetOnExit)
        {
            isCurrent = false;
            resetOnExit = false;
            return null;
        }

        #endregion
    }
}