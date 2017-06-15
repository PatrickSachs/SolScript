namespace SolScript.Compiler
{
    /// <summary>
    ///     Implement this interface on all compileable elements.
    /// </summary>
    public interface ISolCompileable
    {
        /// <summary>
        ///     Validates if this element is valid and may be run.
        /// </summary>
        /// <param name="context">The validation context.</param>
        /// <returns>true if the object is valid, false if not.</returns>
        /// <remarks>Error should not be thown, add them to the <see cref="SolValidationContext.Errors" /> collection instead.</remarks>
        ValidationResult Validate(SolValidationContext context);

        /*/// <summary>
        ///     Compiles the element to the given writer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="context">The compilation context.</param>
        /// <exception cref="IOException">An I/O error occured.</exception>
        /// <exception cref="SolCompilerException">Failed to compile. (See possible inner exceptions for details)</exception>
        void Compile(BinaryWriter writer, SolCompliationContext context);*/
    }
}