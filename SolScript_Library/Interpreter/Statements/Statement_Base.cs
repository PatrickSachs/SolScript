using System;
using Irony.Parsing;
using SolScript.Interpreter.Exceptions;
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
    public class Statement_Base : SolStatement//, IWrittenInClass
    {
        /// <inheritdoc />
        public Statement_Base(SolAssembly assembly, SourceLocation location, SolExpression indexer) : base(assembly, location)
        {
            //WrittenInClass = writtenInClass;
            Indexer = indexer;
        }

        /// <summary>
        ///     The expression returning the index.
        /// </summary>
        public readonly SolExpression Indexer;

        /*#region IWrittenInClass Members

        /// <inheritdoc />
        public string WrittenInClass { get; }

        #endregion*/

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Self statement cannot be executed on a null current class.</exception>
        /// <exception cref="SolRuntimeException">Failed to resolve variable.</exception>
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            terminators = Terminators.None;
            SolClass currentClass = context.CurrentClass;
            if (currentClass == null) {
                throw new InvalidOperationException(Resources.Err_NotValidInGlobal.ToString("base"));
            }
            // todo: check if commenting this out wont cause problems
            /*SolClass.Inheritance inheritance = currentClass.FindInheritance(context.CurrentClass.InheritanceChain.Definition);
            if (inheritance == null) {
                throw new SolRuntimeException(context,
                    "Tried to access class \"" + currentClass.Type + "\" on inheritance level \"" + WrittenInClass + "\". The class does not contain said inheritance.");
            }*/
            SolValue indexerRaw = Indexer.Evaluate(context, parentVariables);
            SolString index = indexerRaw as SolString;
            if (index == null) {
                throw new SolRuntimeException(context, Resources.Err_InvalidIndexerType.ToString(currentClass.Type + ".base", indexerRaw.Type));
            }
            try {
                // The base keyword may not access locals.              
                return currentClass.GetVariables(SolAccessModifier.Internal, SolVariableMode.Base).Get(index.Value);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, "Failed to resolve a self statement variable.", ex);
            }
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return "Statement_Base(Indexer=" + Indexer + ")";
        }

        #endregion
    }
}