using System;
using System.Text;
using NodeParser;
using SolScript.Compiler;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     This expression is used to create new functions("lamda functions") at runtime.
    /// </summary>
    public class Expression_CreateFunction : SolExpression
    {
        /// <summary>
        ///     Creates a new expression.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="location">The location in code.</param>
        /// <param name="chunk">The function chunk.</param>
        /// <param name="type">The function return type.</param>
        /// <param name="parameters">The function parameters.</param>
        /// <exception cref="ArgumentNullException">An argument is <see langword="null"/></exception>
        public Expression_CreateFunction(SolAssembly assembly, NodeLocation location, SolChunk chunk, SolType type, SolParameterInfo parameters) : base(assembly, location)
        {
            if (chunk == null) {
                throw new ArgumentNullException(nameof(chunk));
            }
            if (parameters == null) {
                throw new ArgumentNullException(nameof(parameters));
            }
            Chunk = chunk;
            Type = type;
            Parameters = parameters;
        }

        /// <summary>
        ///     The function chunk.
        /// </summary>
        public readonly SolChunk Chunk;

        /// <summary>
        ///     The function parameters.
        /// </summary>
        public readonly SolParameterInfo Parameters;

        /// <summary>
        ///     The function return type.
        /// </summary>
        public readonly SolType Type;

        #region Overrides

        /// <inheritdoc />
        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            SolClassEntry entry;
            IClassLevelLink link = context.PeekClassEntry(out entry) && !entry.IsGlobal ? (IClassLevelLink)entry : null;
            return new SolScriptLamdaFunction(Assembly, Location, Parameters, Type, Chunk, parentVariables, link);
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("function(");
            builder.Append(Parameters);
            builder.Append(") ");
            builder.Append(Chunk);
            builder.Append(" end");
            return builder.ToString();
        }

        /// <inheritdoc />
        public override bool IsConstant => false;

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            return Chunk.Validate(context);
        }

        #endregion
    }
}