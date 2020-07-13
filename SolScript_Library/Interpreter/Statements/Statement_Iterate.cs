using Irony.Parsing;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_Iterate : SolStatement {
        public Statement_Iterate(SolAssembly assembly, SourceLocation location, SolExpression iteratorGetter, string iteratorName,
            SolChunk chunk) : base(assembly, location) {
            IteratorGetter = iteratorGetter;
            IteratorName = iteratorName;
            Chunk = chunk;
        }

        public readonly SolChunk Chunk;
        public readonly SolExpression IteratorGetter;
        public readonly string IteratorName;

        #region Overrides

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables) {
            Terminators = Terminators.None;
            ChunkVariables vars = new ChunkVariables(Assembly);
            SolValue iterator = IteratorGetter.Evaluate(context, parentVariables);
            vars.Declare(IteratorName, new SolType("any", true));
            foreach (SolValue value in iterator.Iterate(context)) {
                ChunkVariables chunkVariables = new ChunkVariables(Assembly) {Parent = vars };
                vars.Assign(IteratorName, value);
                SolValue returnValue = Chunk.ExecuteInTarget(context, chunkVariables);
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
            return $"Statement_Iterate(IteratorGetter={IteratorGetter}, IteratorName={IteratorName}, Chunk={Chunk})";
        }

        #endregion
    }
}