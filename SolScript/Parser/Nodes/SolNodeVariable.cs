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
using System.Runtime.Remoting.Messaging;
using Irony.Parsing;
using NodeParser.Nodes;
using NodeParser.Nodes.Terminals;
using SolScript.Interpreter;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Parser.Nodes.Expressions;

namespace SolScript.Parser.Nodes
{
    /// <summary>
    ///     Node for variables that can be assigned and read from.
    /// </summary>
    public class SolNodeVariable : AParserNode<AVariable>
    {
        /// <inheritdoc />
        protected override BnfExpression Rule_Impl
            => TERMINAL<IdentifierNode>()
               | (NODE<SolNodeExpression>() + BRACES("[", NODE<SolNodeExpression>(), "]"))
               | (NODE<SolNodeExpression>() + PUNCTUATION(".") + TERMINAL<IdentifierNode>());
        /*/// <inheritdoc />
        protected override BnfExpression Rule_Impl
            => TERMINAL<IdentifierNode>(id: "name")
               | (NODE<SolNodeExpression>(id: "indexable") + BRACES("[", NODE<SolNodeExpression>(id: "key"), "]"))
               | (NODE<SolNodeExpression>(id: "raw_indexable") + PUNCTUATION(".") + TERMINAL<IdentifierNode>(id: "raw_index"));*/

        #region Overrides

        /// <inheritdoc />
        protected override AVariable BuildAndGetNode(IAstNode[] astNodes)
        {
            IAstNode node1 = astNodes[0];
            if (node1 is IdentifierNode) {
                return new AVariable.Named((string) node1.GetValue());
            }
            IAstNode node2 = astNodes[1];
            if (node2 is BraceNode) {
                return new AVariable.Indexed((SolExpression) node1.GetValue(), (SolExpression) node2.GetValue());
            }
            string indexName = (string) node2.GetValue();
            return new AVariable.Indexed((SolExpression) node1.GetValue(), new Expression_Literal(SolAssembly.CurrentlyParsingThreadStatic, node2.Location, SolString.ValueOf(indexName)));
            /*IdentifierNode name = OfId<IdentifierNode>("name", true);
            if (name != null) {
                return new AVariable.Named(name.GetValue());
            }
            SolNodeExpression indexable = OfId<SolNodeExpression>("indexable");
            if (indexable != null) {
                SolNodeExpression key = OfId<SolNodeExpression>("key");
                return new AVariable.Indexed(indexable.GetValue(), key.GetValue());
            }
            SolNodeExpression indexableRaw = OfId<SolNodeExpression>("raw_indexable");
            if (indexableRaw != null) {
                string key = OfId<IdentifierNode>("raw_index").GetValue();
                return new AVariable.Indexed(indexableRaw.GetValue(), new Expression_Literal(SolAssembly.CurrentlyParsingThreadStatic, Location, SolString.ValueOf(key)));
            }
            throw new InvalidOperationException("Unsupported variable node.");*/
        }

        #endregion
    }
}