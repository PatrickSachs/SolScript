using JetBrains.Annotations;
using SevenBiT.Inspector;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    public enum VariableGet
    {
        Success,
        FailedNotDeclared,
        FailedNotAssigned,
        FailedNativeError
    }

    /// <summary> Interface all variable lookups must implement. </summary>
    public interface IVariables
    {
        /// <summary> The assembly this variable lookup belongs to. </summary>
        [NotNull]
        SolAssembly Assembly { get; }

        /// <summary> The parent variables of these variables. </summary>
        [CanBeNull]
        IVariables Parent { get; set; }

        /// <summary> Gets the value assigned to the given name. </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <exception cref="SolVariableException"> The value has not been declared. </exception>
        [NotNull]
        SolValue Get([NotNull] string name);

        /// <summary>
        ///     Tries to get the value assigned to the given name. The result is only
        ///     valid if the method returned <see cref="VariableGet.Success" />.
        /// </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="value"> A pointer to where the variable should be saved. </param>
        VariableGet TryGet([NotNull] string name, [CanBeNull] out SolValue value);

        /// <summary>
        ///     Declares the value with the given name and type and also provides
        ///     some (optional) annotations.
        /// </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="type">
        ///     The type of the variable. Only values assignable to this
        ///     type can be assigned.
        /// </param>
        /// <exception cref="SolVariableException">
        ///     A variable with this name has already been declared.
        /// </exception>
        void Declare([NotNull] string name, SolType type);

        /// <summary> Declares a native variable. </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="type">
        ///     The type of the variable. Only values assignable to this
        ///     type can be assigned.
        /// </param>
        /// <param name="field"> The native field handle. </param>
        /// <param name="fieldReference"> The reference to the native object handle. </param>
        /// <exception cref="SolVariableException">
        ///     A variable with this name has already been declared.
        /// </exception>
        void DeclareNative([NotNull] string name, SolType type, [NotNull] InspectorField field, [NotNull] DynamicReference fieldReference);

        /// <summary> Assigns annotations to a given variable. </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="annotations"> The annotations to assign to the variable. </param>
        void AssignAnnotations([NotNull] string name, [ItemNotNull] params SolClass[] annotations);

        /// <summary> Assigns a value to the variable with the given name. </summary>
        /// <exception cref="SolVariableException">
        ///     Np variable with this name has been
        ///     decalred.
        /// </exception>
        /// <exception cref="SolVariableException"> The type does not match. </exception>
        void Assign([NotNull] string name, [NotNull] SolValue value);

        /// <summary> Is a variable with this name declared? </summary>
        bool IsDeclared([NotNull] string name);

        /// <summary>
        ///     Is a variable with this name assigned(Also returns false if the
        ///     variable is not declared)?
        /// </summary>
        bool IsAssigned([NotNull] string name);
    }
}