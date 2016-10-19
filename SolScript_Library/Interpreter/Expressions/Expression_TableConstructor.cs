using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public class Expression_TableConstructor : SolExpression {
        public Expression_TableConstructor(SourceLocation location) : base(location) {
        }

        public SolExpression[] Keys;
        public SolExpression[] Values;

        public override SolValue Evaluate(SolExecutionContext context) {
            context.CurrentLocation = Location;
            SolTable table = new SolTable();
            for (int i = 0; i < Keys.Length; i++) {
                SolValue key = Keys[i].Evaluate(context);
                SolValue value = Values[i].Evaluate(context);
                table[key] = value;
            }
            return table;
        }

        protected override string ToString_Impl() {
            return
                $"Expression_TableConstructor(Keys={string.Join(", ", (object[]) Keys)}, Values={string.Join(", ", (object[]) Values)})";
        }
    }
}