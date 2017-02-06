using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    public class Expression_Statement : TerminatingSolExpression
    {
        public Expression_Statement(SolAssembly assembly, SolSourceLocation location) : base(assembly, location) {}

        public SolStatement Statement;

        #region Overrides

        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            context.CurrentLocation = Location;
            SolValue value = Statement.Execute(context, parentVariables, out terminators);
            return value;
        }

        protected override string ToString_Impl()
        {
            return Statement.ToString();
        }

        #endregion
    }
}