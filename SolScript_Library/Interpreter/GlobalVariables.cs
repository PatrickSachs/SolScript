using System;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The global variables class is the base class for all variables registered in global context.
    /// </summary>
    public class GlobalVariables : IVariables
    {
        /// <summary>
        ///     Creates a new global variables instance.
        /// </summary>
        /// <param name="assembly">The assembly the global variables belong to.</param>
        public GlobalVariables(SolAssembly assembly)
        {
            Members = new Variables(assembly);
        }

        /// <summary>
        ///     The member variables.
        /// </summary>
        protected readonly Variables Members;

        #region IVariables Members

        /// <summary> The assembly this variable lookup belongs to. </summary>
        public SolAssembly Assembly => Members.Assembly;

        /// <summary>
        ///     The parent context. (read-only for <see cref="GlobalVariables" />).
        /// </summary>
        /// <exception cref="NotSupportedException" accessor="set">Cannot change the parent of global variables.</exception>
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
            if (Parent != null) {
                return Parent.IsAssigned(name);
            }
            return false;
        }

        #endregion

        /// <summary>
        ///     Gets the parent variables of these global variables.
        /// </summary>
        /// <returns>The parent variables.</returns>
        [CanBeNull]
        protected virtual IVariables GetParent()
        {
            return null;
        }
    }
}