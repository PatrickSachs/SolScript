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
using PSUtility.Enumerables;
using PSUtility.Strings;
using SolScript.Compiler;
using SolScript.Exceptions;
using SolScript.Interpreter.Statements;
using SolScript.Interpreter.Types;
using SolScript.Properties;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     A <see cref="SolExpression" /> is the base class for all expressions in SolScript. An expression differs from a
    ///     <see cref="SolStatement" /> by that fact that it cannot stand alone. An expression always needs a context in order
    ///     to have any further meaning(e.g. The numer literal '42' is an expression to create a number literal. Creating a
    ///     number literal itself would have very little purpose, so it needs to stand inside of a statement, e.g. the
    ///     parameters of a function call).
    /// </summary>
    public abstract class SolExpression : ISourceLocateable, ISolCompileable
    {
        /// <summary>
        ///     Creates a new expression.
        /// </summary>
        /// <param name="assembly">The assembly this expression is in.</param>
        /// <param name="location">The source location of this expression.</param>
        protected SolExpression(SolAssembly assembly, NodeLocation location)
        {
            Assembly = assembly;
            Location = location;
        }

        private static readonly BiDictionary<byte, Type> s_ByteIdToType = new BiDictionary<byte, Type>();
        private static readonly PSDictionary<byte, CompilerData> s_ByteIdToData = new PSDictionary<byte, CompilerData>();

        /// <summary>
        ///     The assembly this expression is located in.
        /// </summary>
        public readonly SolAssembly Assembly;

        /// <summary>
        ///     Is this value constant? Constant values can/are/do: <br />a.) Be evaluated with a <see langword="null" /> execution
        ///     context.<br />b.) Not manipulate any state.
        /// </summary>
        public abstract bool IsConstant { get; }

        #region ISolCompileable Members

        /// <inheritdoc />
        public abstract ValidationResult Validate(SolValidationContext context);

        #endregion

        #region ISourceLocateable Members

        /// <inheritdoc />
        public NodeLocation Location { get; }

        #endregion

        #region Overrides

        /// <inheritdoc />
        public override string ToString() => ToString_Impl();

        #endregion

        /// <summary>
        ///     Registers data required for the compiler.
        /// </summary>
        /// <param name="expressionType">The type of expression the data should be registered for.</param>
        /// <param name="bytecodeId">The id the expression will use in bytecode.</param>
        /// <param name="factory">The factory method used to create instances of this expression from the compiler.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="expressionType" /> is <see langword="null" /> -or-
        ///     <paramref name="factory" /> is <see langword="null" />
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     The <paramref name="bytecodeId" /> is already used by another type. -or- The
        ///     <paramref name="expressionType" /> is already used by another bytecode ID.
        /// </exception>
        internal static void RegisterCompilerData([NotNull] Type expressionType, byte bytecodeId, [NotNull] Func<SolExpression> factory)
        {
            if (expressionType == null) {
                throw new ArgumentNullException(nameof(expressionType));
            }
            if (factory == null) {
                throw new ArgumentNullException(nameof(factory));
            }
            if (s_ByteIdToType.Contains(bytecodeId)) {
                throw new ArgumentException(CompilerResources.Err_BytecodeIdAlreadyUsed.FormatWith(bytecodeId, expressionType, s_ByteIdToType[bytecodeId]));
            }
            if (s_ByteIdToType.Contains(expressionType)) {
                throw new ArgumentException(CompilerResources.Err_TypeAlreadyAssingedToBytecodeId.FormatWith(expressionType, bytecodeId, s_ByteIdToType[expressionType]));
            }
            s_ByteIdToType.Add(bytecodeId, expressionType);
            s_ByteIdToData.Add(bytecodeId, new CompilerData(expressionType, bytecodeId, factory));
        }

        /// <summary>
        ///     This method evaluates the expression to produce the result of the expression. Be very careful and considerate when
        ///     it comes to data that persists between evaluations.
        /// </summary>
        /// <param name="context">The currently active execution context.</param>
        /// <param name="parentVariables">The current variable context for this expression.</param>
        /// <returns>The result of the expression.</returns>
        /// <exception cref="SolRuntimeException">A runtime error occured while evaluating the expression.</exception>
        public abstract SolValue Evaluate(SolExecutionContext context, IVariables parentVariables);

        /// <summary>Gets the constant value of this expression.</summary>
        /// <returns>The value.</returns>
        /// <exception cref="InvalidOperationException">The expression is not constant.</exception>
        /// <seealso cref="IsConstant" />
        public SolValue GetConstant()
        {
            if (!IsConstant) {
                throw new InvalidOperationException(Resources.Err_ExpressionIsNotConstant.FormatWith(GetType()));
            }
            return Evaluate(null, null);
        }

        /// <summary>
        ///     Formats the expression to a string for debugging purposes.
        /// </summary>
        /// <returns>The string.</returns>
        protected abstract string ToString_Impl();

        #region Nested type: CompilerData

        private class CompilerData
        {
            public CompilerData(Type type, byte bytecodeId, Func<SolExpression> factory)
            {
                BytecodeId = bytecodeId;
                Factory = factory;
                Type = type;
            }

            public readonly byte BytecodeId;
            public readonly Func<SolExpression> Factory;
            public readonly Type Type;
        }

        #endregion

        /*/// <inheritdoc />
        /// <exception cref="IOException">An I/O error occured.</exception>
        /// <exception cref="SolCompilerException">Failed to compile. (See possible inner exceptions for details)</exception>
        public void Compile(BinaryWriter writer, SolCompliationContext context)
        {
            //writer.Write(BytecodeId);
            Location.CompileTo(writer, context);
            //CompileImpl(writer, context);
        }*/

        /*/// <summary>
        ///     The factory method used to create this expression. (Must be constant)
        /// </summary>
        internal abstract Func<SolExpression> BytecodeFactory { get; }

        /// <summary>
        ///     The id this expression will use in bytecode. (Must be constant)
        /// </summary>
        internal abstract byte BytecodeId { get; }*/

        /*/// <summary>
        ///     Compiles the statement to the given binary writer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="context">The compilation context.</param>
        /// <exception cref="IOException">An I/O error occured.</exception>
        /// <exception cref="SolCompilerException">Failed to compile. (See possible inner exceptions for details)</exception>
        protected abstract void CompileImpl(BinaryWriter writer, SolCompliationContext context);*/
    }
}