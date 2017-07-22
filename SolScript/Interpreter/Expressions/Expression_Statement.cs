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
using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     This expression wraps a statement inside of it. The runtime allows the wrapping of any statement while the language
    ///     only supports a selected few.
    /// </summary>
    public class Expression_Statement : SolExpression
    {
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="statement" /> is <see langword="null" /></exception>
        public Expression_Statement(SolAssembly assembly, NodeLocation location, SolStatement statement) : base(assembly, location)
        {
            if (statement == null) {
                throw new ArgumentNullException(nameof(statement));
            }
            Statement = statement;
        }

        /// <inheritdoc />
        public override bool IsConstant => false;

        /// <summary>
        ///     The statement wrapped in this expression.
        /// </summary>
        public SolStatement Statement { get; /*[UsedImplicitly] internal set;*/ }

        #region Overrides

        /// <inheritdoc />
        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            context.CurrentLocation = Location;
            Terminators terminators;
            SolValue value = Statement.Execute(context, parentVariables, out terminators);
            return value;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return Statement.ToString();
        }

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            return Statement.Validate(context);
        }

        #endregion
    }
}