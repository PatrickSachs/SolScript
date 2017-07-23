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

namespace SolScript.Parser.Nodes
{
    /// <summary>
    ///     A node for creating a class definition.
    /// </summary>
    public class SolNodeClass : AParserNode<SolClassDefinition>
    {
        /// <summary>
        /// A class member.
        /// </summary>
        public class Member : AParserNode<SolMemberDefinition>
        {
            /// <inheritdoc />
            protected override BnfExpression Rule_Impl
                => NODE<SolNodeField>() | NODE<SolNodeFunction>();

            /// <inheritdoc />
            protected override SolMemberDefinition BuildAndGetNode(IAstNode[] astNodes)
            {
                //return (SolMemberDefinition) astNodes[0].As<DefaultAst>()[0].GetValue();
                return (SolMemberDefinition) astNodes[0].GetValue();
            }
        }

        /// <summary>
        ///     The default type mode if non has been specified.
        /// </summary>
        public static SolTypeMode TypeModeImplicit { get; set; } = SolTypeMode.Default;

        /// <inheritdoc />
        protected override BnfExpression Rule_Impl
            =>
                NODE<SolNodeAnnotation>().LIST<SolAnnotationDefinition>(null, TermListOptions.StarList)
                + NODE<SolNodeClassModifier>().OPT()
                + KEYWORD("class")
                + TERMINAL<IdentifierNode>()
                + NODE<Member>().LIST<SolMemberDefinition>(null, TermListOptions.StarList)
                + KEYWORD("end");

        #region Overrides

        /// <inheritdoc />
        protected override SolClassDefinition BuildAndGetNode(IAstNode[] astNodes)
        {
            IEnumerable<SolAnnotationDefinition> annotations = astNodes[0].As<ListNode<SolAnnotationDefinition>>().GetValue();
            SolTypeMode typeMode = astNodes[1].As<OptionalNode>().GetValue(TypeModeImplicit);
            string name = astNodes[3].As<IdentifierNode>().GetValue();
            IEnumerable<SolMemberDefinition> members = astNodes[4].As<ListNode<SolMemberDefinition>>().GetValue();
            /*IEnumerable<SolAnnotationDefinition> annotations = OfId<ListNode<SolAnnotationDefinition>>("annotations").GetValue();
            SolTypeMode typeMode = OfId<SolNodeClassModifier>("modifier", true)?.GetValue() ?? TypeModeImplicit;
            string name = OfId<IdentifierNode>("name").GetValue();
            IEnumerable<SolMemberDefinition> members = OfId<ListNode<SolMemberDefinition>>("members").GetValue();*/
            SolClassDefinition definition = new SolClassDefinition(SolAssembly.CurrentlyParsingThreadStatic, Location, false) {
                Type = name,
                TypeMode = typeMode
            };
            // todo: validate against duplicates
            foreach (SolMemberDefinition member in members) {
                SolFieldDefinition field = member as SolFieldDefinition;
                if (field != null) {
                    definition.AssignFieldDirect(field);
                }
                SolFunctionDefinition function = member as SolFunctionDefinition;
                if (function != null) {
                    definition.AssignFunctionDirect(function);
                }
            }
            foreach (SolAnnotationDefinition annotation in annotations) {
                definition.AddAnnotation(annotation);
            }
            return definition;
        }

        #endregion
    }
}