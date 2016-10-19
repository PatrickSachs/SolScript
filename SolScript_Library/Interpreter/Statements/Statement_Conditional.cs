using System.Collections.Generic;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_Conditional : SolStatement {
        [CanBeNull] public SolChunk Else;

        public IfBranch[] If;

        public override SolValue Execute(SolExecutionContext context)
        {
            context.CurrentLocation = Location;
            DidTerminateParent = false;
            foreach (IfBranch branch in If) {
                if (branch.Condition.Evaluate(context).IsTrue()) {
                    SolValue value = branch.Chunk.Execute(context);
                    if (branch.Chunk.DidTerminateParent) {
                        DidTerminateParent = true;
                    }
                    return value;
                }
            }
            if (Else != null) {
                SolValue value = Else.Execute(context);
                if (Else.DidTerminateParent) {
                    DidTerminateParent = true;
                }
                return value;
            }
            return SolNil.Instance;
        }

        protected override string ToString_Impl() {
            return $"Statement_Conditional(If=[{string.Join(",", (IEnumerable<IfBranch>) If)}], Else={Else})";
        }

        #region Nested type: IfBranch

        public class IfBranch {
            public SolChunk Chunk;
            public SolExpression Condition;

            public override string ToString() {
                return $"IfBranch(Condition={Condition}, Chunk={Chunk})";
            }
        }

        #endregion

        public Statement_Conditional(SourceLocation location) : base(location) {
        }
    }
}