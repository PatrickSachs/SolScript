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

namespace SolScript.Parser.Nodes.Expressions
{
    /// <summary>
    ///     A node for creating tertiary expressions.
    /// </summary>
    public class SolNodeExpressionTertiary : AParserNode<Expression_Tertiary>
    {
        /// <inheritdoc />
        protected override BnfExpression Rule_Impl
            =>
                NODE<SolNodeExpression>(id: "1")
                + PUNCTUATION("?")
                + BRACES("(",
                    NODE<SolNodeExpression>(id: "2")
                    + PUNCTUATION(":")
                    + NODE<SolNodeExpression>(id: "3"),
                    ")", true)
        ;

        #region Overrides

        /// <inheritdoc />
        protected override Expression_Tertiary BuildAndGetNode(IAstNode[] astNodes)
        {
            SolExpression e1 = OfId<SolNodeExpression>("1").GetValue();
            SolExpression e2 = OfId<SolNodeExpression>("2").GetValue();
            SolExpression e3 = OfId<SolNodeExpression>("3").GetValue();
            return new Expression_Tertiary(SolAssembly.CurrentlyParsingThreadStatic, Location,
                Expression_Tertiary.Conditional.Instance, e1, e2, e3);
        }

        #endregion
    }
}