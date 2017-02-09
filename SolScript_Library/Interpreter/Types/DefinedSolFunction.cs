namespace SolScript.Interpreter.Types
{
    public abstract class DefinedSolFunction : SolFunction
    {
        /// <inheritdoc />
        public override SolAssembly Assembly => Definition.Assembly;

        /// <inheritdoc />
        public override SolParameterInfo ParameterInfo => Definition.ParameterInfo;

        /// <inheritdoc />
        public override SolType ReturnType => Definition.ReturnType;

        /// <inheritdoc />
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