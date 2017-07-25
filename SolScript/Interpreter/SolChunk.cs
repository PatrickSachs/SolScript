using System;
using System.Collections.Generic;
using NodeParser;
using PSUtility.Enumerables;
using SolScript.Compiler;
using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;
using SolScript.Utility;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     A chunk is a series of statements running in their own variable context. Blocks are typically used as the body of
    ///     functions or generally inside isolated blocks such as iterator bodies.
    /// </summary>
    public class SolChunk : ISourceLocateable, ISolCompileable
    {
        /// <summary>
        ///     Creates a new chunk.
        /// </summary>
        /// <param name="location">The code location of this chunk.</param>
        /// <param name="statements">The statements in this chunk.</param>
        /// <param name="assembly">The assembly of this chunk</param>
        public SolChunk(SolAssembly assembly, NodeLocation location, IEnumerable<SolStatement> statements)
        {
            Assembly = assembly;
            Id = s_NextId++;
            m_Statements = InternalHelper.CreateArray(statements);
            Location = location;
        }

        // The id of the next chunk.
        private static uint s_NextId;

        /// <summary>
        ///     The assembly.
        /// </summary>
        public readonly SolAssembly Assembly;

        /// <summary>
        ///     The id of this chunk.
        /// </summary>
        public readonly uint Id;

        // The statement array.
        private readonly Array<SolStatement> m_Statements;

        /// <summary>
        ///     The statements in this chunk.
        /// </summary>
        public ReadOnlyList<SolStatement> Statements => m_Statements.AsReadOnly();

        #region ISolCompileable Members

        /// <inheritdoc />
        public ValidationResult Validate(SolValidationContext context)
        {
            bool success = true;
            SolValidationContext.Chunk valChunk = new SolValidationContext.Chunk(this);
            context.Chunks.Push(valChunk);

            ValidationResult returnResult = null;
            foreach (SolStatement statement in Statements) {
                ValidationResult thisResult = statement.Validate(context);
                // Validate all statements even if one fails.
                if (success && !thisResult) {
                    success = false;
                }
            }

            /*ValidationResult lastResult = ReturnExpression?.Validate(context);
            if (!lastResult) {
                success = false;
            }*/

            SolValidationContext.Chunk popped = context.Chunks.Pop();
            if (!ReferenceEquals(popped, valChunk)) {
                // ReSharper disable once ExceptionNotDocumented
                throw new InvalidOperationException("Internal chunk validation corruption. Expected " + valChunk.SolChunk + " but got " + popped.SolChunk + ".");
            }
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse - No its not. Its even marked with [CanBeNull]
            return new ValidationResult(success, returnResult?.Type ?? new SolType(SolNil.TYPE, true));
        }

        #endregion

        #region ISourceLocateable Members

        /// <inheritdoc />
        public NodeLocation Location { get; }

        #endregion

        #region Overrides

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return 30 + (int) Id;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "Chunk#" + Id;
        }

        #endregion

        /// <summary>
        ///     Executes the statements in this block one by one.
        /// </summary>
        /// <param name="context">The current content.</param>
        /// <param name="variables">The variables this chunk should be executed in.</param>
        /// <param name="terminators">The terminators produced by this chunk execution.</param>
        /// <returns>The return value(or nil if no return statement).</returns>
        public SolValue Execute(SolExecutionContext context, IVariables variables, out Terminators terminators)
        {
            foreach (SolStatement statement in m_Statements) {
                SolValue value = statement.Execute(context, variables, out terminators);
                // If either return, break, or continue occured we break out of the current chunk.
                if (terminators != Terminators.None) {
                    return value;
                }
            }
            /*if (ReturnExpression != null) {
                SolValue value = ReturnExpression.Evaluate(context, variables, out terminators);
                return value;
            }*/
            terminators = Terminators.None;
            return SolNil.Instance;
        }
    }
}