using Irony.Parsing;
using NodeParser;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     This definitions contains information about a field in SolScript.
    /// </summary>
    public sealed class SolFieldDefinition : SolMemberDefinition
    {
        /// <inheritdoc />
        public SolFieldDefinition(SolAssembly assembly, NodeLocation location) : base(assembly, location) { }

        //internal SolFieldDefinition() { }

        /// <summary>
        ///     This class wraps the initializer of the field. Make sure to check the
        ///     <see cref="SolFieldInitializerWrapper.FieldType" /> before obtaining an actual reference.
        /// </summary>
        public SolFieldInitializerWrapper Initializer { get; internal set; }
    }
}