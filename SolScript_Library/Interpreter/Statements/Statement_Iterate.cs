using Irony.Parsing;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_Iterate : SolStatement {
        public Statement_Iterate(SolAssembly assembly, SolSourceLocation location, SolExpression iteratorGetter, string iteratorName,
            SolChunk chunk) : base(assembly, location) {
            IteratorGetter = iteratorGetter;
            IteratorName = iteratorName;
            Chunk = chunk;
        }

        public readonly SolChunk Chunk;
        public readonly SolExpression IteratorGetter;
        public readonly string IteratorName;

        #region Overrides

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators) {
            Variables vars = new Variables(Assembly) {Parent = parentVariables};
            SolValue iterator = IteratorGetter.Evaluate(context, parentVariables);
            vars.Declare(IteratorName, new SolType("any", true));
            foreach (SolValue value in iterator.Iterate(context)) {
                Variables variables = new Variables(Assembly) {Parent = vars };
                vars.Assign(IteratorName, value);
                Terminators chunkTerminators;
                SolValue returnValue = Chunk.Execute(context, variables, out chunkTerminators);
                if (InternalHelper.DidReturn(chunkTerminators)) {
                    terminators = Terminators.Return;
                    return returnValue;
                }
                if (InternalHelper.DidBreak(chunkTerminators)) {
                    break;
                }
                // Continue is breaking the chunk execution.
                if (InternalHelper.DidContinue(chunkTerminators)) {
                }
            }
            terminators = Terminators.None;
            return SolNil.Instance;
        }

        protected override string ToString_Impl() {
            return $"Statement_Iterate(IteratorGetter={IteratorGetter}, IteratorName={IteratorName}, Chunk={Chunk})";
        }

        #endregion
    }
}