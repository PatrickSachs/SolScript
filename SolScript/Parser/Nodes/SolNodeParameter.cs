// ---------------------------------------------------------------------
// SolScript - A simple but powerful scripting language.
// Offical repository: https://bitbucket.org/PatrickSachs/solscript/
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
using NodeParser.Nodes.Terminals;
using PSUtility.Enumerables;
using SolScript.Interpreter;
using SolScript.Interpreter.Types;

namespace SolScript.Parser.Nodes
{
    /// <summary>
    ///     Node for a function parameter.
    /// </summary>
    public class SolNodeParameter : AParserNode<SolParameter>
    {
        /// <summary>
        ///     The default type when not specifying the type.
        /// </summary>
        public static SolType TypeImplicit { get; set; } = new SolType(SolValue.ANY_TYPE, false);

        /// <inheritdoc />
        protected override BnfExpression Rule_Impl
            => TERMINAL<IdentifierNode>()
               + (PUNCTUATION(":") + NODE<SolNodeTypeReference>()).Q();

        #region Overrides

        /// <inheritdoc />
        protected override SolParameter BuildAndGetNode(IAstNode[] astNodes)
        {
            string name = (string) astNodes[0].GetValue();
            ReadOnlyList<IAstNode> q = astNodes[1].As<DefaultAst>().GetValue();
            return q.Count == 0 
                ? new SolParameter(name, TypeImplicit) 
                : new SolParameter(name, q[0].As<SolNodeTypeReference>().GetValue());
            /*SolType type = astNodes.NodeValue((node, index) => node is SolNodeTypeReference, TypeImplicit);
            return new SolParameter(name, type);*/
        }

        #endregion
    }
}