namespace SolScript.Interpreter
{
    public interface ISourceLocateable
    {
        SolSourceLocation Location { get; }
    }
}