using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     A tertiary expression is made out of three expressions and one operation resulting in a single value.
    /// </summary>
    /// <seealso cref="Expression_Binary" />
    /// <seealso cref="Expression_Unary" />
    public sealed class Expression_Tertiary : SolExpression
    {
        /// <inheritdoc />
        public Expression_Tertiary(SolAssembly assembly, SolSourceLocation location,
            OperationRef operation, SolExpression first, SolExpression second, SolExpression third) : base(assembly, location)
        {
            Operation = operation;
            First = first;
            Second = second;
            Third = third;
        }

        /// <summary>
        ///     The first expression.
        /// </summary>
        public readonly SolExpression First;

        /// <summary>
        ///     The operation resolving the three expressions.
        /// </summary>
        public readonly OperationRef Operation;

        /// <summary>
        ///     The second expression.
        /// </summary>
        public readonly SolExpression Second;

        /// <summary>
        ///     The thrid expression.
        /// </summary>
        public readonly SolExpression Third;

        #region Overrides

        /// <inheritdoc />
        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            return Operation.Perform(this, parentVariables, context);
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return First + Operation.Operator1 + Second + Operation.Operator2 + Third;
        }

        #endregion

        #region Nested type: Conditional

        /// <summary>
        ///     Used to resolve expressions in the style of <c>(First ? Second : Third)</c><br />If
        ///     <see cref="Expression_Tertiary.First" /> is true then the operation returns
        ///     <see cref="Expression_Tertiary.Second" />, otherwise <see cref="Expression_Tertiary.Third" />.
        /// </summary>
        public sealed class Conditional : OperationRef
        {
            private Conditional() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly Conditional Instance = new Conditional();

            /// <inheritdoc />
            public override string Operator1 => "?";

            /// <inheritdoc />
            public override string Operator2 => ":";

            #region Overrides

            /// <inheritdoc />
            public override SolValue Perform(Expression_Tertiary expression, IVariables parentVariables, SolExecutionContext context)
            {
                if (expression.First.Evaluate(context, parentVariables).IsTrue(context)) {
                    return expression.Second.Evaluate(context, parentVariables);
                }
                return expression.Third.Evaluate(context, parentVariables);
            }

            #endregion
        }

        #endregion

        #region Nested type: OperationRef

        /// <summary>
        ///     An operation used to evaluate a tertiary expression.
        /// </summary>
        public abstract class OperationRef
        {
            /// <summary>
            ///     The name of the first operation.
            /// </summary>
            public abstract string Operator1 { get; }

            /// <summary>
            ///     The name of the second operation.
            /// </summary>
            public abstract string Operator2 { get; }

            /// <summary>
            ///     Performs the evaluation and returns the value.
            /// </summary>
            /// <param name="expression">The expression being evaluated.</param>
            /// <param name="parentVariables">The parent variables.</param>
            /// <param name="context">The current context.</param>
            /// <returns>The evaluated value.</returns>
            // The tertiary values are resolved on demand since not all three may be required. And possibly not even be desired to be evaluated. 
            public abstract SolValue Perform(Expression_Tertiary expression, IVariables parentVariables, SolExecutionContext context);
        }

        #endregion
    }
}