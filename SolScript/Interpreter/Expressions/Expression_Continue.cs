using System;
using System.Collections.Generic;
using SolScript.Compiler;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     This expression is used to jump to the next iterator block. (Terminates)
    /// </summary>
    public class Expression_Continue : TerminatingSolExpression
    {
        private Expression_Continue(SolAssembly assembly) : base(assembly, SolSourceLocation.Empty()) {}

        // The singleton holder.
        private static readonly Dictionary<SolAssembly, Expression_Continue> s_Lookup = new Dictionary<SolAssembly, Expression_Continue>();

        #region Overrides

        /// <inheritdoc />
        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            terminators = Terminators.Continue;
            return SolNil.Instance;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return "continue";
        }

        #endregion

        /// <summary>
        ///     Gets the singleton instance for the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The expression.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="assembly" /> is null.</exception>
        public static Expression_Continue InstanceOf(SolAssembly assembly)
        {
            Expression_Continue instance;
            if (s_Lookup.TryGetValue(assembly, out instance)) {
                return instance;
            }
            return s_Lookup[assembly] = new Expression_Continue(assembly);
        }

        /// <inheritdoc />
        public override bool IsConstant => false;

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            return new ValidationResult(true, SolType.AnyNil);
        }
    }
}