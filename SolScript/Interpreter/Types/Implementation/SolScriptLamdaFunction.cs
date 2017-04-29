using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     A lamda function. No real documentation yet since lamda functions are subject to change and to be expanded.
    /// </summary>
    public sealed class SolScriptLamdaFunction : SolLamdaFunction
    {
        /// <summary>
        ///     Creates a new script lamda function from the given parameters.
        /// </summary>
        /// <param name="assembly">The assembly this function belongs to.</param>
        /// <param name="location">The location in the source code this functionwas declared at.</param>
        /// <param name="parameterInfo">The function parameters.</param>
        /// <param name="returnType">The function return type.</param>
        /// <param name="chunk">The function chunk, containing the actual code of the function.</param>
        /// <param name="parentVariables">
        ///     The parent variables of the function. Set this to a non-null value if the function was
        ///     e.g. declared inside  another function and thus needs to have access to that functions variable scope.
        /// </param>
        public SolScriptLamdaFunction([NotNull] SolAssembly assembly, SourceLocation location, [NotNull] SolParameterInfo parameterInfo,
            SolType returnType, [NotNull] SolChunk chunk, [CanBeNull] IVariables parentVariables)
            : base(assembly, location, parameterInfo, returnType)
        {
            m_Chunk = chunk;
            m_ParentVariables = parentVariables;
        }

        private readonly SolChunk m_Chunk;
        [CanBeNull] private readonly IVariables m_ParentVariables;

        #region Overrides

        /// <inheritdoc />
        protected override SolClass GetClassInstance(out bool isCurrent, out bool resetOnExit)
        {
            isCurrent = false;
            resetOnExit = false;
            return null;
        }

        /// <inheritdoc />
        protected override string ToString_Impl(SolExecutionContext context)
        {
            return $"function#{Id}<lamda>";
        }

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            Variables variables = new Variables(Assembly) {Parent = m_ParentVariables};
            try {
                InsertParameters(variables, args);
            } catch (SolVariableException ex) {
                throw SolRuntimeException.InvalidFunctionCallParameters(context, ex);
            }
            // Functions pretty much eat the terminators since that's what the terminators are supposed to terminate down to.
            Terminators terminators;
            SolValue value = m_Chunk.Execute(context, variables, out terminators);
            return value;
        }

        #endregion
    }
}