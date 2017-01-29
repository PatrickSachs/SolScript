using System;
using System.Reflection;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     This type represents a constructor imported from C#. Constructors
    ///     needs a different implementation since they don't have a backing
    ///     MethodInfo(instead a ConstructorInfo) and do not simply return a SolValue,
    ///     but instead create a ClrObject which needs to be registered inside the
    ///     SolClass to that a valid object for instance access exists.
    /// </summary>
    public class SolNativeClassConstructorFunction : SolNativeClassFunction
    {
        public SolNativeClassConstructorFunction([NotNull] SolClass instance, [NotNull] SolFunctionDefinition definition) : base(instance, definition) {}

        //public new SolParameterInfo.Native ParameterInfo => (SolParameterInfo.Native) base.ParameterInfo;

        #region Overrides

        public override object ConvertTo(Type type)
        {
            throw new NotImplementedException();
        }

        protected override string ToString_Impl(SolExecutionContext context)
        {
            return "function#" + Id + "<class#" + Definition.DefinedIn.Type + ">";
        }

        public override int GetHashCode()
        {
            unchecked {
                return 12 + (int) Id;
            }
        }

        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            SolClass.Inheritance inheritance = Instance.FindInheritance(Definition.DefinedIn);
            if (inheritance == null) {
                throw new SolRuntimeException(context, "Cannot call this constructor function on a class of type \"" + Instance.Type + "\".");
            }
            if (Instance.IsInitialized) {
                throw new SolRuntimeException(context, "Cannot call constructor of an initialized \"" + Instance.Type + "\" class instance.");
            }
            object[] nativeObjects;
            if (ParameterInfo.AllowOptional) {
                nativeObjects = new object[ParameterInfo.Count + 1];
                SolMarshal.MarshalFromSol(Assembly, args, ParameterInfo.NativeTypes, nativeObjects, 1);
                nativeObjects[0] = context;
            } else {
                nativeObjects = SolMarshal.MarshalFromSol(Assembly, args, ParameterInfo.NativeTypes);
            }
            try {
                inheritance.NativeObject = Definition.Chunk.GetNativeConstructor().Invoke(nativeObjects);
            } catch (TargetInvocationException ex) {
                if (ex.InnerException is SolRuntimeException) {
                    throw ex.InnerException;
                }
                throw new SolRuntimeException(context, "A native exception occured while calling this constructor function.", ex.InnerException);
            }
            return Instance;
        }

        #endregion
    }
}