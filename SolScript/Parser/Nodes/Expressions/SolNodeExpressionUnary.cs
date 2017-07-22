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

using Irony.Parsing;
using NodeParser.Nodes;
using SolScript.Interpreter;
using SolScript.Interpreter.Expressions;
using SolScript.Parser.Nodes.Operators;

namespace SolScript.Parser.Nodes.Expressions
{
    /// <summary>
    ///     The node for an unary operation.
    /// </summary>
    public class SolNodeExpressionUnary : AParserNode<Expression_Unary>
    {
        /// <inheritdoc />
        protected override BnfExpression Rule_Impl
            =>
                NODE<SolNodeUnaryOperator>(id: "op")
                + NODE<SolNodeExpression>(id: "expr")
        ;

        #region Overrides

        /// <inheritdoc />
        protected override Expression_Unary BuildAndGetNode(IAstNode[] astNodes)
        {
            Expression_Unary.OperationRef op = (Expression_Unary.OperationRef)astNodes[0].GetValue();
            SolExpression expr = (SolExpression) astNodes[1].GetValue();
            return new Expression_Unary(SolAssembly.CurrentlyParsingThreadStatic, Location, op, expr);
        }

        #endregion
    }
}