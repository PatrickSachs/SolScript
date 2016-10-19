using Irony.Parsing;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_Iterate : SolStatement {
        public Statement_Iterate(SourceLocation location, SolExpression iteratorGetter, string iteratorName,
            SolChunk chunk) : base(location) {
            IteratorGetter = iteratorGetter;
            IteratorName = iteratorName;
            Chunk = chunk;
        }

        public readonly SolChunk Chunk;
        public readonly SolExpression IteratorGetter;
        public readonly string IteratorName;

        public override SolValue Execute(SolExecutionContext context) {
            DidTerminateParent = false;
            SolExecutionContext localContext = new SolExecutionContext(context.Assembly);
            localContext.VariableContext.ParentContext = context.VariableContext;
            SolValue iterator = IteratorGetter.Evaluate(localContext);
            foreach (SolValue value in iterator.Iterate()) {
                localContext.VariableContext.SetValue(IteratorName, value, new SolType(value.Type, value.Type == "nil"),
                    true);
                SolValue returnValue = Chunk.Execute(localContext, SolChunk.ContextMode.RunInLocal);
                if (Chunk.DidTerminateParent) {
                    return returnValue;
                }
            }
            // what should an iterator block return?
            return SolNil.Instance;
        }

        protected override string ToString_Impl() {
            return $"Statement_Iterate(IteratorGetter={IteratorGetter}, IteratorName={IteratorName}, Chunk={Chunk})";
        }
    }
}