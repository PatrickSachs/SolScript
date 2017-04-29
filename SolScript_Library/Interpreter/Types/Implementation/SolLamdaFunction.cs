using Irony.Parsing;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     Base class for all lambda <see cref="SolFunction" />s. Lamda functions can be described as anonymous functions and
    ///     thus are pretty much the purest form of a function. They do not require an actual definition and and can be created
    ///     at any given point without having to worry about anything.
    /// </summary>
    public abstract class SolLamdaFunction : SolFunction
    {
        // No third party primitives
        internal SolLamdaFunction(SolAssembly assembly, SourceLocation location, SolParameterInfo parameterInfo, SolType returnType)
        {
            Assembly = assembly;
            ParameterInfo = parameterInfo;
            ReturnType = returnType;
            Location = location;
        }

        /// <inheritdoc />
        public override SolAssembly Assembly { get; }

        /// <inheritdoc />
        public override SolParameterInfo ParameterInfo { get; }

        /// <inheritdoc />
        public override SolType ReturnType { get; }

        /// <inheritdoc />
        public override SourceLocation Location { get; }
    }
}