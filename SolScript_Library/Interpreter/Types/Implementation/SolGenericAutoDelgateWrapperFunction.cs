using System;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <inheritdoc cref="SolAutoDelegateWrapperFunction"/>
    public sealed class SolGenericAutoDelgateWrapperFunction<T> : SolFunction
    {
        private readonly AutoDelegate<T> m_AutoDelegate;

        /// <exception cref="SolMarshallingException">No matching SolType for the return type <typeparamref name="T"/>.</exception>
        public SolGenericAutoDelgateWrapperFunction(SolAssembly assembly, AutoDelegate<T> autoDelegate)
        {
            Assembly = assembly;
            m_AutoDelegate = autoDelegate;
            ReturnType = SolMarshal.GetSolType(assembly, typeof(T));
        }

        /// <inheritdoc />
        protected override AutoDelegate<T1> CreateAutoDelegateImpl<T1>()
        {
            if (typeof(T1) == typeof(T)) {
                return (AutoDelegate<T1>)(object)m_AutoDelegate;
            }
            return base.CreateAutoDelegateImpl<T1>();
        }

        /// <inheritdoc />
        public override SolAssembly Assembly { get; }

        /// <inheritdoc />
        public override SolParameterInfo ParameterInfo => SolAutoDelegateWrapperFunction.AnyParameters;

        /// <inheritdoc />
        public override SolType ReturnType { get; }

        /// <inheritdoc />
        public override SolSourceLocation Location => SolSourceLocation.Native();

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A native exception occured while calling the auto delegate wrapper function.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            T returnValue;
            try
            {
                returnValue = m_AutoDelegate.Invoke(args);
            }
            catch (Exception ex)
            {
                throw new SolRuntimeException(context, "A native exception occured while calling the auto delegate wrapper function.", ex);
            }
            try
            {
                return SolMarshal.MarshalFromNative(Assembly, typeof(T), returnValue);
            }
            catch (SolMarshallingException ex)
            {
                throw new SolRuntimeException(context, "Failed to marshal the return value of an auto delgate back to SolScript.", ex);
            }
        }
    }
}