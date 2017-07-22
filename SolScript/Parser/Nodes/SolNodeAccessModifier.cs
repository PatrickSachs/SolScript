using System.Collections.Generic;
using NodeParser.Nodes.NonTerminals;
using SolScript.Interpreter;

namespace SolScript.Parser.Nodes
{
    /// <summary>
    ///     A node that specifies the access of something(global, internal, local)
    /// </summary>
    public class SolNodeAccessModifier : ConstantNode<SolAccessModifier>
    {
        /// <inheritdoc />
        protected override bool AreKeywords => true;

        #region Overrides

        /// <inheritdoc />
        protected override IEnumerable<Constant> GetConstants()
        {
            yield return new Constant("global", SolAccessModifier.Global);
            yield return new Constant("local", SolAccessModifier.Local);
            yield return new Constant("internal", SolAccessModifier.Internal);
        }

        #endregion
    }
}