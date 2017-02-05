namespace SolScript.Interpreter
{
    /// <summary>
    ///     The <see cref="ISourceLocateable" /> interface assists in finding a certain element in the SolScript code.
    /// </summary>
    public interface ISourceLocateable
    {
        /// <summary>
        ///     The position in code.
        /// </summary>
        SolSourceLocation Location { get; }
    }
}