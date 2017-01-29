using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    public class Expression_Number : SolExpression
    {
        public Expression_Number(SolAssembly assembly, SolSourceLocation location, double value) : base(assembly, location)
        {
            Value = new SolNumber(value);
        }

        public Expression_Number(SolAssembly assembly, SolSourceLocation location, SolNumber value) : base(assembly, location)
        {
            Value = value;
        }

        public readonly SolNumber Value;

        #region Overrides

        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            return Value;
        }

        protected override string ToString_Impl()
        {
            return Value.ToString();
        }

        #endregion
    }
}