using System;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_Do : SolStatement {
        public Statement_Do([NotNull] SolAssembly assembly, SolSourceLocation location, SolChunk chunk) : base(assembly, location) {
            Chunk = chunk;
        }

        public readonly SolChunk Chunk;

        public override Terminators Terminators => Chunk.Terminators;

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables) {
            Variables isolatedVariables = new Variables(Assembly) {Parent = parentVariables};
            return Chunk.ExecuteInTarget(context, isolatedVariables);
        }

        protected override string ToString_Impl() {
            return $"Statement_Do({Chunk})";
        }
    }
}