using System;
using Irony.Parsing;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     This is the base class for all definitions in SolScript.
    /// </summary>
    public abstract class SolDefinitionBase : ISourceLocateable//, ISourceLocationInjector
    {
        internal SolDefinitionBase(SolAssembly assembly, SourceLocation location)
        {
            if (assembly == null) {
                throw new ArgumentNullException(nameof(assembly));
            }
            Assembly = assembly;
            InjectSourceLocation(location);
        }

        // No 3rd party definitions.
        internal SolDefinitionBase()
        {
            Assembly = SolAssembly.CurrentlyParsing;
        }

        /// <summary>
        ///     The <see cref="SolAssembly" /> this definition is defined in.
        /// </summary>
        public SolAssembly Assembly { get; }

        #region ISourceLocateable Members

        /// <summary>
        ///     Where in the SolScript code has this definition been defined?
        /// </summary>
        public SourceLocation Location { get; internal set; }

        #endregion

        /// <inheritdoc />
        public void InjectSourceLocation(SourceLocation location)
        {
            Location = location;
        }
    }
}