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

using NodeParser;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     Base class for all lambda <see cref="SolFunction" />s. Lamda functions can be described as anonymous functions and
    ///     thus are pretty much the purest form of a function. They do not require an actual definition and and can be created
    ///     at any given point without having to worry about anything.
    /// </summary>
    public abstract class SolLamdaFunction : SolFunction
    {
        // No third party primitives
        internal SolLamdaFunction(
            SolAssembly assembly,
            NodeLocation location,
            SolParameterInfo parameterInfo,
            SolType returnType,
            IClassLevelLink definedIn)
        {
            DefinedIn = definedIn;
            Assembly = assembly;
            ParameterInfo = parameterInfo;
            ReturnType = returnType;
            Location = location;
        }

        /// <inheritdoc />
        public override SolAssembly Assembly { get; }

        /// <inheritdoc />
        public override IClassLevelLink DefinedIn { get; }

        /// <inheritdoc />
        public override NodeLocation Location { get; }

        /// <inheritdoc />
        public override SolParameterInfo ParameterInfo { get; }

        /// <inheritdoc />
        public override SolType ReturnType { get; }

        /*/// <inheritdoc />
        protected override SolClass GetClassInstance(out bool isCurrent, out bool resetOnExit)
        {
            isCurrent = true;
            resetOnExit = true;
            return DefinedIn;
        }*/
    }
}