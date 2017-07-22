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
using NodeParser;
using SolScript.Compiler;
using SolScript.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements
{
    /// <summary>
    ///     This statement returns a value from inside the chunk and terminates it.
    /// </summary>
    public class Statement_Return : SolStatement
    {
        /// <inheritdoc />
        public Statement_Return(SolAssembly assembly, NodeLocation location, [CanBeNull] SolExpression returnExpression) : base(assembly, location)
        {
            ReturnExpression = returnExpression;
        }

        /// <summary>
        ///     The expression determining the return value.
        /// </summary>
        [CanBeNull]
        public SolExpression ReturnExpression { get; }

        #region Overrides

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return "return " + ReturnExpression;
        }

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            return ReturnExpression?.Validate(context) ?? new ValidationResult(true, new SolType(SolNil.TYPE, true));
        }

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured while evaluating the expression.</exception>
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            terminators = Terminators.Return;
            return ReturnExpression?.Evaluate(context, parentVariables) ?? SolNil.Instance;
        }

        #endregion
    }
}