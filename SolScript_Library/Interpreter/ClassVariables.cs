using System;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter
{
    public abstract class ClassVariables : IVariables
    {
        protected ClassVariables(SolAssembly assembly)
        {
            Members = new Variables(assembly);
        }

        protected readonly Variables Members;

        public abstract SolClassDefinition Definition { get; }

        #region IVariables Members

        /// <summary> The assembly this variable lookup belongs to. </summary>
        public SolAssembly Assembly => Definition.Assembly;

        public IVariables Parent {
            get { return GetParent(); }
            set { throw new NotSupportedException("Cannot change the parent of class variables."); }
        }

        /// <summary> Gets the value assigned to the given name. </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <exception cref="SolVariableException"> The value has not been declared or assigned. </exception>
        public SolValue Get(string name)
        {
            SolValue value;
            VariableState result = TryGet(name, out value);
            if (result != VariableState.Success) {
                throw InternalHelper.CreateVariableGetException(name, result, null);
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
            if (membersState != VariableState.FailedNotDeclared)
            {
                // Not declared variables can be functions or parent variables
                // since functions are create lazily.
                return membersState;
            }
            value = AttemptFunctionCreation(name);
            if (value != null) {
                return VariableState.Success;
            }
            if (Parent != null) {
                return Parent.TryGet(name, out value);
            }
            return VariableState.FailedNotDeclared;
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
        ///     A variable with this name has already
        ///     been declared.
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
        ///     Np variable with this name has been
        ///     decalred.
        /// </exception>
        /// <exception cref="SolVariableException"> The type does not match. </exception>
        public void Assign(string name, SolValue value)
        {
            if (Members.IsDeclared(name)) {
                Members.Assign(name, value);
            } else if (Definition.HasFunction(name, OnlyUseDeclaredFunctions)) {
                throw new SolVariableException("Cannot assign values to class function \"" + name + "\", they are immutable.");
            } else if (Parent != null) {
                Parent.Assign(name, value);
            } else {
                throw new SolVariableException("Cannot assign value to variable \"" + name + "\", not variable with this name has been declared.");
            }
        }

        /// <summary> Is a variable with this name declared? </summary>
        public bool IsDeclared(string name)
        {
            if (Members.IsDeclared(name)) {
                return true;
            }
            SolFunctionDefinition definition;
            if (Definition.TryGetFunction(name, OnlyUseDeclaredFunctions, out definition) && ValidateFunctionDefinition(definition)) {
                return true;
            }
            if (Parent != null) {
                return Parent.IsDeclared(name);
            }
            return false;
        }

        /// <summary>
        /// If this is true only functions declared in the the <see cref="Definition"/> directly will be used for the functions of this <see cref="ClassVariables"/> instance.
        /// </summary>
        protected abstract bool OnlyUseDeclaredFunctions { get; }

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
            if (Definition.TryGetFunction(name, OnlyUseDeclaredFunctions, out definition) && ValidateFunctionDefinition(definition)) {
                return true;
            }
            if (Parent != null) {
                return Parent.IsAssigned(name);
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
        public SolFunction AttemptFunctionCreation(string name)
        {
            SolFunctionDefinition functionDefinition;
            if (Definition.TryGetFunction(name, OnlyUseDeclaredFunctions, out functionDefinition) && ValidateFunctionDefinition(functionDefinition)) {
                SolFunction function;
                switch (functionDefinition.Chunk.ChunkType) {
                    case SolChunkWrapper.Type.ScriptChunk:
                        function = new SolScriptClassFunction(GetInstance(), functionDefinition);
                        break;
                    case SolChunkWrapper.Type.NativeMethod:
                        function = new SolNativeClassMemberFunction(GetInstance(), functionDefinition);
                        break;
                    case SolChunkWrapper.Type.NativeConstructor:
                        function = new SolNativeClassConstructorFunction(GetInstance(), functionDefinition);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                SolDebug.WriteLine("Created function instance '" + name + " for class '" + GetInstance() + " :: " + function);
                // ReSharper disable once ExceptionNotDocumented
                // "function!" is always compatible with functions.
                Members.SetValue(name, function, new SolType(SolFunction.TYPE, false));
                return function;
            }
            return null;
        }

        protected abstract SolClass GetInstance();

        protected abstract bool ValidateFunctionDefinition(SolFunctionDefinition definition);

        [CanBeNull]
        protected abstract IVariables GetParent();
    }
}