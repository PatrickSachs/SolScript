using System;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter
{
    public class GlobalVariables : IVariables
    {
        public GlobalVariables(SolAssembly assembly)
        {
            Members = new Variables(assembly);
        }

        protected readonly Variables Members;

        #region IVariables Members

        /// <summary> The assembly this variable lookup belongs to. </summary>
        public SolAssembly Assembly => Members.Assembly;

        public IVariables Parent {
            get { return GetParent(); }
            set { throw new NotSupportedException("Cannot change the parent of global variables."); }
        }

        /// <summary> Gets the value assigned to the given name. </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <exception cref="SolVariableException"> The value has not been declared or assigned. </exception>
        public SolValue Get(string name)
        {
            SolValue value;
            VariableState state = TryGet(name, out value);
            if (state != VariableState.Success) {
                throw InternalHelper.CreateVariableGetException(name, state, null);
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
            VariableState state = Members.TryGet(name, out value);
            if (state != VariableState.FailedNotDeclared) {
                // Not declared variables can be functions or parent variables.
                // Functions are created lazily.
                // todo: no more lazy function cration for globals, no real benifit in doing so.
                return state;
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
            // In case we are trying to assign annotations to a function whcih has not been created.
            if (!IsDeclared(name)) {
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
                return;
            }
            SolFunctionDefinition definition;
            if (Assembly.TypeRegistry.TryGetGlobalFunction(name, out definition)) {
                throw new SolVariableException("Cannot assign values to class function \"" + name + "\", they are immutable.");
            }
            if (Parent == null) {
                throw new SolVariableException("Cannot assign value to variable \"" + name + "\", no variable with this name has been declared.");
            }
            Parent.Assign(name, value);
        }

        /// <summary> Is a variable with this name declared? </summary>
        public bool IsDeclared(string name)
        {
            if (Members.IsDeclared(name)) {
                return true;
            }
            SolFunctionDefinition definition;
            if (Assembly.TypeRegistry.TryGetGlobalFunction(name, out definition) && ValidateFunctionDefinition(definition)) {
                return true;
            }
            if (Parent != null) {
                return Parent.IsDeclared(name);
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
            if (Assembly.TypeRegistry.TryGetGlobalFunction(name, out definition) && ValidateFunctionDefinition(definition)) {
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
            if (Assembly.TypeRegistry.TryGetGlobalFunction(name, out functionDefinition) && ValidateFunctionDefinition(functionDefinition)) {
                SolFunction function;
                switch (functionDefinition.Chunk.ChunkType) {
                    case SolChunkWrapper.Type.ScriptChunk:
                        function = new SolScriptGlobalFunction(functionDefinition);
                        break;
                    case SolChunkWrapper.Type.NativeMethod:
                        function = new SolNativeGlobalFunction(functionDefinition, DynamicReference.NullReference.Instance);
                        break;
                    case SolChunkWrapper.Type.NativeConstructor:
                        throw new InvalidOperationException("A native constructor cannot be a global function.");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                SolDebug.WriteLine("created global func " + name + " : " + function);
                // ReSharper disable once ExceptionNotDocumented
                // "function!" is always compatible with functions.
                Members.SetValue(name, function, new SolType(SolFunction.TYPE, false));
                return function;
            }
            return null;
        }

        public virtual IVariables GetParent()
        {
            return null;
        }

        protected virtual bool ValidateFunctionDefinition(SolFunctionDefinition definition)
        {
            return definition.AccessModifier == AccessModifier.None;
        }
    }
}