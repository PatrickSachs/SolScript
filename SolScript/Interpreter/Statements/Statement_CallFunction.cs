using System;
using System.Collections.Generic;
using Irony.Parsing;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter.Statements
{
    /// <summary>
    ///     This statement calls a function.
    /// </summary>
    public class Statement_CallFunction : SolStatement
    {
        /// <summary>
        ///     Creates a call function statement.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="location">The location in source code.</param>
        /// <param name="statementDefinedInClass">The class name the statement was defined in. (Obsolete; value not used)</param>
        /// <param name="functionGetter">The statement used to get the function.</param>
        /// <param name="args">The function argument expressions.</param>
        public Statement_CallFunction(SolAssembly assembly, SourceLocation location, string statementDefinedInClass,
            SolExpression functionGetter, Array<SolExpression> args) : base(assembly, location)
        {
            FunctionGetter = functionGetter;
            m_Arguments = args;
            //StatementDefinedInClassName = statementDefinedInClass;
        }

        /// <summary>
        ///     The statement getting the function.
        /// </summary>
        public readonly SolExpression FunctionGetter;

        /*// WARNING: This could have been defined __ anywhere__ not even necessarily within the class inheritance tree!
        /// <summary>
        ///     This defines where the STATEMENT CALLING the function has been defined, and NOT where the FUNCTION ITSELF has been
        ///     defined.
        /// </summary>
        /// <remarks>
        ///     If you want to get a handle on the class that defined a function, cast the function itself to
        ///     <see cref="SolClassFunction" /> and obtain the class using
        ///     <see cref="SolClassFunction.ClassInstance" />.
        /// </remarks>
        [CanBeNull, Obsolete]
        public readonly string StatementDefinedInClassName;*/

        // Raw argument array.
        private readonly Array<SolExpression> m_Arguments;

        /// <summary>
        ///     Obtains the arguments used when calling the function.
        /// </summary>
        public ReadOnlyList<SolExpression> Arguments => m_Arguments.AsReadOnly();

        #region Overrides

        /// <inheritdoc />
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            context.CurrentLocation = Location;
            SolValue functionRaw = FunctionGetter.Evaluate(context, parentVariables);
            SolFunction function = functionRaw as SolFunction;
            if (function == null) {
                throw new SolRuntimeException(context, $"Tried to call a \"{functionRaw.Type}\" value.");
            }
            SolValue[] callArgs = GetArguments(context, parentVariables);
            terminators = Terminators.None;
            return function.Call(context, callArgs);
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return $"{FunctionGetter}({m_Arguments.JoinToString()})";
        }

        #endregion

        private SolValue[] GetArguments(SolExecutionContext context, IVariables parentVariables)
        {
            var callArgs = new SolValue[m_Arguments.Length];
            for (int i = 0; i < callArgs.Length; i++) {
                callArgs[i] = m_Arguments[i].Evaluate(context, parentVariables);
            }
            return callArgs;
        }
    }
}