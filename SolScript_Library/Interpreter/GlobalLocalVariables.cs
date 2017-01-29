namespace SolScript.Interpreter
{
    public class GlobalLocalVariables : GlobalVariables
    {
        #region Overrides

        public GlobalLocalVariables(SolAssembly assembly) : base(assembly) {}

        protected override bool ValidateFunctionDefinition(SolFunctionDefinition definition)
        {
            return definition.AccessModifier == AccessModifier.Local;
        }

        public override IVariables GetParent()
        {
            return Assembly.InternalVariables;
        }

        #endregion
    }
}