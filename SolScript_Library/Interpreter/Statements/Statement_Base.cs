using System;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements
{
    /// <summary>
    ///     The base-Statement is used to access the fields and functions(global and internal access) of the base class.<br />
    ///     This e.g. allows to override functions while still providing the functionality of the overridden function.
    /// </summary>
    public class Statement_Base : SolStatement, IWrittenInClass
    {
        /// <inheritdoc />
        public Statement_Base(SolAssembly assembly, SolSourceLocation location, string writtenInClass, SolExpression indexer) : base(assembly, location)
        {
            WrittenInClass = writtenInClass;
            Indexer = indexer;
        }

        /// <summary>
        ///     The expression returning the index.
        /// </summary>
        public readonly SolExpression Indexer;

        #region IWrittenInClass Members

        /// <inheritdoc />
        public string WrittenInClass { get; }

        #endregion

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Self statement cannot be executed on a null current class.</exception>
        /// <exception cref="SolRuntimeException">Failed to resolve variable.</exception>
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            terminators = Terminators.None;
            SolClass currentClass = context.CurrentClass;
            if (currentClass == null) {
                throw new InvalidOperationException("self statement cannot be executed on a null current class.");
            }
            SolClass.Inheritance inheritance = currentClass.FindInheritance(WrittenInClass);
            if (inheritance == null) {
                throw new SolRuntimeException(context,
                    "Tried to access class \"" + currentClass.Type + "\" on inheritance level \"" + WrittenInClass + "\". The class does not contain said inheritance.");
            }
            SolValue indexerRaw = Indexer.Evaluate(context, parentVariables);
            SolString index = indexerRaw as SolString;
            if (index == null) {
                throw new SolRuntimeException(context, "Cannot index self by a \"" + indexerRaw.Type + "\" value. Only strings can be used.");
            }
            try {
                // The base keyword may not access locals.
                return inheritance.GetVariables(SolAccessModifier.Internal, SolVariableMode.Base).Get(index.Value);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, "Failed to resolve a self statement variable.", ex);
            }
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return "Statement_Base(WrittenInClass=" + WrittenInClass + ", Indexer=" + Indexer + ")";
        }

        #endregion
    }
}