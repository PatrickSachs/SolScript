using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Compiler;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Utility;

namespace SolScript.Interpreter.Statements {
    public class Statement_While : SolStatement {
        public Statement_While([NotNull] SolAssembly assembly, SourceLocation location, SolExpression condition, SolChunk chunk) : base(assembly, location) {
            Chunk = chunk;
            Condition = condition;
        }

        public readonly SolChunk Chunk;
        public readonly SolExpression Condition;

        #region Overrides

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators) {
            while (Condition.Evaluate(context, parentVariables).IsTrue(context)) {
                // The chunk is running in a new variable context in order to discard the
                // locals from the previous iteration.
                Variables variables = new Variables(Assembly) {Parent = parentVariables};
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

        /// <inheritdoc />
        protected override string ToString_Impl() {
            return $"Statement_While(Condition={Condition}, Chunk={Chunk})";
        }

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            if (!Condition.Validate(context)) {
                return ValidationResult.Failure();
            }
            return Chunk.Validate(context);
        }

        #endregion
    }
}