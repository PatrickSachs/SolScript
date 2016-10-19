using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public class Expression_Unary : SolExpression {
        public Expression_Unary(SourceLocation location) : base(location) {
        }

        public OperationRef Operation;
        public SolExpression ValueGetter;

        public override SolValue Evaluate(SolExecutionContext context)
        {
            context.CurrentLocation = Location;
            return Operation.Perform(ValueGetter.Evaluate(context), context);
        }

        protected override string ToString_Impl() {
            return $"Expression_Unary(Operation={Operation}, ValueGetter={ValueGetter})";
        }

        #region Nested type: GetNOperation

        public class GetNOperation : OperationRef {
            private GetNOperation() {
            }

            public static GetNOperation Instance = new GetNOperation();

            public override SolValue Perform(SolValue value, SolExecutionContext context) {
                return value.GetN();
            }
        }

        #endregion

        #region Nested type: MinusOperation

        public class MinusOperation : OperationRef {
            private MinusOperation() {
            }

            public static MinusOperation Instance = new MinusOperation();

            public override SolValue Perform(SolValue value, SolExecutionContext context) {
                return value.Minus();
            }
        }

        #endregion

        #region Nested type: NotOperation

        public class NotOperation : OperationRef {
            private NotOperation() {
            }

            public static NotOperation Instance = new NotOperation();

            public override SolValue Perform(SolValue value, SolExecutionContext context) {
                return value.Not();
            }
        }

        #endregion

        #region Nested type: OperationRef

        public abstract class OperationRef {
            public abstract SolValue Perform(SolValue value, SolExecutionContext context);
        }

        #endregion

        #region Nested type: PlusOperation

        public class PlusOperation : OperationRef {
            private PlusOperation() {
            }

            public static PlusOperation Instance = new PlusOperation();

            public override SolValue Perform(SolValue value, SolExecutionContext context) {
                return value.Plus();
            }
        }

        #endregion
    }
}