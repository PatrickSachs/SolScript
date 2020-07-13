using Irony.Parsing;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_For : SolStatement {
        public Statement_For(SolAssembly assembly, SourceLocation location, SolStatement initialization, 
            SolExpression condition, SolStatement afterthought, SolChunk chunk) 
            : base(assembly, location) {
            Initialization = initialization;
            Condition = condition;
            Afterthought = afterthought;
            Chunk = chunk;
        }

        public readonly SolStatement Afterthought;
        public readonly SolExpression Condition;
        public readonly SolStatement Initialization;
        public readonly SolChunk Chunk;

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables) {
            context.CurrentLocation = Location;
            Terminators = Terminators.None;
            //SolExecutionContext localContext = SolExecutionContext.Nested(context);
            SolValue stackValue = Initialization.Execute(context, parentVariables);
            while (Condition.Evaluate(context, parentVariables).IsTrue(context)) {
                // The chunk is running in a new context in order to discard the
                // locals for the previous iteration.
                ChunkVariables chunkVariables = new ChunkVariables(Assembly) {Parent = parentVariables};
                stackValue = Chunk.ExecuteInTarget(context, chunkVariables);
                Terminators terminators = Chunk.Terminators;
                if (InternalHelper.DidReturn(terminators)) {
                    Terminators = Terminators.Return;
                    return stackValue;
                }
                if (InternalHelper.DidBreak(terminators)) {
                    Terminators = Terminators.None;
                    break;
                }
                // Continue is breaking the chunk execution.
                if (InternalHelper.DidContinue(terminators)) {
                    Terminators = Terminators.None;
                }
                stackValue = Afterthought.Execute(context, parentVariables);
            }
            return stackValue;
        }

        protected override string ToString_Impl() {
            return $"Statement_For(Initialization={Initialization}, Condition={Condition}, "+
                $"Afterthought={Afterthought}, Chunk={Chunk})";
        }
    }
}