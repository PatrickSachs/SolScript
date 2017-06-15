using System;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Compiler;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_Do : SolStatement {
        public Statement_Do([NotNull] SolAssembly assembly, SourceLocation location, SolChunk chunk) : base(assembly, location) {
            Chunk = chunk;
        }

        public readonly SolChunk Chunk;
        
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators) {
            Variables isolatedVariables = new Variables(Assembly) {Parent = parentVariables};
            return Chunk.Execute(context, isolatedVariables, out terminators);
        }

        protected override string ToString_Impl() {
            return $"Statement_Do({Chunk})";
        }

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            return Chunk.Validate(context);
        }
    }
}