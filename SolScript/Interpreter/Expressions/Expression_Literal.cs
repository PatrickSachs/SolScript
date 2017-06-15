using System;
using System.IO;
using SolScript.Compiler;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     This expression is used to return a constant value.
    /// </summary>
    public sealed class Expression_Literal : SolExpression
    {
        private Expression_Literal() : base(SolAssembly.CurrentlyParsing, SolSourceLocation.Empty())
        {
        }

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
        public SolValue Value { get; private set; }

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

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            return new ValidationResult(true, new SolType(Value.Type, Value is SolNil));
        }

        /// <inheritdoc />
        public override bool IsConstant => true;

        /*/// <inheritdoc />
        internal override Func<SolExpression> BytecodeFactory => () => new Expression_Literal();

        /// <inheritdoc />
        internal override byte BytecodeId => 0;*/

        /*/// <inheritdoc />
        /// <exception cref="IOException">An I/O error occured.</exception>
        /// <exception cref="SolCompilerException">Failed to compile. (See possible inner exceptions for details)</exception>
        protected override void CompileImpl(BinaryWriter writer, SolCompliationContext context)
        {
            writer.Write((byte)Value.TypeCode);
            switch (Value.TypeCode) {
                case SolTypeCode.Nil:
                    break;
                case SolTypeCode.Bool:
                    writer.Write(((SolBool)Value).Value ? (byte)1 : (byte)0);
                    break;
                case SolTypeCode.Number:
                    writer.Write(((SolNumber)Value).Value);
                    break;
                case SolTypeCode.String:
                    writer.Write(((SolString)Value).Value);
                    break;
                case SolTypeCode.Table:
                    break;
                case SolTypeCode.Function:
                    writer.Write(((SolFunction)Value).Id);
                    break;
                case SolTypeCode.Class:
                    writer.Write(((SolClass)Value).Id);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }*/

        #endregion
    }
}