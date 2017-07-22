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
using NodeParser.Nodes;
using NodeParser.Nodes.Terminals;
using SolScript.Interpreter;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;
using SolScript.Parser.Nodes.Expressions;

namespace SolScript.Parser.Nodes.Statements
{
    /// <summary>
    ///     A statement allowing to call code from the base class.
    ///     <br />
    ///     The base statement is laregly a replica of the variable node, however, we do not allow to obtain a reference to
    ///     "base" itself, since it is the same class instance as the super class.
    /// </summary>
    public class SolNodeStatementBase : AParserNode<Statement_Base>
    {
        /// <inheritdoc />
        protected override BnfExpression Rule_Impl
            =>
                (KEYWORD("base") + PUNCTUATION(".") + TERMINAL<IdentifierNode>(id: "identifier"))
                | (KEYWORD("base") + BRACES("[", NODE<SolNodeExpression>(id: "expr"), "]"))
        ;

        #region Overrides

        /// <inheritdoc />
        protected override Statement_Base BuildAndGetNode(IAstNode[] astNodes)
        {
            IdentifierNode id = OfId<IdentifierNode>("identifier", true);
            SolExpression indexer = id != null 
                ? new Expression_Literal(SolAssembly.CurrentlyParsingThreadStatic, id.Location, SolString.ValueOf(id.GetValue())) 
                : OfId<SolNodeExpression>("expr").GetValue();

            return new Statement_Base(SolAssembly.CurrentlyParsingThreadStatic, Location, indexer);
        }

        #endregion
    }
}