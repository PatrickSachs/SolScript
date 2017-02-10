using SolScript.Interpreter.Expressions;

namespace SolScript.Interpreter
{
    public sealed class SolAnnotationDefinition : SolDefinitionBase
    {
        public SolAnnotationDefinition(SolSourceLocation location, SolClassDefinition definition, SolExpression[] arguments) : base(definition.Assembly, location)
        {
            Definition = definition;
            Arguments = arguments;
        }

        public readonly SolExpression[] Arguments;
        public readonly SolClassDefinition Definition;
    }
}