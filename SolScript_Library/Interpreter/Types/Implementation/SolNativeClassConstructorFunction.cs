using System;
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

        #region Overrides

        public override object ConvertTo(Type type)
        {
            throw new NotImplementedException();
        }

        protected override string ToString_Impl(SolExecutionContext context)
        {
            return "function#" + Id + "<class#" + Definition.DefinedIn.NotNull().Type + ">";
        }

        public override int GetHashCode()
        {
            unchecked {
                return 12 + (int) Id;
            }
        }

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured while calling the function.</exception>
        /// <exception cref="InvalidOperationException">A critical internal error occured. Excecution may have to be halted.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            SolClass.Inheritance inheritance = Instance.FindInheritance(Definition.DefinedIn);
            if (inheritance == null) {
                throw new SolRuntimeException(context, "Cannot call this constructor function on a class of type \"" + Instance.Type + "\".");
            }
            if (Instance.IsInitialized) {
                throw new SolRuntimeException(context, "Cannot call constructor of an initialized \"" + Instance.Type + "\" class instance.");
            }
            object[] values;
            try {
                values = ParameterInfo.Marshal(context, args);
            } catch (SolMarshallingException ex) {
                throw new SolRuntimeException(context, "Could to marshal the function parameters to native objects: " + ex.Message, ex);
            }
            inheritance.NativeObject = InternalHelper.SandboxInvokeMethod(context, Definition.Chunk.GetNativeConstructor(), null, values);
            return SolNil.Instance;
        }

        #endregion
    }
}