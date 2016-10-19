using System;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_Return : SolStatement {
        public Statement_Return(SourceLocation location) : base(location) {
        }

        [CanBeNull] public SolExpression ValueGetter;

        public override bool DidTerminateParent {
            get { return false; }
            protected set { throw new NotSupportedException("Return statements always terminate!"); }
        }

        public override SolValue Execute(SolExecutionContext context) {
            context.CurrentLocation = Location;
            return ValueGetter?.Evaluate(context) ?? SolNil.Instance;
        }

        protected override string ToString_Impl() {
            return $"Statement_Return(ValueGetter={ValueGetter})";
        }
    }
}