using System;
using System.Collections.Generic;
using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     This expression is used to break out of iterator blocks. (Terminates)
    /// </summary>
    public class Expression_Break : TerminatingSolExpression
    {
        /// <inheritdoc />
        private Expression_Break(SolAssembly assembly) : base(assembly, SolSourceLocation.Empty()) {}

        #region Overrides

        /// <inheritdoc />
        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            terminators = Terminators.Break;
            return SolNil.Instance;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return "break";
        }

        #endregion

        // The singleton holder.
        private static readonly Dictionary<SolAssembly, Expression_Break> s_Lookup = new Dictionary<SolAssembly, Expression_Break>();

        /// <summary>
        ///     Gets the singleton instance for the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The expression.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="assembly" /> is null.</exception>
        public static Expression_Break InstanceOf(SolAssembly assembly)
        {
            if (assembly == null) {
                throw new ArgumentNullException(nameof(assembly));
            }
            Expression_Break instance;
            if (s_Lookup.TryGetValue(assembly, out instance)) {
                return instance;
            }
            return s_Lookup[assembly] = new Expression_Break(assembly);
        }
    }
}