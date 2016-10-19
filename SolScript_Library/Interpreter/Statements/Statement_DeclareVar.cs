using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_DeclareVar : SolStatement {
        public Statement_DeclareVar(SourceLocation location) : base(location) {
        }

        public bool Local;
        public string Name;
        public SolType Type;
        [CanBeNull]public SolExpression ValueGetter;

        public override SolValue Execute(SolExecutionContext context)
        {
            context.CurrentLocation = Location;
            SolValue value = ValueGetter?.Evaluate(context);
            context.VariableContext.DeclareVariable(Name, value, Type, Local);
            return value ?? SolNil.Instance;
        }

        protected override string ToString_Impl() {
            return $"Statement_DeclareVar(Name={Name}, Local={Local}, Type={Type}, ValueGetter={ValueGetter})";
        }
    }
}