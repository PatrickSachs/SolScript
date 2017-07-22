// ---------------------------------------------------------------------
// SolScript - A simple but powerful scripting language.
// Offical repository: https://bitbucket.org/PatrickSachs/solscript/
// SolScript is licensed unter The MIT License.
// ---------------------------------------------------------------------
// ReSharper disable ArgumentsStyleStringLiteral

using System.Linq;
using Irony.Parsing;
using NodeParser.Nodes;
using NodeParser.Nodes.NonTerminals;
using PSUtility.Enumerables;
using SolScript.Interpreter;
using SolScript.Interpreter.Statements;
using SolScript.Parser.Nodes.Statements;

namespace SolScript.Parser.Nodes
{
    /// <summary>
    ///     A node representing a chunk.
    /// </summary>
    public class SolNodeChunk : AParserNode<SolChunk>
    {
        /// <inheritdoc />
        protected override BnfExpression Rule_Impl
            => NODE<SolNodeStatement>().LIST<SolStatement>(null, TermListOptions.StarList);

        #region Overrides

        /// <inheritdoc />
        protected override SolChunk BuildAndGetNode(IAstNode[] astNodes)
        {
            var list = (ListNode<SolStatement>) astNodes[0];
            var statements = list.Count == 0 ? Enumerable.Empty<SolStatement>() : list.GetValue();
            return new SolChunk(SolAssembly.CurrentlyParsingThreadStatic, Location, statements);
        }

        #endregion
    }
}