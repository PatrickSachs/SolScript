using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public class Expression_Binary : SolExpression {
        public SolExpression Left;
        public OperationRef Operation;
        public SolExpression Right;

        public override SolValue Evaluate(SolExecutionContext context) {
            context.CurrentLocation = Location;
            return Operation.Perform(Left.Evaluate(context), Right.Evaluate(context), context);
        }

        protected override string ToString_Impl() {
            return $"Expression_Binary(Left={Left}, Right={Right}, Operation={Operation.GetType().Name})";
        }

        #region Nested type: Addition

        public class Addition : OperationRef {
            private Addition() {
            }

            public static readonly Addition Instance = new Addition();

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.Add(right);
            }
        }

        #endregion

        #region Nested type: And

        public class And : OperationRef {
            private And() {
            }

            public static readonly And Instance = new And();

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.And(right);
            }
        }

        #endregion

        #region Nested type: CompareEqual

        public class CompareEqual : OperationRef {
            private CompareEqual() {
            }

            public static readonly CompareEqual Instance = new CompareEqual();

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return SolBoolean.ValueOf(left.IsEqual(right));
            }
        }

        #endregion

        #region Nested type: CompareGreater

        public class CompareGreater : OperationRef {
            private CompareGreater() {
            }

            public static readonly CompareGreater Instance = new CompareGreater();

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return SolBoolean.ValueOf(left.GreaterThan(right));
            }
        }

        #endregion

        #region Nested type: CompareGreaterOrEqual

        public class CompareGreaterOrEqual : OperationRef {
            private CompareGreaterOrEqual() {
            }

            public static readonly CompareGreaterOrEqual Instance = new CompareGreaterOrEqual();

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return SolBoolean.ValueOf(left.GreaterThanOrEqual(right));
            }
        }

        #endregion

        #region Nested type: CompareNotEqual

        public class CompareNotEqual : OperationRef {
            private CompareNotEqual() {
            }

            public static readonly CompareNotEqual Instance = new CompareNotEqual();

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return SolBoolean.ValueOf(left.NotEqual(right));
            }
        }

        #endregion

        #region Nested type: CompareSmaller

        public class CompareSmaller : OperationRef {
            private CompareSmaller() {
            }

            public static readonly CompareSmaller Instance = new CompareSmaller();

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return SolBoolean.ValueOf(left.SmallerThan(right));
            }
        }

        #endregion

        #region Nested type: CompareSmallerOrEqual

        public class CompareSmallerOrEqual : OperationRef {
            private CompareSmallerOrEqual() {
            }

            public static readonly CompareSmallerOrEqual Instance = new CompareSmallerOrEqual();

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return SolBoolean.ValueOf(left.SmallerThanOrEqual(right));
            }
        }

        #endregion

        #region Nested type: Concatenation

        /// <remarks> Note: This operation is right-associative </remarks>
        public class Concatenation : OperationRef {
            private Concatenation() {
            }

            public static readonly Concatenation Instance = new Concatenation();

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.Concatenate(right);
            }
        }

        #endregion

        #region Nested type: Division

        public class Division : OperationRef {
            private Division() {
            }

            public static readonly Division Instance = new Division();

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.Divide(right);
            }
        }

        #endregion

        #region Nested type: Exponentiation

        /// <remarks> Note: This operation is right-associative </remarks>
        public class Exponentiation : OperationRef {
            private Exponentiation() {
            }

            public static readonly Exponentiation Instance = new Exponentiation();

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.Exponentiate(right);
            }
        }

        #endregion

        #region Nested type: Modulus

        public class Modulus : OperationRef {
            private Modulus() {
            }

            public static readonly Modulus Instance = new Modulus();

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.Modulu(right);
            }
        }

        #endregion

        #region Nested type: Multiplication

        public class Multiplication : OperationRef {
            private Multiplication() {
            }

            public static readonly Multiplication Instance = new Multiplication();

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.Multiply(right);
            }
        }

        #endregion

        #region Nested type: OperationRef

        public abstract class OperationRef {
            public abstract SolValue Perform(SolValue left, SolValue right, SolExecutionContext context);
        }

        #endregion

        #region Nested type: Or

        public class Or : OperationRef {
            private Or() {
            }

            public static readonly Or Instance = new Or();

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.Or(right);
            }
        }

        #endregion

        #region Nested type: Substraction

        public class Substraction : OperationRef {
            private Substraction() {
            }

            public static readonly Substraction Instance = new Substraction();

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.Subtract(right);
            }
        }

        #endregion

        public Expression_Binary(SourceLocation location) : base(location) {
        }
    }
}