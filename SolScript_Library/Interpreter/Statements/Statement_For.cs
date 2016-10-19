using Irony.Parsing;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_For : SolStatement {
        public Statement_For(SourceLocation location, SolStatement initialization, 
            SolExpression condition, SolStatement afterthought, SolChunk chunk) 
            : base(location) {
            Initialization = initialization;
            Condition = condition;
            Afterthought = afterthought;
            Chunk = chunk;
        }

        public readonly SolStatement Afterthought;
        public readonly SolExpression Condition;
        public readonly SolStatement Initialization;
        public readonly SolChunk Chunk;

        public override SolValue Execute(SolExecutionContext context) {
            DidTerminateParent = false;
            SolExecutionContext localContext = new SolExecutionContext(context.Assembly);
            localContext.VariableContext.ParentContext = context.VariableContext;
            SolValue returnValue = Initialization.Execute(localContext);
            while (Condition.Evaluate(localContext).IsTrue()) {
                // The chunk is running in a new context in order to discard the
                // locals for the previous iteration.
                returnValue = Chunk.Execute(localContext, SolChunk.ContextMode.RunInLocal);
                if (Chunk.DidTerminateParent) {
                    DidTerminateParent = true;
                    return returnValue;
                }
                returnValue = Afterthought.Execute(localContext);
            }
            return returnValue;
        }

        protected override string ToString_Impl() {
            return $"Statement_For(Initialization={Initialization}, Condition={Condition}, "+
                $"Afterthought={Afterthought}, Chunk={Chunk})";
        }
    }
}