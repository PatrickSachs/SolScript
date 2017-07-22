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
using NodeParser.Nodes.Terminals;
using SolScript.Interpreter;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Statements;
using SolScript.Parser.Nodes.Expressions;

namespace SolScript.Parser.Nodes.Statements
{
    /// <summary>
    ///     Statement for declaring a variable.
    /// </summary>
    public class SolNodeStatementDeclareVariable : AParserNode<Statement_DeclareVariable>
    {
        /// <summary>
        ///     The default type used when no type has been specified.
        /// </summary>
        public SolType VariableTypeImplicit { get; set; } = SolType.AnyNil;

        /// <inheritdoc />
        protected override BnfExpression Rule_Impl
            => KEYWORD("var")
               + TERMINAL<IdentifierNode>(id: "name")
               + (PUNCTUATION(":") + NODE<SolNodeTypeReference>(id: "type")).Q()
               + (OPERATOR("=") + NODE<SolNodeExpression>(id: "expression")).Q()
        ;

        #region Overrides

        /// <inheritdoc />
        protected override Statement_DeclareVariable BuildAndGetNode(IAstNode[] astNodes)
        {
            string name = OfId<IdentifierNode>("name").GetValue();
            SolType type = OfId<SolNodeTypeReference>("type", true)?.GetValue() ?? VariableTypeImplicit;
            SolExpression expression = OfId<SolNodeExpression>("expression", true)?.GetValue();
            return new Statement_DeclareVariable(SolAssembly.CurrentlyParsingThreadStatic, Location, name, type, expression);
        }

        #endregion
    }
}