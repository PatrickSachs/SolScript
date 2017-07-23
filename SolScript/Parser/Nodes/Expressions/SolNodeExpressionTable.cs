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
using SolScript.Interpreter;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Parser.Nodes.Expressions
{
    /// <summary>
    ///     This node contains the creation expression of a table.
    /// </summary>
    public class SolNodeExpressionTable : AParserNode<Expression_TableConstructor>
    {
        /// <inheritdoc />
        protected override BnfExpression Rule_Impl
            => BRACES("{",
                NODE<TableFieldNode>().LIST<KeyValuePair<SolExpression, SolExpression>>(PUNCTUATION(","), TermListOptions.StarList | TermListOptions.AllowTrailingDelimiter),
                "}")
        ;

        #region Overrides

        /// <inheritdoc />
        protected override Expression_TableConstructor BuildAndGetNode(IAstNode[] astNodes)
        {
            var fields = astNodes[0].As<BraceNode>().GetValue<IEnumerable<KeyValuePair<SolExpression, SolExpression>>>();
            return new Expression_TableConstructor(SolAssembly.CurrentlyParsingThreadStatic, Location, fields);
        }

        #endregion

        #region Nested type: TableFieldNode

        private class TableFieldNode : AParserNode<KeyValuePair<SolExpression, SolExpression>>
        {
            /// <inheritdoc />
            protected override BnfExpression Rule_Impl
                => (BRACES("[", NODE<SolNodeExpression>(), "]") + OPERATOR("=") + NODE<SolNodeExpression>()).TRANS()
                   | (TERMINAL<IdentifierNode>() + OPERATOR("=") + NODE<SolNodeExpression>())
                   | NODE<SolNodeExpression>()
            ;

            #region Overrides

            /// <inheritdoc />
            /// <exception cref="InvalidOperationException">Invalid table field.</exception>
            protected override KeyValuePair<SolExpression, SolExpression> BuildAndGetNode(IAstNode[] astNodes)
            {
                IAstNode node1 = astNodes[0];
                SolNodeExpression array = node1 as SolNodeExpression;
                if (array != null) {
                    return new KeyValuePair<SolExpression, SolExpression>(null, array.GetValue());
                }
                IdentifierNode rawKey = node1 as IdentifierNode;
                if (rawKey != null) {
                    return new KeyValuePair<SolExpression, SolExpression>(
                        new Expression_Literal(SolAssembly.CurrentlyParsingThreadStatic, rawKey.Location, SolString.ValueOf(rawKey.GetValue())),
                        ((SolNodeExpression) astNodes[2]).GetValue()
                    );
                }
                BraceNode indexed = node1 as BraceNode;
                if (indexed != null) {
                    return new KeyValuePair<SolExpression, SolExpression>(
                        (SolExpression) indexed.GetValue(),
                        (SolExpression) astNodes[2].GetValue()
                    );
                }
                /*SolNodeExpression array = OfId<SolNodeExpression>("array");
                if (array != null) {
                    return new KeyValuePair<SolExpression, SolExpression>(null, array.GetValue());
                }
                SolNodeExpression rawKey = OfId<SolNodeExpression>("raw_key");
                if (rawKey != null) {
                    return new KeyValuePair<SolExpression, SolExpression>(rawKey.GetValue(), OfId<SolNodeExpression>("raw_value").GetValue());
                }
                SolNodeExpression key = OfId<SolNodeExpression>("key");
                if (key != null) {
                    return new KeyValuePair<SolExpression, SolExpression>(key.GetValue(), OfId<SolNodeExpression>("value").GetValue());
                }*/
                throw new InvalidOperationException("Invalid table field.");
            }

            #endregion
        }

        #endregion
    }
}