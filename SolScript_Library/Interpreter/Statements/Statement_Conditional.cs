using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

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
        /// <param name="assembly">The assembly.</param>
        /// <param name="location">The source code location.</param>
        /// <param name="if">An array of all if branches.</param>
        /// <param name="else">The (optional) fallback else branch.</param>
        public Statement_Conditional(SolAssembly assembly, SolSourceLocation location, Array<IfBranch> @if, [CanBeNull] SolChunk @else) : base(assembly, location)
        {
            m_If = @if;
            Else = @else;
        }

        /// <summary>
        ///     The chunk that will be executed if no if statement applies.
        /// </summary>
        [CanBeNull]
        public readonly SolChunk Else;

        private readonly Array<IfBranch> m_If;

        /// <summary>
        ///     A read only list of all possible if branches.
        /// </summary>
        public IReadOnlyList<IfBranch> If => m_If;

        #region Overrides

        /// <inheritdoc />
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            context.CurrentLocation = Location;
            foreach (IfBranch branch in If) {
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
            return $"Statement_Conditional(If=[{If.JoinToString()}], Else={Else})";
        }

        #endregion

        #region Nested type: IfBranch

        public class IfBranch
        {
            public SolChunk Chunk;
            public SolExpression Condition;

            #region Overrides

            public override string ToString()
            {
                return $"IfBranch(Condition={Condition}, Chunk={Chunk})";
            }

            #endregion
        }

        #endregion
    }
}