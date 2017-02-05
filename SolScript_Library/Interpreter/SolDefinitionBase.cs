namespace SolScript.Interpreter
{
    /// <summary>
    ///     This is the base class for all definitions in SolScript.
    /// </summary>
    public abstract class SolDefinitionBase : ISourceLocateable
    {
        // The internal ctor prevents users from creating their own definitions
        // for now. This may be supported later on.
        // todo: investigate user created definitions.
        internal SolDefinitionBase(SolAssembly assembly)
        {
            Assembly = assembly;
        }

        internal SolDefinitionBase() { }

        /// <summary>
        ///     The <see cref="SolAssembly" /> this definition is defined in.
        /// </summary>
        public virtual SolAssembly Assembly { get; }

        #region ISourceLocateable Members

        /// <summary>
        ///     Where in the SolScript code has this definiton been defined?
        /// </summary>
        public abstract SolSourceLocation Location { get; }

        #endregion

        /*/// <summary>
        ///     The current state the definition is in.
        /// </summary>
        /// <seealso cref="DefinitionState" />
        public DefinitionState State { get; private set; }

        /// <summary>
        ///     Assets state the <see cref="State" /> of this definition is exactly <paramref name="state" />.
        /// </summary>
        /// <param name="state">The required state.</param>
        /// <param name="message">The exception message if the assetion fails.</param>
        /// <exception cref="InvalidOperationException">The assertion failed.</exception>
        protected void AssetState(DefinitionState state, string message = "Invalid definition state.")
        {
            if (state != State) {
                throw new InvalidOperationException(message + $" Expected {state}, but the state was {State}.");
            }
        }

        /// <summary>
        ///     Builds the actual definiton. This advances <see cref="State" /> to <see cref="DefinitionState.Definition" />.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="State" /> is not <see cref="DefinitionState.Builder" />.</exception>
        /// <exception cref="SolTypeRegistryException">An error occured while generating the definition.</exception>
        public void BuildDefinition()
        {
            AssetState(DefinitionState.Builder, "Invalid state to build the definiton in.");
            BuildDefinition_Impl();
            State = DefinitionState.Definition;
        }

        /// <summary>
        ///     Your implementation to build the actual definition.
        /// </summary>
        /// <exception cref="SolTypeRegistryException">
        ///     An error occured while generating the definition. All exceptions must be
        ///     wrapped inside this one.
        /// </exception>
        protected abstract void BuildDefinition_Impl();*/
    }
}