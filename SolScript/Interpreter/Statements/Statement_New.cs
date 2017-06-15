using System.Collections.Generic;
using Irony.Parsing;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Compiler;
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
        public Statement_New([NotNull] SolAssembly assembly, SourceLocation location, string typeName, params SolExpression[] arguments) : base(assembly, location)
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
        public ReadOnlyList<SolExpression> Arguments => m_Arguments.AsReadOnly();

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

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            SolClassDefinition def;
            if (!Assembly.TryGetClass(TypeName, out def)) {
                return ValidationResult.Failure();
            }
            if (!def.CanBeCreated()) {
                return ValidationResult.Failure();
            }
            SolFunctionDefinition ctor;
            try
            {
                SolClassDefinition.MetaFunctionLink ctorMetaFunc;
                if (!def.TryGetMetaFunction(SolMetaFunction.__new, out ctorMetaFunc) && Arguments.Count != 0) {
                    return ValidationResult.Failure();
                }
                ctor = ctorMetaFunc?.Definition;
            } catch (SolVariableException) {
                return ValidationResult.Failure();
            }
            if (ctor != null) {
                var parmInfo = ctor.ParameterInfo;
                if (Arguments.Count > parmInfo.Count && !parmInfo.AllowOptional) {
                    return ValidationResult.Failure();
                }
                // todo: default values for arguments/parameters
                if (Arguments.Count < parmInfo.Count) {
                    return ValidationResult.Failure();
                }
                for (int i = 0; i < Arguments.Count; i++) {
                    SolExpression argument = Arguments[i];
                    var argRes = argument.Validate(context);
                    if (!argRes) {
                        return ValidationResult.Failure();
                    }
                    if (i < parmInfo.Count) {
                        SolParameter p = parmInfo[i];
                        if (!p.Type.IsCompatible(Assembly, argRes.Type)) {
                            return ValidationResult.Failure();
                        }
                    }
                }
            }
            return new ValidationResult(true, new SolType(TypeName, false));
        }

        #endregion
    }
}