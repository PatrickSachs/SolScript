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
using NodeParser.Nodes.NonTerminals;
using NodeParser.Nodes.Terminals;
using SolScript.Interpreter;
using SolScript.Utility;

namespace SolScript.Parser.Nodes
{
    /// <summary>
    ///     A function definion node.
    /// </summary>
    public class SolNodeFunction : AParserNode<SolFunctionDefinition>
    {
        /// <summary>
        ///     The default access modifier on functions.
        /// </summary>
        public static SolAccessModifier AccessModifierImplicit { get; set; } = SolAccessModifier.Global;

        /// <summary>
        ///     The default member modifier on functions. It is not recommended to change this value.
        /// </summary>
        public static SolMemberModifier MemberModifierImplicit { get; set; } = SolMemberModifier.Default;

        /// <summary>
        ///     The default return type if none was specified. Should prefarable be a nilable type since function without return
        ///     statement return nil.
        /// </summary>
        public static SolType ReturnTypeDefault { get; set; } = SolType.AnyNil;

        /// <inheritdoc />
        protected override BnfExpression Rule_Impl
            => ID(NODE<SolNodeAnnotation>().LIST<SolAnnotationDefinition>(null, TermListOptions.StarList), "annotations")
               + NODE<SolNodeAccessModifier>(id: "accessMod").Q()
               + NODE<SolNodeMemberModifier>(id: "memberMod").Q()
               + KEYWORD("function")
               + TERMINAL<IdentifierNode>(id: "name")
               + BRACES("(",
                   NODE<SolNodeParameters>(id: "params"),
                   ")")
               + (PUNCTUATION(":") + NODE<SolNodeTypeReference>(id: "return")).Q()
               + NODE<SolNodeChunk>(id: "chunk")
               + KEYWORD("end");

        #region Overrides

        /// <inheritdoc />
        protected override SolFunctionDefinition BuildAndGetNode(IAstNode[] astNodes)
        {
            SolAccessModifier accessModifier = astNodes.NodeValue((node, index) => node is SolNodeAccessModifier, AccessModifierImplicit);
            SolMemberModifier memberModifier = astNodes.NodeValue((node, index) => node is SolNodeMemberModifier, MemberModifierImplicit);
            SolChunk chunk = astNodes.NodeValue((node, index) => node is SolNodeChunk, (SolChunk) null).NotNull();
            SolParameterInfo param = OfId<SolNodeParameters>("params").GetValue();
            SolType returnType = OfId<SolNodeTypeReference>("return")?.GetValue() ?? ReturnTypeDefault;
            string name = OfId<IdentifierNode>("name").GetValue();
            var annotNode = OfId<ListNode<SolAnnotationDefinition>>("annotations");

            SolFunctionDefinition definition = new SolFunctionDefinition(SolAssembly.CurrentlyParsingThreadStatic, Location) {
                AccessModifier = accessModifier,
                MemberModifier = memberModifier,
                Chunk = new SolChunkWrapper(chunk),
                Type = returnType,
                ParameterInfo = param,
                Name = name
            };
            if (annotNode.Count > 0) {
                foreach (SolAnnotationDefinition annotation in annotNode.GetValue()) {
                    definition.AddAnnotation(annotation);
                }
            }
            return definition;
        }

        #endregion
    }
}