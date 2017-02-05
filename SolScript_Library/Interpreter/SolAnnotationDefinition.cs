using SolScript.Interpreter.Expressions;

namespace SolScript.Interpreter
{
    public sealed class SolAnnotationDefinition : SolDefinitionBase
    {
        public SolAnnotationDefinition(SolSourceLocation location, SolClassDefinition definition, SolExpression[] arguments)
        {
            Location = location;
            Definition = definition;
            Arguments = arguments;
        }

        public override SolAssembly Assembly => Definition.Assembly;
        public readonly SolExpression[] Arguments;
        public readonly SolClassDefinition Definition;
        public override SolSourceLocation Location { get; }
    }
}