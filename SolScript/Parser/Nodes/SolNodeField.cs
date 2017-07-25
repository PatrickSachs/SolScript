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

using System.Collections.Generic;
using Irony.Parsing;
using NodeParser;
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
                NODE<SolNodeAnnotation>().LIST<SolAnnotationDefinition>(null, TermListOptions.StarList)
                + NODE<SolNodeAccessModifier>().OPT()
                + TERMINAL<IdentifierNode>()
                + (PUNCTUATION(":") + NODE<SolNodeTypeReference>()).OPT()
                + (OPERATOR("=") + NODE<SolNodeExpression>()).OPT()
        ;

        #region Overrides

        /// <inheritdoc />
        protected override SolFieldDefinition BuildAndGetNode(IAstNode[] astNodes)
        {
            IEnumerable<SolAnnotationDefinition> annotations = astNodes[0].As<ListNode<SolAnnotationDefinition>>().GetValue();
            SolAccessModifier modifier = astNodes[1].As<OptionalNode>().GetValue(AccessModifierImplict);
            string name = astNodes[2].As<IdentifierNode>().GetValue();
            SolType type = astNodes[3].As<OptionalNode>().GetValue(TypeImplicit);
            SolExpression initializer = astNodes[4].As<OptionalNode>().GetValue(null, list => list[1].As<SolNodeExpression>().GetValue());

            SolFieldDefinition definition = new SolFieldDefinition(SolAssembly.CurrentlyParsingThreadStatic, Location) {
                Name = name,
                Type = type,
                AccessModifier = modifier,
                // A null expression is wrapped in the initializer if the field should not be initialized. We cannot simply set a null
                // initializer wrapper since the wrapper decides if the field is native/script based and thus changes the way the field
                // is declared even if it is no assigned.
                Initializer = new SolFieldInitializerWrapper(initializer)
            };
            foreach (SolAnnotationDefinition annotation in annotations) {
                definition.AddAnnotation(annotation);
            }
            return definition;
        }

        #endregion
    }
}