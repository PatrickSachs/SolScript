using Irony.Parsing;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Utility;

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

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators) {
            context.CurrentLocation = Location;
            //SolExecutionContext localContext = SolExecutionContext.Nested(context);
            Terminators initTerminators;
            SolValue stackValue = Initialization.Execute(context, parentVariables, out initTerminators);
            while (Condition.Evaluate(context, parentVariables).IsTrue(context)) {
                // The chunk is running in a new context in order to discard the
                // locals for the previous iteration.
                Variables variables = new Variables(Assembly) {Parent = parentVariables};
                Terminators chunkTerminators;
                stackValue = Chunk.Execute(context, variables, out chunkTerminators);
                if (InternalHelper.DidReturn(chunkTerminators)) {
                    terminators = Terminators.Return;
                    return stackValue;
                }
                if (InternalHelper.DidBreak(chunkTerminators)) {
                    break;
                }
                // Continue is breaking the chunk execution.
                if (InternalHelper.DidContinue(chunkTerminators)) {
                }
                Terminators afterTerminators;
                stackValue = Afterthought.Execute(context, parentVariables, out afterTerminators);
            }
            terminators = Terminators.None;
            return stackValue;
        }

        protected override string ToString_Impl() {
            return $"Statement_For(Initialization={Initialization}, Condition={Condition}, "+
                $"Afterthought={Afterthought}, Chunk={Chunk})";
        }
    }
}