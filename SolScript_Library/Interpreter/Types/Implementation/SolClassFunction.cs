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

        #region Overrides

        /// <inheritdoc />
        protected override string ToString_Impl(SolExecutionContext context)
        {
            return "function#" + Id + "<" + ClassInstance.InheritanceChain.Definition.Type + "." + Definition.Name + ">";
        }

        #endregion
    }
}