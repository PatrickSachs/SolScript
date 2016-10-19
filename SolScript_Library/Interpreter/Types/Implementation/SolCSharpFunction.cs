using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation {
    public class SolCSharpFunction : SolFunction {
        public SolCSharpFunction([NotNull] DynamicReference instanceRef)
            : base(new SourceLocation(0, 0, 0), new VarContext()) {
            Instance = instanceRef;
        }

        public readonly DynamicReference Instance;

        private Type[] m_MarshallTypes;
        private bool m_SendContext;
        public MethodInfo Method;

        protected override int GetHashCode_Impl() {
            unchecked {
                int hash = 10 + Method.Name.GetHashCode() + (Method.DeclaringType?.GetHashCode() ?? 0);
                foreach (Type type in m_MarshallTypes) {
                    hash += type.GetHashCode();
                }
                return hash;
            }
        }

        public static Type[] GetParameterInfoTypes(ParameterInfo[] parameterInformation, out bool outSendContext) {
            if (parameterInformation.Length > 0 &&
                parameterInformation[0].ParameterType == typeof (SolExecutionContext)) {
                // First parameter is of type SolExecutionContext
                outSendContext = true;
                return parameterInformation.Skip(1)
                    .Select(pi => pi.ParameterType)
                    .ToArray();
            }
            // Otherwise (Should be default case)
            outSendContext = false;
            return parameterInformation.Select(pi => pi.ParameterType).ToArray();
        }

        public override SolValue Call(SolValue[] args, SolExecutionContext context) {
            if (m_MarshallTypes == null) {
                m_MarshallTypes = GetParameterInfoTypes(Method.GetParameters(), out m_SendContext);
            }
            object[] values;
            if (m_SendContext) {
                // If the method wants the ExecutionContext aswell, the last parameter will be the context.
                values = new object[m_MarshallTypes.Length + 1];
                values[0] = context;
                SolMarshal.MarshalTo(args, m_MarshallTypes, values, 1);
            } else {
                values = SolMarshal.Marshal(args, m_MarshallTypes);
            }
            DynamicReference.ReferenceState referenceState;
            object reference = Instance.GetReference(out referenceState);
            switch (referenceState) {
                case DynamicReference.ReferenceState.Retrieved:
                    break;
                case DynamicReference.ReferenceState.NotRetrieved:
                    throw new SolScriptInterpreterException(Location + " : Tried to call an instance function of " +
                                                            Instance +
                                                            ", but the type has not been initialized yet. Make sure to call all __new_XYZ functions before making any instance calls.");
                default:
                    throw new ArgumentOutOfRangeException();
            }
            object clrReturn;
            try {
                // The reference may still be null at this point. However this probably means that we are working on a static class.
                clrReturn = Method.Invoke(reference, values);
            } catch (TargetInvocationException ex) {
                // todo: proper exception unwrapping
                // todo: callstack
                // todo: proper line & file info in all exceptions (comes hand in hand with a callstack)
                if (ex.InnerException != null) {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }
                throw;
            }
            return SolMarshal.MarshalFrom(Method.ReturnType.UnderlyingSystemType, clrReturn);
        }

        /// <summary> Creates a new SolFunction constructed from the passed MethodInfo. The
        ///     created function will behave in the exact same way as a "normal"
        ///     SolFunction declared in code. </summary>
        /// <param name="method"> The method info </param>
        /// <param name="instanceRef"> The dynamic reference to the invocation target
        ///     (read: the object instance to call the method on) </param>
        /// <returns> The newly created function </returns>
        [NotNull]
        public static SolCSharpFunction CreateFrom([NotNull] MethodInfo method, [NotNull] DynamicReference instanceRef) {
            // todo: create marshalling info from SolScript->C# right away. This requires double reflection and analysis of all parameters.
            var parameters = method.GetParameters();
            int cIdxOffset = 0;
            int sParamCount = parameters.Length;
            bool sAllowOptional = false;
            if (parameters.Length > 0 && parameters[0].ParameterType == typeof (SolExecutionContext)) {
                cIdxOffset += 1;
                sParamCount -= 1;
            }
            if (parameters.Length > 0 && parameters[parameters.Length - 1].ParameterType.IsArray) {
                sParamCount -= 1;
                sAllowOptional = true;
            }
            var sFuncParams = new SolParameter[sParamCount];
            for (int i = 0; i < sParamCount; i++) {
                ParameterInfo parameter = parameters[i + cIdxOffset];
                sFuncParams[i] = new SolParameter(parameter.Name, SolMarshal.GetSolType(parameter.ParameterType));
            }
            SolCSharpFunction function = new SolCSharpFunction(instanceRef) {
                Method = method,
                Parameters = sFuncParams,
                ParameterAllowOptional = sAllowOptional,
                Return = SolMarshal.GetSolType(method.ReturnType)
            };
            return function;
        }

        /// <summary> Tries to convert the local value into a value of a C# type. May
        ///     return null. </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolScriptMarshallingException"> The value cannot be converted. </exception>
        [CanBeNull]
        public override object ConvertTo(Type type) {
            if (type == typeof (SolValue) || type == typeof (SolCSharpFunction)) {
                return this;
            }
            throw new SolScriptMarshallingException("function", type);
        }

        protected override string ToString_Impl() {
            return "function<clr#" + Method.Name + ">";
        }
    }
}