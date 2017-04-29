using System;
using System.Linq;
using System.Reflection;
using PSUtility.Enumerables;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;
using SolScript.Utility;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     Base class for the variables declared at each inheritance level of a class.
    /// </summary>
    public abstract class DeclaredClassInheritanceVariables : IVariables
    {
        // No 3rd party impls.
        internal DeclaredClassInheritanceVariables(SolClass.Inheritance inheritance)
        {
            Inheritance = inheritance;
            Members = new Variables(inheritance.Definition.Assembly);
        }

        /// <summary>
        ///     The members.
        /// </summary>
        protected readonly Variables Members;

        internal SolClass.Inheritance Inheritance { get; }

        #region IVariables Members

        /// <summary> The assembly this variable lookup belongs to. </summary>
        public SolAssembly Assembly => Inheritance.Definition.Assembly;

        /// <summary> Gets the value assigned to the given name. </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <exception cref="SolVariableException"> The value has not been declared or assigned. </exception>
        public SolValue Get(string name)
        {
            SolValue value;
            VariableState result = TryGet(name, out value);
            if (result != VariableState.Success) {
                throw InternalHelper.CreateVariableGetException(name, result, null, Inheritance.Definition.Location);
            }
            return value.NotNull();
        }

        /// <summary>
        ///     Tries to get the value assigned to the given name. The result is only
        ///     valid if the method returned <see cref="VariableState.Success" />.
        /// </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="value"> A pointer to where the variable should be saved. </param>
        public VariableState TryGet(string name, out SolValue value)
        {
            VariableState membersState = Members.TryGet(name, out value);
            if (membersState != VariableState.FailedNotDeclared) {
                // Not declared variables can be functions
                // since functions are create lazily.
                return membersState;
            }
            value = AttemptFunctionCreation(name);
            if (value != null) {
                return VariableState.Success;
            }
            return VariableState.FailedNotDeclared;
        }

        /// <inheritdoc />
        /// <exception cref="SolVariableException">Failed to get the annotations.</exception>
        public IReadOnlyList<SolClass> GetAnnotations(string name)
        {
            if (Members.IsDeclared(name)) {
                return Members.GetAnnotations(name);
            }
            if (AttemptFunctionCreation(name) != null) {
                return Members.GetAnnotations(name);
            }
            return EmptyReadOnlyList<SolClass>.Value;
        }

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
        public void Declare(string name, SolType type)
        {
            Members.Declare(name, type);
        }

        /// <summary> Declares a native variable. </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="type">
        ///     The type of the variable. Only values assignable to this
        ///     type can be assigned.
        /// </param>
        /// <param name="field"> The native field handle. </param>
        /// <param name="fieldReference"> The reference to the native object handle. </param>
        /// <exception cref="SolVariableException">Another variable with the same name is already declared.</exception>
        public void DeclareNative(string name, SolType type, FieldOrPropertyInfo field, DynamicReference fieldReference)
        {
            Members.DeclareNative(name, type, field, fieldReference);
        }

        /// <summary> Assigns annotations to a given variable. </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="annotations"> The annotations to assign to the variable. </param>
        /// <exception cref="SolVariableException">Could not assign the annotations.</exception>
        public void AssignAnnotations(string name, params SolClass[] annotations)
        {
            if (!IsDeclared(name)) {
                // The function may not have been created yet.
                AttemptFunctionCreation(name);
            }
            Members.AssignAnnotations(name, annotations);
        }

        /// <summary> Assigns a value to the variable with the giv en name. </summary>
        /// <exception cref="SolVariableException">
        ///     No variable with this name has been declared.
        /// </exception>
        /// <exception cref="SolVariableException"> The type does not match. </exception>
        public SolValue Assign(string name, SolValue value)
        {
            SolFunctionDefinition definition;
            if (Members.IsDeclared(name)) {
                return Members.Assign(name, value);
            }
            if (Inheritance.Definition.TryGetFunction(name, true, out definition) && ValidateFunctionDefinition(definition)) {
                throw new SolVariableException(Inheritance.Definition.Location, "Cannot assign values to class function \"" + name + "\", they are immutable.");
            }
            throw new SolVariableException(Inheritance.Definition.Location, "Cannot assign value to variable \"" + name + "\", not variable with this name has been declared.");
        }

        /// <summary> Is a variable with this name declared? </summary>
        public bool IsDeclared(string name)
        {
            if (Members.IsDeclared(name)) {
                return true;
            }
            SolFunctionDefinition definition;
            if (Inheritance.Definition.TryGetFunction(name, true, out definition) && ValidateFunctionDefinition(definition)) {
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Is a variable with this name assigned(Also returns false if the
        ///     variable is not declared)?
        /// </summary>
        public bool IsAssigned(string name)
        {
            if (Members.IsAssigned(name)) {
                return true;
            }
            SolFunctionDefinition definition;
            if (Inheritance.Definition.TryGetFunction(name, true, out definition) && ValidateFunctionDefinition(definition)) {
                return true;
            }
            return false;
        }

        #endregion

        /// <summary>
        ///     Attempts to create, declared and assign the function with the given name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The function.</returns>
        /// <remarks>
        ///     This method uses <see cref="Variables.SetValue" /> - This means that any possibly previously declared
        ///     variables with this name will be overwritten. Make sure to check if a field with this name has been declared
        ///     beforehand.
        /// </remarks>
        /// <exception cref="SolVariableException">An error occured.</exception>
        public SolFunction AttemptFunctionCreation(string name)
        {
            SolFunctionDefinition functionDefinition;
            if (Inheritance.Definition.TryGetFunction(name, true, out functionDefinition) && ValidateFunctionDefinition(functionDefinition)) {
                SolFunction function;
                //ICustomAttributeProvider provider;
                switch (functionDefinition.Chunk.ChunkType) {
                    case SolChunkWrapper.Type.ScriptChunk:
                        function = new SolScriptClassFunction(Inheritance.Instance, functionDefinition);
                        //provider = null;
                        break;
                    case SolChunkWrapper.Type.NativeMethod:
                        function = new SolNativeClassMemberFunction(Inheritance.Instance, functionDefinition);
                        //provider = functionDefinition.Chunk.GetNativeMethod();
                        break;
                    case SolChunkWrapper.Type.NativeConstructor:
                        function = new SolNativeClassConstructorFunction(Inheritance.Instance, functionDefinition);
                        //provider = functionDefinition.Chunk.GetNativeConstructor();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                SolDebug.WriteLine("Created function instance '" + name + "' for class '" + Inheritance.Instance.Type + "' - " + function + " [level: " + GetType().Name + "]");
                Members.Declare(name, new SolType(SolFunction.TYPE, false));
                if (functionDefinition.DeclaredAnnotations.Count > 0) {
                    SolClass[] annotations;
                    try {
                        SolExecutionContext context = new SolExecutionContext(Assembly, "Function \"" + name + "\" annotation creator");
                        IVariables variables = Inheritance.GetVariables(SolAccessModifier.Local, SolVariableMode.All);
                        annotations = functionDefinition.DeclaredAnnotations
                            .Select(a => a.Assembly.New(a.Definition, ClassCreationOptions.Enforce(), a.Arguments.Evaluate(context, variables)))
                            .ToArray();
                    } catch (SolTypeRegistryException ex) {
                        throw new SolVariableException(functionDefinition.Location, "Failed to create an annotation for function \"" + Inheritance.Definition.Type + "." + name + "\".", ex);
                    }
                    Members.AssignAnnotations(name, annotations);
                }
                Members.Assign(name, function);
                return function;
            }
            return null;
        }

        /// <summary>
        ///     Validates if an obtained function definitions is applicable of to this variable source. Do not rely on the internal
        ///     state. It is possible that some definitions will be validated multiple times.
        /// </summary>
        /// <param name="definition">The definition.</param>
        /// <returns>true if it is applicable, flase if not.</returns>
        protected abstract bool ValidateFunctionDefinition(SolFunctionDefinition definition);
    }
}