<<<<<<< HEAD
=======
using System;
using JetBrains.Annotations;
>>>>>>> caba3b9ea6294d526427ca8d5238e923cf6094c9
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    /// <summary>
<<<<<<< HEAD
    ///     These <see cref="IVariables" /> are used for global variables marked with the
    ///     <see cref="SolAccessModifier.None" /> <see cref="SolAccessModifier" />.
    /// </summary>
    public class GlobalVariable : GlobalVariablesBase
    {
        /// <inheritdoc />
        public GlobalVariable(SolAssembly assembly) : base(assembly) {}

        // These options will be used for creating a singeton. Their creation will need to be enforced since they are not creatable.
        private static readonly ClassCreationOptions s_SingletonClassCreationOptions = new ClassCreationOptions.Customizable().SetEnforceCreation(true).SetCallConstructor(false);

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolVariableException">An error occured.</exception>
        protected override AdditionalMemberInfo GetAddtionalMember(string name)
        {
            SolClassDefinition definition;
            if (Assembly.TypeRegistry.IsSingleton(name, out definition)) {
                // Singletons are created in two steps. First the instance is created and registered, then the ctor is being called.
                // This is due to the reason that inside the ctor the singleton might try to refer to itself which would lead to
                // infinite recursion.
                return new AdditionalMemberInfo(new SolType(definition.Type, false),
                    delegate {
                        SolDebug.WriteLine("Creating \"" + definition.Type + "\" singleton for variable \"" + name + "\" ... ");
                        try {
                            SolClass instance = Assembly.TypeRegistry.CreateInstance(definition, s_SingletonClassCreationOptions);
                            instance.IsInitialized = false;
                            return instance;
                        } catch (SolTypeRegistryException ex) {
                            throw new SolVariableException("Failed to create singleton instance \"" + definition.Type + "\".", ex);
                        }
                    },
                    delegate(SolValue value) {
                        SolClass instance = (SolClass) value;
                        try {
                            instance.CallConstructor(new SolExecutionContext(Assembly, "Singleton \"" + definition.Type + "\" creation context"));
                        } catch (SolRuntimeException ex) {
                            throw new SolVariableException("An error occured while creating the \"" + definition.Type + "\" singleton instance.", ex);
                        }
                        instance.IsInitialized = true;
                    }
                );
            }
            return null;
        }

        /// <inheritdoc />
        protected override IVariables GetParent()
        {
            return null;
        }

        #endregion
=======
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
>>>>>>> caba3b9ea6294d526427ca8d5238e923cf6094c9
    }
}