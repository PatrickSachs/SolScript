using System;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Utility;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     This type represents a constructor imported from native code. Constructors
    ///     needs a different implementation since they don't have a backing
    ///     MethodInfo(instead a ConstructorInfo) and do not simply return a SolValue,
    ///     but instead create a NativeObject which needs to be registered inside the
    ///     SolClass to that a valid object for instance access exists.<br />If you are looking for the constructor function of
    ///     script functions: Script functions simply use a "normal" <see cref="SolScriptClassFunction" /> as constructor,
    ///     since the constructor is only a meta-function invoked upon creation of the class.
    /// </summary>
    /// <remarks>This is also the reason why native classes break if you do not invoke their constructor.</remarks>
    public sealed class SolNativeClassConstructorFunction : SolNativeClassFunction
    {
        /// <summary>
        ///     Creates a new constructor from the given parameters.
        /// </summary>
        /// <param name="instance">The class instance this function is the constructor of.</param>
        /// <param name="definition">The function definition of this constructor.</param>
        public SolNativeClassConstructorFunction([NotNull] SolClass instance, [NotNull] SolFunctionDefinition definition) : base(instance, definition) {}

        #region Overrides

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked {
                return 12 + (int) Id;
            }
        }

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured while calling the function.</exception>
        /// <exception cref="InvalidOperationException">A critical internal error occured. Execution may have to be halted.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            SolClass.Inheritance inheritance = ClassInstance.FindInheritance(Definition.DefinedIn);
            if (inheritance == null) {
                throw new SolRuntimeException(context, "Cannot call this constructor function on a class of type \"" + ClassInstance.Type + "\".");
            }
            if (ClassInstance.IsInitialized) {
                throw new SolRuntimeException(context, "Cannot call constructor of an initialized \"" + ClassInstance.Type + "\" class instance.");
            }
            object[] values;
            try {
                values = ParameterInfo.Marshal(context, args);
            } catch (SolMarshallingException ex) {
                throw new SolRuntimeException(context, "Could to marshal the function parameters to native objects: " + ex.Message, ex);
            }
            inheritance.NativeObject = InternalHelper.SandboxInvokeMethod(context, Definition.Chunk.GetNativeConstructor(), null, values);
            SolMarshal.GetAssemblyCache(Assembly).StoreReference(inheritance.NativeObject.NotNull(), ClassInstance);
            return SolNil.Instance;
        }

        #endregion
    }
}