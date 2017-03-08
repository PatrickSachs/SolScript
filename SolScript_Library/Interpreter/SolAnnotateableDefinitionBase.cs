using SolScript.Interpreter.Exceptions;
using SolScript.Utility;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     This is the base class for all annotateable definitions.
    /// </summary>
    public abstract class SolAnnotateableDefinitionBase : SolDefinitionBase
    {
        // No 3rd party definitions.
        internal SolAnnotateableDefinitionBase(SolAssembly assembly, SolSourceLocation location) : base(assembly, location) {}

        /// <summary>
        ///     All annotations of this definition.
        /// </summary>
        public abstract IReadOnlyList<SolAnnotationDefinition> Annotations { get; }
    }
}