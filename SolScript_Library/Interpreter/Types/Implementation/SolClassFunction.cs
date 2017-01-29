namespace SolScript.Interpreter.Types.Implementation
{
    public abstract class SolClassFunction : DefinedSolFunction
    {
        public abstract SolClassDefinition GetDefiningClass();
    }
}