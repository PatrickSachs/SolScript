using System;
using JetBrains.Annotations;
using SolScript.Compiler;
using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;
using SolScript.Utility;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     This expression wraps a statement inside of it. The runtime allows the wrapping of any statement while the language
    ///     only supports a selected few.
    /// </summary>
    public class Expression_Statement : TerminatingSolExpression
    {
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="statement"/> is <see langword="null"/></exception>
        public Expression_Statement(SolStatement statement)
        {
            if (statement == null) {
                throw new ArgumentNullException(nameof(statement));
            }
            Statement = statement;
        }

        /// <summary>
        ///     Used by the parser.
        /// </summary>
        [Obsolete(InternalHelper.O_PARSER_MSG, InternalHelper.O_PARSER_ERR)]
        internal Expression_Statement() {}

        /// <summary>
        ///     The statement wrapped in this expression.
        /// </summary>
        public SolStatement Statement { get; /*[UsedImplicitly] internal set;*/ }

        #region Overrides

        /// <inheritdoc />
        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            context.CurrentLocation = Location;
            SolValue value = Statement.Execute(context, parentVariables, out terminators);
            return value;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return Statement.ToString();
        }

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            return Statement.Validate(context);
        }

        /// <inheritdoc />
        public override bool IsConstant => false;

        #endregion
    }
}