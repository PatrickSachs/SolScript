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

using PSUtility.Strings;
using SolScript.Exceptions;
using SolScript.Properties;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     This class is used for global functions declared in script.
    /// </summary>
    public sealed class SolScriptGlobalFunction : GlobalSolFunction
    {
        /// <summary>
        ///     Creates the function.
        /// </summary>
        /// <param name="definition">The function definition.</param>
        public SolScriptGlobalFunction(SolFunctionDefinition definition)
        {
            Definition = definition;
        }

        /// <inheritdoc />
        public override SolFunctionDefinition Definition { get; }

        #region Overrides

        /*/// <inheritdoc />
        protected override SolClass GetClassInstance(out bool isCurrent, out bool resetOnExit)
        {
            isCurrent = true;
            resetOnExit = true;
            return null;
        }*/

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked {
                return 14 + (int) Id;
            }
        }

        /// <inheritdoc />
        public override bool Equals(object other)
        {
            return other == this;
        }

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            Variables varContext = new Variables(Assembly) {
                Parent = Assembly.LocalVariables
            };
            try {
                InsertParameters(varContext, args);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, Resources.Err_InvalidFunctionCallParameters.FormatWith(Name), ex);
            }
            // Functions pretty much eat the terminators since that's what the terminators are supposed to terminate down to.
            Terminators terminators;
            SolValue returnValue = Definition.Chunk.GetScriptChunk().Execute(context, varContext, out terminators);
            return returnValue;
        }

        #endregion
    }
}