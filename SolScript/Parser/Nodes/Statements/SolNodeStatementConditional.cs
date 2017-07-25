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
using NodeParser;
using NodeParser.Nodes;
using NodeParser.Nodes.NonTerminals;
using PSUtility.Enumerables;
using SolScript.Interpreter;
using SolScript.Interpreter.Expressions;
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
               + NODE<SolNodeExpression>()
               + KEYWORD("then")
               + NODE<SolNodeChunk>()
               + NODE<ElseIfNode>().LIST<Statement_Conditional.IfBranch>(null, TermListOptions.StarList)
               + (
                   KEYWORD("else")
                   + NODE<SolNodeChunk>()
               ).OPT()
               + KEYWORD("end")
        ;

        #region Overrides

        /// <inheritdoc />
        protected override Statement_Conditional BuildAndGetNode(IAstNode[] astNodes)
        {
            Statement_Conditional.IfBranch ifBranch = new Statement_Conditional.IfBranch(astNodes[1].As<SolNodeExpression>().GetValue(), astNodes[3].As<SolNodeChunk>().GetValue());
            IEnumerable<Statement_Conditional.IfBranch> branches = astNodes[4].As<ListNode<Statement_Conditional.IfBranch>>().GetValue().Concat(EnumerableConcat.Prepend, ifBranch);
            SolChunk elseB = astNodes[5].As<OptionalNode>().GetValue(null, list => list[1].As<SolNodeChunk>().GetValue());
            return new Statement_Conditional(SolAssembly.CurrentlyParsingThreadStatic, Location, branches, elseB);
        }

        #endregion

        #region Nested type: ElseIfNode

        private class ElseIfNode : AParserNode<Statement_Conditional.IfBranch>
        {
            /// <inheritdoc />
            protected override BnfExpression Rule_Impl
                => KEYWORD("elseif")
                   + NODE<SolNodeExpression>()
                   + KEYWORD("then")
                   + NODE<SolNodeChunk>();

            #region Overrides

            /// <inheritdoc />
            protected override Statement_Conditional.IfBranch BuildAndGetNode(IAstNode[] astNodes)
            {
                SolExpression condition = astNodes[1].As<SolNodeExpression>().GetValue();
                SolChunk chunk = astNodes[3].As<SolNodeChunk>().GetValue();
                return new Statement_Conditional.IfBranch(condition, chunk);
            }

            #endregion
        }

        #endregion
    }
}