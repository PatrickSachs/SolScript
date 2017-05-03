using System.Collections.Generic;
using Irony.Parsing;
using PSUtility.Enumerables;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     This is the base class for all annotateable definitions.
    /// </summary>
    public abstract class SolAnnotateableDefinitionBase : SolDefinitionBase
    {
        /// <inheritdoc />
        internal SolAnnotateableDefinitionBase(SolAssembly assembly, SourceLocation location) : base(assembly, location) {}
        // No 3rd party definitions.
        internal SolAnnotateableDefinitionBase() {}

        /// <summary>
        ///     All annotations declared in this definition.
        /// </summary>
        public abstract ReadOnlyList<SolAnnotationDefinition> DeclaredAnnotations { get; }

        /// <summary>
        ///     Internal helper method to add an annotation to this definition.
        /// </summary>
        /// <param name="annotation">The annotation.</param>
        internal abstract void AddAnnotation(SolAnnotationDefinition annotation);
    }
}