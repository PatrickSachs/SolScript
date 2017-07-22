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
using NodeParser.Nodes.NonTerminals;
using NodeParser.Nodes.Terminals;
using SolScript.Interpreter;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Parser.Nodes.Expressions;

namespace SolScript.Parser.Nodes
{
    /// <summary>
    ///     Node for a field in SolScript.
    /// </summary>
    public class SolNodeField : AParserNode<SolFieldDefinition>
    {
        /// <summary>
        ///     The access modifier used when none was specified.
        /// </summary>
        public static SolAccessModifier AccessModifierImplict { get; set; } = SolAccessModifier.Local;

        /// <summary>
        ///     The default field type when no type was specified.
        /// </summary>
        public static SolType TypeImplicit { get; set; } = new SolType(SolValue.ANY_TYPE, false);

        /// <inheritdoc />
        protected override BnfExpression Rule_Impl
            =>
                ID(NODE<SolNodeAnnotation>().LIST<SolAnnotationDefinition>(null, TermListOptions.StarList), "annotations")
                + NODE<SolNodeAccessModifier>(id: "access").Q()
                + TERMINAL<IdentifierNode>(id: "name")
                + (PUNCTUATION(":") + NODE<SolNodeTypeReference>(id: "type")).Q()
                + (OPERATOR("=")
                   + NODE<SolNodeExpression>(id: "init")
                ).Q()
        ;

        #region Overrides

        /// <inheritdoc />
        protected override SolFieldDefinition BuildAndGetNode(IAstNode[] astNodes)
        {
            SolAccessModifier modifier = OfId<SolNodeAccessModifier>("access", true)?.GetValue() ?? AccessModifierImplict;
            string name = OfId<IdentifierNode>("name").GetValue();
            SolType type = OfId<SolNodeTypeReference>("type", true)?.GetValue() ?? TypeImplicit;
            SolExpression initializer = OfId<SolNodeExpression>("init", true)?.GetValue();
            var annotNode = OfId<ListNode<SolAnnotationDefinition>>("annotations");

            SolFieldDefinition definition = new SolFieldDefinition(SolAssembly.CurrentlyParsingThreadStatic, Location) {
                Name = name,
                Type = type,
                AccessModifier = modifier,
                Initializer = initializer != null ? new SolFieldInitializerWrapper(initializer) : null
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