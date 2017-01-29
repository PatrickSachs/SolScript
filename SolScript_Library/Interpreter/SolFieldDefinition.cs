using JetBrains.Annotations;
using SevenBiT.Inspector;
using SolScript.Interpreter.Expressions;

namespace SolScript.Interpreter
{
    public class SolFieldDefinition
    {
        public SolFieldDefinition([CanBeNull]SolClassDefinition definedIn = null)
        {
            DefinedIn = definedIn;
        }
        [CanBeNull]
        public readonly SolClassDefinition DefinedIn;
        public SolExpression FieldInitializer;
        public AccessModifier Modifier;
        public InspectorField NativeBackingField;
        public SolType Type;
    }
}