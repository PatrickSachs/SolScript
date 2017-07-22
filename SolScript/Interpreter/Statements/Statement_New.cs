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

using System.Collections.Generic;
using JetBrains.Annotations;
using NodeParser;
using PSUtility.Enumerables;
using SolScript.Compiler;
using SolScript.Exceptions;
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
        /// <param name="class">The class that should be created.</param>
        /// <param name="arguments">The constructor arguments.</param>
        public Statement_New([NotNull] SolAssembly assembly, NodeLocation location, SolClassDefinitionReference @class, IEnumerable<SolExpression> arguments) : base(assembly, location)
        {
            Class = @class;
            m_Arguments = InternalHelper.CreateArray(arguments);
        }

        // The constructor arguments.
        private readonly Array<SolExpression> m_Arguments;

        /// <summary>
        ///     The constructor arguments.
        /// </summary>
        public ReadOnlyList<SolExpression> Arguments => m_Arguments.AsReadOnly();

        /// <summary>
        ///     The class that should be created.
        /// </summary>
        public SolClassDefinitionReference Class { get; }

        #region Overrides

        /// <exception cref="SolRuntimeException">An error occured while creating the class instance.</exception>
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            context.CurrentLocation = Location;
            var arguments = new SolValue[m_Arguments.Length];
            for (int i = 0; i < arguments.Length; i++) {
                arguments[i] = m_Arguments[i].Evaluate(context, parentVariables);
            }
            SolClassDefinition definition;
            if (!Class.TryGetDefinition(out definition)) {
                throw new SolRuntimeException(context, $"The class \"{Class.Name}\" does not exist.");
            }
            SolClass instance;
            try {
                instance = Assembly.New(definition, ClassCreationOptions.Default(context), arguments);
            } catch (SolTypeRegistryException ex) {
                throw new SolRuntimeException(context, $"An error occured while creating a class instance of type \"{Class.Name}\".", ex);
            }
            terminators = Terminators.None;
            return instance;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return
                $"new {Class.Name}({m_Arguments.JoinToString()})";
        }

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            SolClassDefinition def;
            if (!Class.TryGetDefinition(out def)) {
                return ValidationResult.Failure();
            }
            if (!def.CanBeCreated()) {
                return ValidationResult.Failure();
            }
            SolFunctionDefinition ctor;
            try {
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
            return new ValidationResult(true, new SolType(Class.Name, false));
        }

        #endregion
    }
}