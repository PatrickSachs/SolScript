using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     This expression is used to create new functions("lamda functions") at runtime.
    /// </summary>
    public class Expression_CreateFunc : SolExpression
    {
        /// <summary>
        ///     Creates a new expression.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="location">The location in code.</param>
        /// <param name="chunk">The function chunk.</param>
        /// <param name="type">The function return type.</param>
        /// <param name="parameterAllowOptional">Are optional function parameters allowed?</param>
        /// <param name="parameters">The function parameters.</param>
        public Expression_CreateFunc(SolChunk chunk, SolType type, SolParameterInfo parameters)
        {
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
            return new SolScriptLamdaFunction(Assembly, Location, Parameters, Type, Chunk, parentVariables);
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return
                $"Expression_CreateFunc(Type={Type}, Parameters={Parameters}, Chunk={Chunk})";
        }

        #endregion
    }
}