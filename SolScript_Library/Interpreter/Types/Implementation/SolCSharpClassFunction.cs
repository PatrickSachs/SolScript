using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Library;

namespace SolScript.Interpreter.Types.Implementation {
    public class SolCSharpClassFunction : SolFunction {
        public SolCSharpClassFunction([NotNull] SolAssembly assembly, SolType returnType, bool allowOptionalParams, [NotNull] SolParameter[] parameters, SolClassDefinition definedIn,
            SolFunctionDefinition definition, Type[] marshallTypes, bool sendContext) : base(assembly, SourceLocation.Empty, returnType, allowOptionalParams, parameters) {
            DefinedIn = definedIn;
            Definition = definition;
            MarshallTypes = marshallTypes;
            SendContext = sendContext;
        }

        protected readonly SolClassDefinition DefinedIn;
        protected readonly SolFunctionDefinition Definition;
        protected readonly Type[] MarshallTypes;
        protected readonly bool SendContext;

        #region Overrides

        protected override int GetHashCode_Impl() {
            unchecked {
                int hash = 10 + Definition.Creator2.Name.GetHashCode() + (Definition.Creator2.DeclaringType?.GetHashCode() ?? 0);
                foreach (Type type in MarshallTypes) {
                    hash += type.GetHashCode();
                }
                return hash;
            }
        }

        public override SolValue Call(SolExecutionContext context, SolClass instance, params SolValue[] args) {
            SolClass.Inheritance inheritance = instance.FindInheritance(DefinedIn);
            if (inheritance == null) {
                throw SolScriptInterpreterException.InvalidTypes(context, DefinedIn.Type, instance.Type, "Cannot call the function on this class.");
            }
            object[] values;
            if (SendContext) {
                values = new object[MarshallTypes.Length + 1];
                SolMarshal.MarshalFromSol(args, MarshallTypes, values, 1);
                values[0] = context;
            } else {
                values = SolMarshal.MarshalFromSol(args, MarshallTypes);
            }
            object nativeObject;
            try {
                nativeObject = Definition.Creator2.Invoke(inheritance.NativeObject, values);
            } catch (TargetInvocationException ex) {
                // todo: proper exception unwrapping
                // todo: callstack
                // todo: proper line & file info in all exceptions (comes hand in hand with a callstack)
                if (ex.InnerException != null) {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }
                throw;
            }
            return SolMarshal.MarshalFromCSharp(Assembly, Definition.Creator2.ReturnType, nativeObject);
        }

        /// <summary> Tries to convert the local value into a value of a C# type. May
        ///     return null. </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolScriptMarshallingException"> The value cannot be converted. </exception>
        [CanBeNull]
        public override object ConvertTo(Type type) {
            if (type == typeof (SolValue) || type == typeof (SolCSharpClassFunction)) {
                return this;
            }
            throw new SolScriptMarshallingException("function", type);
        }

        protected override string ToString_Impl([CanBeNull] SolExecutionContext context) {
            return "function" + Id + "<native#"+DefinedIn.NativeType.Name+"#" + Definition.Creator2.Name + ">";
        }

        #endregion

        internal static ParamLink[] GetParameterInfoTypes(SolAssembly assembly, ParameterInfo[] parameterInformation, out bool outSendContext) {
            if (parameterInformation.Length > 0 && parameterInformation[0].ParameterType == typeof (SolExecutionContext)) {
                // First parameter is of type SolExecutionContext
                outSendContext = true;
                return parameterInformation.Skip(1).Select(pi => new ParamLink(pi, new SolParameter(pi.Name, SolMarshal.GetSolType(assembly, pi.ParameterType)))).ToArray();
            }
            // Otherwise (Should be default case)
            outSendContext = false;
            return parameterInformation.Select(pi => new ParamLink(pi, new SolParameter(pi.Name, SolMarshal.GetSolType(assembly, pi.ParameterType)))).ToArray();
        }

        /// <summary> Creates a new SolFunction constructed from the passed MethodInfo. The
        ///     created function will behave in the exact same way as a "normal"
        ///     SolFunction declared in code. </summary>
        /// <param name="method"> The method info </param>
        /// <param name="instanceRef"> The dynamic reference to the invocation target
        ///     (read: the object instance to call the method on) </param>
        /// <returns> The newly created function </returns>
        [NotNull]
        public static SolCSharpClassFunction CreateFrom([NotNull] SolClassDefinition definedIn, SolFunctionDefinition definition) {
            // todo: duplicate code with c# ctor
            MethodInfo method = definition.Creator2;
            bool sSendContext;
            var link = GetParameterInfoTypes(definedIn.Assembly, method.GetParameters(), out sSendContext);
            var sFuncParams = new SolParameter[link.Length];
            var sMarshalTypes = new Type[link.Length];
            bool sAllowOptional = false;
            for (int i = 0; i < link.Length; i++) {
                ParamLink activeLink = link[i];
                SolContract paramContract = activeLink.NativeParameter.GetCustomAttribute<SolContract>();
                sMarshalTypes[i] = activeLink.NativeParameter.ParameterType;
                sFuncParams[i] = paramContract == null ? activeLink.SolParameter : new SolParameter(activeLink.SolParameter.Name, paramContract.GetSolType());
                if (i == link.Length - 1) {
                    sAllowOptional = activeLink.NativeParameter.GetCustomAttribute<ParamArrayAttribute>() != null;
                }
            }
            SolContract contract = method.GetCustomAttribute<SolContract>();
            SolType returnType = contract?.GetSolType() ?? SolMarshal.GetSolType(definedIn.Assembly, method.ReturnType);
            return new SolCSharpClassFunction(definedIn.Assembly, returnType, sAllowOptional, sFuncParams, definedIn, definition, sMarshalTypes, sSendContext);
        }

        #region Nested type: ParamLink

        internal struct ParamLink {
            public readonly ParameterInfo NativeParameter;
            public readonly SolParameter SolParameter;

            public ParamLink(ParameterInfo nativeParameter, SolParameter solParameter) {
                SolParameter = solParameter;
                NativeParameter = nativeParameter;
            }
        }

        #endregion
    }
}