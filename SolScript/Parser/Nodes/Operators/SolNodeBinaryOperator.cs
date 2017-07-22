// ---------------------------------------------------------------------
// SolScript - A simple but powerful scripting language.
// Offical repository: https://bitbucket.org/PatrickSachs/solscript/
// SolScript is licensed unter The MIT License.
// ---------------------------------------------------------------------
// ReSharper disable ArgumentsStyleStringLiteral

using System.Collections.Generic;
using Irony.Parsing;
using NodeParser.Nodes.NonTerminals;
using SolScript.Interpreter.Expressions;

namespace SolScript.Parser.Nodes.Operators
{
    /// <summary>
    ///     A binary operator for binary expression.
    /// </summary>
    public class SolNodeBinaryOperator : OperatorNode<Expression_Binary.OperationRef>
    {
        #region Overrides

        /// <inheritdoc />
        protected override IEnumerable<Operator> GetOperators()
        {
            yield return new Operator("||", Expression_Binary.Or.Instance, 1, Associativity.Left);
            yield return new Operator("&&", Expression_Binary.And.Instance, 2, Associativity.Left);
            yield return new Operator("==", Expression_Binary.CompareEqual.Instance, 3, Associativity.Left);
            yield return new Operator("!=", Expression_Binary.CompareNotEqual.Instance, 3, Associativity.Left);
            yield return new Operator(">=", Expression_Binary.CompareGreaterOrEqual.Instance, 3, Associativity.Left);
            yield return new Operator("<=", Expression_Binary.CompareSmallerOrEqual.Instance, 3, Associativity.Left);
            yield return new Operator(">", Expression_Binary.CompareGreater.Instance, 3, Associativity.Left);
            yield return new Operator("<", Expression_Binary.CompareSmaller.Instance, 3, Associativity.Left);
            yield return new Operator("..", Expression_Binary.Concatenation.Instance, 4, Associativity.Left);
            yield return new Operator("??", Expression_Binary.NilCoalescing.Instance, 5, Associativity.Left);
            yield return new Operator("+", Expression_Binary.Addition.Instance, 6, Associativity.Left, "+_binary");
            yield return new Operator("-", Expression_Binary.Subtraction.Instance, 6, Associativity.Left, "-_binary");
            yield return new Operator("*", Expression_Binary.Multiplication.Instance, 6, Associativity.Left);
            yield return new Operator("/", Expression_Binary.Division.Instance, 6, Associativity.Left);
            yield return new Operator("%", Expression_Binary.Modulo.Instance, 7, Associativity.Left);
            yield return new Operator("^", Expression_Binary.Exponentiation.Instance, 9, Associativity.Right);
        }

        #endregion
    }
}