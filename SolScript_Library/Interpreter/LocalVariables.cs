namespace SolScript.Interpreter
{
    /// <summary>
    ///     These <see cref="IVariables" /> are used for global variables marked with the
    ///     <see cref="SolAccessModifier.Local" /> <see cref="SolAccessModifier" />.
    /// </summary>
    public class LocalVariables : GlobalVariablesBase
    {
        ///<inheritdoc />
        public LocalVariables(SolAssembly assembly) : base(assembly) {}

        #region Overrides

        ///<inheritdoc />
        protected override IVariables GetParent()
        {
            return Assembly.InternalVariables;
        }

        /// <inheritdoc />
        protected override AdditionalMemberInfo GetAdditionalMember(string name)
        {
            return null;
        }

        #endregion
    }
}