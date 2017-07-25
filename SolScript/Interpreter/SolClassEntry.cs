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
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The current class we are in, containg the class instance, and which class inheritance level of said instance we are
    ///     currently in. This is required to e.g. resolve which class inheritance level "base" exactly is.
    /// </summary>
    /// <remarks>
    ///     Can only safely cast to <see cref="IClassLevelLink" /> if <see cref="IsGlobal" /> is false. Global class level
    ///     links are null instead of having null values.
    /// </remarks>
    public struct SolClassEntry : IClassLevelLink
    {
        /// <summary>
        ///     Is the entry global?
        /// </summary>
        public readonly bool IsGlobal;

        /// <summary>
        ///     The class instance.
        /// </summary>
        public readonly SolClass Instance;

        /// <summary>
        ///     The inheritance level we are at.
        /// </summary>
        public readonly SolClassDefinition Level;

        /// <inheritdoc />
        private SolClassEntry(bool global, SolClass instance, SolClassDefinition level)
        {
            Instance = instance;
            Level = level;
            IsGlobal = global;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Instance.Definition.Type + "#" + Level.Type;
        }

        /// <summary>
        ///     Creates a new global class entry.
        /// </summary>
        /// <returns>The global class entry.</returns>
        public static SolClassEntry Global() => new SolClassEntry(true, null, null);

        /// <summary>
        ///     Creates a new class based class entry.
        /// </summary>
        /// <param name="instance">The instance we entered.</param>
        /// <param name="level">The inheritance level.</param>
        /// <returns>The class based class entry.</returns>
        /// <exception cref="ArgumentNullException">An argument is <see langword="null" /></exception>
        public static SolClassEntry Class([NotNull] SolClass instance, [NotNull] SolClassDefinition level)
        {
            if (instance == null) {
                throw new ArgumentNullException(nameof(instance));
            }
            if (level == null) {
                throw new ArgumentNullException(nameof(level));
            }
            return new SolClassEntry(false, instance, level);
        }

        /// <inheritdoc />
        SolClass IClassLevelLink.ClassInstance => Instance;

        /// <inheritdoc />
        SolClassDefinition IClassLevelLink.InheritanceLevel => Level;
    }
}