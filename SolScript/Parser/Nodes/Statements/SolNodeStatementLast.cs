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

using System;
using Irony.Parsing;
using NodeParser;
using NodeParser.Nodes;
using NodeParser.Nodes.Terminals;
using SolScript.Interpreter;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Statements;
using SolScript.Parser.Nodes.Expressions;

namespace SolScript.Parser.Nodes.Statements
{
    /// <summary>
    ///     The last/terminating statement in a chunk.
    /// </summary>
    public class SolNodeStatementLast : AParserNode<SolStatement>
    {
        /// <inheritdoc />
        protected override BnfExpression Rule_Impl
            =>
                (
                    KEYWORD("return", Terminators.Return)
                    | KEYWORD("break", Terminators.Break)
                    | KEYWORD("continue", Terminators.Continue)
                )
                + NODE<SolNodeExpression>().OPT();

        #region Overrides

        /// <inheritdoc />
        protected override SolStatement BuildAndGetNode(IAstNode[] astNodes)
        {
            Terminators terminators = astNodes[0].As<DefaultAst>()[0].As<KeyTermNode<Terminators>>().GetValue();
            SolExpression expr = astNodes[1].As<OptionalNode>().GetValue<SolExpression>(null);
            /*Terminators terminators = (Terminators) ((DefaultAst) astNodes[0])[0].GetValue();
            SolExpression expr = OfId<SolNodeExpression>("expr", true)?.GetValue<SolExpression>();*/
            switch (terminators) {
                case Terminators.Return:
                    return new Statement_Return(SolAssembly.CurrentlyParsingThreadStatic, Location, expr);
                case Terminators.Break:
                    return new Statement_Break(SolAssembly.CurrentlyParsingThreadStatic, Location);
                case Terminators.Continue:
                    return new Statement_Continue(SolAssembly.CurrentlyParsingThreadStatic, Location);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }
}