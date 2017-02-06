using System.Collections.Generic;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_Conditional : SolStatement {
        public Statement_Conditional(SolAssembly assembly, SolSourceLocation location) : base(assembly, location) {
        }

        [CanBeNull] public SolChunk Else;

        public IfBranch[] If;

        #region Overrides

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables) {
            context.CurrentLocation = Location;
            Terminators = Terminators.None;
            foreach (IfBranch branch in If) {
                Variables branchVariables = new Variables(Assembly) {Parent = parentVariables};
                if (branch.Condition.Evaluate(context, parentVariables).IsTrue(context)) {
                    SolValue value = branch.Chunk.ExecuteInTarget(context, branchVariables);
                    Terminators = branch.Chunk.Terminators;
                    return value;
                }
            }
            if (Else != null) {
                Variables branchVariables = new Variables(Assembly) {Parent = parentVariables};
                SolValue value = Else.ExecuteInTarget(context, branchVariables);
                Terminators = Else.Terminators;
                return value;
            }
            return SolNil.Instance;
        }

        protected override string ToString_Impl() {
            return $"Statement_Conditional(If=[{string.Join(",", (IEnumerable<IfBranch>) If)}], Else={Else})";
        }

        #endregion

        #region Nested type: IfBranch

        public class IfBranch {
            public SolChunk Chunk;
            public SolExpression Condition;

            #region Overrides

            public override string ToString() {
                return $"IfBranch(Condition={Condition}, Chunk={Chunk})";
            }

            #endregion
        }

        #endregion
    }
}