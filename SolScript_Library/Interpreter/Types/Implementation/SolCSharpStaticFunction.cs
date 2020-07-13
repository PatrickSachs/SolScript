using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Library;

namespace SolScript.Interpreter.Types.Implementation {
    public class SolCSharpStaticFunction : SolFunction {
        private SolCSharpStaticFunction([NotNull] SolAssembly assembly, SolType returnType,
            MethodInfo method, DynamicReference instance, bool sendContext, bool allowOptionalParams,
            [ItemNotNull] Type[] marshalTypes, [ItemNotNull] SolParameter[] parameters)
            : base(assembly, SourceLocation.Empty, returnType, allowOptionalParams, parameters) {
            m_Method = method;
            m_Instance = instance;
            m_SendContext = sendContext;
            m_MarshalTypes = marshalTypes;
        }

        private readonly DynamicReference m_Instance;
        private readonly Type[] m_MarshalTypes;

        private readonly MethodInfo m_Method;
        private readonly bool m_SendContext;

        #region Overrides

        /// <summary> Tries to convert the local value into a value of a C# type. May
        ///     return null. </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolScriptMarshallingException"> The value cannot be converted. </exception>
        [CanBeNull]
        public override object ConvertTo(Type type) {
            throw new NotImplementedException();
        }

        protected override string ToString_Impl([CanBeNull] SolExecutionContext context) {
            return "function" + Id + "<native#" + m_Method.Name + ">";
        }

        protected override int GetHashCode_Impl() {
            return 0;
        }

        public override SolValue Call(SolExecutionContext context, SolClass instance, params SolValue[] args) {
            object[] values;
            if (m_SendContext) {
                values = new object[m_MarshalTypes.Length + 1];
                SolMarshal.MarshalFromSol(args, m_MarshalTypes, values, 1);
                values[0] = context;
            } else {
                values = SolMarshal.MarshalFromSol(args, m_MarshalTypes);
            }
            object nativeObject;
            try {
                DynamicReference.GetState getState;
                object nativeInstance = m_Instance.GetReference(out getState);
                switch (getState) {
                    case DynamicReference.GetState.Retrieved:
                        nativeObject = m_Method.Invoke(nativeInstance, values);
                        break;
                    case DynamicReference.GetState.NotRetrieved:
                        throw new InvalidOperationException("The internal reference of the function " + m_Method.Name + "(" + (m_Method.DeclaringType?.FullName) + ") could not be resolved.");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            } catch (TargetInvocationException ex) {
                // todo: proper exception unwrapping
                // todo: callstack
                // todo: proper line & file info in all exceptions (comes hand in hand with a callstack)
                if (ex.InnerException != null) {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }
                throw;
            }
            return SolMarshal.MarshalFromCSharp(Assembly, m_Method.ReturnType, nativeObject);
        }

        #endregion

        public static SolCSharpStaticFunction CreateFrom(SolAssembly assembly, MethodInfo method, DynamicReference instance) {
            // todo: duplicate code with c# class func
            bool sSendContext;
            var link = SolCSharpClassFunction.GetParameterInfoTypes(assembly, method.GetParameters(), out sSendContext);
            var sFuncParams = new SolParameter[link.Length];
            var sMarshalTypes = new Type[link.Length];
            bool sAllowOptional = false;
            for (int i = 0; i < link.Length; i++) {
                SolCSharpClassFunction.ParamLink activeLink = link[i];
                SolContract paramContract = activeLink.NativeParameter.GetCustomAttribute<SolContract>();
                sMarshalTypes[i] = activeLink.NativeParameter.ParameterType;
                sFuncParams[i] = paramContract == null ? activeLink.SolParameter : new SolParameter(activeLink.SolParameter.Name, paramContract.GetSolType());
                if (i == link.Length - 1) {
                    sAllowOptional = activeLink.NativeParameter.GetCustomAttribute<ParamArrayAttribute>() != null;
                }
            }
            SolContract contract = method.GetCustomAttribute<SolContract>();
            SolType returnType = contract?.GetSolType() ?? SolMarshal.GetSolType(assembly, method.ReturnType);
            return new SolCSharpStaticFunction(assembly, returnType, method, instance, sSendContext, sAllowOptional, sMarshalTypes, sFuncParams);
        }
    }
}