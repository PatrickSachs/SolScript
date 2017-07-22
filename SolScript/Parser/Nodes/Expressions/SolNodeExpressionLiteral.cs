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
using NodeParser.Nodes;
using NodeParser.Nodes.NonTerminals;
using NodeParser.Nodes.Terminals;
using SolScript.Interpreter;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Parser.Literals;
using SolScript.Parser.Terminals;

namespace SolScript.Parser.Nodes.Expressions
{
    /// <summary>
    ///     A node for a literal expression.
    /// </summary>
    public class SolNodeExpressionLiteral : AParserNode<Expression_Literal>
    {
        /// <inheritdoc />
        protected override BnfExpression Rule_Impl
            => NODE<BoolNode>()
               | NODE<NilNode>()
               | TERMINAL<NumberNode>()
               | TERMINAL<ShortStringNode>()
               | TERMINAL<LongStringNode>()
        ;

        #region Overrides

        /// <inheritdoc />
        protected override Expression_Literal BuildAndGetNode(IAstNode[] astNodes)
        {
            return new Expression_Literal(SolAssembly.CurrentlyParsingThreadStatic, Location, (SolValue) astNodes[0].GetValue());
        }

        #endregion

        #region Nested type: BoolNode

        private class BoolNode : ConstantNode<SolBool>
        {
            /// <inheritdoc />
            protected override bool AreKeywords => true;

            #region Overrides

            /// <inheritdoc />
            protected override IEnumerable<Constant> GetConstants()
            {
                yield return new Constant("true", SolBool.True);
                yield return new Constant("false", SolBool.False);
            }

            #endregion
        }

        #endregion

        #region Nested type: LongStringNode

        private class LongStringNode : LiteralNode<SolString>
        {
            #region Overrides

            /// <inheritdoc />
            protected override bool TryParse(ParseTreeNode input, out SolString parsed)
            {
                parsed = SolString.ValueOf((string) input.Token.Value);
                return true;
            }

            /// <inheritdoc />
            protected override BnfTerm CreateTerminal()
            {
                return new SolScriptLongStringTerminal("_long_string");
            }

            #endregion
        }

        #endregion

        #region Nested type: NilNode

        private class NilNode : AParserNode<SolNil>
        {
            /// <inheritdoc />
            protected override BnfExpression Rule_Impl => KEYWORD("nil", SolNil.Instance);

            #region Overrides

            /// <inheritdoc />
            public override BnfTerm BuildBnfTerm()
            {
                var term = base.BuildBnfTerm();
                term.Name = "_nil";
                return term;
            }

            /// <inheritdoc />
            protected override SolNil BuildAndGetNode(IAstNode[] astNodes)
            {
                return SolNil.Instance;
            }

            #endregion
        }

        #endregion

        #region Nested type: NumberNode

        private class NumberNode : LiteralNode<SolNumber>
        {
            #region Overrides

            /// <inheritdoc />
            protected override BnfTerm CreateTerminal()
            {
                NumberLiteral term = new NumberLiteral("_number", NumberOptions.AllowStartEndDot) {
                    DefaultFloatType = TypeCode.Double
                };
                term.AddPrefix("0x", NumberOptions.Hex);
                return term;
            }

            /// <inheritdoc />
            protected override bool TryParse(ParseTreeNode input, out SolNumber parsed)
            {
                object boxed = input.Token.Value;
                if (boxed is double) {
                    parsed = new SolNumber((double) boxed);
                    return true;
                }
                if (boxed is int) {
                    parsed = new SolNumber((int) boxed);
                    return true;
                }
                if (boxed is long) {
                    parsed = new SolNumber((long) boxed);
                    return true;
                }
                parsed = null;
                return false;
            }

            #endregion
        }

        #endregion

        #region Nested type: ShortStringNode

        private class ShortStringNode : LiteralNode<SolString>
        {
            #region Overrides

            /// <inheritdoc />
            protected override bool TryParse(ParseTreeNode input, out SolString parsed)
            {
                parsed = SolString.ValueOf((string) input.Token.Value);
                return true;
            }

            /// <inheritdoc />
            protected override BnfTerm CreateTerminal()
            {
                return new SolScriptStringLiteral("_short_string");
            }

            #endregion
        }

        #endregion
    }
}