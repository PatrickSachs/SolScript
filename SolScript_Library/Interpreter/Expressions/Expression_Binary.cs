using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     A binary expression is made out of two expressions and one operation evaluating them.
    /// </summary>
    public class Expression_Binary : SolExpression
    {
        /// <inheritdoc />
        public Expression_Binary()
        {
        }

        /// <inheritdoc />
        public Expression_Binary(OperationRef operation, SolExpression first, SolExpression second)
        {
            Operation = operation;
            First = first;
            Second = second;
        }
        
        /// <summary>
        ///     The first expression.
        /// </summary>
        public SolExpression First { get; internal set; }

        /// <summary>
        ///     The operation resolving the two expressions.
        /// </summary>
        public OperationRef Operation { get; internal set; }

        /// <summary>
        ///     The second expression.
        /// </summary>
        public SolExpression Second { get; internal set; }

        #region Overrides

        /// <inheritdoc />
        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            context.CurrentLocation = Location;
            return Operation.Perform(this, parentVariables, context);
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return $"{First} {Operation.Operator1} {Second}";
        }

        #endregion

        #region Nested type: Addition

        /// <summary>
        ///     This operation is used to add two values together.<br />Returns the result of the addition.
        /// </summary>
        public class Addition : OperationRef
        {
            private Addition() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly Addition Instance = new Addition();

            /// <inheritdoc />
            public override string Operator1 => "+";

            #region Overrides

            /// <inheritdoc />
            public override SolValue Perform(Expression_Binary expression, IVariables parentVariables, SolExecutionContext context)
            {
                return expression.First.Evaluate(context, parentVariables).Add(context, expression.Second.Evaluate(context, parentVariables));
            }

            #endregion
        }

        #endregion

        #region Nested type: And

        /// <summary>
        ///     The and value is used to evaluate if two values are true or false.<br />If the first value is false the operation
        ///     return false and does not evaluate the second.<br />If the first value is not false the operation returns if the
        ///     second value is true.
        /// </summary>
        public class And : OperationRef
        {
            private And() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly And Instance = new And();

            /// <inheritdoc />
            public override string Operator1 => "&&";

            #region Overrides

            /// <inheritdoc />
            public override SolValue Perform(Expression_Binary expression, IVariables parentVariables, SolExecutionContext context)
            {
                if (expression.First.Evaluate(context, parentVariables).IsFalse(context)) {
                    return SolBool.False;
                }
                return SolBool.ValueOf(expression.Second.Evaluate(context, parentVariables).IsTrue(context));
            }

            #endregion
        }

        #endregion

        #region Nested type: CompareEqual

        /// <summary>
        ///     This operation checks if two values are equal.<br /> Returns true if they are, false if not.
        /// </summary>
        public class CompareEqual : OperationRef
        {
            private CompareEqual() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly CompareEqual Instance = new CompareEqual();

            /// <inheritdoc />
            public override string Operator1 => "==";

            #region Overrides

            /// <inheritdoc />
            public override SolValue Perform(Expression_Binary expression, IVariables parentVariables, SolExecutionContext context)
            {
                return SolBool.ValueOf(expression.First.Evaluate(context, parentVariables).IsEqual(context, expression.Second.Evaluate(context, parentVariables)));
            }

            #endregion
        }

        #endregion

        #region Nested type: CompareGreater

        /// <summary>
        ///     This operation checks if the left value is greater than the right.<br /> Returns true if it is, false if not.
        /// </summary>
        public class CompareGreater : OperationRef
        {
            private CompareGreater() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly CompareGreater Instance = new CompareGreater();

            /// <inheritdoc />
            public override string Operator1 => ">";

            #region Overrides

            /// <inheritdoc />
            public override SolValue Perform(Expression_Binary expression, IVariables parentVariables, SolExecutionContext context)
            {
                return SolBool.ValueOf(expression.First.Evaluate(context, parentVariables).GreaterThan(context, expression.Second.Evaluate(context, parentVariables)));
            }

            #endregion
        }

        #endregion

        #region Nested type: CompareGreaterOrEqual

        /// <summary>
        ///     This operation checks if the left value is greater or equal to the right.<br />Returns true if it is, false if not.
        /// </summary>
        public class CompareGreaterOrEqual : OperationRef
        {
            private CompareGreaterOrEqual() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly CompareGreaterOrEqual Instance = new CompareGreaterOrEqual();

            /// <inheritdoc />
            public override string Operator1 => ">=";

            #region Overrides

            /// <inheritdoc />
            public override SolValue Perform(Expression_Binary expression, IVariables parentVariables, SolExecutionContext context)
            {
                return SolBool.ValueOf(expression.First.Evaluate(context, parentVariables).GreaterThanOrEqual(context, expression.Second.Evaluate(context, parentVariables)));
            }

            #endregion
        }

        #endregion

        #region Nested type: CompareNotEqual

        /// <summary>
        ///     This operation checks if two values are not equal.<br />Returns true if they are not equal, false if they are
        ///     equal.
        /// </summary>
        public class CompareNotEqual : OperationRef
        {
            private CompareNotEqual() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly CompareNotEqual Instance = new CompareNotEqual();

            /// <inheritdoc />
            public override string Operator1 => "!=";

            #region Overrides

            /// <inheritdoc />
            public override SolValue Perform(Expression_Binary expression, IVariables parentVariables, SolExecutionContext context)
            {
                return SolBool.ValueOf(expression.First.Evaluate(context, parentVariables).NotEqual(context, expression.Second.Evaluate(context, parentVariables)));
            }

            #endregion
        }

        #endregion

        #region Nested type: CompareSmaller

        /// <summary>
        ///     This operation checks if the left value is smaller than the right.<br />Returns true if it is, false if not.
        /// </summary>
        public class CompareSmaller : OperationRef
        {
            private CompareSmaller() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly CompareSmaller Instance = new CompareSmaller();

            /// <inheritdoc />
            public override string Operator1 => "<";

            #region Overrides

            /// <inheritdoc />
            public override SolValue Perform(Expression_Binary expression, IVariables parentVariables, SolExecutionContext context)
            {
                return SolBool.ValueOf(expression.First.Evaluate(context, parentVariables).SmallerThan(context, expression.Second.Evaluate(context, parentVariables)));
            }

            #endregion
        }

        #endregion

        #region Nested type: CompareSmallerOrEqual

        /// <summary>
        ///     This operation checks if the left value is smaller or equal to the right.<br />Returns true if it is, false if not.
        /// </summary>
        public class CompareSmallerOrEqual : OperationRef
        {
            private CompareSmallerOrEqual() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly CompareSmallerOrEqual Instance = new CompareSmallerOrEqual();

            /// <inheritdoc />
            public override string Operator1 => "<=";

            #region Overrides

            /// <inheritdoc />
            public override SolValue Perform(Expression_Binary expression, IVariables parentVariables, SolExecutionContext context)
            {
                return SolBool.ValueOf(expression.First.Evaluate(context, parentVariables).SmallerThanOrEqual(context, expression.Second.Evaluate(context, parentVariables)));
            }

            #endregion
        }

        #endregion

        #region Nested type: Concatenation

        /// <summary>Concats two values.<br />Returns the result of the operation.</summary>
        /// <remarks> Note: This operation is right-associative </remarks>
        public class Concatenation : OperationRef
        {
            private Concatenation() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly Concatenation Instance = new Concatenation();

            /// <inheritdoc />
            public override string Operator1 => "..";

            #region Overrides

            /// <inheritdoc />
            public override SolValue Perform(Expression_Binary expression, IVariables parentVariables, SolExecutionContext context)
            {
                return expression.First.Evaluate(context, parentVariables).Concatenate(context, expression.Second.Evaluate(context, parentVariables));
            }

            #endregion
        }

        #endregion

        #region Nested type: Division

        /// <summary>
        ///     Divides two values.<br />Returns the result of the division.
        /// </summary>
        public class Division : OperationRef
        {
            private Division() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly Division Instance = new Division();

            /// <inheritdoc />
            public override string Operator1 => "/";

            #region Overrides

            /// <inheritdoc />
            public override SolValue Perform(Expression_Binary expression, IVariables parentVariables, SolExecutionContext context)
            {
                return expression.First.Evaluate(context, parentVariables).Divide(context, expression.Second.Evaluate(context, parentVariables));
            }

            #endregion
        }

        #endregion

        #region Nested type: Exponentiation

        /// <summary>Performs an exponentiation with the two values.<br />Returns the result of the exponentiation.</summary>
        /// <remarks> Note: This operation is right-associative </remarks>
        public class Exponentiation : OperationRef
        {
            private Exponentiation() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly Exponentiation Instance = new Exponentiation();


            /// <inheritdoc />
            public override string Operator1 => "^";

            #region Overrides

            /// <inheritdoc />
            public override SolValue Perform(Expression_Binary expression, IVariables parentVariables, SolExecutionContext context)
            {
                return expression.First.Evaluate(context, parentVariables).Exponentiate(context, expression.Second.Evaluate(context, parentVariables));
            }

            #endregion
        }

        #endregion

        #region Nested type: Modulo

        /// <summary>
        ///     Determines the reminder between two values.<br />Returns the reminder.
        /// </summary>
        public class Modulo : OperationRef
        {
            private Modulo() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly Modulo Instance = new Modulo();

            /// <inheritdoc />
            public override string Operator1 => "%";

            #region Overrides

            /// <inheritdoc />
            public override SolValue Perform(Expression_Binary expression, IVariables parentVariables, SolExecutionContext context)
            {
                return expression.First.Evaluate(context, parentVariables).Modulo(context, expression.Second.Evaluate(context, parentVariables));
            }

            #endregion
        }

        #endregion

        #region Nested type: Multiplication

        /// <summary>
        ///     This operation is used to multiply one value by another.
        /// </summary>
        public class Multiplication : OperationRef
        {
            private Multiplication() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly Multiplication Instance = new Multiplication();

            /// <inheritdoc />
            public override string Operator1 => "*";

            #region Overrides

            /// <inheritdoc />
            public override SolValue Perform(Expression_Binary expression, IVariables parentVariables, SolExecutionContext context)
            {
                return expression.First.Evaluate(context, parentVariables).Multiply(context, expression.Second.Evaluate(context, parentVariables));
            }

            #endregion
        }

        #endregion

        #region Nested type: NilCoalescing

        /// <summary>
        ///     This operation is used to filter out nil values.
        /// </summary>
        public class NilCoalescing : OperationRef
        {
            private NilCoalescing() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly NilCoalescing Instance = new NilCoalescing();

            /// <inheritdoc />
            public override string Operator1 => "??";

            #region Overrides

            /// <inheritdoc />
            public override SolValue Perform(Expression_Binary expression, IVariables parentVariables, SolExecutionContext context)
            {
                SolValue left = expression.First.Evaluate(context, parentVariables);
                if (left.Type != SolNil.TYPE) {
                    return left;
                }
                return expression.Second.Evaluate(context, parentVariables);
            }

            #endregion
        }

        #endregion

        #region Nested type: OperationRef

        /// <summary>
        ///     An operation used to resolve a binary expression.
        /// </summary>
        public abstract class OperationRef
        {
            /// <summary>
            ///     The name of the operator. Debugging purposes only.
            /// </summary>
            public abstract string Operator1 { get; }

            /// <summary>
            ///     Performs the operation.
            /// </summary>
            /// <param name="expression">The expression to evaluate on.</param>
            /// <param name="parentVariables">The currently active parent variables.</param>
            /// <param name="context">The execution context we are in.</param>
            /// <returns>The evaluated value.</returns>
            public abstract SolValue Perform(Expression_Binary expression, IVariables parentVariables, SolExecutionContext context);
        }

        #endregion

        #region Nested type: Or

        /// <summary>
        ///     The and value is used to evaluate if one of the two values is true.<br />If the first value is true the operation
        ///     return true and does not evaluate the second.<br />If the first value is not true the operation returns if the
        ///     second value is true.
        /// </summary>
        public class Or : OperationRef
        {
            private Or() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly Or Instance = new Or();

            /// <inheritdoc />
            public override string Operator1 => "||";

            #region Overrides

            /// <inheritdoc />
            public override SolValue Perform(Expression_Binary expression, IVariables parentVariables, SolExecutionContext context)
            {
                if (expression.First.Evaluate(context, parentVariables).IsTrue(context)) {
                    return SolBool.True;
                }
                return SolBool.ValueOf(expression.Second.Evaluate(context, parentVariables).IsTrue(context));
            }

            #endregion
        }

        #endregion

        #region Nested type: Substraction

        /// <summary>
        ///     Subtracts two values from another. <br />Returns the result of the substraction.
        /// </summary>
        public class Substraction : OperationRef
        {
            private Substraction() {}

            /// <summary>
            ///     The singleton instance.
            /// </summary>
            public static readonly Substraction Instance = new Substraction();

            /// <inheritdoc />
            public override string Operator1 => "-";

            #region Overrides

            /// <inheritdoc />
            public override SolValue Perform(Expression_Binary expression, IVariables parentVariables, SolExecutionContext context)
            {
                return expression.First.Evaluate(context, parentVariables).Subtract(context, expression.Second.Evaluate(context, parentVariables));
            }

            #endregion
        }

        #endregion
    }
}