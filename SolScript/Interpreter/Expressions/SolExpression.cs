using Irony.Parsing;
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
    public abstract class SolExpression : ISourceLocateable //, ISourceLocationInjector
    {
        /// <summary>
        ///     Creates a new expression.
        /// </summary>
        /// <param name="assembly">The assembly this expression is in.</param>
        /// <param name="location">The source location of this expression.</param>
        protected SolExpression(SolAssembly assembly, SourceLocation location)
        {
            Assembly = assembly;
            InjectSourceLocation(location);
        }

        /// <summary>
        ///     Creates a new <see cref="SolExpression" />.
        /// </summary>
        protected SolExpression()
        {
            Assembly = SolAssembly.CurrentlyParsing;
        }

        /// <summary>
        ///     The assembly this expression is located in.
        /// </summary>
        public readonly SolAssembly Assembly;

        internal SourceLocation LocationMutable;

        #region ISourceLocateable Members

        /// <inheritdoc />
        public SourceLocation Location => LocationMutable;

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
        /// <returns>The result of the expression.</returns>
        public abstract SolValue Evaluate(SolExecutionContext context, IVariables parentVariables);

        /// <summary>
        ///     Formats the expression to a string for debugging purposes.
        /// </summary>
        /// <returns>The string.</returns>
        protected abstract string ToString_Impl();

        /// <inheritdoc />
        public void InjectSourceLocation(SourceLocation location)
        {
            LocationMutable = location;
            //SolDebug.WriteLine("Injected " + location + " into " + this + ".");
        }
    }
}