using System;
using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_Do : SolStatement {
        public Statement_Do(SourceLocation location, SolChunk chunk) : base(location) {
            Chunk = chunk;
        }

        public readonly SolChunk Chunk;

        public override bool DidTerminateParent {
            get { return Chunk.DidTerminateParent; }
            protected set {
                throw new NotSupportedException("Cannot change DidTerminateParent property of a Statement_Do!");
            }
        }

        public override SolValue Execute(SolExecutionContext context) {
            return Chunk.Execute(context, SolChunk.ContextMode.RunInLocal);
        }

        protected override string ToString_Impl() {
            return $"Statement_Do({Chunk})";
        }
    }
}