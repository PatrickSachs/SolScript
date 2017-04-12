using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;
using SolScript.Utility;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     A chunk is a series of statements running in their own variable context. Blocks are typically used as the body of
    ///     functions or generally inside isolated blocks such as iterator bodies.
    /// </summary>
    public class SolChunk : ISourceLocateable
    {
        /// <summary>
        ///     Creates a new chunk.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="location">The source location.</param>
        /// <param name="returnExpression">The optional return/break/continue expression.</param>
        /// <param name="statements">The statements in this chunk.</param>
        public SolChunk(SolAssembly assembly, SolSourceLocation location, [CanBeNull] TerminatingSolExpression returnExpression, params SolStatement[] statements)
        {
            Assembly = assembly;
            Id = s_NextId++;
            ReturnExpression = returnExpression;
            m_Statements = new Array<SolStatement>(statements);
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
        ///     The optional return/continue/break expression.
        /// </summary>
        [CanBeNull] public readonly TerminatingSolExpression ReturnExpression;

        /// <summary>
        ///     The statements in this chunk.
        /// </summary>
        public IReadOnlyList<SolStatement> Statements => m_Statements;

        #region ISourceLocateable Members

        /// <inheritdoc />
        public SolSourceLocation Location { get; }

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
            if (ReturnExpression != null) {
                SolValue value = ReturnExpression.Evaluate(context, variables, out terminators);
                return value;
            }
            terminators = Terminators.None;
            return SolNil.Instance;
        }
    }
}