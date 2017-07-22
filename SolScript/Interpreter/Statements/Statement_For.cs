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
using System.Text;
using NodeParser;
using SolScript.Compiler;
using SolScript.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Utility;

namespace SolScript.Interpreter.Statements
{
    /// <summary>
    ///     A for loop allows for simple iteration with user defined flow control.
    /// </summary>
    public class Statement_For : SolStatement
    {
        /// <summary>
        ///     Creates a for-loop.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="location">The location in code.</param>
        /// <param name="initialization">The initializer.</param>
        /// <param name="condition">The condition that must be met to enter the chunk.</param>
        /// <param name="afterthought">The modifier called after each successful chunk call.</param>
        /// <param name="chunk">The called chunk.</param>
        public Statement_For(SolAssembly assembly, NodeLocation location, Init initialization,
            SolExpression condition, SolExpression afterthought, SolChunk chunk)
            : base(assembly, location)
        {
            Initialization = initialization;
            Condition = condition;
            Afterthought = afterthought;
            Chunk = chunk;
        }

        /// <summary>
        ///     The modifier called after each successful chunk call.
        /// </summary>
        public SolExpression Afterthought { get; }

        /// <summary>
        ///     The called chunk.
        /// </summary>
        public SolChunk Chunk { get; }

        /// <summary>
        ///     The condition that must be met to enter the chunk.
        /// </summary>
        public SolExpression Condition { get; }

        /// <summary>
        ///     The initializer of this statement.
        /// </summary>
        public Init Initialization { get; }

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured while evaluating the statement.</exception>
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            context.CurrentLocation = Location;
            Terminators initTerminators;
            SolValue stackValue = Initialization.IsExpression
                ? Initialization.Expression.Evaluate(context, parentVariables)
                : Initialization.Statement.Execute(context, parentVariables, out initTerminators);
            // todo: handle init terminators
            while (Condition.Evaluate(context, parentVariables).IsTrue(context)) {
                // The chunk is running in a new context in order to discard the
                // locals for the previous iteration.
                Variables variables = new Variables(Assembly) {Parent = parentVariables};
                Terminators chunkTerminators;
                stackValue = Chunk.Execute(context, variables, out chunkTerminators);
                if (InternalHelper.DidReturn(chunkTerminators)) {
                    terminators = Terminators.Return;
                    return stackValue;
                }
                if (InternalHelper.DidBreak(chunkTerminators)) {
                    break;
                }
                // Continue is breaking the chunk execution.
                if (InternalHelper.DidContinue(chunkTerminators)) {}
                stackValue = Afterthought.Evaluate(context, parentVariables);
            }
            terminators = Terminators.None;
            return stackValue;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("for ");
            builder.Append(Initialization);
            builder.Append(", ");
            builder.Append(Condition);
            builder.Append(", ");
            builder.Append(Afterthought);
            builder.AppendFormat(" do");
            builder.AppendLine(Chunk.ToString().Replace("\n", "\n  "));
            builder.Append("end");
            return builder.ToString();
        }

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            var iniRes = Initialization.Validate(context);
            if (!iniRes) {
                return ValidationResult.Failure();
            }
            var conRes = Condition.Validate(context);
            if (!conRes) {
                return ValidationResult.Failure();
            }
            var aftRes = Afterthought.Validate(context);
            if (!aftRes) {
                return ValidationResult.Failure();
            }
            return Chunk.Validate(context);
        }

        #endregion

        #region Nested type: Init

        /// <summary>
        ///     Contains initializer data for the for statement.
        /// </summary>
        public class Init : ISolCompileable
        {
            /// <inheritdoc />
            /// <exception cref="ArgumentNullException"><paramref name="expression" /> is <see langword="null" /></exception>
            public Init(SolExpression expression)
            {
                if (expression == null) {
                    throw new ArgumentNullException(nameof(expression));
                }
                Expression = expression;
                Statement = null;
            }

            /// <inheritdoc />
            /// <exception cref="ArgumentNullException"><paramref name="statement" /> is <see langword="null" /></exception>
            public Init(Statement_DeclareVariable statement)
            {
                if (statement == null) {
                    throw new ArgumentNullException(nameof(statement));
                }
                Expression = null;
                Statement = statement;
            }

            /// <inheritdoc />
            public override string ToString()
            {
                return IsExpression ? Expression.ToString() : Statement.ToString();
            }

            /// <summary>
            ///     The expression.
            /// </summary>
            public SolExpression Expression { get; }

            /// <summary>
            ///     Does this initializer use an expression or is special statement treatment required?
            /// </summary>
            public bool IsExpression => Expression != null;

            /// <summary>
            ///     The special statement.
            /// </summary>
            public Statement_DeclareVariable Statement { get; }

            #region ISolCompileable Members

            /// <summary>
            ///     Validates the expression or statement.
            /// </summary>
            /// <param name="c">The validation context.</param>
            /// <returns>The validation result.</returns>
            public ValidationResult Validate(SolValidationContext c)
            {
                return IsExpression ? Expression.Validate(c) : Statement.Validate(c);
            }

            #endregion
        }

        #endregion
    }
}