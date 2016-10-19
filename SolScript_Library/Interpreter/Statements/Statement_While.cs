using Irony.Parsing;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_While : SolStatement {
        public Statement_While(SourceLocation location, SolExpression condition, SolChunk chunk) : base(location) {
            Condition = condition;
            Chunk = chunk;
        }

        public readonly SolChunk Chunk;
        public readonly SolExpression Condition;

        public override SolValue Execute(SolExecutionContext context) {
            SolExecutionContext localContext = new SolExecutionContext(context.Assembly);
            localContext.VariableContext.ParentContext = context.VariableContext;
            while (Condition.Evaluate(localContext).IsTrue()) {
                // The chunk is running in a new context in order to discard the
                // locals for the previous iteration.
                SolValue returnValue = Chunk.Execute(localContext, SolChunk.ContextMode.RunInLocal);
                if (Chunk.DidTerminateParent) {
                    DidTerminateParent = true;
                    return returnValue;
                }
            }
            // todo: while return value?
            return SolNil.Instance;
        }

        protected override string ToString_Impl() {
            return $"Statement_While(Condition={Condition}, Chunk={Chunk})";
        }
    }
}