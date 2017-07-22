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
using NodeParser.Nodes.Terminals;
using SolScript.Interpreter;

namespace SolScript.Parser.Nodes
{
    /// <summary>
    ///     A node including a type name and optional nilability.
    /// </summary>
    public class SolNodeTypeReference : AParserNode<SolType>
    {
        /// <summary>
        ///     The token used for nilable types.
        /// </summary>
        public const string NILABLE = "?";

        /// <summary>
        ///     The token used for not nilable types.
        /// </summary>
        public const string NOT_NILABLE = "!";

        /// <summary>
        ///     If no expliclt nilability was specified, should types be nilable(true) or not(false)?
        /// </summary>
        public static bool CanBeNilImplicit { get; set; } = false;

        /// <inheritdoc />
        // todo: PUNCTUATION causes the ast node for the nilable notation to be a default ast node.
        protected override BnfExpression Rule_Impl
            => SHIFT()
               + TERMINAL<IdentifierNode>()
               + (
                   TERM(NILABLE, true, NILABLE + "_typeRef")
                   | TERM(NOT_NILABLE, false, NOT_NILABLE + "_typeRef")
               ).OPT()
        ;

        #region Overrides

        /// <inheritdoc />
        protected override SolType BuildAndGetNode(IAstNode[] astNodes)
        {
            string typeName = (string) astNodes[0].GetValue();
            OptionalNode typeOpt = (OptionalNode) astNodes[1];
            bool canBeNil = typeOpt.HasValue ? (bool)typeOpt.GetValue() : CanBeNilImplicit;
            return new SolType(typeName, canBeNil);
        }

        #endregion
    }
}