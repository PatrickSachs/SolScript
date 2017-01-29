namespace SolScript.Interpreter.Expressions
{
    public abstract class TerminatingSolExpression : SolExpression, ITerminateable
    {
        public TerminatingSolExpression(SolAssembly assembly, SolSourceLocation location) : base(assembly, location) {}

        #region ITerminateable Members

        public abstract Terminators Terminators { get; }

        #endregion
    }
}