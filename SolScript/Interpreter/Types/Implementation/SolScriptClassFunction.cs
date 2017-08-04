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
using SolScript.Utility;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     This class is used for class functions declared in code.
    /// </summary>
    public sealed class SolScriptClassFunction : SolClassFunction
    {
        /// <summary>
        ///     Creates a new function instance.
        /// </summary>
        /// <param name="inClass">The class this function belongs to.</param>
        /// <param name="definition">The definition of this function.</param>
        public SolScriptClassFunction(IClassLevelLink inClass, SolFunctionDefinition definition) : base(inClass)
        {
            //ClassInstance = inClass;
            Definition = definition;
        }

        /// <inheritdoc />
        public override SolAssembly Assembly => Definition.DefinedIn.NotNull().Assembly;

        /// <inheritdoc />
        public override SolFunctionDefinition Definition { get; }

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            //SolClass.Inheritance inheritance = ClassInstance.FindInheritance(Definition.DefinedIn).NotNull();
            SolClass.Inheritance inheritance = DefinedIn.Inheritance();
            Variables varContext = new Variables(Assembly) {
                Parent = inheritance.GetVariables(SolAccessModifier.Local, SolVariableMode.All)
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

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked {
                return 11 + (int) Id;
            }
        }

        /// <inheritdoc />
        public override bool Equals(object other)
        {
            return other == this;
        }

        #endregion

        /*/// <inheritdoc />
        public override SolClass ClassInstance { get; }*/
    }
}