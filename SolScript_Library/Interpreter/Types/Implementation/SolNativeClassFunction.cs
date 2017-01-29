using System;
using System.Reflection;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation
{
    public class SolNativeClassFunction : SolClassFunction
    {
        public SolNativeClassFunction([NotNull] SolClass instance, SolFunctionDefinition definition)
        {
            Definition = definition;
            Instance = instance;
        }

        [NotNull] protected readonly SolClass Instance;
        public new SolParameterInfo.Native ParameterInfo => (SolParameterInfo.Native) base.ParameterInfo;

        public override SolFunctionDefinition Definition { get; }

        #region Overrides

        public override bool Equals(object other)
        {
            return other == this;
        }

        public override int GetHashCode()
        {
            unchecked {
                return 10 + (int) Id;
            }
        }

        /// <exception cref="SolRuntimeException">A runtime error occured.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            SolClass.Inheritance inheritance = Instance.FindInheritance(Definition.DefinedIn);
            if (inheritance == null) {
                throw new InvalidOperationException($"Internal error: Failed to find inheritance on class instance \"{Instance.Type}\".");
            }
            object[] values = ParameterInfo.Marshal(context, args);
            MethodInfo nativeMethod = Definition.Chunk.GetNativeMethod();
            object nativeObject;
            try {
                nativeObject = nativeMethod.Invoke(inheritance.NativeObject, values);
            } catch (TargetInvocationException ex) {
                if (ex.InnerException is SolRuntimeException) {
                    throw (SolRuntimeException)ex.InnerException;
                }
                throw new SolRuntimeException(context, "A native exception occured while calling this instance function.", ex.InnerException);
            }
            SolValue returnValue = SolMarshal.MarshalFromCSharp(Assembly, nativeMethod.ReturnType, nativeObject);
            return returnValue;
        }

        /// <summary>
        ///     Tries to convert the local value into a value of a C# type. May
        ///     return null.
        /// </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolMarshallingException"> The value cannot be converted. </exception>
        public override object ConvertTo(Type type)
        {
            if (type.IsAssignableFrom(typeof(SolNativeClassFunction))) {
                return this;
            }
            throw new SolMarshallingException("function", type);
        }

        protected override string ToString_Impl(SolExecutionContext context)
        {
            return "function#" + Id + "<class#" + (Definition.DefinedIn?.Type ?? "<ERROR>") + ">";
        }

        public override SolClassDefinition GetDefiningClass()
        {
            return Definition.DefinedIn;
        }

        #endregion
    }
}