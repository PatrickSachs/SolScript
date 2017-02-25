using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     This expression is used to statically return boolean values.
    /// </summary>
    public sealed class Expression_Bool : SolExpression
    {
        /// <inheritdoc />
        public Expression_Bool(SolAssembly assembly, SolSourceLocation location, SolBool value) : base(assembly, location)
        {
            Value = value;
        }

        /// <summary>
        ///     The bool value this expression evaluates to.
        /// </summary>
        public SolBool Value { get; }

        #region Overrides

        /// <inheritdoc />
        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            return Value;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return Value.Value ? SolBool.TRUE_STRING : SolBool.FALSE_STRING;
        }

        #endregion
    }
}