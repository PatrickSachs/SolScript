namespace SolScript.Interpreter
{
    /// <summary>
    ///     This is the base class for all definitions in SolScript.
    /// </summary>
    public abstract class SolDefinitionBase : ISourceLocateable
    {
        // No 3rd party definitions.
        internal SolDefinitionBase(SolAssembly assembly, SolSourceLocation location)
        {
            Assembly = assembly;
            Location = location;
        }

        /// <summary>
        ///     The <see cref="SolAssembly" /> this definition is defined in.
        /// </summary>
        public SolAssembly Assembly { get; }

        #region ISourceLocateable Members

        /// <summary>
        ///     Where in the SolScript code has this definiton been defined?
        /// </summary>
        public SolSourceLocation Location { get; }

        #endregion
    }
}