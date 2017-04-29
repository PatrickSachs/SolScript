using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    /// Subclassing this expression allows an expression to return terminators. This is used to implement return, break and continue.
    /// </summary>
    // todo: remove terminating expressions entirely and make them statements. Not too big of a deal.
    public abstract class TerminatingSolExpression : SolExpression
    {
        /// <inheritdoc />
        protected TerminatingSolExpression(SolAssembly assembly, SourceLocation location) : base(assembly, location) {}

        /// <inheritdoc />
        protected TerminatingSolExpression() {}

        /// <inheritdoc />
        public sealed override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            Terminators terminators;
            //SolDebug.WriteLine("WARNING: CALLED A TERMINATING SOL EXPRESSION USING BASE METHOD. -- " + GetType().Name);
            return Evaluate(context, parentVariables, out terminators);
        }

        /// <inheritdoc />
        public abstract SolValue Evaluate(SolExecutionContext context, IVariables parentVariables, out Terminators terminators);
    }
}