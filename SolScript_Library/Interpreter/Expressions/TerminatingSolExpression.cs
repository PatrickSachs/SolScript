using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    public abstract class TerminatingSolExpression : SolExpression
    {
        public TerminatingSolExpression(SolAssembly assembly, SolSourceLocation location) : base(assembly, location) {}
        
     
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