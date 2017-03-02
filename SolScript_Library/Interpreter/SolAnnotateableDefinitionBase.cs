using System.Collections.Generic;
using SolScript.Interpreter.Exceptions;

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
        public abstract IReadOnlyList<SolAnnotationDefinition> Annotations { get; protected set; }

        /// <summary>
        ///     Creates the value of the <see cref="Annotations" /> property from the given list of
        ///     <see cref="SolAnnotationData" />.
        /// </summary>
        /// <param name="data">The annotation data.</param>
        /// <exception cref="SolMarshallingException">An annotation class does not exist.</exception>
        protected void AnnotationsFromData(IReadOnlyList<SolAnnotationData> data)
        {
            var annotations = new SolAnnotationDefinition[data.Count];
            for (int i = 0; i < annotations.Length; i++) {
                SolClassDefinition annotationDefinition;
                if (!Assembly.TryGetClass(data[i].Name, out annotationDefinition)) {
                    throw new SolMarshallingException(data[i].Name, "The annotation class used does not exist.");
                }
                annotations[i] = new SolAnnotationDefinition(data[i].Location, annotationDefinition, data[i].Arguments);
            }
            Annotations = annotations;
        }
    }
}