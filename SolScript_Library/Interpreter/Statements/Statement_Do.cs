using System;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_Do : SolStatement {
        public Statement_Do([NotNull] SolAssembly assembly, SourceLocation location, SolChunk chunk) : base(assembly, location) {
            Chunk = chunk;
        }

        public readonly SolChunk Chunk;

        public override Terminators Terminators => Chunk.Terminators;

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables) {
            ChunkVariables isolatedVariables = new ChunkVariables(Assembly) {Parent = parentVariables};
            return Chunk.ExecuteInTarget(context, isolatedVariables);
        }

        protected override string ToString_Impl() {
            return $"Statement_Do({Chunk})";
        }
    }
}