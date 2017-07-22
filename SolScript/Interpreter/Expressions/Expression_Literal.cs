using System;
using System.IO;
using NodeParser;
using SolScript.Compiler;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     This expression is used to return a constant value.
    /// </summary>
    public sealed class Expression_Literal : SolExpression
    {
        /// <summary>
        /// Creates a literal expression.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="location">The location in code.</param>
        /// <param name="value">The value this expression evaluates to.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
        public Expression_Literal(SolAssembly assembly, NodeLocation location, SolValue value) : base(assembly, location)
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
            if (Value is SolString) {
                return "\"" + Value + "\"";
            }
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