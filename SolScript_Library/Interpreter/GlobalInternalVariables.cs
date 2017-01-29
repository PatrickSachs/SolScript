namespace SolScript.Interpreter
{
    public class GlobalInternalVariables : GlobalVariables
    {
        #region Overrides

        public GlobalInternalVariables(SolAssembly assembly) : base(assembly) {}

        protected override bool ValidateFunctionDefinition(SolFunctionDefinition definition)
        {
            return definition.AccessModifier == AccessModifier.Internal;
        }

        public override IVariables GetParent()
        {
            return Assembly.GlobalVariables;
        }

        #endregion
    }
}