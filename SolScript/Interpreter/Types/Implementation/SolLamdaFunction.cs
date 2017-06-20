using Irony.Parsing;
using JetBrains.Annotations;

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
        internal SolLamdaFunction(
            SolAssembly assembly, 
            SourceLocation location, 
            SolParameterInfo parameterInfo, 
            SolType returnType,
            SolClass definedIn)
        {
            Assembly = assembly;
            ParameterInfo = parameterInfo;
            ReturnType = returnType;
            DefinedIn = definedIn;
            Location = location;
        }

        /// <inheritdoc />
        public override SolAssembly Assembly { get; }

        /// <inheritdoc />
        public override SolParameterInfo ParameterInfo { get; }

        /// <inheritdoc />
        public override SolType ReturnType { get; }

        /// <summary>
        /// The class this lamda function was defined in. null for global lamdas.
        /// </summary>
        [CanBeNull]
        public SolClass DefinedIn { get; }

        /// <inheritdoc />
        public override SourceLocation Location { get; }

        /// <inheritdoc />
        protected override SolClass GetClassInstance(out bool isCurrent, out bool resetOnExit)
        {
            isCurrent = true;
            resetOnExit = true;
            return DefinedIn;
        }
    }
}