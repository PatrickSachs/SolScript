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
using System.Collections.Generic;
using NodeParser;
using PSUtility.Enumerables;
using SolScript.Interpreter.Expressions;
using SolScript.Properties;
using SolScript.Utility;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     This definition is used to declare the usage of an annotation.
    /// </summary>
    public sealed class SolAnnotationDefinition : SolDefinition
    {
        /// <summary>Creates a new annotation definition.</summary>
        /// <param name="location">The location in node.</param>
        /// <param name="type">The annotation type name.</param>
        /// <param name="arguments">The annotation constructor arguments.</param>
        /// <param name="assembly">The assembly owing the annotation.</param>
        /// <exception cref="ArgumentNullException">
        ///     An argument is <see langword="null" />
        /// </exception>
        public SolAnnotationDefinition(SolAssembly assembly, NodeLocation location, SolClassDefinitionReference type, IEnumerable<SolExpression> arguments) : base(assembly, location)
        {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            if (arguments == null) {
                throw new ArgumentNullException(nameof(arguments));
            }
            m_Arguments = InternalHelper.CreateArray(arguments);
            m_Reference = type;
        }

        private readonly Array<SolExpression> m_Arguments;

        private readonly SolClassDefinitionReference m_Reference;

        /// <summary>
        ///     The annotation constructor arguments.
        /// </summary>
        public ReadOnlyList<SolExpression> Arguments => m_Arguments.AsReadOnly();

        /// <summary>
        ///     The annotation class name.
        /// </summary>
        /// <remarks>Always valid.</remarks>
        public string ClassName => m_Reference.Name;

        /// <summary>
        ///     The annotation class definition.
        /// </summary>
        /// <remarks>May not be valid dependig on assembly state.</remarks>
        /// <exception cref="InvalidOperationException">Failed to get the class definition.</exception>
        public SolClassDefinition Definition {
            get {
                SolClassDefinition definition;
                if (!m_Reference.TryGetDefinition(out definition)) {
                    throw new InvalidOperationException(Resources.Err_ClassDefinitionNotValid.ToString(m_Reference.Name));
                }
                return definition;
            }
        }
    }
}