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
using NodeParser.Nodes.NonTerminals;
using SolScript.Interpreter;

namespace SolScript.Parser.Nodes
{
    /// <summary>
    ///     A node representing a member modifier.
    /// </summary>
    public class SolNodeMemberModifier : ConstantNode<SolMemberModifier>
    {
        /// <inheritdoc />
        protected override bool AreKeywords => true;

        #region Overrides

        /// <inheritdoc />
        protected override IEnumerable<Constant> GetConstants()
        {
            yield return new Constant("default", SolMemberModifier.Default, "default_memberModifier");
            yield return new Constant("abstract", SolMemberModifier.Abstract, "abstract_memberModifier");
            yield return new Constant("override", SolMemberModifier.Override);
        }

        #endregion
    }
}