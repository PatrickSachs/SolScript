using System;
using System.Reflection;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Library;

namespace SolScript.Interpreter.Types.Implementation {
    /// <summary> This type represents a constructor imported from C#. Constructors
    ///     needs a different implementation since they don't have a backing
    ///     MethodInfo(instead a ConstructorInfo) and do not simply return a SolValue,
    ///     but instead create a ClrObject which needs to be registered inside the
    ///     SolClass to that a valid object for instance access exists. </summary>
    public class SolCSharpConstructorFunction : SolCSharpClassFunction {
        public SolCSharpConstructorFunction([NotNull] SolAssembly assembly, SolType returnType, 
            bool allowOptionalParams, [NotNull] SolParameter[] parameters, [NotNull] SolClassDefinition definedIn, 
            [NotNull] SolFunctionDefinition definition, [NotNull] Type[] marshallTypes, bool sendContext) : 
            base(assembly, returnType, allowOptionalParams, parameters, definedIn, definition, marshallTypes, sendContext) {
        }

        [NotNull]
        public static new SolCSharpConstructorFunction CreateFrom([NotNull] SolClassDefinition definedIn, SolFunctionDefinition definition)
        {
            // todo: duplicate code with c# func
            ConstructorInfo constructor = definition.Creator3;
            bool sSendContext;
            ParamLink[] link = SolCSharpClassFunction.GetParameterInfoTypes(definedIn.Assembly, constructor.GetParameters(), out sSendContext);
            SolParameter[] sFuncParams = new SolParameter[link.Length];
            Type[] sMarshalTypes = new Type[link.Length];
            bool sAllowOptional = false;
            for (int i = 0; i < link.Length; i++)
            {
                ParamLink activeLink = link[i];
                SolContract paramContract = activeLink.NativeParameter.GetCustomAttribute<SolContract>();
                sMarshalTypes[i] = activeLink.NativeParameter.ParameterType;
                sFuncParams[i] = paramContract == null ? activeLink.SolParameter : new SolParameter(activeLink.SolParameter.Name, paramContract.GetSolType());
                if (i == link.Length - 1)
                {
                    sAllowOptional = activeLink.NativeParameter.GetCustomAttribute<ParamArrayAttribute>() != null;
                }
            }
            SolContract contract = constructor.GetCustomAttribute<SolContract>();
            SolType returnType = contract?.GetSolType() ?? SolMarshal.GetSolType(definedIn.Assembly, definedIn.NativeType);
            return new SolCSharpConstructorFunction(definedIn.Assembly, returnType, sAllowOptional, sFuncParams, definedIn, definition, sMarshalTypes, sSendContext);
        }

        #region Overrides

        [CanBeNull]
        public override object ConvertTo(Type type) {
            throw new NotImplementedException();
        }

        protected override string ToString_Impl([CanBeNull] SolExecutionContext context) {
            return "function#" + Id + "<native#" + Definition.Creator3.Name + ">";
        }

        protected override int GetHashCode_Impl() {
            unchecked {
                return 12 + Definition.Creator3.GetHashCode();
            }
        }

        public override SolValue Call(SolExecutionContext context, SolClass instance, params SolValue[] args) {
            SolClass.Inheritance inheritance = instance.FindInheritance(DefinedIn);
            if (inheritance == null) {
                throw SolScriptInterpreterException.InvalidTypes(context, DefinedIn.Type, instance.Type, "Cannot call the constructor on this class.");
            }
            if (instance.IsInitialized) {
                throw SolScriptInterpreterException.IllegalAccessType(context, instance.Type, "Cannot call constructor - The class is already initialized.");
            }
            object[] nativeObjects;
            if (SendContext) {
                nativeObjects = new object[MarshallTypes.Length + 1];
                SolMarshal.MarshalFromSol(args, MarshallTypes, nativeObjects, 1);
                nativeObjects[0] = context;
            } else {
                nativeObjects = SolMarshal.MarshalFromSol(args, MarshallTypes);
            }
            inheritance.NativeObject = Definition.Creator3.Invoke(nativeObjects);
            instance.IsInitialized = true;
            return instance;
        }

        #endregion
    }
}