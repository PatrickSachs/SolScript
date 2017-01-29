using JetBrains.Annotations;

namespace SolScript.Interpreter
{
    public interface IStatementOrExpressionWrittenInClass
    {
        [CanBeNull]
        string WrittenInClass { get; }
    }
}