using System.Collections.Generic;
using Irony.Parsing;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_New : SolStatement {
        public Statement_New(SourceLocation location) : base(location) {
        }

        public SolExpression[] Arguments;
        public string TypeName;

        public override SolValue Execute(SolExecutionContext context)
        {
            context.CurrentLocation = Location;
            var arguments = new SolValue[Arguments.Length];
            for (int i = 0; i < arguments.Length; i++) {
                arguments[i] = Arguments[i].Evaluate(context);
            }
            SolCustomType instance = context.Assembly.TypeRegistry.CreateInstance(context.Assembly, TypeName,
                arguments);
            return instance;
        }

        protected override string ToString_Impl() {
            return
                $"Statement_New(TypeName={TypeName}, Arguments={string.Join(",", (IEnumerable<SolExpression>) Arguments)})";
        }
    }
}