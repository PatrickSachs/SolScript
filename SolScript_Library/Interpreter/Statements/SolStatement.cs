using System;
using Irony.Parsing;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements
{
    /// <summary>
    ///     This is the base class for all statement in SolScript. Statements(as opposed to <see cref="SolExpression" />s) can
    ///     stand alone inside a <see cref="SolChunk" /> and may or may not be made out of several <see cref="SolExpression" />
    ///     s(e.g. a function call can stand alone in a chunk and takes several expressions as arguments).
    /// </summary>
    public abstract class SolStatement : ISourceLocateable//, ISourceLocationInjector
    {
        /// <summary>
        ///     Creates a new statement.
        /// </summary>
        /// <param name="assembly">The assembly this statement is in.</param>
        /// <param name="location">The location of this statement.</param>
        /// <exception cref="ArgumentNullException"><paramref name="assembly" /> is null.</exception>
        protected SolStatement(SolAssembly assembly, SourceLocation location)
        {
            if (assembly == null) {
                throw new ArgumentNullException(nameof(assembly));
            }
            Assembly = assembly;
            InjectSourceLocation(location);
        }

        /// <summary>
        ///     Used by the parser.
        /// </summary>
        internal SolStatement()
        {
            Assembly = SolAssembly.CurrentlyParsing;
        }

        /// <summary>
        ///     The assembly this statement is in.
        /// </summary>
        public readonly SolAssembly Assembly;

        #region ISourceLocateable Members

        /// <inheritdoc />
        public SourceLocation Location { get; private set; }

        #endregion

        #region Overrides

        /// <inheritdoc />
        public override string ToString()
        {
            return ToString_Impl();
        }

        #endregion

        /// <summary>
        ///     Executes the statement and produces its result.
        /// </summary>
        /// <param name="context">The currently active execution context.</param>
        /// <param name="parentVariables">The variable source of this statement.</param>
        /// <param name="terminators">The terminators. Terminators are used to break or continue in e.g. iterators.</param>
        /// <returns>The result of the execution.</returns>
        public abstract SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators);

        /// <summary>
        ///     Formats this statement to a string for debuging purposes.
        /// </summary>
        /// <returns>The string.</returns>
        protected abstract string ToString_Impl();

        /// <inheritdoc />
        public void InjectSourceLocation(SourceLocation location)
        {
            Location = location;
        }
    }
}