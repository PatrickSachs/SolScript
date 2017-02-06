using System;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     This type represents a constructor imported from native code. Constructors
    ///     needs a different implementation since they don't have a backing
    ///     MethodInfo(instead a ConstructorInfo) and do not simply return a SolValue,
    ///     but instead create a NativeObject which needs to be registered inside the
    ///     SolClass to that a valid object for instance access exists.<br />If you are looking for the constrcutor function of
    ///     script functions: Script functions simply use a "normal" <see cref="SolScriptClassFunction" /> as constrcutor,
    ///     since the constructor is only a meta-function invoked upon creation of the class.
    /// </summary>
    /// <remarks>This is also the reason why native classes break if you do not invoke their constrcutor.</remarks>
    // todo: investigate proving a way to create native classes without having to invoke their SolScript constrcutor(unlikely though).
    public sealed class SolNativeClassConstructorFunction : SolNativeClassFunction
    {
        /// <summary>
        ///     Creates a new Constrcutor from the given parameters.
        /// </summary>
        /// <param name="instance">The class instance this function is the consturcor of.</param>
        /// <param name="definition">The function definition of this constrcutor.</param>
        public SolNativeClassConstructorFunction([NotNull] SolClass instance, [NotNull] SolFunctionDefinition definition) : base(instance, definition) {}

        #region Overrides

        /// <inheritdoc />
        public override object ConvertTo(Type type)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        protected override string ToString_Impl(SolExecutionContext context)
        {
            return "function#" + Id + "<class#" + Definition.DefinedIn.NotNull().Type + ">";
        }

        /// <inheritdoc />
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
            return SolNil.Instance;
        }

        #endregion
    }
}