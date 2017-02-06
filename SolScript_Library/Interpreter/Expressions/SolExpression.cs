using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     A <see cref="SolExpression" /> is the base class for all expressions in SolScript. An expression differs from a
    ///     <see cref="SolStatement" /> by that fact that it cannot stand alone. An expression always needs a context in order
    ///     to have any further meaning(e.g. The numer literal '42' is an expression to create a number literal. Creating a
    ///     number literal itself would have very little purpose, so it needs to stand inside of a statement, e.g. the
    ///     parameters of a function call).
    /// </summary>
    public abstract class SolExpression : ISourceLocateable
    {
        /// <summary>
        ///     Creates a new <see cref="SolExpression" />.
        /// </summary>
        /// <param name="assembly">The assembly this expression is located in.</param>
        /// <param name="location">The location in code this expression is located at.</param>
        protected SolExpression(SolAssembly assembly, SolSourceLocation location)
        {
            Assembly = assembly;
            Location = location;
        }

        /// <summary>
        ///     The assembly this expression is located in.
        /// </summary>
        public readonly SolAssembly Assembly;

        #region ISourceLocateable Members

        /// <inheritdoc />
        public SolSourceLocation Location { get; }

        #endregion

        #region Overrides

        /// <inheritdoc />
        public override string ToString() => ToString_Impl();

        #endregion

        /// <summary>
        ///     This method evaluates the expression to produce the result of the expression. Be very careful and considerate when
        ///     it comes to data that persists between evaluations.
        /// </summary>
        /// <param name="context">The currently active execution context.</param>
        /// <param name="parentVariables">The current variable context for this expression.</param>
        /// <param name="terminators">The terminators. Terminators are used to break or continue in e.g. iterators.</param>
        /// <returns>The result of the expression.</returns>
        public abstract SolValue Evaluate(SolExecutionContext context, IVariables parentVariables);

        /// <summary>
        ///     Formats the expression to a string for debugging purposes.
        /// </summary>
        /// <returns>The string.</returns>
        protected abstract string ToString_Impl();
    }
}