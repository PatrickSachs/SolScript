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

using System;
using JetBrains.Annotations;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     The <see cref="SolClassFunction" /> class is the base class for functions declared in a class, both native and
    ///     script based.
    /// </summary>
    public abstract class SolClassFunction : DefinedSolFunction
    {
        /// <summary>
        ///     Creates the class function.
        /// </summary>
        /// <param name="definedIn">The class/inheritance level the function was declared in.</param>
        /// <exception cref="ArgumentNullException"><paramref name="definedIn" /> is <see langword="null" /></exception>
        internal SolClassFunction([NotNull] IClassLevelLink definedIn)
        {
            if (definedIn == null) {
                throw new ArgumentNullException(nameof(definedIn));
            }
            DefinedIn = definedIn;
        }

        /*/// <summary>
        /// The class instance of this class function.
        /// </summary>
        public abstract SolClass ClassInstance { get; }
        
        /// <inheritdoc />
        protected override SolClass GetClassInstance(out bool isCurrent, out bool resetOnExit)
        {
            isCurrent = true;
            resetOnExit = true;
            return ClassInstance;
        }*/

        /// <inheritdoc />
        [NotNull]
        public override IClassLevelLink DefinedIn { get; }

        /*#region Overrides

        /// <inheritdoc />
        protected override string ToString_Impl(SolExecutionContext context)
        {
            //return "function#" + Id + "<" + ClassInstance.InheritanceChain.Definition.Type+"#"+ClassInstance.Id + "." + Definition.Name + "/"+Definition.DefinedIn.Type+">";
            return "function#" + Id + "<" + DefinedIn + ">";
        }

        #endregion*/
    }
}