// ---------------------------------------------------------------------
// SolScript - A simple but powerful scripting language.
// Offical repository: https://bitbucket.org/PatrickSachs/solscript/
// SolScript is licensed unter The MIT License.
// ---------------------------------------------------------------------
// ReSharper disable ArgumentsStyleStringLiteral

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using NodeParser;
using PSUtility.Enumerables;
using SolScript.Compiler;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Utility;

namespace SolScript.Interpreter.Statements
{
    /// <summary>
    ///     This statement is used for <c>if ... else if ... else ... end</c> statements. It allows to check various conditions
    ///     before falling back to an (optional) else chunk.
    /// </summary>
    public class Statement_Conditional : SolStatement
    {
        /// <summary>
        ///     Creates a new conditional statement.
        /// </summary>
        /// <param name="location">The code location.</param>
        /// <param name="if">An array of all if branches.</param>
        /// <param name="else">The (optional) fallback else branch.</param>
        /// <param name="assembly">The assembly.</param>
        /// <exception cref="ArgumentNullException"><paramref name="if" /> is <see langword="null" /></exception>
        public Statement_Conditional(SolAssembly assembly, NodeLocation location, IEnumerable<IfBranch> @if, [CanBeNull] SolChunk @else) : base(assembly, location)
        {
            if (@if == null) {
                throw new ArgumentNullException(nameof(@if));
            }
            m_If = InternalHelper.CreateArray(@if);
            Else = @else;
        }

        private readonly Array<IfBranch> m_If;

        /// <summary>
        ///     The chunk that will be executed if no if statement applies.
        /// </summary>
        [CanBeNull]
        public SolChunk Else { get; }

        /// <summary>
        ///     The if branches of this conditional statement.
        /// </summary>
        [ItemNotNull]
        public ReadOnlyList<IfBranch> If => m_If.AsReadOnly();

        #region Overrides

        /// <inheritdoc />
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            context.CurrentLocation = Location;
            foreach (IfBranch branch in m_If) {
                Variables branchVariables = new Variables(Assembly) {Parent = parentVariables};
                if (branch.Condition.Evaluate(context, parentVariables).IsTrue(context)) {
                    SolValue value = branch.Chunk.Execute(context, branchVariables, out terminators);
                    return value;
                }
            }
            if (Else != null) {
                Variables branchVariables = new Variables(Assembly) {Parent = parentVariables};
                SolValue value = Else.Execute(context, branchVariables, out terminators);
                return value;
            }
            terminators = Terminators.None;
            return SolNil.Instance;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            StringBuilder builder = new StringBuilder();
            foreach (IfBranch ifBranch in m_If) {
                if (builder.Length != 0) {
                    builder.Append(" else");
                }
                builder.Append(ifBranch);
            }
            if (Else != null) {
                builder.Append(" else ");
                builder.Append(Else);
            }
            builder.Append(" end");
            return builder.ToString();
            //return $"Statement_Conditional(If=[{m_If.JoinToString()}], Else={Else})";
        }

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            foreach (IfBranch branch in m_If) {
                ValidationResult conRes = branch.Condition.Validate(context);
                if (!conRes) {
                    return ValidationResult.Failure();
                }
                ValidationResult chkRes = branch.Chunk.Validate(context);
                if (!chkRes) {
                    return ValidationResult.Failure();
                }
            }
            ValidationResult elsRes = Else?.Validate(context);
            // todo: somehow determine if/else return type?
            return new ValidationResult(true, SolType.AnyNil);
        }

        #endregion

        #region Nested type: IfBranch

        /// <summary>
        ///     Represents a branch is a conditional statement.
        /// </summary>
        public class IfBranch
        {
            /// <inheritdoc />
            /// <exception cref="ArgumentNullException">An argument is <see langword="null" /></exception>
            public IfBranch([NotNull] SolExpression condition, [NotNull] SolChunk chunk)
            {
                if (condition == null) {
                    throw new ArgumentNullException(nameof(condition));
                }
                if (chunk == null) {
                    throw new ArgumentNullException(nameof(chunk));
                }
                Condition = condition;
                Chunk = chunk;
            }

            /// <summary>
            ///     The chunk to be executed if the condition is met.
            /// </summary>
            [NotNull]
            public SolChunk Chunk { get; }

            /// <summary>
            ///     The condition.
            /// </summary>
            [NotNull]
            public SolExpression Condition { get; }

            #region Overrides

            /// <inheritdoc />
            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("if ");
                builder.Append(Condition);
                builder.Append(" then ");
                builder.Append(Chunk);
                return builder.ToString();
            }

            #endregion
        }

        #endregion
    }
}