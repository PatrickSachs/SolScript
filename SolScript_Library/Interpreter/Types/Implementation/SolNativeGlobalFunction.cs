using System;
using System.Reflection;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Library;

namespace SolScript.Interpreter.Types.Implementation
{
    public class SolNativeGlobalFunction : DefinedSolFunction
    {
        public SolNativeGlobalFunction(SolFunctionDefinition definition, DynamicReference instance)
        {
            Definition = definition;
            m_Instance = instance;
        }

        private readonly DynamicReference m_Instance;
        public override SolFunctionDefinition Definition { get; }
        public new SolParameterInfo.Native ParameterInfo => (SolParameterInfo.Native) base.ParameterInfo;

        #region Overrides

        /// <summary>
        ///     Tries to convert the local value into a value of a C# type. May
        ///     return null.
        /// </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolMarshallingException"> The value cannot be converted. </exception>
        public override object ConvertTo(Type type)
        {
            throw new NotImplementedException();
        }

        protected override string ToString_Impl(SolExecutionContext context)
        {
            return "function#" + Id + "<global>";
        }

        public override int GetHashCode()
        {
            unchecked {
                return 13 + (int) Id;
            }
        }

        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            object[] values = ParameterInfo.Marshal(context, args);
            MethodInfo nativeMethod = Definition.Chunk.GetNativeMethod();
            object nativeObject;
            DynamicReference.GetState getState;
            object nativeInstance = m_Instance.GetReference(out getState);
            switch (getState) {
                case DynamicReference.GetState.Retrieved:
                    try {
                        nativeObject = nativeMethod.Invoke(nativeInstance, values);
                    } catch (TargetInvocationException ex) {
                        if (ex.InnerException is SolRuntimeException) {
                            throw ex.InnerException;
                        }
                        throw new SolRuntimeException(context, "A native exception occured while calling this global function.", ex.InnerException);
                    }
                    break;
                case DynamicReference.GetState.NotRetrieved:
                    throw new InvalidOperationException($"The internal reference of the native method {nativeMethod.Name}({nativeMethod.DeclaringType?.FullName ?? "?"}) could not be resolved.");
                default:
                    throw new ArgumentOutOfRangeException();
            }
            SolValue returnValue = SolMarshal.MarshalFromCSharp(Assembly, nativeMethod.ReturnType, nativeObject);
            return returnValue;
        }

        public override bool Equals(object other)
        {
            return other == this;
        }

        #endregion
    }
}