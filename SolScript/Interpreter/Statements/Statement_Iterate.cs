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
    ///     A statement iteration over a value by converting it into a table.
    /// </summary>
    // todo: we need a proper iterable type, converting everything into tables is really hacky and inefficent.
    public class Statement_Iterate : SolStatement
    {
        /// <summary>
        ///     Creates a new iterator statement.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="location">The code location.</param>
        /// <param name="iteratorGetter">The expression obtaining the value that shoould be iterated.</param>
        /// <param name="iteratorName">The name of the iterator variable.</param>
        /// <param name="chunk">The chunk that should be called for each iteration.</param>
        public Statement_Iterate(SolAssembly assembly, NodeLocation location, SolExpression iteratorGetter, string iteratorName,
            SolChunk chunk) : base(assembly, location)
        {
            IteratorGetter = iteratorGetter;
            IteratorName = iteratorName;
            Chunk = chunk;
        }

        /// <summary>
        ///     The chunk that should be called for each iteration.
        /// </summary>
        public SolChunk Chunk { get; }

        /// <summary>
        ///     The expression obtaining the value that shoould be iterated.
        /// </summary>
        public SolExpression IteratorGetter { get; }

        /// <summary>
        ///     The name of the iterator variable.
        /// </summary>
        public string IteratorName { get; }

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured while evaluating the statement.</exception>
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            Variables vars = new Variables(Assembly) {Parent = parentVariables};
            SolValue iterator = IteratorGetter.Evaluate(context, parentVariables);
            try {
                vars.Declare(IteratorName, new SolType("any", true));
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, "Failed to declare the iterator variable \"" + IteratorName + "\".", ex);
            }
            foreach (SolValue value in iterator.Iterate(context)) {
                Variables variables = new Variables(Assembly) {Parent = vars};
                try {
                    vars.Assign(IteratorName, value);
                } catch (SolVariableException ex) {
                    throw new SolRuntimeException(context, "Failed to assign an iterator value of type \"" + value.Type + "\" to the iterator variable \"" + IteratorName + "\".", ex);
                }
                Terminators chunkTerminators;
                SolValue returnValue = Chunk.Execute(context, variables, out chunkTerminators);
                if (InternalHelper.DidReturn(chunkTerminators)) {
                    terminators = Terminators.Return;
                    return returnValue;
                }
                if (InternalHelper.DidBreak(chunkTerminators)) {
                    break;
                }
                // Continue is breaking the chunk execution.
                if (InternalHelper.DidContinue(chunkTerminators)) {}
            }
            terminators = Terminators.None;
            return SolNil.Instance;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("for(");
            builder.Append(IteratorName);
            builder.Append(" in ");
            builder.Append(IteratorGetter);
            builder.AppendLine(") do");
            builder.AppendLine(Chunk.ToString().Replace("\n", "\n  "));
            builder.Append("end");
            return builder.ToString();
        }

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            var getRes = IteratorGetter.Validate(context);
            if (!getRes) {
                return ValidationResult.Failure();
            }
            // todo: these vars do not belong to a chunk. refactor the way the validation var stack works?
            var itVars = new SolValidationContext.Chunk(Chunk);
            itVars.AddVariable(IteratorName, getRes.Type);
            context.Chunks.Push(itVars);
            var chkRes = Chunk.Validate(context);
            if (context.Chunks.Pop() != itVars) {
                // ReSharper disable once ExceptionNotDocumented
                throw new InvalidOperationException("Chunk stack corrupted.");
            }
            return chkRes;
        }

        #endregion
    }
}