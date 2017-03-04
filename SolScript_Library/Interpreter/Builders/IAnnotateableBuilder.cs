using System.Collections.Generic;
using SolScript.Utility;

namespace SolScript.Interpreter.Builders
{
    /// <summary>
    ///     This interface implements functionality to add annotations to a builder.
    /// </summary>
    public interface IAnnotateableBuilder
    {
        /// <summary>
        ///     All currently registered annotations.
        /// </summary>
        IReadOnlyList<SolAnnotationData> Annotations { get; }

        /// <summary>
        ///     Adds a new annotation to this builder.
        /// </summary>
        /// <param name="annotation">The annotation.</param>
        /// <returns>The builder.</returns>
        IAnnotateableBuilder AddAnnotation(SolAnnotationData annotation);

        /// <summary>
        ///     Adds multiple new annotations to this builder.
        /// </summary>
        /// <param name="annotations">The annotations.</param>
        /// <returns>The builder.</returns>
        IAnnotateableBuilder AddAnnotations(params SolAnnotationData[] annotations);

        /// <summary>
        ///     Removes all annotations from this builder.
        /// </summary>
        /// <returns>The builder.</returns>
        IAnnotateableBuilder ClearAnnotations();
    }
}