namespace SolScript.Interpreter
{
    /// <summary>
    ///     These <see cref="IVariables" /> are used for global variables marked with the
    ///     <see cref="SolAccessModifier.Internal" /> <see cref="SolAccessModifier" />.
    /// </summary>
    public class InternalVariables : GlobalVariablesBase
    {
        ///<inheritdoc />
        public InternalVariables(SolAssembly assembly) : base(assembly) {}

        #region Overrides

        ///<inheritdoc />
        protected override IVariables GetParent()
        {
            return Assembly.GlobalVariables;
        }

        /// <inheritdoc />
        protected override AdditionalMemberInfo GetAdditionalMember(string name)
        {
            return null;
        }

        #endregion
    }
}