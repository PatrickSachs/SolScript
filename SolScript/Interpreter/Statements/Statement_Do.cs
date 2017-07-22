using System;
using System.Text;
using JetBrains.Annotations;
using NodeParser;
using SolScript.Compiler;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements
{
    /// <summary>
    ///     The "do"-Statement allows to execute code in an isolated block allowing you to manually scope variables or simply
    ///     create a visually separated area.
    /// </summary>
    public class Statement_Do : SolStatement
    {
        /// <summary>
        ///     The assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="location">The code location.</param>
        /// <param name="chunk">The chunk to execute.</param>
        /// <exception cref="ArgumentNullException">An argument is <see langword="null" /></exception>
        public Statement_Do([NotNull] SolAssembly assembly, NodeLocation location, [NotNull] SolChunk chunk) : base(assembly, location)
        {
            if (chunk == null) {
                throw new ArgumentNullException(nameof(chunk));
            }
            Chunk = chunk;
        }

        /// <summary>
        ///     The wrapped chunk.
        /// </summary>
        public SolChunk Chunk { get; }

        #region Overrides

        /// <inheritdoc />
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            Variables isolatedVariables = new Variables(Assembly) {Parent = parentVariables};
            return Chunk.Execute(context, isolatedVariables, out terminators);
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("do ");
            builder.Append(Chunk);
            builder.Append(" end");
            return builder.ToString();
        }

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            return Chunk.Validate(context);
        }

        #endregion
    }
}