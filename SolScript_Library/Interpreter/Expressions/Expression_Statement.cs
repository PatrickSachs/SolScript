using Irony.Parsing;
using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public class Expression_Statement : SolExpression {
        public SolStatement Statement;

        public override SolValue Evaluate(SolExecutionContext context)
        {
            context.CurrentLocation = Location;
            return Statement.Execute(context);
        }

        protected override string ToString_Impl() {
            SolDebug.WriteLine("EXST CALLING " + Statement.GetType().Name);
            return $"Expression_Statement({Statement})";
        }

        public Expression_Statement(SourceLocation location) : base(location) {
        }
    }
}