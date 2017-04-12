using System.Collections.Generic;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Utility;

namespace SolScript.Interpreter.Statements
{
    /// <summary>
    ///     This statement creates a new class instance.
    /// </summary>
    public class Statement_New : SolStatement
    {
        /// <summary>
        ///     Creates the statement from the given parameters.
        /// </summary>
        /// <param name="assembly">The assembly this statement belongs to.</param>
        /// <param name="location">The location in source code.</param>
        /// <param name="typeName">The name of the class that should be created.</param>
        /// <param name="arguments">The constructor arguments.</param>
        public Statement_New([NotNull] SolAssembly assembly, SolSourceLocation location, string typeName, params SolExpression[] arguments) : base(assembly, location)
        {
            m_Arguments = new Array<SolExpression>(arguments);
            TypeName = typeName;
        }

        // The constructor arguments.
        private readonly Array<SolExpression> m_Arguments;

        /// <summary>
        ///     The name of the class that should be created.
        /// </summary>
        public readonly string TypeName;

        /// <summary>
        ///     The constructor arguments.
        /// </summary>
        public IReadOnlyList<SolExpression> Arguments => m_Arguments;

        #region Overrides

        /// <exception cref="SolRuntimeException">An error occured while creating the class instance.</exception>
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            context.CurrentLocation = Location;
            var arguments = new SolValue[m_Arguments.Length];
            for (int i = 0; i < arguments.Length; i++) {
                arguments[i] = m_Arguments[i].Evaluate(context, parentVariables);
            }
            SolClass instance;
            try {
                instance = Assembly.New(TypeName, ClassCreationOptions.Default(context), arguments);
            } catch (SolTypeRegistryException ex) {
                throw new SolRuntimeException(context, $"An error occured while creating a class instance of type \"{TypeName}\".", ex);
            }
            terminators = Terminators.None;
            return instance;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return
                $"new {TypeName}({m_Arguments.JoinToString()})";
        }

        #endregion
    }
}