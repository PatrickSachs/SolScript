using System;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     This expression is used to return a constant value.
    /// </summary>
    public sealed class Expression_Literal : SolExpression
    {
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" /></exception>
        public Expression_Literal(SolValue value) : base(SolAssembly.CurrentlyParsing, SolSourceLocation.Empty())
        {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            Value = value;
        }

        /// <summary>
        ///     The value this expression evaluates to.
        /// </summary>
        public SolValue Value { get; }

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
}