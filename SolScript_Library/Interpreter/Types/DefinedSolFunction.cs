namespace SolScript.Interpreter.Types
{
    public abstract class DefinedSolFunction : SolFunction
    {
        public override SolAssembly Assembly => Definition.Assembly;
        public override SolParameterInfo ParameterInfo => Definition.ParameterInfo;
        public override SolType ReturnType => Definition.ReturnType;
        public override SolSourceLocation Location => Definition.Location;

        public abstract SolFunctionDefinition Definition { get; }

        #region Overrides

        /// <inheritdoc />
        protected override string ToString_Impl(SolExecutionContext context)
        {
            return "function#" + Id + "<" + Definition.Name + ">";
        }

        #endregion
    }
}