using System;
using System.Collections.Generic;
using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     This statement always returns nil. The advantage of using this expression instead of a literal expression with an
    ///     embedded nil is memory efficiency.
    /// </summary>
    public class Expression_Nil : SolExpression
    {
        private Expression_Nil(SolAssembly assembly) : base(assembly, SolSourceLocation.Empty()) {}

        // The singleton holder.
        private static readonly Dictionary<SolAssembly, Expression_Nil> s_Lookup = new Dictionary<SolAssembly, Expression_Nil>();

        #region Overrides

        /// <inheritdoc />
        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            return SolNil.Instance;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return "nil";
        }

        #endregion

        /// <summary>
        ///     Gets the singleton instance for the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The expression.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="assembly" /> is null.</exception>
        public static Expression_Nil InstanceOf(SolAssembly assembly)
        {
            if (assembly == null) {
                throw new ArgumentNullException(nameof(assembly));
            }
            Expression_Nil instance;
            if (s_Lookup.TryGetValue(assembly, out instance)) {
                return instance;
            }
            return s_Lookup[assembly] = new Expression_Nil(assembly);
        }
    }
}