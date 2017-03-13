namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     The <see cref="SolClassFunction" /> class is the base class for functions declared in a class, both native and
    ///     script based.
    /// </summary>
    public abstract class SolClassFunction : DefinedSolFunction
    {
        /// <summary>
        /// The class instance of this class function.
        /// </summary>
        public abstract SolClass ClassInstance { get; }

        /// <inheritdoc />
        protected override SolClass GetClassInstance(out bool isCurrent, out bool resetOnExit)
        {
            isCurrent = true;
            resetOnExit = true;
            return ClassInstance;
        }

        // No third party functions.
        internal SolClassFunction() {}

        #region Overrides

        /// <inheritdoc />
        protected override string ToString_Impl(SolExecutionContext context)
        {
            return "function#" + Id + "<" + ClassInstance.InheritanceChain.Definition.Type + "." + Definition.Name + "#"+Definition.DefinedIn.Type+">";
        }

        #endregion
    }
}