using JetBrains.Annotations;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Statements;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     Use this interface if a <see cref="SolExpression" /> or <see cref="SolStatement" /> was written inside of a class.
    /// </summary>
    public interface IWrittenInClass
    {
        /// <summary>
        ///     This class name the expression or statement was written inside. Null if in none.
        /// </summary>
        [CanBeNull]
        string WrittenInClass { get; }
    }
}