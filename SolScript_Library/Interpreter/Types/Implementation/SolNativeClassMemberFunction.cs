using System;
using System.Reflection;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Utility;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     This <see cref="SolFunction" /> is used for native class functions in SolScript.
    /// </summary>
    public sealed class SolNativeClassMemberFunction : SolNativeClassFunction
    {
        /// <summary>
        ///     Creates a new native instance function from the given parameters.
        /// </summary>
        /// <param name="instance">The class instance this function belongs to.</param>
        /// <param name="definition">The definition of this function.</param>
        public SolNativeClassMemberFunction([NotNull] SolClass instance, SolFunctionDefinition definition) : base(instance, definition) {}

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured.</exception>
        /// <exception cref="InvalidOperationException">A critical internal error occured.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            SolClass.Inheritance inheritance = ClassInstance.FindInheritance(Definition.DefinedIn);
            if (inheritance == null) {
                throw new InvalidOperationException($"Internal error: Failed to find inheritance on class instance \"{ClassInstance.Type}\".");
            }
            object[] values;
            try {
                values = ParameterInfo.Marshal(context, args);
            } catch (SolMarshallingException ex) {
                throw new SolRuntimeException(context, "Could to marshal the function parameters to native objects: " + ex.Message, ex);
            }
            MethodInfo nativeMethod = Definition.Chunk.GetNativeMethod();
            object nativeObject = InternalHelper.SandboxInvokeMethod(context, Definition.Chunk.GetNativeMethod(), inheritance.NativeObject, values);
            SolValue returnValue;
            try {
                returnValue = SolMarshal.MarshalFromNative(Assembly, nativeMethod.ReturnType, nativeObject);
            } catch (SolMarshallingException ex) {
                throw new SolRuntimeException(context, "Failed to marshal the return value(Native Type: \"" + (nativeObject?.GetType().Name ?? "null") + "\") to SolScript.", ex);
            }
            return returnValue;
        }

        #endregion
    }
}