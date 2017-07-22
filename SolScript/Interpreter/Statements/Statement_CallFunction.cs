using System;
using System.Collections.Generic;
using System.Linq;
using Irony.Parsing;
using JetBrains.Annotations;
using NodeParser;
using PSUtility.Enumerables;
using SolScript.Compiler;
using SolScript.Exceptions;
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
        /// <param name="functionGetter">The statement used to get the function.</param>
        /// <param name="args">The function argument expressions.</param>
        public Statement_CallFunction(SolAssembly assembly, NodeLocation location, SolExpression functionGetter, IEnumerable<SolExpression> args) : base(assembly, location)
        {
            FunctionGetter = functionGetter;
            SolExpression[] array = args.ToArray();
            if (array.Length == 0) {
                array = ArrayUtility.Empty<SolExpression>();
            }
            m_Arguments = new Array<SolExpression>(array);
        }

        /// <summary>
        ///     The statement getting the function.
        /// </summary>
        public readonly SolExpression FunctionGetter;

        // Raw argument array.
        private readonly Array<SolExpression> m_Arguments;

        /// <summary>
        ///     Obtains the arguments used when calling the function.
        /// </summary>
        public ReadOnlyList<SolExpression> Arguments => m_Arguments.AsReadOnly();

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">Can only call a function value.</exception>
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

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            throw new NotImplementedException();
        }
    }
}