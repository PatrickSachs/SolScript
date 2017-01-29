using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public class Expression_Binary : SolExpression {
        public Expression_Binary(SolAssembly assembly, SolSourceLocation location) : base(assembly, location) {
        }

        public SolExpression Left;
        public OperationRef Operation;
        public SolExpression Right;

        #region Overrides

        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables) {
            context.CurrentLocation = Location;
            return Operation.Perform(Left.Evaluate(context, parentVariables), Right.Evaluate(context, parentVariables), context);
        }

        protected override string ToString_Impl() {
            return $"{Left} {Operation.Name} {Right}";
        }

        #endregion

        public class NilCoalescing : OperationRef
        {
            private NilCoalescing()
            {
            }

            public static readonly NilCoalescing Instance = new NilCoalescing();
            public override string Name => "??";
            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.IsEqual(context, SolNil.Instance) ? right : left;
            }
        }

        #region Nested type: Addition

        public class Addition : OperationRef {
            private Addition() {
            }

            public static readonly Addition Instance = new Addition();

            public override string Name => "+";

            #region Overrides

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.Add(context, right);
            }

            #endregion
        }

        #endregion

        #region Nested type: And

        public class And : OperationRef {
            private And() {
            }

            public static readonly And Instance = new And();
            public override string Name => "&&";

            #region Overrides

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.And(context, right);
            }

            #endregion
        }

        #endregion

        #region Nested type: CompareEqual

        public class CompareEqual : OperationRef {
            private CompareEqual() {
            }

            public static readonly CompareEqual Instance = new CompareEqual();
            public override string Name => "==";

            #region Overrides

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return SolBool.ValueOf(left.IsEqual(context, right));
            }

            #endregion
        }

        #endregion

        #region Nested type: CompareGreater

        public class CompareGreater : OperationRef {
            private CompareGreater() {
            }

            public static readonly CompareGreater Instance = new CompareGreater();
            public override string Name => ">";

            #region Overrides

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return SolBool.ValueOf(left.GreaterThan(context, right));
            }

            #endregion
        }

        #endregion

        #region Nested type: CompareGreaterOrEqual

        public class CompareGreaterOrEqual : OperationRef {
            private CompareGreaterOrEqual() {
            }

            public static readonly CompareGreaterOrEqual Instance = new CompareGreaterOrEqual();

            public override string Name => ">=";

            #region Overrides

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return SolBool.ValueOf(left.GreaterThanOrEqual(context, right));
            }

            #endregion
        }

        #endregion

        #region Nested type: CompareNotEqual

        public class CompareNotEqual : OperationRef {
            private CompareNotEqual() {
            }

            public static readonly CompareNotEqual Instance = new CompareNotEqual();
            public override string Name => "!=";

            #region Overrides

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return SolBool.ValueOf(left.NotEqual(context, right));
            }

            #endregion
        }

        #endregion

        #region Nested type: CompareSmaller

        public class CompareSmaller : OperationRef {
            private CompareSmaller() {
            }

            public static readonly CompareSmaller Instance = new CompareSmaller();

            public override string Name => "<";

            #region Overrides

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return SolBool.ValueOf(left.SmallerThan(context, right));
            }

            #endregion
        }

        #endregion

        #region Nested type: CompareSmallerOrEqual

        public class CompareSmallerOrEqual : OperationRef {
            private CompareSmallerOrEqual() {
            }

            public static readonly CompareSmallerOrEqual Instance = new CompareSmallerOrEqual();

            public override string Name => "<=";

            #region Overrides

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return SolBool.ValueOf(left.SmallerThanOrEqual(context, right));
            }

            #endregion
        }

        #endregion

        #region Nested type: Concatenation

        /// <remarks> Note: This operation is right-associative </remarks>
        public class Concatenation : OperationRef {
            private Concatenation() {
            }

            public static readonly Concatenation Instance = new Concatenation();
            public override string Name => "..";

            #region Overrides

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.Concatenate(context, right);
            }

            #endregion
        }

        #endregion

        #region Nested type: Division

        public class Division : OperationRef {
            private Division() {
            }

            public static readonly Division Instance = new Division();
            public override string Name => "/";

            #region Overrides

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.Divide(context, right);
            }

            #endregion
        }

        #endregion

        #region Nested type: Exponentiation

        /// <remarks> Note: This operation is right-associative </remarks>
        public class Exponentiation : OperationRef {
            private Exponentiation() {
            }

            public static readonly Exponentiation Instance = new Exponentiation();

            public override string Name => "^";

            #region Overrides

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.Exponentiate(context, right);
            }

            #endregion
        }

        #endregion

        #region Nested type: Modulus

        public class Modulus : OperationRef {
            private Modulus() {
            }

            public static readonly Modulus Instance = new Modulus();
            public override string Name => "%";

            #region Overrides

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.Modulo(context, right);
            }

            #endregion
        }

        #endregion

        #region Nested type: Multiplication

        public class Multiplication : OperationRef {
            private Multiplication() {
            }

            public static readonly Multiplication Instance = new Multiplication();

            public override string Name => "*";

            #region Overrides

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.Multiply(context, right);
            }

            #endregion
        }

        #endregion

        #region Nested type: OperationRef

        public abstract class OperationRef {
            public abstract string Name { get; }
            public abstract SolValue Perform(SolValue left, SolValue right, SolExecutionContext context);
        }

        #endregion

        #region Nested type: Or

        public class Or : OperationRef {
            private Or() {
            }

            public static readonly Or Instance = new Or();
            public override string Name => "||";

            #region Overrides

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.Or(context, right);
            }

            #endregion
        }

        #endregion

        #region Nested type: Substraction

        public class Substraction : OperationRef {
            private Substraction() {
            }

            public static readonly Substraction Instance = new Substraction();
            public override string Name => "-";

            #region Overrides

            public override SolValue Perform(SolValue left, SolValue right, SolExecutionContext context) {
                return left.Subtract(context, right);
            }

            #endregion
        }

        #endregion
    }
}