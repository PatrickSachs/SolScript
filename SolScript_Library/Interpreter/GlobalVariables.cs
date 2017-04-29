using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     These <see cref="IVariables" /> are used for global variables marked with the
    ///     <see cref="SolAccessModifier.Global" /> <see cref="SolAccessModifier" />.
    /// </summary>
    public class GlobalVariable : GlobalVariablesBase
    {
        /// <inheritdoc />
        public GlobalVariable(SolAssembly assembly) : base(assembly) {}

        // These options will be used for creating a singeton. Their creation will need to be enforced since they are not creatable.
        private static readonly ClassCreationOptions s_SingletonClassCreationOptions = new ClassCreationOptions.Customizable()
            .SetEnforceCreation(true)
            .SetCallConstructor(false)
            .SetMarkAsInitialized(false);

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolVariableException">An error occured.</exception>
        protected override AdditionalMemberInfo GetAdditionalMember(string name)
        {
            SolClassDefinition definition;
            if (Assembly.TryGetClass(name, out definition) && definition.TypeMode == SolTypeMode.Singleton) {
                // Singletons are created in two steps. First the instance is created and registered, then the ctor is being called.
                // This is due to the reason that inside the ctor the singleton might try to refer to itself which would lead to
                // infinite recursion.
                return new AdditionalMemberInfo(new SolType(definition.Type, false),
                    delegate {
                        SolDebug.WriteLine("Creating \"" + definition.Type + "\" singleton for variable \"" + name + "\" ... ");
                        try {
                            SolClass instance = Assembly.New(definition, s_SingletonClassCreationOptions);
                            return instance;
                        } catch (SolTypeRegistryException ex) {
                            throw new SolVariableException(definition.Location, "An error occured while creating the \"" + definition.Type + "\" singleton instance. (Phase 1)", ex);
                        }
                    },
                    delegate(SolValue value) {
                        SolClass instance = (SolClass) value;
                        try {
                            instance.CallConstructor(new SolExecutionContext(Assembly, "Singleton \"" + definition.Type + "\" creation context"));
                        } catch (SolRuntimeException ex) {
                            throw new SolVariableException(definition.Location, "An error occured while creating the \"" + definition.Type + "\" singleton instance. (Phase 2)", ex);
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
    }
}