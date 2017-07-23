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
using SolScript.Interpreter;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Statements;
using SolScript.Parser.Nodes.Expressions;

namespace SolScript.Parser.Nodes.Statements
{
    /// <summary>
    ///     Node for create a variable assignment statement.
    /// </summary>
    public class SolNodeStatementAssignVariable : AParserNode<Statement_AssignVariable>
    {
        /// <inheritdoc />
        protected override BnfExpression Rule_Impl
            => NODE<SolNodeVariable>() + OPERATOR("=") + NODE<SolNodeExpression>()
        ;

        #region Overrides

        /// <inheritdoc />
        protected override Statement_AssignVariable BuildAndGetNode(IAstNode[] astNodes)
        {
            AVariable variable = astNodes[0].As<SolNodeVariable>().GetValue();
            SolExpression value = astNodes[2].As<SolNodeExpression>().GetValue();
            return new Statement_AssignVariable(SolAssembly.CurrentlyParsingThreadStatic, Location, variable, value);
        }

        #endregion
    }
}