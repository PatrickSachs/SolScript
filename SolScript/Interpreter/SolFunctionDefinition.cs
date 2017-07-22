using Irony.Parsing;
using JetBrains.Annotations;
using NodeParser;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The function definition class is used by the Type Registry to save metadata about a function and to allow for a
    ///     two-step creation of functions.<br />
    ///     A function definition may or may not be for a class or global function.<br />
    /// </summary>
    public sealed class SolFunctionDefinition : SolMemberDefinition
    {
        internal SolFunctionDefinition(SolAssembly assembly, NodeLocation location) : base(assembly, location) {}

        /// <summary>
        ///     The wrapper around the actual code of the function. The function may either be a chunk declared in your code or a
        ///     native method/constructor.
        /// </summary>
        public SolChunkWrapper Chunk { get; internal set; }

        /// <summary>
        ///     The member modifier specifying the type of function.
        /// </summary>
        /// <seealso cref="SolMemberModifier" />
        public SolMemberModifier MemberModifier { get; internal set; }

        /// <summary>
        ///     Information about parameters of the function.
        /// </summary>
        public SolParameterInfo ParameterInfo { get; [UsedImplicitly] internal set; }
    }
}