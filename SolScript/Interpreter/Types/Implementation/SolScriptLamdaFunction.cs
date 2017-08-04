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

using JetBrains.Annotations;
using NodeParser;
using PSUtility.Strings;
using SolScript.Exceptions;
using SolScript.Properties;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     A lamda function. No real documentation yet since lamda functions are subject to change and to be expanded.
    /// </summary>
    public sealed class SolScriptLamdaFunction : SolLamdaFunction
    {
        /// <summary>
        ///     Creates a new script lamda function from the given parameters.
        /// </summary>
        /// <param name="assembly">The assembly this function belongs to.</param>
        /// <param name="location">The location in the source code this functionwas declared at.</param>
        /// <param name="parameterInfo">The function parameters.</param>
        /// <param name="returnType">The function return type.</param>
        /// <param name="chunk">The function chunk, containing the actual code of the function.</param>
        /// <param name="parentVariables">
        ///     The parent variables of the function. Set this to a non-null value if the function was
        ///     e.g. declared inside  another function and thus needs to have access to that functions variable scope.
        /// </param>
        /// <param name="definedIn">The class the function was defined in.</param>
        public SolScriptLamdaFunction([NotNull] SolAssembly assembly, NodeLocation location, [NotNull] SolParameterInfo parameterInfo,
            SolType returnType, [NotNull] SolChunk chunk, [CanBeNull] IVariables parentVariables, [CanBeNull] IClassLevelLink definedIn)
            : base(assembly, location, parameterInfo, returnType, definedIn)
        {
            m_Chunk = chunk;
            m_ParentVariables = parentVariables;
        }

        private readonly SolChunk m_Chunk;

        [CanBeNull]
        private readonly IVariables m_ParentVariables;

        #region Overrides

        /*/// <inheritdoc />
        protected override string ToString_Impl(SolExecutionContext context)
        {
            return $"function#{Id}<lamda>";
        }*/

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            Variables variables = new Variables(Assembly) {Parent = m_ParentVariables};
            try {
                InsertParameters(variables, args);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, Resources.Err_InvalidFunctionCallParameters.FormatWith(Name), ex);
            }
            // Functions pretty much eat the terminators since that's what the terminators are supposed to terminate down to.
            Terminators terminators;
            SolValue value = m_Chunk.Execute(context, variables, out terminators);
            return value;
        }

        #endregion
    }
}