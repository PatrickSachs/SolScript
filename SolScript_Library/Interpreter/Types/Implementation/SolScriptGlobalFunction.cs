using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     This class is used for global functions declared in script.
    /// </summary>
    public sealed class SolScriptGlobalFunction : DefinedSolFunction
    {
        /// <summary>
        ///     Creates the function.
        /// </summary>
        /// <param name="definition">The function definition.</param>
        public SolScriptGlobalFunction(SolFunctionDefinition definition)
        {
            Definition = definition;
        }

        /// <inheritdoc />
        public override SolFunctionDefinition Definition { get; }

        #region Overrides

        /// <inheritdoc />
        protected override SolClass GetClassInstance(out bool isCurrent, out bool resetOnExit)
        {
            isCurrent = true;
            resetOnExit = true;
            return null;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked {
                return 14 + (int) Id;
            }
        }

        /// <inheritdoc />
        public override bool Equals(object other)
        {
            return other == this;
        }

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            // todo: internal access for global variables need to be figured out. (meaning, what does internal on globals even mean?)
            Variables varContext = new Variables(Assembly) {
                Parent = Assembly.LocalVariables
            };
            try {
                InsertParameters(varContext, args);
            } catch (SolVariableException ex) {
                throw SolRuntimeException.InvalidFunctionCallParameters(context, ex);
            }
            // Functions pretty much eat the terminators since that's what the terminators are supposed to terminate down to.
            Terminators terminators;
            SolValue returnValue = Definition.Chunk.GetScriptChunk().Execute(context, varContext, out terminators);
            return returnValue;
        }

        #endregion
    }
}