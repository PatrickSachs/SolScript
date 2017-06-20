using System;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Exceptions;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    /// Wraps a special delegate in a function that is directly passed all arguments passed from SolScript.
    /// <br/>
    /// No marshalling of any type is done, all values are directly used.
    ///  </summary>
    public sealed class SolScriptDelegateWrapperFunction : SolFunction
    {
        /// <summary>
        /// Creates the delegate wrapper.
        /// </summary>
        /// <param name="assembly">The associated assembly.</param>
        /// <param name="fDelegate">The actual delegate.</param>
        /// <param name="parameters">The parameters of the function, or simply pass null or allow any arguments to be used.</param>
        public SolScriptDelegateWrapperFunction(
            SolAssembly assembly, 
            DirectDelegate fDelegate, 
            [CanBeNull] SolParameterInfo parameters = null)
        {
            m_Delegate = fDelegate;
            Assembly = assembly;
            ParameterInfo = parameters ?? SolParameterInfo.Any;
        }

        private readonly DirectDelegate m_Delegate;

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
        /// <exception cref="SolMarshallingException"> The value cannot be converted. </exception>
        public override object ConvertTo(Type type)
        {
            // Directly return the delegate to avoid nesting delegates in delegates.
            if (type == typeof(DirectDelegate)) {
                return m_Delegate;
            }
            return base.ConvertTo(type);
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