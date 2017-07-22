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
using NodeParser.Nodes;
using NodeParser.Nodes.NonTerminals;
using PSUtility.Enumerables;
using SolScript.Interpreter;
using SolScript.Interpreter.Statements;
using SolScript.Parser.Nodes.Expressions;

namespace SolScript.Parser.Nodes.Statements
{
    /// <summary>
    ///     Node for a conditional if/elseif/else statement.
    /// </summary>
    public class SolNodeStatementConditional : AParserNode<Statement_Conditional>
    {
        /// <inheritdoc />
        protected override BnfExpression Rule_Impl
            => KEYWORD("if")
               + NODE<SolNodeExpression>(id: "if")
               + KEYWORD("then")
               + NODE<SolNodeChunk>(id: "then")
               + ID(NODE<ElseIfNode>().LIST<Statement_Conditional.IfBranch>(null, TermListOptions.StarList), "elseif")
               + (
                   KEYWORD("else")
                   + NODE<SolNodeChunk>(id: "else")
               ).Q()
               + KEYWORD("end");

        #region Overrides

        /// <inheritdoc />
        protected override Statement_Conditional BuildAndGetNode(IAstNode[] astNodes)
        {
            var ifB = new Statement_Conditional.IfBranch(OfId<SolNodeExpression>("if").GetValue(), OfId<SolNodeChunk>("then").GetValue());
            var elseif = OfId<ListNode<Statement_Conditional.IfBranch>>("elseif");
            IEnumerable<Statement_Conditional.IfBranch> branches = elseif != null
                ? elseif.GetValue().Concat(EnumerableConcat.Prepend, ifB)
                : new[] {ifB};
            SolChunk elseB = OfId<SolNodeChunk>("else")?.GetValue();
            return new Statement_Conditional(SolAssembly.CurrentlyParsingThreadStatic, Location, branches, elseB);
        }

        #endregion

        #region Nested type: ElseIfNode

        private class ElseIfNode : AParserNode<Statement_Conditional.IfBranch>
        {
            /// <inheritdoc />
            protected override BnfExpression Rule_Impl
                => KEYWORD("elseif")
                   + NODE<SolNodeExpression>(id: "condition")
                   + KEYWORD("then")
                   + NODE<SolNodeChunk>(id: "chunk");

            #region Overrides

            /// <inheritdoc />
            protected override Statement_Conditional.IfBranch BuildAndGetNode(IAstNode[] astNodes)
            {
                SolNodeExpression condition = OfId<SolNodeExpression>("condition");
                SolNodeChunk chunk = OfId<SolNodeChunk>("chunk");
                return new Statement_Conditional.IfBranch(condition.GetValue(), chunk.GetValue());
            }

            #endregion
        }

        #endregion
    }
}