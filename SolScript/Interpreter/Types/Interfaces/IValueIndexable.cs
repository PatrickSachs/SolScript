using SolScript.Exceptions;

namespace SolScript.Interpreter.Types.Interfaces
{
    /// <summary>
    ///     This interface is used to allow direct indexing on a SolValue.
    /// </summary>
    public interface IValueIndexable
    {
        /// <summary>
        ///     Indexes the <see cref="IValueIndexable" /> by the given SolValue and returns the result.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value associated with this key. The return value should be determinstic.</returns>
        /// <exception cref="SolVariableException">An error occured while retrieving this variable.</exception>
        SolValue this[SolValue key] { get; set; }
    }
}