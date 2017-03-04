using System;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;
using SolScript.Utility;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The global variables class is the base class for all variables registered in global context.
    /// </summary>
    public abstract class GlobalVariablesBase : IVariables
    {
        /// <summary>
        ///     Creates a new global variables instance.
        /// </summary>
        /// <param name="assembly">The assembly the global variables belong to.</param>
        protected GlobalVariablesBase(SolAssembly assembly)
        {
            Members = new Variables(assembly);
        }

        /// <summary>
        ///     The member variables.
        /// </summary>
        // todo: the variable wrapper introduced quite a lot of overhead. simplify this.
        protected readonly Variables Members;

        #region IVariables Members

        /// <summary> The assembly this variable lookup belongs to. </summary>
        public SolAssembly Assembly => Members.Assembly;

        /// <summary>
        ///     The parent context. (read-only for <see cref="GlobalVariablesBase" />).
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
        /// <exception cref="SolVariableException">An error occured.</exception>
        public VariableState TryGet(string name, out SolValue value)
        {
            VariableState state = Members.TryGet(name, out value);
            if (state != VariableState.FailedNotDeclared) {
                return state;
            }
            value = GetAndRegisterAdditional(name);
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
            GetAndRegisterAdditional(name);
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
            GetAndRegisterAdditional(name);
            Members.DeclareNative(name, type, field, fieldReference);
        }

        /// <summary> Assigns annotations to a given variable. </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="annotations"> The annotations to assign to the variable. </param>
        /// <exception cref="SolVariableException">An error occured.</exception>
        public void AssignAnnotations(string name, params SolClass[] annotations)
        {
            if (!IsDeclared(name) && GetAndRegisterAdditional(name) == null) {
                throw new SolVariableException("Cannot assign annotations to gloval variable \"" + name + "\". No variable with this name has been declared.");
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
            if (Members.IsDeclared(name) || GetAndRegisterAdditional(name) != null) {
                Members.Assign(name, value);
            }
            else if (Parent != null) {
                Parent.Assign(name, value);
            } else {
                throw new SolVariableException("Cannot assign value to variable \"" + name + "\", no variable with this name has been declared.");
            }
        }

        /// <summary> Is a variable with this name declared? </summary>
        /// <exception cref="SolVariableException">An error occured.</exception>
        public bool IsDeclared(string name)
        {
            if (Members.IsDeclared(name)||GetAndRegisterAdditional(name) != null) {
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
        /// <exception cref="SolVariableException">An error occured.</exception>
        public bool IsAssigned(string name)
        {
            if (Members.IsAssigned(name) || GetAndRegisterAdditional(name) != null) {
                return true;
            }
            if (Parent != null) {
                return Parent.IsAssigned(name);
            }
            return false;
        }

        #endregion

        /// <summary>
        /// Checks if an additional member with the given name exists. If it does the method creates and registers it.
        /// </summary>
        /// <exception cref="SolVariableException">An error occured.</exception>
        private SolValue GetAndRegisterAdditional(string name)
        {
            AdditionalMemberInfo additional = GetAdditionalMember(name);
            if (additional != null) {
                Members.Declare(name, additional.Type);
                SolValue value = additional.Creator();
                Members.Assign(name, value);
                additional.Finalizer(value);
                return value;
            }
            return null;
        }

        /// <summary>
        ///     Addtional member retievement method. If no member with the given <paramref name="name" /> could be found in the
        ///     <see cref="Members" /> variables this method will be called.
        /// </summary>
        /// <param name="name">The name of the member to get.</param>
        /// <returns>
        ///     null if no additional member with this name exists, otherwise info about this member. The return value of this
        ///     method(if not null) will be registered in the <see cref="Members" /> variables.
        /// </returns>
        /// <exception cref="SolVariableException">An error occured.</exception>
        protected abstract AdditionalMemberInfo GetAdditionalMember(string name);

        /// <summary>
        ///     Gets the parent variables of these global variables.
        /// </summary>
        /// <returns>The parent variables.</returns>
        [CanBeNull]
        protected abstract IVariables GetParent();
        #region Nested type: AdditionalMemberInfo

        /// <summary>
        ///     The additional info is used to lazily create additional members.
        /// </summary>
        protected class AdditionalMemberInfo
        {
            /// <summary>
            ///     Creates a new additonal info instance.
            /// </summary>
            /// <param name="type">The type of the member and the field.</param>
            /// <param name="creator"> The creator of the value. Called after the field has been declared.</param>
            /// <param name="finalizer">The finalizer of the value. Called after the field has been assigned.</param>
            public AdditionalMemberInfo(SolType type, Func<SolValue> creator, Action<SolValue> finalizer)
            {
                Type = type;
                Creator = creator;
                Finalizer = finalizer;
            }

            /// <summary>
            ///     The creator of the value. Called after the field has been declared.
            /// </summary>
            /// <exception cref="SolVariableException">An error occured.</exception>
            public readonly Func<SolValue> Creator;

            /// <summary>
            ///     The finalizer of the value. Called after the field has been assigned.
            /// </summary>
            /// <exception cref="SolVariableException">An error occured.</exception>
            public readonly Action<SolValue> Finalizer;

            /// <summary>
            ///     The type of the member and the field.
            /// </summary>
            public readonly SolType Type;
        }

        #endregion
    }
}