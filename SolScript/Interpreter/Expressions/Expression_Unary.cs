using Irony.Parsing;
using SolScript.Compiler;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    public class Expression_Unary : SolExpression
    {
        public Expression_Unary() {}

        /// <inheritdoc />
        public Expression_Unary(OperationRef operation, SolExpression valueGetter) : base()
        {
            Operation = operation;
            ValueGetter = valueGetter;
        }

        public OperationRef Operation { get; internal set; }
        public SolExpression ValueGetter { get; internal set; }

        #region Overrides

        /// <inheritdoc />
        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            context.CurrentLocation = Location;
            return Operation.Perform(ValueGetter.Evaluate(context, parentVariables), context);
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return $"{Operation.Name}{ValueGetter}";
        }

        /// <inheritdoc />
        // todo: determine contstant by taking op and value into account
        public override bool IsConstant => false;

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            ValidationResult valRes = ValueGetter.Validate(context);
            if (!valRes) {
                return ValidationResult.Failure();
            }
            // todo: validate operation
            return new ValidationResult(true, valRes.Type);
        }

        #endregion

        #region Nested type: GetNOperation

        public class GetNOperation : OperationRef
        {
            private GetNOperation() {}

            public static readonly GetNOperation Instance = new GetNOperation();
            public override string Name => "#";

            #region Overrides

            public override SolValue Perform(SolValue value, SolExecutionContext context)
            {
                return value.GetN(context);
            }

            #endregion
        }

        #endregion

        #region Nested type: MinusOperation

        public class MinusOperation : OperationRef
        {
            private MinusOperation() {}

            public static readonly MinusOperation Instance = new MinusOperation();
            public override string Name => "-";

            #region Overrides

            public override SolValue Perform(SolValue value, SolExecutionContext context)
            {
                return value.Minus(context);
            }

            #endregion
        }

        #endregion

        #region Nested type: NotOperation

        public class NotOperation : OperationRef
        {
            private NotOperation() {}

            public static NotOperation Instance = new NotOperation();
            public override string Name => "!";

            #region Overrides

            public override SolValue Perform(SolValue value, SolExecutionContext context)
            {
                return value.Not(context);
            }

            #endregion
        }

        #endregion

        #region Nested type: OperationRef

        public abstract class OperationRef // : ISolCompileable
        {
            /// <summary>
            /// An easily debuggable display name.
            /// </summary>
            public abstract string Name { get; }

            /// <summary>
            /// Performs the operation.
            /// </summary>
            /// <param name="value">The value to perform the operation on.</param>
            /// <param name="context">The current context.</param>
            /// <returns>The result.</returns>
            public abstract SolValue Perform(SolValue value, SolExecutionContext context);

            ///// <inheritdoc />
            //public abstract ValidationResult Validate(SolValidationContext context);
        }

        #endregion

        #region Nested type: PlusOperation

        public class PlusOperation : OperationRef
        {
            private PlusOperation() {}

            public static PlusOperation Instance = new PlusOperation();

            public override string Name => "+";

            #region Overrides

            public override SolValue Perform(SolValue value, SolExecutionContext context)
            {
                return value.Plus(context);
            }

            #endregion
        }

        #endregion
    }
}