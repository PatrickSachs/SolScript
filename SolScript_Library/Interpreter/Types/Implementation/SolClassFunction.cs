namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     The <see cref="SolClassFunction" /> class is the base class for functions declared in a class, both native and
    ///     script based.
    /// </summary>
    public abstract class SolClassFunction : DefinedSolFunction
    {
        // No third party functions.
        internal SolClassFunction() {}

        /// <summary>
        ///     The class instance of this function.
        /// </summary>
        public abstract SolClass ClassInstance { get; }

        /// <summary>
        ///     Obtains a reference to the class definition this function was defined in.
        /// </summary>
        /// <returns>The <see cref="SolClassDefinition" /> the function was declared in.</returns>
        public abstract SolClassDefinition GetDefiningClass();
    }
}