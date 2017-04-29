/*using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     An expression that always evaluates to a fixed number value.
    /// </summary>
    public class Expression_Number : SolExpression
    {
        public Expression_Number()
        {
            
        }

        /// <inheritdoc />
        public Expression_Number(SolAssembly assembly, SolSourceLocation location, SolNumber value) : base(assembly, location)
        {
            MutableValue = value;
        }

        internal SolNumber MutableValue;

        /// <summary>
        ///     The number this expression evaluates to.
        /// </summary>
        public SolNumber Value => MutableValue;

        #region Overrides

        /// <inheritdoc />
        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            return Value;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return Value.ToString();
        }

        #endregion
    }
}*/