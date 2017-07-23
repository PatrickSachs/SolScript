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
using System.Collections.Generic;
using Irony.Parsing;
using NodeParser;
using NodeParser.Nodes;
using NodeParser.Nodes.NonTerminals;
using NodeParser.Nodes.Terminals;
using PSUtility.Enumerables;
using SolScript.Interpreter;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Statements;
using SolScript.Parser.Nodes.Expressions;

namespace SolScript.Parser.Nodes.Statements
{
    /// <summary>
    ///     A node for a statement creating a new class instance.
    /// </summary>
    public class SolNodeStatementNew : AParserNode<Statement_New>
    {
        /// <inheritdoc />
        protected override BnfExpression Rule_Impl
            =>
                KEYWORD("new")
                + TERMINAL<IdentifierNode>()
                + BRACES("(",
                    NODE<SolNodeExpression>().LIST<SolExpression>(PUNCTUATION(","), TermListOptions.StarList | TermListOptions.AllowTrailingDelimiter)
                    , ")")
        ;

        #region Overrides

        /// <inheritdoc />
        protected override Statement_New BuildAndGetNode(IAstNode[] astNodes)
        {
            string name = astNodes[1].As<IdentifierNode>().GetValue();
            IEnumerable<SolExpression> args = astNodes[2].As<BraceNode>().GetValue<IEnumerable<SolExpression>>();
            /*string name = OfId<IdentifierNode>("name").GetValue();
            ListNode<SolExpression> args = OfId<ListNode<SolExpression>>("args");*/

            return new Statement_New(SolAssembly.CurrentlyParsingThreadStatic, Location, new SolClassDefinitionReference(SolAssembly.CurrentlyParsingThreadStatic, name), args);
        }

        #endregion
    }
}