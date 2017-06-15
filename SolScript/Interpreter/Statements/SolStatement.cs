using System;
using System.IO;
using Irony.Parsing;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using PSUtility.Strings;
using SolScript.Compiler;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Properties;
using SolScript.Utility;

namespace SolScript.Interpreter.Statements
{
    /// <summary>
    ///     This is the base class for all statement in SolScript. Statements(as opposed to <see cref="SolExpression" />s) can
    ///     stand alone inside a <see cref="SolChunk" /> and may or may not be made out of several <see cref="SolExpression" />
    ///     s(e.g. a function call can stand alone in a chunk and takes several expressions as arguments).
    /// </summary>
    public abstract class SolStatement : ISourceLocateable, ISolCompileable
        //, ISourceLocationInjector
    {
        /// <summary>
        ///     Creates a new statement.
        /// </summary>
        /// <param name="assembly">The assembly this statement is in.</param>
        /// <param name="location">The location of this statement.</param>
        /// <exception cref="ArgumentNullException"><paramref name="assembly" /> is null.</exception>
        protected SolStatement(SolAssembly assembly, SourceLocation location)
        {
            if (assembly == null) {
                throw new ArgumentNullException(nameof(assembly));
            }
            Assembly = assembly;
            InjectSourceLocation(location);
        }

        /// <summary>
        ///     Used by the parser.
        /// </summary>
        internal SolStatement()
        {
            Assembly = SolAssembly.CurrentlyParsing;
        }

        private static readonly BiDictionary<byte, Type> s_ByteIdToType = new BiDictionary<byte, Type>();
        private static readonly PSDictionary<byte, CompilerData> s_ByteIdToData = new PSDictionary<byte, CompilerData>();

        /// <summary>
        ///     The assembly this statement is in.
        /// </summary>
        public readonly SolAssembly Assembly;

        /*/// <summary>
        ///     The factory method used to create this statement. (Must be constant)
        /// </summary>
        internal abstract Func<SolStatement> BytecodeFactory { get; }

        /// <summary>
        ///     The id this statement will use in bytecode. (Must be constant)
        /// </summary>
        internal abstract byte BytecodeId { get; }*/

        #region ISourceLocateable Members

        /// <inheritdoc />
        public SourceLocation Location { get; private set; }

        #endregion

        #region Overrides

        /// <inheritdoc />
        public override string ToString()
        {
            return ToString_Impl();
        }

        #endregion

        /// <summary>
        ///     Registers data required for the compiler.
        /// </summary>
        /// <param name="statementType">The type of statement the data should be registered for.</param>
        /// <param name="bytecodeId">The id the statement will use in bytecode.</param>
        /// <param name="factory">The factory method used to create instances of this statement from the compiler.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="statementType" /> is <see langword="null" /> -or-
        ///     <paramref name="factory" /> is <see langword="null" />
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     The <paramref name="bytecodeId" /> is already used by another type. -or- The
        ///     <paramref name="statementType" /> is already used by another bytecode ID.
        /// </exception>
        internal static void RegisterCompilerData([NotNull] Type statementType, byte bytecodeId, [NotNull] Func<SolStatement> factory)
        {
            if (statementType == null) {
                throw new ArgumentNullException(nameof(statementType));
            }
            if (factory == null) {
                throw new ArgumentNullException(nameof(factory));
            }
            if (s_ByteIdToType.Contains(bytecodeId)) {
                throw new ArgumentException(CompilerResources.Err_BytecodeIdAlreadyUsed.FormatWith(bytecodeId, statementType, s_ByteIdToType[bytecodeId]));
            }
            if (s_ByteIdToType.Contains(statementType)) {
                throw new ArgumentException(CompilerResources.Err_TypeAlreadyAssingedToBytecodeId.FormatWith(statementType, bytecodeId, s_ByteIdToType[statementType]));
            }
            s_ByteIdToType.Add(bytecodeId, statementType);
            s_ByteIdToData.Add(bytecodeId, new CompilerData(statementType, bytecodeId, factory));
        }

        /// <summary>
        ///     Executes the statement and produces its result.
        /// </summary>
        /// <param name="context">The currently active execution context.</param>
        /// <param name="parentVariables">The variable source of this statement.</param>
        /// <param name="terminators">The terminators. Terminators are used to break or continue in e.g. iterators.</param>
        /// <returns>The result of the execution.</returns>
        public abstract SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators);

        /// <summary>
        ///     Formats this statement to a string for debuging purposes.
        /// </summary>
        /// <returns>The string.</returns>
        protected abstract string ToString_Impl();

        /// <inheritdoc />
        public void InjectSourceLocation(SourceLocation location)
        {
            Location = location;
        }

        /// <inheritdoc />
        public abstract ValidationResult Validate(SolValidationContext context);

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
        /// Compiles the statement to the given binary writer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="context">The compilation context.</param>
        /// <exception cref="IOException">An I/O error occured.</exception>
        /// <exception cref="SolCompilerException">Failed to compile. (See possible inner exceptions for details)</exception>
        protected abstract void CompileImpl(BinaryWriter writer, SolCompliationContext context);*/

        #region Nested type: CompilerData

        private class CompilerData
        {
            public CompilerData(Type type, byte bytecodeId, Func<SolStatement> factory)
            {
                BytecodeId = bytecodeId;
                Factory = factory;
                Type = type;
            }

            public readonly byte BytecodeId;
            public readonly Func<SolStatement> Factory;
            public readonly Type Type;
        }

        #endregion
    }
}