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
using PSUtility.Strings;
using SolScript.Compiler;
using SolScript.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Properties;
using SolScript.Utility;

namespace SolScript.Interpreter.Statements
{
    /// <summary>
    ///     The base-Statement is used to access the fields and functions(global and internal access) of the base class.<br />
    ///     This e.g. allows to override functions while still providing the functionality of the overridden function.
    /// </summary>
    public class Statement_Base : SolStatement //, IWrittenInClass
    {
        /// <inheritdoc />
        public Statement_Base(SolAssembly assembly, NodeLocation location, SolExpression indexer) : base(assembly, location)
        {
            //WrittenInClass = writtenInClass;
            Indexer = indexer;
        }

        /// <summary>
        ///     The expression returning the index.
        /// </summary>
        public readonly SolExpression Indexer;

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Self statement cannot be executed on a null current class.</exception>
        /// <exception cref="SolRuntimeException">Failed to resolve variable.</exception>
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            terminators = Terminators.None;
            SolClassEntry entry;
            if (!context.PeekClassEntry(out entry) || entry.IsGlobal) {
                throw new InvalidOperationException(Resources.Err_NotValidInGlobal.ToString("base"));
            }
            SolClass.Inheritance inheritance = entry.Inheritance();
            SolValue indexerRaw = Indexer.Evaluate(context, parentVariables);
            SolString index = indexerRaw as SolString;
            if (index == null) {
                throw new SolRuntimeException(context, Resources.Err_InvalidIndexerType.ToString(entry + ".base", indexerRaw.Type));
            }
            try {
                // The base keyword may not access locals.              
                return inheritance.GetVariables(SolAccessModifier.Internal, SolVariableMode.Base).Get(index.Value);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, "Failed to resolve a base variable lookup in " + entry + ".", ex);
            }


            /*terminators = Terminators.None;
            SolClass currentClass = context.CurrentClass;
            if (currentClass == null)
            {
                throw new InvalidOperationException(Resources.Err_NotValidInGlobal.ToString("base"));
            }
            // todo: check if commenting this out wont cause problems
            ***SolClass.Inheritance inheritance = currentClass.FindInheritance(context.CurrentClass.InheritanceChain.Definition);
            if (inheritance == null) {
                throw new SolRuntimeException(context,
                    "Tried to access class \"" + currentClass.Type + "\" on inheritance level \"" + WrittenInClass + "\". The class does not contain said inheritance.");
            }***
            SolValue indexerRaw = Indexer.Evaluate(context, parentVariables);
            SolString index = indexerRaw as SolString;
            if (index == null)
            {
                throw new SolRuntimeException(context, Resources.Err_InvalidIndexerType.ToString(currentClass.Type + ".base", indexerRaw.Type));
            }
            try
            {
                // The base keyword may not access locals.              
                return currentClass.GetVariables(SolAccessModifier.Internal, SolVariableMode.Base).Get(index.Value);
            }
            catch (SolVariableException ex)
            {
                throw new SolRuntimeException(context, "Failed to resolve a self statement variable.", ex);
            }*/
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("base[");
            builder.Append(Indexer);
            builder.Append("]");
            return builder.ToString();
        }

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            SolClassDefinition definition = context.InClassDefinition;
            if (definition == null) {
                context.Errors.Add(new SolError(Location, CompilerResources.Err_BaseNotInClass));
                return ValidationResult.Failure();
            }
            if (definition.BaseClass == null) {
                context.Errors.Add(new SolError(Location, CompilerResources.Err_BaseWithoutBaseClass.FormatWith(definition.Type)));
                return ValidationResult.Failure();
            }
            ValidationResult key = Indexer.Validate(context);
            if (!key) {
                return ValidationResult.Failure();
            }
            if (!Indexer.IsConstant) {
                context.Errors.Add(new SolError(Indexer.Location, CompilerResources.Err_CannotIndexBaseDynamically));
                return ValidationResult.Failure();
            }
            SolValue constant;
            SolString index = (constant = Indexer.GetConstant()) as SolString;
            if (index == null) {
                context.Errors.Add(new SolError(Indexer.Location, CompilerResources.Err_CannotIndexBaseWithType.FormatWith(constant.Type)));
                return ValidationResult.Failure();
            }
            // todo: check if member even exists!
            // todo: possibly allos dynamic indexing (would require language and framework support)
            return new ValidationResult(true, new SolType(definition.Type, false));
        }

        #endregion

        /*#region IWrittenInClass Members

        /// <inheritdoc />
        public string WrittenInClass { get; }

        #endregion*/
    }
}