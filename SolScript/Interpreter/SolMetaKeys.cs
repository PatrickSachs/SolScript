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

using PSUtility.Metadata;
using SolScript.Interpreter.Expressions;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     All default meta keys usable to access a <see cref="SolAssembly" />. The key name are equal to their field names.
    ///     Feel free to use new and existing keys to share data between libraries in an isoalted native space.
    /// </summary>
    public static class SolMetaKeys
    {
        /// <summary>
        ///     A meta value containing the self referencing expression.
        /// </summary>
        internal static readonly MetaKey<Expression_Self> ExpressionSelf = new MetaKey<Expression_Self>(nameof(Expression_Self));

        /// <summary>
        ///     Used by the marshaller to cache information about the assembly.
        /// </summary>
        internal static readonly MetaKey<SolMarshal.AssemblyCache> SolMarshalAssemblyCache = new MetaKey<SolMarshal.AssemblyCache>(nameof(SolMarshalAssemblyCache));

        /// <summary>
        ///     An empty chunk at native code location.
        /// </summary>
        internal static readonly MetaKey<SolChunkWrapper> EmptyChunk = new MetaKey<SolChunkWrapper>(nameof(EmptyChunk));
    }
}