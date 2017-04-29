namespace SolScript.Interpreter
{
    /// <summary>
    /// These <see cref="IVariables"/> are used for global variables marked with the <see cref="SolAccessModifier.Local"/> <see cref="SolAccessModifier"/>.
    /// </summary>
    public class GlobalLocalVariables : GlobalVariables
    {
        #region Overrides

        ///<inheritdoc />
        public GlobalLocalVariables(SolAssembly assembly) : base(assembly) {}

        ///<inheritdoc />
        protected override IVariables GetParent()
        {
            return Assembly.InternalVariables;
        }

        #endregion
    }
}