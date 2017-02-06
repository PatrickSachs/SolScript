using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_While : SolStatement {
        public Statement_While([NotNull] SolAssembly assembly, SolSourceLocation location, SolExpression condition, SolChunk chunk) : base(assembly, location) {
            Chunk = chunk;
            Condition = condition;
        }

        public readonly SolChunk Chunk;
        public readonly SolExpression Condition;

        #region Overrides

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables) {
            Terminators = Terminators.None;
            while (Condition.Evaluate(context, parentVariables).IsTrue(context)) {
                // The chunk is running in a new variable context in order to discard the
                // locals from the previous iteration.
                Variables variables = new Variables(Assembly) {Parent = parentVariables};
                SolValue returnValue = Chunk.ExecuteInTarget(context, variables);
                Terminators terminators = Chunk.Terminators;
                if (InternalHelper.DidReturn(terminators)) {
                    Terminators = Terminators.Return;
                    return returnValue;
                }
                if (InternalHelper.DidBreak(terminators)) {
                    Terminators = Terminators.None;
                    break;
                }
                // Continue is breaking the chunk execution.
                if (InternalHelper.DidContinue(terminators)) {
                    Terminators = Terminators.None;
                }
            }
            return SolNil.Instance;
        }

        protected override string ToString_Impl() {
            return $"Statement_While(Condition={Condition}, Chunk={Chunk})";
        }

        #endregion
    }
}