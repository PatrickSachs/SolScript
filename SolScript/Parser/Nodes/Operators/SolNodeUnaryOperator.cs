// ---------------------------------------------------------------------
// SolScript - A simple but powerful scripting language.
// Official repository: https://bitbucket.org/PatrickSachs/solscript/
// ---------------------------------------------------------------------
// Copyright 2017 Patrick Sachs
// Permission is hereby granted, free of charge, to any person obtaining 
// a copy of this software and associated documentation files (the 
// "Software"), to deal in the Software without restriction, including 
// without limitation the rights to use, copy, modify, merge, publish, 
// distribute, sublicense, and/or sell copies of the Software, and to 
// permit persons to whom the Software is furnished to do so, subject to 
// the following conditions:
// 
// The above copyright notice and this permission notice shall be 
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN 
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.
// ---------------------------------------------------------------------
// ReSharper disable ArgumentsStyleStringLiteral

using System.Collections.Generic;
using Irony.Parsing;
using NodeParser.Nodes.NonTerminals;
using SolScript.Interpreter.Expressions;

namespace SolScript.Parser.Nodes.Operators
{
    /// <summary>
    ///     The node for operatiors in an unary expression.
    /// </summary>
    public class SolNodeUnaryOperator : OperatorNode<Expression_Unary.OperationRef>
    {
        #region Overrides

        /// <inheritdoc />
        protected override IEnumerable<Operator> GetOperators()
        {
            // We have two + and - operators: One for binary and one for unary. We thus need a different name for this one
            // to avoid conflicts in the node parser.
            // The ! conflicts with the type reference nilability.
            yield return new Operator("+", Expression_Unary.PlusOperation.Instance, 8, Associativity.Left, "+_unary");
            yield return new Operator("-", Expression_Unary.PlusOperation.Instance, 8, Associativity.Left, "-_unary");
            yield return new Operator("!", Expression_Unary.NotOperation.Instance, 9, Associativity.Right, "!_unary");
            yield return new Operator("#", Expression_Unary.GetNOperation.Instance, 9, Associativity.Right);
        }

        #endregion
    }
}