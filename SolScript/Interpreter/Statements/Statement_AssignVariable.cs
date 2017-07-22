using System;
using NodeParser;
using SolScript.Compiler;
using SolScript.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements
{
    /// <summary>
    ///     The assign var statement is used to assign values to variables. It can be chained due to also being an expression.
    /// </summary>
    public class Statement_AssignVariable : SolStatement//, IWrittenInClass
    {
        /// <summary>
        ///     Creates a new statement.
        /// </summary>
        /// <param name="location">The location in code this statement is at.</param>
        /// <param name="target">The operation used to actually assign the value.</param>
        /// <param name="valueGetter">The expression used to obtain the value that should be assigned.</param>
        /// <param name="assembly">The assembly this staement belongs to.</param>
        /// <exception cref="ArgumentNullException">An argument is <see langword="null" /></exception>
        public Statement_AssignVariable(SolAssembly assembly, NodeLocation location, AVariable target, SolExpression valueGetter) : base(assembly, location)
        {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }
            if (valueGetter == null) {
                throw new ArgumentNullException(nameof(valueGetter));
            }
            Target = target;
            ValueGetter = valueGetter;
        }

        /// <summary>
        ///     The operation used to actually assign the value.
        /// </summary>
        public readonly AVariable Target;

        /// <summary>
        ///     The expression used to obtain the value that should be assigned.
        /// </summary>
        public readonly SolExpression ValueGetter;

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">Failed to assign the variable.</exception>
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            context.CurrentLocation = Location;
            SolValue value = ValueGetter.Evaluate(context, parentVariables);
            try {
                value = Target.Set(value, context, parentVariables);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, "Failed to assign the variable.", ex);
            }
            terminators = Terminators.None;
            return value;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return $"{Target} = {ValueGetter}";
        }

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            ValidationResult target = Target.Validate(context);
            ValidationResult value = ValueGetter.Validate(context);
            // todo: get variable type if named
            return new ValidationResult(target && value, value.Type);
        }

        #endregion
    }
}