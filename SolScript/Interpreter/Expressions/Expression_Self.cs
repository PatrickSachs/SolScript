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
using NodeParser;
using SolScript.Compiler;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     The self-Statement can be used to obtain a reference to the current class. <br />The reasons to use self over
    ///     direct identifier access is the possibility to index the own class by using expressions, or to access
    ///     fields/functions with the same name as variable.
    /// </summary>
    public class Expression_Self : SolExpression
    {
        /// <inheritdoc />
        public Expression_Self(SolAssembly assembly, NodeLocation location) : base(assembly, location) {}

        /// <inheritdoc />
        public override bool IsConstant => false;

        #region Overrides

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            if (context.InClassDefinition == null) {
                return ValidationResult.Failure();
            }
            return new ValidationResult(true, new SolType(context.InClassDefinition.Type, false));
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Self expression cannot be executed in global context.</exception>
        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            SolClassEntry entry;
            if (!context.PeekClassEntry(out entry) || entry.IsGlobal) {
                throw new InvalidOperationException("Tried to use the \"self\" expression in global context. This is not allowed.");
            }
            return entry.Instance;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return "self";
        }

        #endregion
    }
}