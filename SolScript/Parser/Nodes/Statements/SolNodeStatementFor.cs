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
using NodeParser;
using NodeParser.Nodes;
using PSUtility.Enumerables;
using SolScript.Interpreter;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Statements;
using SolScript.Parser.Nodes.Expressions;

namespace SolScript.Parser.Nodes.Statements
{
    /// <summary>
    ///     A node for a for-loop.
    /// </summary>
    public class SolNodeStatementFor : AParserNode<Statement_For>
    {
        /// <summary>
        ///     The rule for this is largely borrowed from Java.
        ///     <br />
        ///     The initializer can either be an expression or a variable declaration statement. (Supporting statement or
        ///     expression requires specific implementation in the runtime.)
        ///     <br />
        ///     The condition is an expression.
        ///     <br />
        ///     The afterthought is a an expression.
        /// </summary>
        // no IDs due to | inside the braces.
        protected override BnfExpression Rule_Impl
            =>
                KEYWORD("for")
                + BRACES("(",
                    (NODE<SolNodeExpression>() | NODE<SolNodeStatementDeclareVariable>())
                    + PUNCTUATION(",")
                    + NODE<SolNodeExpression>()
                    + PUNCTUATION(",")
                    + NODE<SolNodeExpression>()
                    , ")", true)
                + KEYWORD("do")
                + NODE<SolNodeChunk>()
                + KEYWORD("end")
        ;

        #region Overrides

        /// <inheritdoc />
        protected override Statement_For BuildAndGetNode(IAstNode[] astNodes)
        {
            var braces = astNodes[1].GetValue<ReadOnlyList<IAstNode>>();
            IAstNode initExprNode = braces[0].GetValue<ReadOnlyList<IAstNode>>()[0];
            SolExpression condition = braces[1].GetValue<SolExpression>();
            SolExpression afterthought = braces[2].GetValue<SolExpression>();
            SolChunk chunk = astNodes[3].GetValue<SolChunk>();
            Statement_For.Init init = initExprNode is SolNodeStatementDeclareVariable
                ? new Statement_For.Init((Statement_DeclareVariable)initExprNode.GetValue())
                : new Statement_For.Init((SolExpression)initExprNode.GetValue());
            return new Statement_For(SolAssembly.CurrentlyParsingThreadStatic, Location, init, condition, afterthought, chunk);
            /*var condition = OfId<SolNodeExpression>("condition").GetValue();
            var afterthought = OfId<SolNodeExpression>("afterthought").GetValue();
            var chunk = OfId<SolNodeChunk>("chunk").GetValue();

            var initExprNode = OfId<SolNodeExpression>("init_expr");
            if (initExprNode == null) {
                var initDecl = OfId<SolNodeStatementDeclareVariable>("init_decl").GetValue();
                return new Statement_For(SolAssembly.CurrentlyParsingThreadStatic, Location,
                    new Statement_For.Init(initDecl), condition, afterthought, chunk);
            }
            return new Statement_For(SolAssembly.CurrentlyParsingThreadStatic, Location,
                new Statement_For.Init(initExprNode.GetValue()), condition, afterthought, chunk);*/
        }

        #endregion
    }
}