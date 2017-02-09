using System;
using System.Reflection;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     This class is used for native global functions.
    /// </summary>
    public sealed class SolNativeGlobalFunction : DefinedSolFunction
    {
        /// <summary>
        ///     Creates the function instance.
        /// </summary>
        /// <param name="definition">The function definiton.</param>
        /// <param name="instance">The reference to the native object to invoke the function on.</param>
        /// <seealso cref="DynamicReference"/>
        public SolNativeGlobalFunction(SolFunctionDefinition definition, DynamicReference instance)
        {
            Definition = definition;
            m_Instance = instance;
        }

        // The native object to invoke the function on. Typically a null reference unless manually created by the user.
        private readonly DynamicReference m_Instance;

        /// <inheritdoc />
        public override SolFunctionDefinition Definition { get; }

        /// <inheritdoc cref="SolFunction.ParameterInfo" />
        public new SolParameterInfo.Native ParameterInfo => (SolParameterInfo.Native) base.ParameterInfo;

        #region Overrides

        /// <inheritdoc />
        public override object ConvertTo(Type type)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked {
                return 13 + (int) Id;
            }
        }

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured.</exception>
        /// <exception cref="InvalidOperationException">A critical internal error occured. Excecution may have to be halted.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            object[] values;
            try {
                values = ParameterInfo.Marshal(context, args);
            } catch (SolMarshallingException ex) {
                throw new SolRuntimeException(context, "Could to marshal the function parameters to native objects: " + ex.Message, ex);
            }
            MethodInfo nativeMethod = Definition.Chunk.GetNativeMethod();
            object nativeObject;
            DynamicReference.GetState getState;
            object nativeInstance = m_Instance.GetReference(out getState);
            switch (getState) {
                case DynamicReference.GetState.Retrieved:
                    nativeObject = InternalHelper.SandboxInvokeMethod(context, nativeMethod, nativeInstance, values);
                    break;
                case DynamicReference.GetState.NotRetrieved:
                    throw new InvalidOperationException($"The internal reference of the native method {nativeMethod.Name}({nativeMethod.DeclaringType?.FullName ?? "?"}) could not be resolved.");
                default:
                    throw new ArgumentOutOfRangeException();
            }
            SolValue returnValue = SolMarshal.MarshalFromNative(Assembly, nativeMethod.ReturnType, nativeObject);
            return returnValue;
        }

        /// <inheritdoc />
        public override bool Equals(object other)
        {
            return other == this;
        }

        #endregion
    }
}